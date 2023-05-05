using Blazored.SessionStorage;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Shared.Redirect
{
    public partial class RedirectToLogin
    {
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ISessionStorageService SessionStorage { get; set; }
        [Parameter] public string? RedirectURL { get; set; }


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!string.IsNullOrEmpty(RedirectURL))
            {
                await SessionStorage.SetItemAsync("RedirectURL", RedirectURL);
                NavManager.NavigateTo("/Login/ReturnUrl=" + Uri.EscapeDataString(RedirectURL), true);
                return;
            }

            NavManager.NavigateTo("/Login", true);
            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
