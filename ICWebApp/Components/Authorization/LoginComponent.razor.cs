using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using ICWebApp.DataStore;
using Microsoft.AspNetCore.Components.Authorization;
using ICWebApp.Application.Helper;
using ICWebApp.Domain.DBModels;
using Microsoft.JSInterop;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components.Forms;

namespace ICWebApp.Components.Authorization
{
    public partial class LoginComponent
    {
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }   
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public string? RedirectUrl { get; set; }
        public bool IsBusy { get => _isBusy; set => _isBusy = value; }
        private Login login;
        private MSG_SystemMessages? Message;
        private bool _isBusy = true;
        private bool ShowResetPasswordButton = true;
        private EditForm LoginForm;

        protected override void OnInitialized()
        {
            login = new Login();

            EnviromentService.OnIsMobileChanged += EnviromentService_OnIsMobileChanged;

            StateHasChanged();

            base.OnInitialized();
        }
        private void EnviromentService_OnIsMobileChanged()
        {
            StateHasChanged();
        }
        private async void HandleValidSubmit()
        {
            if (LoginForm == null || LoginForm.EditContext == null)
                return;

            if (!LoginForm.EditContext.Validate())
                return;

            IsBusy = true;
            StateHasChanged();

            Message = await AccountService.Login(login);
            StateHasChanged();

            if (Message != null && Message.Code == "LOGIN_SUCCESS")
            {
                var UserRights = AuthProvider.GetUserRoles();

                if (UserRights != null && UserRights.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(RedirectUrl))
                    {
                        RedirectUrl = RedirectUrl.Replace("ReturnUrl=", "");
                        NavManager.NavigateTo(Uri.UnescapeDataString(RedirectUrl), true);
                        return;
                    }

                    if (UserRights.Select(p => p.AUTH_RolesID).Contains(AuthRoles.Employee) ||  //EMPLOYEE
                        UserRights.Select(p => p.AUTH_RolesID).Contains(AuthRoles.Administrator)     //Admin
                       ) 
                    { 
                        NavManager.NavigateTo("/Backend/Landing", true);
                        StateHasChanged();
                        return;
                    }

                    NavManager.NavigateTo("/", true);
                    StateHasChanged();
                    return;
                }
            }

            IsBusy = false;
            StateHasChanged();
        }
        private async void HandleResetPassword()
        {
            if (login != null)
            {
                IsBusy = true;
                ShowResetPasswordButton = false;
                StateHasChanged();

                Message = await AccountService.PasswordForgotten(login);

                if(Message == null || Message.MSG_SystemMessageTypesID != Guid.Parse("9574B88A-DAC4-4101-A27A-E5306224A6C0"))
                {
                    ShowResetPasswordButton = true;
                }

                IsBusy = false;
                StateHasChanged();

                await Task.Delay(20000);
                Message = null;
                ShowResetPasswordButton = true;
                StateHasChanged();
            }
        }
        private void HandleRegisterPassword()
        {
            NavManager.NavigateTo("/Registration", true);
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var prefixes = await AuthProvider.GetProgrammPrefixes();

                var CurrentMunicipality = prefixes.Where(p => p.Prefix != null && NavManager.BaseUri.Contains(p.Prefix)).FirstOrDefault();

                if (CurrentMunicipality != null && CurrentMunicipality.AUTH_Municipality_ID != null)
                {
                    SessionWrapper.AUTH_Municipality_ID = CurrentMunicipality.AUTH_Municipality_ID;
                }
                else if (NavManager.BaseUri.Contains("localhost"))
                {
                    SessionWrapper.AUTH_Municipality_ID = ComunixSettings.TestMunicipalityID;
                }
                else if (NavManager.BaseUri.Contains("192.168.77"))
                {
                    SessionWrapper.AUTH_Municipality_ID = ComunixSettings.TestMunicipalityID;
                }
                else
                {
                    if (!NavManager.BaseUri.Contains("spid"))
                    {
                        NavManager.NavigateTo("https://innovation-consulting.it/", true);
                    }
                }
                IsBusy = false;
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
