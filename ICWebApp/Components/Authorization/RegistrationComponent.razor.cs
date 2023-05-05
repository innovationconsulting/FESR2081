using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using IntlTelInputBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;

namespace ICWebApp.Components.Authorization
{
    public partial class RegistrationComponent
    {
        [Inject] IAccountService AccountService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IMSGProvider MSGProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicator { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IMETAProvider MetaProvider { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Parameter] public bool Embeedded { get; set; } = false;
        [Parameter] public EventCallback<Guid?> EventCallback { get; set; }
        [Parameter] public bool ShowPrivacy { get; set; } = true;
        private PRIV_Privacy? Privacy { get; set; }

        private EditContext editContext { get; set; }
        private AUTH_Users user;
        private AUTH_Users_Anagrafic anagrafic;
        private bool validFiscalNumber = true;
        private bool _privacyAccepted = false;
        private bool _privacyMuniciplityAccepted = false;
        private bool _privacyCommunicationsAccepted = false;
        private bool _displayPrivacyNotAcceptedError = false;
        private string validFiscalNumberCSS
        {
            get
            {
                if (!validFiscalNumber || (editContext != null && editContext.GetValidationMessages(new FieldIdentifier(anagrafic, "ReqFiscalNumber")).Count() > 0))
                {
                    return "outline: 1px solid red !important;";
                }

                return "";
            }
        }
        private string validPhoneNumberCSS
        {
            get
            {
                if (editContext != null && editContext.GetValidationMessages(new FieldIdentifier(anagrafic, "ReqPhoneNumber")).Count() > 0)
                {
                    return "outline: 1px solid red !important;";
                }

                return "";
            }
        }
        private string valFiscalNumber
        {
            get
            {
                return anagrafic.ReqFiscalNumber;
            }
            set
            {
                anagrafic.ReqFiscalNumber = value;
                validFiscalNumber = true;
                //ValidateFiscalNumber();
            }
        }
        private bool MaleSelected
        {
            get
            {

                if (anagrafic != null && anagrafic.Gender == "M")
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (anagrafic != null && value == true)
                {
                    anagrafic.Gender = "M";
                    StateHasChanged();
                }
            }
        }
        private bool FemaleSelected
        {
            get
            {

                if (anagrafic != null && anagrafic.Gender == "W")
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (anagrafic != null && value == true)
                {
                    anagrafic.Gender = "W";
                    StateHasChanged();
                }
            }
        }
        private bool IsBusy = true;
        private bool IsValidPassword = false;
        private PasswordHelper pwhelper = new PasswordHelper();
        private string PasswordQuality { get; set; } = "";
        private string Password
        {
            get
            {
                return anagrafic.Password;
            }
            set
            {
                anagrafic.Password = value;
                PasswordQuality = pwhelper.GetPasswordStrength(value).ToString();
                IsValidPassword = pwhelper.IsValidPassword(value);
                StateHasChanged();
            }
        }
        private Guid SelectedMunicipality 
        {
            get
            {
                if(anagrafic != null && anagrafic.SelectedMunicipality != null) 
                    return anagrafic.SelectedMunicipality.Value;

                return Guid.Empty;
            }
            set
            {
                if(value != Guid.Empty)
                {
                    UpdateAddressData(value);
                }

                if (anagrafic != null)
                {
                    anagrafic.SelectedMunicipality = value;
                }
            }
        }
        private List<META_IstatComuni>? MunicipalitiesList { get; set; }
        private bool AddressNotFound { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            user = new AUTH_Users();
            anagrafic = new AUTH_Users_Anagrafic();

            editContext = new EditContext(anagrafic);

            user.ID = Guid.NewGuid();
            user.PasswordHash = "placeholder";
            user.Username = "placeholder";

            if (Embeedded)
            {
                anagrafic.Password = "default";
                anagrafic.ConfirmPassword = "default";
            }

            anagrafic.AUTH_Users_ID = user.ID;
            anagrafic.ReqGender = "M";
            anagrafic.Email = "placeholder";

            anagrafic.ReqPhoneNumber = "+39";

            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            IsBusy = false;

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Privacy == null)
            {
                var municipality = await SessionWrapper.GetMunicipalityID();

                if (municipality != null)
                {
                    Privacy = await PrivProvider.GetPrivacy(municipality.Value);
                }

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private void HandleInvalidSubmit()
        {
            ValidateFiscalNumber();
            if (!_privacyAccepted || !_privacyCommunicationsAccepted || !_privacyMuniciplityAccepted)
            {
                _displayPrivacyNotAcceptedError = true;
                StateHasChanged();
            }
        }
        private async void HandleValidSubmit()
        {
            ValidateFiscalNumber();
            if (user != null && anagrafic != null && validFiscalNumber && (IsValidPassword || Embeedded) && anagrafic.ReqPhoneNumber.Trim() != "+39" && ((_privacyAccepted && _privacyMuniciplityAccepted && _privacyCommunicationsAccepted) || ShowPrivacy == false))
            {
                IsBusy = true;
                StateHasChanged();

                anagrafic.Email = anagrafic.DA_Email;
                anagrafic.RegisteredOffice = "online";
                anagrafic.Address = anagrafic.DomicileStreetAddress + " " + anagrafic.DomicilePostalCode + " " + anagrafic.DomicileMunicipality;

                user.Firstname = anagrafic.FirstName;
                user.Lastname = anagrafic.LastName;
                user.Email = anagrafic.DA_Email;
                if (anagrafic.MobilePhone != null && !anagrafic.MobilePhone.StartsWith("+"))
                {
                    anagrafic.MobilePhone = "+" + anagrafic.MobilePhone;
                }
                user.PhoneNumber = anagrafic.MobilePhone;
                user.DA_Email = anagrafic.DA_Email;
                user.RegistrationMode = "custom";
                user.Password = anagrafic.Password;
                user.ConfirmPassword = anagrafic.ConfirmPassword;

                if (Embeedded)
                {
                    anagrafic.RegisteredOffice = "Citizen Backend";
                    user.RegistrationMode = "Citizen Backend";
                }

                user.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();

                var ResultUser = await AccountService.Register(user, anagrafic);

                if (ResultUser != null)
                {
                    anagrafic.AUTH_Users_ID = ResultUser.ID;

                    await AuthProvider.SetAnagrafic(anagrafic);

                    await AuthProvider.SetRole(ResultUser.ID, AuthRoles.Citizen);    //CITIZEN

                    if (!Embeedded)
                    {
                        var login = new Login();

                        login.UserName = user.Username;
                        login.Password = user.Password;

                        await AccountService.Login(login);
                        await AccountService.SendWelcomeMail(user);

                        NavManager.NavigateTo("/", true);
                        StateHasChanged();
                    }
                    else
                    {
                        EventCallback.InvokeAsync(ResultUser.ID);
                    }
                }

                IsBusy = false;
                StateHasChanged();
            }
            else
            {
                if (!_privacyAccepted || !_privacyCommunicationsAccepted || !_privacyMuniciplityAccepted)
                {
                    _displayPrivacyNotAcceptedError = true;
                    StateHasChanged();
                }
            }
        }
        private void BackToPreviousPage()
        {
            if (!Embeedded)
            {
                NavManager.NavigateTo("/", true);
            }
            else
            {
                EventCallback.InvokeAsync(null);
            }
        }
        private void ValidateFiscalNumber()
        {
            if (anagrafic.FiscalNumber == null)
            {
                validFiscalNumber = true;
            }
            else
            {
                if (Embeedded)
                {
                    validFiscalNumber = AccountService.IsFiscalNumberUnique(anagrafic.FiscalNumber, true);
                }
                else
                {
                    validFiscalNumber = AccountService.IsFiscalNumberUnique(anagrafic.FiscalNumber, false);
                }

            }
            
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
        private async void UpdateAddressData(Guid Municipality_ID)
        {
            var data = await MetaProvider.GetMunicipality(Municipality_ID);

            if(data != null)
            {
                anagrafic.DomicilePostalCode = data.Cap;
                anagrafic.DomicileMunicipality = data.NameDE;
                anagrafic.DomicileProvince = data.RegionCity;
                anagrafic.DomicileNation = "IT";

                StateHasChanged();
            }
        }
    }
}
