using ICWebApp.Application.Interface;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Frontend
{
    public partial class RoomCardComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] IRoomBookingHelper RoomBookingHelper { get; set; }
        [Inject] IBookingService BookingService { get; set; }

        [Parameter] public V_ROOM_Rooms Room { get; set; }
        [Parameter] public EventCallback<V_ROOM_Rooms> TimeSelectionChanged { get; set; }

        IJSObjectReference? _module;

        private bool MapVisible = false;
        private bool DetailsVisible = false;
        private bool CalendarVisible = false;
        public string[] Subdomains { get; set; } = new string[] { "a", "b", "c" };
        public string UrlTemplate { get; set; } = "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_module == null)
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/Rooms/RoomHelper.js");
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        protected override void OnParametersSet()
        {

            StateHasChanged();
            base.OnParametersSet();
        }

        private async void SelectRoom()
        {
            if (Room.HasRooms == false)
            {
                Room.IsSelected = !Room.IsSelected;
                StateHasChanged();
            }
            else
            {
                if (_module != null)
                {
                    await _module.InvokeVoidAsync("ToggleElement", Room.ID.ToString());
                    StateHasChanged();
                }
            }

            BookingService.NotifyValuesChanged();
        }  
        private void ShowMap()
        {
            MapVisible = true;
            StateHasChanged();
        }
        private void HideMap()
        {
            MapVisible = false;
            StateHasChanged();
        }
        private void ShowDetails()
        {
            DetailsVisible = true;
            StateHasChanged();
        }
        private void HideDetails()
        {
            DetailsVisible = false;
            StateHasChanged();
        }
        private void ShowCalendar()
        {
            CalendarVisible = true;
            StateHasChanged();
        }
        private void HideCalendar()
        {
            CalendarVisible = false;
            StateHasChanged();
        }
        private async void PassTroughEvent()
        {
            await TimeSelectionChanged.InvokeAsync(Room);
        }
        private async void OnTimeSelected()
        {
            HideCalendar();
            Room.IsSelected = true;
            await TimeSelectionChanged.InvokeAsync(Room);
        }
    }
}
