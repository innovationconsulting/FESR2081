using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Components.Layout.Frontend
{
    public partial class TopRow
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public bool ShowMenu { get; set; } = false;
        [Parameter] public bool ShowUserData { get; set; } = false;
        [Parameter] public bool ShowSearchButton { get; set; } = true;
        private AUTH_Municipality? Municipality { get; set; }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                BusyIndicatorService.IsBusy = true;
                SessionWrapper.PageIsPublic = true;
                SessionWrapper.OnAUTHMunicipalityIDChanged += SessionWrapper_OnAUTHMunicipalityIDChanged;
                StateHasChanged();

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

                var municipality = await SessionWrapper.GetMunicipalityID();

                if (SessionWrapper != null && municipality != null)
                {

                    Municipality = await AuthProvider.GetMunicipality(municipality.Value);
                }

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private void SessionWrapper_OnAUTHMunicipalityIDChanged()
        {
            StateHasChanged();
        }
        private void ReturnToStart()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/", true);
            StateHasChanged();
        }
    }
}
