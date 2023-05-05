using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.Models.Rooms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Frontend
{
    public partial class SideBarComponent
    {
        [Inject] IRoomBookingHelper RoomBookingHelper { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }

        private string? TimeStampText;
        private string? RoomText;

        protected override void OnInitialized()
        {
            TimeStampText = TextProvider.Get("FRONTEND_BOOKING_NO_TIME_SELECTED");
            RoomText = TextProvider.Get("FRONTEND_BOOKING_NO_ROOM_SELECTED");

            BookingService.OnValuesChanged += BookingService_OnValuesChanged;

            base.OnInitialized();
        }

        private async void BookingService_OnValuesChanged()
        {
            await InvokeAsync(() =>
            {
                if (RoomBookingHelper.TimeFilter != null)
                {
                    TimeStampText = BookingService.GetDatesInString(RoomBookingHelper.TimeFilter);
                }

                if (RoomBookingHelper.RoomList != null)
                {
                    RoomText = BookingService.GetRoomsInString(RoomBookingHelper.RoomList);
                }

                StateHasChanged();
            });
        }
    }
}
