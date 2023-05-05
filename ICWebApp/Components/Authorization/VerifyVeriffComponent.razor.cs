using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Components.CodeInput;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Authorization
{
    public partial class VerifyVeriffComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] public IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IVeriffService VeriffService { get; set; }
        [Parameter] public string ID { get; set; }
        public AUTH_Users? User { get; set; }

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

                if (User.VeriffConfirmed)
                {
                    NavManager.NavigateTo("/");
                    return;
                }

                await VeriffService.InitializeVeriff(User.ID, "veriff-root");

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
    }
}
