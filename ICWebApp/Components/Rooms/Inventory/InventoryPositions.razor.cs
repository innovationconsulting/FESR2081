using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Components.Rooms.Inventory
{
    public partial class InventoryPositions
    {
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public Guid? ROOM_RoomOptions_ID { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        public List<V_ROOM_RoomOptions_Positions>? PositionsList;
        private bool ShowNewAmountWindow = false;
        private ROOM_RoomOptions_Positions? NewItem;
        private string? InventoryMinValueError = null;

        protected override async Task OnInitializedAsync()
        {
            if (ROOM_RoomOptions_ID != null)
            {
                PositionsList = await GetPositions();
            }
            else
            {
                await OnCancel.InvokeAsync();
            }

            StateHasChanged();

            await base.OnInitializedAsync();
        }

        public async Task<List<V_ROOM_RoomOptions_Positions>> GetPositions()
        {
            return await RoomProvider.GetRoomOptionsPositionList(ROOM_RoomOptions_ID.Value);
        }
        public void AddPosition()
        {
            NewItem = new ROOM_RoomOptions_Positions();

            NewItem.ID = Guid.NewGuid();
            NewItem.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
            NewItem.ROOM_Rooms_Options_ID = ROOM_RoomOptions_ID.Value;
            
            ShowNewAmountWindow = true;
            StateHasChanged();
        }
        public async void SavePosition()
        {
            if (NewItem != null)
            {
                if (NewItem.Quantity != null && NewItem.Quantity.Value < 0)
                {
                    InventoryMinValueError = TextProvider.Get("ROOM_INVENTORY_MIN_VALUE");
                    StateHasChanged();
                    return;
                }

                NewItem.CreationDate = DateTime.Now;

                await RoomProvider.SetRoomOptionsPosition(NewItem);

                PositionsList = await GetPositions();

                ShowNewAmountWindow = false;
                StateHasChanged();
            }
        }
        public void HideNewPosition()
        {
            NewItem = null;

            ShowNewAmountWindow = false;
            StateHasChanged();
        }
        public async void DeletePosition(V_ROOM_RoomOptions_Positions Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.RemoveRoomOptionsPosition(Item.ID);
                PositionsList = await GetPositions();

                StateHasChanged();
            }
        }
        private void CloseWindow()
        {
            OnCancel.InvokeAsync();
            StateHasChanged();
        }
    }
}
