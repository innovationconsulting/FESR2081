using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Legal.Frontend
{
    public partial class Privacy
    {
        [Inject] private ITEXTProvider TextProvider { get; set; }
        [Inject] private IBreadCrumbService CrumbService { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }

        private bool IsDataBusy { get; set; }


        protected override void OnInitialized()
        {
            BusyIndicatorService.IsBusy = true;
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_PRIVACY");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Privacy", "MAINMENU_PRIVACY", null);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            base.OnInitialized();
        }
    }
}