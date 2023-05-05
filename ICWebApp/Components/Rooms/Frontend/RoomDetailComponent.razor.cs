using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Frontend
{
    public partial class RoomDetailComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Parameter] public V_ROOM_Rooms? Room { get; set; }

        private List<ROOM_RoomGalerie> RoomGalerie = new List<ROOM_RoomGalerie>();
        private List<V_ROOM_RoomOptions>? RoomInventoryList = new List<V_ROOM_RoomOptions>();
        private List<ROOM_RoomPricing> RoomPricing = new List<ROOM_RoomPricing>();
        private List<V_AUTH_Company_Type> CompanyTypeList = new List<V_AUTH_Company_Type>();
        private List<V_ROOM_Rooms_Contact> RoomContacts = new List<V_ROOM_Rooms_Contact>();

        protected override async Task OnInitializedAsync()
        {
            if (Room != null)
            {
                RoomGalerie = await RoomProvider.GetRoomGalerie(null, Room.ID);
                var data = await RoomProvider.GetRoomOptionsList(Room.ID);

                RoomInventoryList = data.Where(p => p.Enabled == true).ToList();

                RoomPricing = await RoomProvider.GetRoomPricing(Room.ID);

                CompanyTypeList = await AuthProvider.GetVCompanyType();
                CompanyTypeList = CompanyTypeList.Where(p => p.ShowOnRooms == true).ToList();

                var contacts = await RoomProvider.GetVRoomsContacts(Room.ID);

                RoomContacts = contacts.Where(p => p.ShowOnline == true).ToList();

                if (Room.RoomGroupFamilyID != null) 
                {
                    var parentContacts = await RoomProvider.GetVRoomsContacts(Room.RoomGroupFamilyID.Value);

                    parentContacts = parentContacts.Where(p => p.ShowOnline == true).ToList();

                    RoomContacts.AddRange(parentContacts);
                }


                StateHasChanged();
            }

            await base.OnInitializedAsync();
        }
    }
}
