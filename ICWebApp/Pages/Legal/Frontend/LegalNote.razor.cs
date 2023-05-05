using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Legal.Frontend
{
    public partial class LegalNote
    {
        [Inject] private ITEXTProvider TextProvider { get; set; }
        [Inject] private IBreadCrumbService CrumbService { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }

        private bool IsDataBusy { get; set; }


        protected override void OnInitialized()
        {
            BusyIndicatorService.IsBusy = true;
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_LEGAL_NOTE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Cookie", "MAINMENU_LEGAL_NOTE", null);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            base.OnInitialized();
        }
    }
}