using Microsoft.AspNetCore.Components;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Interface.Provider;

namespace ICWebApp.Components.Authorization
{
    public partial class LogoutComponent
    {
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        private async void HandleLogout()
        {
            await AccountService.Logout();
            NavManager.NavigateTo("/");
        }
    }
}
