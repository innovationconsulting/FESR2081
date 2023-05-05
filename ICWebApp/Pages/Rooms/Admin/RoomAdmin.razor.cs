using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace ICWebApp.Pages.Rooms.Admin
{
    public partial class RoomAdmin
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] private IRoomProvider RoomProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<V_ROOM_Rooms> Data = new List<V_ROOM_Rooms>();
        public SchedulerView CurrView { get; set; } = SchedulerView.Month;
        private bool IsDataBusy { get; set; } = true;
        public DateTime? MinDate;

        protected override async Task OnInitializedAsync()
        {
            await GetData();

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Rooms", "ROOMBOOKING_ROOM_MANAGEMENT", null, null);

            SessionWrapper.PageTitle = TextProvider.Get("ROOMBOOKING_ROOM_MANAGEMENT");

            BusyIndicatorService.IsBusy = false;

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                IsDataBusy = true;

                Data = await RoomProvider.GetAllBuildings(SessionWrapper.AUTH_Municipality_ID.Value);

                IsDataBusy = false;
                StateHasChanged();
            }

            return true;
        }
        private void Add()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            NavManager.NavigateTo("/RoomEditAdmin/New" + "/" + 0);
        }
        private void Edit(V_ROOM_Rooms Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            NavManager.NavigateTo("/RoomEditAdmin/" + Item.ID + "/" + 0);
        }
        private async void Delete(V_ROOM_Rooms Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.RemoveRoom(Item.ID);
                await GetData();
                StateHasChanged();
            }
        }
    }
}
