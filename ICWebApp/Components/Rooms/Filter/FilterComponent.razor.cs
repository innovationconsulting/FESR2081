using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Rooms.Filter
{
    public partial class FilterComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IRoomAdministrationHelper RoomAdministrationHelper { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public EventCallback<Administration_Filter_RoomBookingGroup> OnSearch { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public Administration_Filter_RoomBookingGroup Filter { get; set; }
        [Parameter] public bool Modal { get; set; } = false;

        private List<V_ROOM_Booking_Status> StatusList = new List<V_ROOM_Booking_Status>();
        private List<V_ROOM_Rooms> RoomList = new List<V_ROOM_Rooms>();
        private List<V_ROOM_Booking_Group_Users> UserList = new List<V_ROOM_Booking_Group_Users>();
        
        private bool FilterWindowVisible { get; set; } = false;
        private bool PopUpAktivated = false;

        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            StatusList = await RoomProvider.GetBookingStatusList();
            RoomList = await RoomProvider.GetAllRoomsWithoutBuildings(SessionWrapper.AUTH_Municipality_ID.Value);
             
            await GetFilterValues();
            StateHasChanged();
        }
        private async Task<bool> GetFilterValues()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                UserList = await RoomProvider.GetBookingGroupUsers(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return true;
        }
        private void AddFilter(Guid ROOM_Rooms_ID)
        {
            if (Filter.Room_ID == null)
                Filter.Room_ID = new List<Guid>();

            if (Filter.Room_ID.Contains(ROOM_Rooms_ID))
            {
                Filter.Room_ID.Remove(ROOM_Rooms_ID);

            }
            else
            {
                Filter.Room_ID.Add(ROOM_Rooms_ID);
            }

            OnSearch.InvokeAsync(Filter);

            StateHasChanged();
        }
        private void ClearTagFilter()
        {
            if (Filter != null && Filter.Room_ID != null)
            {
                Filter.Room_ID = null;
                OnSearch.InvokeAsync(Filter);
                StateHasChanged();
            }
        }
        private void FilterClear()
        {
            Filter.Text = null;
            Filter.SubmittedFrom = null;
            Filter.SubmittedTo = null;
            Filter.Auth_User_ID = null;
            Filter.Room_ID = new List<Guid>();
            Filter.Booking_Status_ID = new List<Guid>();
            Filter.Booking_Status_ID.Add(Guid.Parse("b99595e0-b4e1-4f46-a10a-7d42f80c491e"));   //REQUEST
            Filter.Booking_Status_ID.Add(Guid.Parse("77a0c136-f145-4f6f-9505-9760b09f37e4"));   //ABGESAGT

            StateHasChanged();
        }
        private void FilterSearch()
        {
            OnSearch.InvokeAsync(Filter);
            FilterWindowVisible = false;
            StateHasChanged();
        }
        private void ToggleFilter()
        {
            FilterWindowVisible = !FilterWindowVisible;

            if (FilterWindowVisible)
            {
                PopUpAktivated = false;
            }

            StateHasChanged();
        }
        private async void EnviromentService_OnScreenClicked()
        {
            if (FilterWindowVisible && PopUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("filter-popup");

                if (!onScreen)
                {
                    ToggleFilter();
                }
            }
            else
            {
                PopUpAktivated = true;
            }
        }
        private void ClearSearchBar()
        {
            if (Filter != null)
            {
                FilterClear();

                FilterSearch();
            }
        }
    }
}
