using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Components.CodeInput;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Authorization
{
    public partial class VerifyPhoneComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] public IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }
        public AUTH_Users? User { get; set; }
        public AUTH_Users_Anagrafic? Anagrafic { get; set; }
        private bool SendMailBusyIndicator = false;
        private bool SendMailSuccessfullIndicator = false;
        private bool ConfirmChange = false;
        private bool WindowVisible { get; set; } = false;
        private ModalWindowParameters? WindowParameters;
        private MSG_SystemMessages? Message { get; set; }
        private CodeInputComponent CodeInput;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {

                if (string.IsNullOrEmpty(ID))
                {
                    return;
                }

                Guid IDGuid;

                if (!Guid.TryParse(ID, out IDGuid))
                {
                    return;
                }

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

                SessionWrapper.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();

                User = await AuthProvider.GetUser(IDGuid);

                if (User == null)
                {
                    return;
                }

                if (User.PhoneNumberConfirmed)
                {
                    NavManager.NavigateTo("/");
                    return;
                }

                await AccountService.SendVerificationSMS(User);

                ConfirmChange = false;
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private async void HandleLogout()
        {
            await AccountService.Logout();
            NavManager.NavigateTo("/", true);

            return;
        }
        private async void HandleSendAgain()
        {
            if (User != null)
            {
                SendMailBusyIndicator = true;
                StateHasChanged();

                await AccountService.SendVerificationSMS(User);

                SendMailSuccessfullIndicator = true;
                SendMailBusyIndicator = false;
                StateHasChanged();

                await Task.Delay(10000);
                SendMailSuccessfullIndicator = false;
                StateHasChanged();
            }
        }
        private async void EditPhone()
        {
            WindowParameters = new ModalWindowParameters();
            WindowParameters.Title = TextProvider.Get("EDIT_PHONE");
            WindowParameters.Width = "550px";
            WindowParameters.Height = "300px";

            if (User != null)
            {
                Anagrafic = await AuthProvider.GetAnagraficByUserID(User.ID);
                Anagrafic.Password = "placeholder";
                Anagrafic.ConfirmPassword = "placeholder";
                Anagrafic.DA_Email = "placeholder";
            }

            WindowVisible = true;
            StateHasChanged();
        }
        private void GetConfirm()
        {
            ConfirmChange = true;
            StateHasChanged();
        }
        private async void HandleSubmit()
        {
            ConfirmChange = false;
            StateHasChanged();

            if (User != null && Anagrafic != null)
            {
                Anagrafic.MobilePhone = Anagrafic.ReqPhoneNumber;
                User.PhoneNumber = Anagrafic.MobilePhone;

                await AuthProvider.SetAnagrafic(Anagrafic);


                await AuthProvider.UpdateUser(User);

                WindowVisible = false;
                NavManager.NavigateTo("/VerifyPhone/" + User.ID, true);
                StateHasChanged();
            }
        }
        private void CloseWindow()
        {
            ConfirmChange = false;
            WindowVisible = false;
            StateHasChanged();
        }
        private async void HandleVerification()
        {
            var Code = await CodeInput.GetValue();

            if (User != null && !string.IsNullOrEmpty(Code))
            {
                Message = await AccountService.VerifyPhone(User.ID, Code);

                if (Message != null && Message.Code != null && Message.Code.Contains("VERIFYPHONE_SUCCESS"))
                {
                    StateHasChanged();

                    Thread.Sleep(300);

                    if (!await AuthProvider.CheckVeriffVerification(User.ID))
                    {
                        NavManager.NavigateTo("/Veriff/" + User.ID, true);
                        StateHasChanged();
                        return;
                    }

                    NavManager.NavigateTo("/", true);
                    StateHasChanged();
                }
            }

            StateHasChanged();
        }
    }
}
