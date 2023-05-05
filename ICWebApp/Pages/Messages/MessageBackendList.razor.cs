using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Stripe;
using Telerik.Blazor;

namespace ICWebApp.Pages.Messages
{
    public partial class MessageBackendList
    {
        [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] IMessageService Messageservice { get; set; }
        [Inject] private ITEXTProvider TextProvider { get; set; }
        [Inject] private NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ILANGProvider LanguageProvider { get; set; }
        [Inject] IMSGProvider MSGProvider { get; set; }


        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string ExternalID { get; set; }

        public EventCallback OnUpdateRead { get; set; }

        private MSG_Message CurrentMessage = new MSG_Message();
        private List<MSG_Message> Messages = new List<MSG_Message>();
        private List<LANG_Languages> LanguageList = new List<LANG_Languages>();
        private bool IsDataBusy { get; set; } = false;
        private bool WindowVisible { get; set; }

        private Guid currentLanguageID = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");


        protected override async Task OnParametersSetAsync()
        {
            BusyIndicatorService.IsBusy = true;
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_MESSAGES");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/MessageCommunications", "MAINMENU_MESSAGES", null, null, true);

            LanguageList = await LanguageProvider.GetAll();

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                await LoadData();
            }

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async void ReadMessage(MSG_Message Item)
        {

            CurrentMessage = Item;
            WindowVisible = true;

            if (Item.FirstReadDate == null)
            {
                Item.FirstReadDate = DateTime.Now; 
                await Messageservice.UpdateMessage(Item); 
                await OnUpdateRead.InvokeAsync();
                
                Messageservice.CallRequestRefresh();
            }


            StateHasChanged();
            //NavManager.NavigateTo("/Messages/Read/" + Item.id);
        }
        private async Task<bool> LoadData()
        {
            IsDataBusy = true;
            StateHasChanged();

            Messages = await Messageservice.GetMessages(SessionWrapper.CurrentUser.ID, null);
            Messages = Messages.Where(a => a.ShowInList == true).ToList();

            IsDataBusy = false;
            StateHasChanged();

            return true;
        }
        private void GoToMessage(MSG_Message? Message)
        {
            if (Message != null)
            {
                Message.FirstReadDate = DateTime.Now;

                MSGProvider.SetMessage(Message);

                if (Message.Link != null)
                {
                    if (NavManager.BaseUri.Contains("localhost"))
                    {
                        Message.Link = Message.Link.Replace("https://test.comunix.bz.it/", "https://localhost:7149/");
                    }

                    if (NavManager.Uri != Message.Link)
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo(Message.Link);
                    }

                    StateHasChanged();
                }
                else
                {
                    if (!NavManager.Uri.Contains("/Backend/MessageCommunications"))
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo("/Backend/MessageCommunications");
                    }
                    StateHasChanged();
                }
            }
        }
    }
}
