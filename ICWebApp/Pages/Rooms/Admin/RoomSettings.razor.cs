using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Rooms.Admin
{
    public partial class RoomSettings

    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IRoomProvider ROOMProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private ROOM_Settings? Configuration { get; set; }


        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_ROOM_SETTINGS");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb(NavManager.Uri, "MAINMENU_BACKEND_ROOM_SETTINGS", null, null, true);

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Configuration = await ROOMProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);
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
                await ROOMProvider.SetSettings(Configuration);
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            return true;
        }
    }
}
