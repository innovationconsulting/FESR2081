using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Layout.Frontend
{
    public partial class Footer
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        private AUTH_Municipality? Municipality;
        private AUTH_Municipality_Footer? MunicipalityFooter;
        private List<AUTH_MunicipalityApps>? AktiveApps = new List<AUTH_MunicipalityApps>();
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                BusyIndicatorService.IsBusy = true;
                SessionWrapper.PageIsPublic = true;
                StateHasChanged();

                var municipality = await SessionWrapper.GetMunicipalityID();

                if (SessionWrapper != null && municipality != null)
                {
                    Municipality = await AuthProvider.GetMunicipality(municipality.Value);
                    MunicipalityFooter = await AuthProvider.GetMunicipalityFooter(municipality.Value, LANGProvider.GetCurrentLanguageID());  
                    AktiveApps = await AuthProvider.GetMunicipalityApps();
                }

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private void GoToRooms()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Rooms");
            StateHasChanged();
        }
        private void GoToMensa()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Canteen");
            StateHasChanged();
        }
        private void GoToApplications()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Form");
            StateHasChanged();
        }
        private void GoToNotifications()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Mantainance");

            StateHasChanged();
        }
    }
}
