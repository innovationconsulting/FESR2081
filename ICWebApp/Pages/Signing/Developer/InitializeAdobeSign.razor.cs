using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Signing.Developer
{
    public partial class InitializeAdobeSign
    {
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISignService SignService { get; set; }
        [Inject] ICONFProvider ConfigProvider { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private ITEXTProvider TextProvider { get; set; }
        private string? LoginUrl { get; set; }
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.GetOrCreate("DEV_ADOBESIGN");
            if (!AuthProvider.HasUserRole(AuthRoles.Developer))
            {
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            var Conf = await ConfigProvider.GetSignConfiguration(null);

            if (Conf != null)
            {
                var State = Guid.NewGuid();

                Conf.State = State.ToString();

                await ConfigProvider.SetSignConfiguration(Conf);

                LoginUrl = await SignService.GetSignOnURL(Conf.State);

                if (LoginUrl != null)
                {
                    NavManager.NavigateTo(LoginUrl, true);
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
    }
}
