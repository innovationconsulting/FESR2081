using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Pages.Form.Frontend;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace ICWebApp.Pages.Rooms.Backend
{
    public partial class RoomBookingManagement
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IRoomAdministrationHelper RoomAdministrationHelper { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<V_ROOM_Booking_Group> Bookinggroups = new List<V_ROOM_Booking_Group>();
        private Administration_Filter_RoomBookingGroup Filter = new Administration_Filter_RoomBookingGroup();
        private bool IsDataBusy = true;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("ROOMBOOKING_ROOM_BOOKING_LIST");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/RoomBooking/List", "ROOMBOOKING_ROOM_BOOKING_LIST", null, null, true);

            if (RoomAdministrationHelper.Filter != null)
            {
                Filter = RoomAdministrationHelper.Filter;
            }
            else
            {
                Filter.Room_ID = new List<Guid>();
                Filter.Booking_Status_ID = new List<Guid>();
                Filter.Booking_Status_ID.Add(ROOMStatus.Comitted);   //REQUEST
                Filter.Booking_Status_ID.Add(ROOMStatus.CancelledByCitizen);   //REQUEST
            }

            Bookinggroups = await GetData();

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<V_ROOM_Booking_Group>> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var data = await RoomProvider.GetBookingGroups(SessionWrapper.AUTH_Municipality_ID.Value, Filter);

                RoomAdministrationHelper.Filter = Filter;

                return data;
            }

            return new List<V_ROOM_Booking_Group>();
        }
        private void ShowDetails(V_ROOM_Booking_Group Item)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/RoomBooking/Detail/" + Item.ID);
            StateHasChanged();
        }
        private async void FilterSearch(Administration_Filter_RoomBookingGroup Filter)
        {
            IsDataBusy = true;
            StateHasChanged();

            Bookinggroups = await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
        {
            var item = (V_ROOM_Booking_Group)Args.Item; ;

            if (item != null)
            {
                ShowDetails(item);
            }

            return true;
        }
    }
}
