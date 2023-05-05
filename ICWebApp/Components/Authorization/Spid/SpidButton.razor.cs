using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Components.Authorization.Spid
{
    public partial class SpidButton
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ISPIDService SPIDService { get; set; }
        private string SpidResponse { get; set; }

        private async Task<bool> OnRedirect()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var result = await SPIDService.AuthenticateSpid(SessionWrapper.AUTH_Municipality_ID.Value, NavManager.BaseUri);

                if(!string.IsNullOrEmpty(result))
                {
                    NavManager.NavigateTo(result);
                    StateHasChanged();
                }
                else
                {
                    SpidResponse = TextProvider.Get("SPID_GENERIC_ERROR");
                }
            }

            StateHasChanged();

            return true;
        }
    }
}
