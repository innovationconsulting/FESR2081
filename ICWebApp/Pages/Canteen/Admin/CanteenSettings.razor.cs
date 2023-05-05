using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Admin
{
    public partial class CanteenSettings

    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        private CANTEEN_Configuration? Configuration { get; set; }


        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_CANTEEN_SETTINGS");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb(NavManager.Uri, "MAINMENU_BACKEND_CANTEEN_SETTINGS", null, null, true);

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Configuration = await CanteenProvider.GetConfiguration(SessionWrapper.AUTH_Municipality_ID.Value);

                if(Configuration == null)
                {
                    Configuration = new CANTEEN_Configuration();
                    Configuration.ID = Guid.NewGuid();
                    Configuration.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;

                    Configuration.BalanceLowWarning = 20;

                    Configuration.BalanceLowServiceStop = -50;

                    Configuration.RechargeMinAmount = 50;

                    await CanteenProvider.SetConfiguration(Configuration);
                }
            }


            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<bool> SaveConfig()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (Configuration != null)
            {
                await CanteenProvider.SetConfiguration(Configuration);
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            return true;
        }
    }
}
