using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Interface;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using DocumentFormat.OpenXml.Wordprocessing;
using ICWebApp.Domain.Models;
using Telerik.Blazor.Components.Map;

namespace ICWebApp.Components.Rooms.Admin
{
    public partial class AdditionalRoomsComponent
    {
        [Inject] ITEXTProvider? TextProvider { get; set; }
        [Inject] ISessionWrapper? SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService? BusyIndicatorService { get; set; }
        [Inject] IRoomProvider? RoomProvider { get; set; }
        [Inject] IBreadCrumbService? CrumbService { get; set; }
        [Inject] IFILEProvider? FileProvider { get; set; }
        [Inject] IAUTHProvider? AuthProvider { get; set; }
        [Inject] ILANGProvider? LangProvider { get; set; }
        [Inject] NavigationManager? NavManager { get; set; }
        [Inject] IRoomGalerieHelper? RoomGalerieHelper { get; set; }

        [CascadingParameter] public DialogFactory? Dialogs { get; set; }
        [Parameter] public Guid? RoomID { get; set; } = Guid.Empty;

        private ROOM_Rooms? Data;

        private List<V_ROOM_Rooms>? AdditionalRoomList { get; set; }


        private async Task<bool> GetAdditionalRoomData()
        {

            AdditionalRoomList = await RoomProvider.GetAllRoomsByBuildingID(RoomID??Guid.Empty);

            return true;
        }

        protected override async Task OnInitializedAsync()
        {
            await GetAdditionalRoomData();
            await base.OnInitializedAsync();
        }

        private async Task<ROOM_Rooms?> GetRoom()
        {
            return await RoomProvider.GetRoom(RoomID ?? Guid.Empty);
        }

        protected override async Task OnParametersSetAsync()
        {
            Data = await GetRoom();
            await GetAdditionalRoomData();
            StateHasChanged();
            await base.OnParametersSetAsync();
        }


        private async void EditAdditionalRoom(V_ROOM_Rooms Item)
        {
            if (Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await RoomProvider.SetRoom(Data);

                foreach (var ext in Data.ROOM_Rooms_Extended)
                {
                    await RoomProvider.SetRoomExtended(ext);
                }

                NavManager.NavigateTo("/RoomEditAdmin/" + Item.ID + "/" + 0 + "/" + 0 + "/" + Data.ID);
                StateHasChanged();
            }
        }
        private async void DeleteAdditionalRoom(V_ROOM_Rooms Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.RemoveRoom(Item.ID);
                await GetAdditionalRoomData();
                StateHasChanged();
            }
        }

    }
}
