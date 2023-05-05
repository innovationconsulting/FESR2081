using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Freshdesk;
using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Classes.Validation;
using ICWebApp.Components.CodeInput;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.User;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;

namespace ICWebApp.Pages.UserManagement.Frontend
{
    public partial class Profile
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IMessageService Messageservice { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ILANGProvider LanguageProvider { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] IMETAProvider MetaProvider { get; set; }
        [Inject] IAccountService AccountService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        string MessageInfo { get; set; } = "";
        private AUTH_Users_Anagrafic? AnagraficData;
        private DomicileData? DomicileData;
        private AccountData? AccountData;
        private KontaktData? KontaktData;
        private GeneralData? GeneralData;
        private List<META_IstatComuni>? MunicipalitiesList;
        private bool EditDomicileDataWindowVisible = false;
        private bool AddressNotFound = false;
        private bool EditPasswordWindowVisible = false;
        private bool IsValidPassword = false;
        private PasswordHelper pwhelper = new PasswordHelper();
        private string PasswordQuality { get; set; } = "";
        private string Password
        {
            get
            {
                return AccountData.Password;
            }
            set
            {
                AccountData.Password = value;
                PasswordQuality = pwhelper.GetPasswordStrength(value).ToString();
                IsValidPassword = pwhelper.IsValidPassword(value);
                StateHasChanged();
            }
        }
        private string? PasswordChangeMessage;
        private bool EditAnagraphicsVisibility = false;
        private bool EditPhoneVisibility = false;
        private bool EditMailVisibility = false;
        private bool ShowMailVerification = false;
        private Timer? _EmailTimer;
        private bool ShowPhoneVerification = false;
        private CodeInputComponent CodeInput;
        private MSG_SystemMessages? Message;

        private CustomValidation customValidation;
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("USER_MYPROFILE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Profile", "USER_MYPROFILE", null, null);

            if(SessionWrapper == null || SessionWrapper.CurrentUser == null || SessionWrapper.CurrentSubstituteUser != null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                await LoadData();
         
                if (await AUTHProvider.CheckUserAnagraficInformation(SessionWrapper.CurrentUser.ID) == false)
                {
                    MessageInfo = TextProvider.Get("USERPROFILE_MISSING_INFORMATIONS");
                    EditDomicileData();
                }
                else
                {
                    MessageInfo = "";
                }

            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private Guid SelectedMunicipality
        {
            get
            {
                if (DomicileData != null && DomicileData.SelectedMunicipality != null)
                    return DomicileData.SelectedMunicipality.Value;

                return Guid.Empty;
            }
            set
            {
                if (value != Guid.Empty)
                {
                    UpdateAddressData(value);
                }

                if (DomicileData != null)
                {
                    DomicileData.SelectedMunicipality = value;
                }
            }
        }
        private async void UpdateAddressData(Guid Municipality_ID)
        {
            var data = await MetaProvider.GetMunicipality(Municipality_ID);

            if (DomicileData != null && data != null)
            {
                DomicileData.ReqDomicilePostalCode = data.Cap;
                DomicileData.ReqDomicileMunicipality = data.NameDE;
                DomicileData.ReqDomicileProvince = data.RegionCity;
                DomicileData.ReqDomicileNation = "IT";

                StateHasChanged();
            }
        }
        private async Task<bool> LoadData()
        {
            AnagraficData = await AUTHProvider.GetAnagraficByUserID(SessionWrapper.CurrentUser.ID);

            return true;
        }
        private void EditDomicileData()
        {
            if (AnagraficData != null)
            {
                DomicileData = new DomicileData();

                DomicileData.ReqDomicileMunicipality = AnagraficData.ReqDomicileMunicipality;
                DomicileData.ReqDomicileStreetAddress = AnagraficData.ReqDomicileStreetAddress;
                DomicileData.ReqDomicilePostalCode = AnagraficData.ReqDomicilePostalCode;
                DomicileData.ReqDomicileProvince = AnagraficData.ReqDomicileProvince;
                DomicileData.ReqDomicileNation = AnagraficData.ReqDomicileNation;
                DomicileData.SelectedMunicipality = AnagraficData.SelectedMunicipality;

                EditDomicileDataWindowVisible = true;
                StateHasChanged();
            }
        }
        private async void SaveDomicileData()
        {
            if (DomicileData != null && AnagraficData != null)
            {
                AnagraficData.ReqDomicileMunicipality = DomicileData.ReqDomicileMunicipality;
                AnagraficData.ReqDomicileStreetAddress = DomicileData.ReqDomicileStreetAddress;
                AnagraficData.ReqDomicilePostalCode = DomicileData.ReqDomicilePostalCode;
                AnagraficData.ReqDomicileProvince = DomicileData.ReqDomicileProvince;
                AnagraficData.ReqDomicileNation = DomicileData.ReqDomicileNation;
                AnagraficData.SelectedMunicipality = DomicileData.SelectedMunicipality;
                AnagraficData.Address = DomicileData.ReqDomicileStreetAddress + " " + DomicileData.ReqDomicilePostalCode + " " + DomicileData.ReqDomicileMunicipality;

                await AUTHProvider.SetAnagrafic(AnagraficData);

                DomicileData = null;
            }

            EditDomicileDataWindowVisible = false;
            StateHasChanged();
        }
        private void AddressNotFoundClick()
        {
            AddressNotFound = true;
            StateHasChanged();
        }
        private void SearchAddressClick()
        {
            AddressNotFound = false;
            StateHasChanged();
        }
        private void CloseDomicileDataWindow()
        {
            DomicileData = null;

            EditDomicileDataWindowVisible = false;
            StateHasChanged();
        }
        private async Task<DataEnvelope<META_IstatComuni>> GetRemoteMunicipalities(ComboBoxReadEventArgs args)
        {
            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            var result = await MunicipalitiesList.ToDataSourceResultAsync(args.Request);

            var dataToReturn = new DataEnvelope<META_IstatComuni>
            {
                Data = result.Data.Cast<META_IstatComuni>().ToList(),
                Total = result.Total
            };

            args.Data = result.Data;
            args.Total = result.Total;

            return await Task.FromResult(dataToReturn);
        }
        private async Task<META_IstatComuni> GetModelFromValue(Guid selectedValue)
        {
            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            return MunicipalitiesList.FirstOrDefault(p => p.ID == selectedValue);
        }
        private void EditAccountData()
        {
            AccountData = new AccountData();

            EditPasswordWindowVisible = true;
            StateHasChanged();
        }
        private void CloseAccountDataWindow()
        {
            AccountData = null;

            EditPasswordWindowVisible = false;
            StateHasChanged();
        }
        private async void SaveAccountData()
        {
            if (AccountData != null && AnagraficData != null && AnagraficData.AUTH_Users_ID != null)
            {
                var user = await AUTHProvider.GetUser(AnagraficData.AUTH_Users_ID.Value);

                if (AccountData != null && AccountData.Password != null && user != null) 
                {
                    user.Password = AccountData.Password;

                    var msg = await AccountService.ChangePassword(user);

                    if (msg != null)
                    {
                        PasswordChangeMessage = msg.Message;
                    }
                }

                AccountData = null;
            }

            EditPasswordWindowVisible = false;
            StateHasChanged();
        }

        private void EditName()
        {
            if (AnagraficData != null)
            {
                GeneralData = new GeneralData();
                GeneralData.Firstname = AnagraficData.FirstName;
                GeneralData.Lastname = AnagraficData.LastName;

                EditAnagraphicsVisibility = true;
                StateHasChanged();
            }
        }
        private async Task SaveName()
        {
            customValidation.ClearErrors();
            if (AnagraficData != null && AnagraficData.AUTH_Users_ID != null && GeneralData != null 
                && !string.IsNullOrEmpty(GeneralData.Firstname) && !string.IsNullOrEmpty(GeneralData.Lastname))
            {
                var user = await AUTHProvider.GetUser(AnagraficData.AUTH_Users_ID.Value);
                AnagraficData.FirstName = GeneralData.Firstname;
                AnagraficData.LastName = GeneralData.Lastname;
                await AUTHProvider.SetAnagrafic(AnagraficData);
                if (user != null)
                {
                    user.Firstname = GeneralData.Firstname;
                    user.Lastname = GeneralData.Lastname;
                    await AUTHProvider.UpdateUser(user);
                }

                EditAnagraphicsVisibility = false;
                StateHasChanged();
                NavManager.NavigateTo(NavManager.Uri, true);
            }
            else if(GeneralData != null)
            {
                var errors = new Dictionary<string, List<string>>();
                if (string.IsNullOrEmpty(GeneralData.Firstname))
                {
                    errors.Add(nameof(GeneralData.Firstname), new()
                    {
                        "required"
                    });
                }

                if (string.IsNullOrEmpty(GeneralData.Lastname))
                {
                    errors.Add(nameof(GeneralData.Lastname), new()
                    {
                        "required"
                    });
                }

                if (errors.Any())
                {
                    customValidation.DisplayErrors(errors);
                }
            }
        }

        private void CloseEditName()
        {
            GeneralData = null;
            EditAnagraphicsVisibility = false;
            StateHasChanged();
        }
        private void EditEmail()
        {
            if (AnagraficData != null)
            {
                KontaktData = new KontaktData();

                KontaktData.Input = AnagraficData.Email;
                ShowMailVerification = false;
            }

            EditMailVisibility = true;
            StateHasChanged();
        }
        private void CloseEditPhone()
        {
            KontaktData = null;
            EditPhoneVisibility = false;
            StateHasChanged();
        }
        private async void SaveEditPhone()
        {
            if (AnagraficData != null && AnagraficData.AUTH_Users_ID != null)
            {
                var user = await AUTHProvider.GetUser(AnagraficData.AUTH_Users_ID.Value);

                var Code = await CodeInput.GetValue();

                if (user != null && !string.IsNullOrEmpty(Code))
                {
                    Message = await AccountService.VerifyPhone(user.ID, Code);

                    if (Message != null && Message.Code != null && Message.Code.Contains("VERIFYPHONE_SUCCESS"))
                    {
                        EditPhoneVisibility = false;
                        StateHasChanged();
                    }
                }
            }

            StateHasChanged();
        }
        private void CloseEditMail()
        {
            KontaktData = null;

            EditMailVisibility = false;
            StateHasChanged();
        }
        private void EditPhone()
        {
            if (AnagraficData != null)
            {
                KontaktData = new KontaktData();

                KontaktData.Input = AnagraficData.Phone;

                ShowPhoneVerification = false;
            }

            EditPhoneVisibility = true;
            StateHasChanged();
        }
        private async void VerifyMail()
        {
            if (KontaktData != null && AnagraficData != null && AnagraficData.AUTH_Users_ID != null && KontaktData.Input != AnagraficData.Email)
            {
                ShowMailVerification = true;

                var user = await AUTHProvider.GetUser(AnagraficData.AUTH_Users_ID.Value);

                if (KontaktData != null && user != null)
                {
                    user.EmailConfirmed = false;
                    user.Email = KontaktData.Input;

                    await AUTHProvider.UpdateUser(user);

                    await AccountService.SendVerificationEmail(user, true);
                }

                AnagraficData.Email = KontaktData.Input;

                await AUTHProvider.SetAnagrafic(AnagraficData);

                KontaktData = null;

                _EmailTimer = new System.Threading.Timer(async (object? stateInfo) =>
                {
                    if (AnagraficData.AUTH_Users_ID != null)
                    {
                        var checkMail = await AUTHProvider.GetUser(AnagraficData.AUTH_Users_ID.Value);

                        if (checkMail != null && checkMail.EmailConfirmed == true)
                        {
                            KontaktData = null;
                            EditMailVisibility = false;
                            await InvokeAsync(() =>
                            {
                                StateHasChanged();
                            });
                            if (_EmailTimer != null)
                            {
                                _EmailTimer.Dispose();
                            }
                        }
                    }
                }, new System.Threading.AutoResetEvent(false), 2000, 2000);
            }
            else
            {
                EditMailVisibility = false;
                StateHasChanged();
            }

            StateHasChanged();
        }
        private async void VerifyPhone()
        {
            if (KontaktData != null && AnagraficData != null && AnagraficData.AUTH_Users_ID != null && KontaktData.Input != AnagraficData.MobilePhone)
            {
                ShowPhoneVerification = true;

                var user = await AUTHProvider.GetUser(AnagraficData.AUTH_Users_ID.Value);

                if (KontaktData != null && user != null)
                {
                    user.PhoneNumberConfirmed = false;
                    user.PhoneNumber = KontaktData.Input;

                    await AUTHProvider.UpdateUser(user);

                    await AccountService.SendVerificationSMS(user);
                }

                AnagraficData.MobilePhone = KontaktData.Input;
                AnagraficData.Phone = KontaktData.Input;

                await AUTHProvider.SetAnagrafic(AnagraficData);

                KontaktData = null;
            }
            else
            {
                EditPhoneVisibility = false;
                StateHasChanged();
            }

            StateHasChanged();
        }
    }
}