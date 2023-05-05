using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Pages.Rooms.Frontend
{
    public partial class BookingComitted
    {
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Parameter] public string BookingGroupID { get; set; }

        protected override void OnInitialized()
        {
            SessionWrapper.PageTitle = TextProvider.Get("FRONTEND_BOOKING_COMMITTED_SUCCESS_TITLE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Rooms", "MAINMENU_ROOMBOOKING", null, null);
            CrumbService.AddBreadCrumb(NavManager.Uri, "FRONTEND_BOOKING_COMMITTED_SUCCESS_TITLE", null, null, true);

            if (string.IsNullOrEmpty(BookingGroupID))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();
                return;
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            base.OnInitialized();
        }
        private void GoToLanding()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/User/Services");
            StateHasChanged();
        }
    }
}
