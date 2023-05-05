using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.UserManagement.Admin
{
    public partial class EmployeeList
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] public IAUTHProvider AuthProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IMailerService MailService { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<AUTH_Users> Data = new List<AUTH_Users>();
        private bool IsDataBusy { get; set; } = true;

        private bool LangSelectWindowVisible { get; set; } = false;
        private string selectedLang;
        private List<LanguageSelection> _languages = new List<LanguageSelection>();
        private AUTH_Users? _selectedUser = null;
        protected override async Task OnInitializedAsync()
        {
            await GetData();

            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_USER_MANAGEMENT");

            _languages = new List<LanguageSelection>() { new LanguageSelection("DE", TextProvider.Get("LANGUAGE_GERMAN")), new LanguageSelection("IT",TextProvider.Get("LANGUAGE_ITALIAN"))};
            selectedLang = _languages.FirstOrDefault()!.Id;
            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            if (SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                var data = await AuthProvider.GetUserList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, AuthRoles.Employee);
                var dataAdmin = await AuthProvider.GetUserList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, AuthRoles.Administrator);

                Data = data;

                var itemsToAdd = dataAdmin.Where(p => !Data.Select(x => x.ID).Contains(p.ID)).ToList();

                Data.AddRange(itemsToAdd);
            }

            return true;
        }

        private void Add()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/User/Management/Add/New");
        }

        private void Edit(AUTH_Users Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/User/Management/Add/" + Item.ID);
        }

        private async void Remove(AUTH_Users Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE_USER"),
                        TextProvider.Get("WARNING")))
                    return;

                IsDataBusy = true;
                StateHasChanged();

                await AuthProvider.RemoveUser(Item);
                await GetData();

                IsDataBusy = false;
                StateHasChanged();
            }
        }
        
        private void OpenWindow(AUTH_Users item)
        {
            LangSelectWindowVisible = true;
            _selectedUser = item;
        }

        private async void ForcePasswordResetAndSendMail()
        {
            if (_selectedUser == null || selectedLang == null)
                return;

            var item = _selectedUser;
            var langId = Guid.Empty;
            if (selectedLang == "DE")
                langId = LanguageSettings.German;
            if (selectedLang == "IT")
                langId = LanguageSettings.Italian;

            if (langId == Guid.Empty)
                return;

            LangSelectWindowVisible = false;
            
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            var token = await AuthProvider.CreateForceResetTokenForUser(item.ID, TimeSpan.FromDays(7));
            
            if (token != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                var url = NavManager.BaseUri + "ForcePasswordReset/" + token;
                var mailText = TextProvider.Get("FORCE_RESET_PASSWORD_MAIL_CONTENT", langId);
                mailText = mailText.Replace("{link}", url);

                var municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                if (municipality != null && municipality.Prefix_Text_SystemTexts_Code != null)
                {
                    var prefix = TextProvider.Get(municipality.Prefix_Text_SystemTexts_Code, langId);

                    mailText = mailText.Replace("{baseurl}", "https://" + prefix + ".comunix.bz.it/");
                }
                else
                {
                    mailText = mailText.Replace("{baseurl}", NavManager.BaseUri);
                }

                mailText = mailText.Replace("{email}", item.Email);
                
                var mail = new MSG_Mailer();
                mail.ToAdress = item.Email;
                mail.Subject = TextProvider.Get("FORCE_RESET_PASSWORD_MAIL_SUBJECT", langId);
                mail.Body = mailText;
                mail.PlannedSendDate = DateTime.Now;
                
                await MailService.SendMail(mail, null, SessionWrapper.AUTH_Municipality_ID.Value, null);
                await AuthProvider.SetLastForceEmailSent(item.ID, DateTime.Now);
                await GetData();
            }

            _selectedUser = null;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }

        private void CancelSending()
        {
            LangSelectWindowVisible = false;
            _selectedUser = null;
        }

        private class LanguageSelection
        {
            public LanguageSelection(string Id, string Name)
            {
                this.Id = Id;
                this.Name = Name;
            }
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}