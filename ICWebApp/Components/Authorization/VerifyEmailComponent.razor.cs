using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Components.CodeInput;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Authorization
{
    public partial class VerifyEmailComponent
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
        private MSG_SystemMessages? Message { get; set; }
        private ModalWindowParameters? WindowParameters;
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

                SessionWrapper.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();

                User = await AuthProvider.GetUser(IDGuid);

                if (User == null)
                {
                    return;
                }

                if (User.EmailConfirmed == true)
                {
                    NavManager.NavigateTo("/");
                    return;
                }

                await AccountService.SendVerificationEmail(User);

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

                await AccountService.SendVerificationEmail(User);

                SendMailSuccessfullIndicator = true;
                SendMailBusyIndicator = false;
                StateHasChanged();

                await Task.Delay(10000);
                SendMailSuccessfullIndicator = false;
                StateHasChanged();
            }
        }
        private async void EditEmail()
        {
            WindowParameters = new ModalWindowParameters();
            WindowParameters.Title = TextProvider.Get("EDIT_EMAIL");
            WindowParameters.Width = "550px";

            if (User != null)
            {
                Anagrafic = await AuthProvider.GetAnagraficByUserID(User.ID);
                Anagrafic.DA_Email = User.Email;
                Anagrafic.Password = "placeholder";
                Anagrafic.ConfirmPassword = "placeholder";
            }

            WindowVisible = true;
            StateHasChanged();
        }
        private async void HandleSubmit()
        {
            ConfirmChange = false;
            StateHasChanged();

            if (User != null && Anagrafic != null)
            {
                Anagrafic.Email = Anagrafic.DA_Email;

                User.Email = Anagrafic.Email;

                await AuthProvider.SetAnagrafic(Anagrafic);
                

                await AuthProvider.UpdateUser(User);

                WindowVisible = false;
                NavManager.NavigateTo("/VerifyEmail/" + User.ID, true);
                StateHasChanged();
            }
        }
        private async void HandleVerification()
        {
            var Code = await CodeInput.GetValue();

            if (User != null && !string.IsNullOrEmpty(Code))
            {
                Message = await AccountService.VerifyEmail(User.ID, Code);

                if (Message != null && Message.Code != null && Message.Code.Contains("VERIFYEMAIL_SUCCESS"))
                {
                    StateHasChanged();

                    Thread.Sleep(300);

                    if (!await AuthProvider.CheckPhoneVerification(User.ID))
                    {
                        NavManager.NavigateTo("/VerifyPhone/" + User.ID, true);
                        StateHasChanged();
                        return;
                    }
                    else if (!await AuthProvider.CheckVeriffVerification(User.ID))
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
        private void CloseWindow()
        {
            ConfirmChange = false;
            WindowVisible = false;
            StateHasChanged();
        }
        private void GetConfirm()
        {
            ConfirmChange = true;
            StateHasChanged();
        }
    }
}
