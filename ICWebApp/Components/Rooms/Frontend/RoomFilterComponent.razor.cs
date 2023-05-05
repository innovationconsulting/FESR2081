using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Rooms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Frontend
{
    public partial class RoomFilterComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IRoomBookingHelper RoomBookingHelper { get; set; }

        [Parameter] public TimeFilter? Filter { get; set; }
        [Parameter] public EventCallback OnSearch { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                if(RoomBookingHelper.RoomList == null)
                    RoomBookingHelper.RoomList = await RoomProvider.GetAllRooms(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private void OnTimeSelectionChanged(V_ROOM_Rooms Room)
        {
            BookingService.NotifyValuesChanged();
            OnSearch.InvokeAsync();
            StateHasChanged();
        }
    }
}
