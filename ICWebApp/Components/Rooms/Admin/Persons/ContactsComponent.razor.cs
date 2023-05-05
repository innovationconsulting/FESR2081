using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Admin.Persons
{
    public partial class ContactsComponent
    {
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public List<ROOM_Rooms_Contact>? Contacts { get; set; }
        [Parameter] public Guid RoomID { get; set; }
        [Parameter] public EventCallback OnContactChanged { get; set; }

        private List<V_ROOM_Contact_Type> ContactTypes = new List<V_ROOM_Contact_Type>();
        private List<ROOM_Rooms_Contact_Type> RoomContactTypes = new List<ROOM_Rooms_Contact_Type>();
        private bool ShowContactSelection = false;
        private bool ShowContactTypeSelection = false;
        private string[]? SelectedContactTypeIDs = null;
        private ROOM_Rooms_Contact? CurrentContact = null;

        protected override async Task OnInitializedAsync()
        {
            ContactTypes = await RoomProvider.GetContactTypes();

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private void AddContact()
        {
            if (Contacts != null)
            {
                ShowContactSelection = true;
                StateHasChanged();
            }
        }
        private void SaveContact(CONT_Contact Item)
        {
            if (Contacts != null)
            {
                ROOM_Rooms_Contact NewItem = new ROOM_Rooms_Contact();

                NewItem.ID = Guid.NewGuid();
                NewItem.CONT_Contact = Item;
                NewItem.CONT_Contact_ID = Item.ID;
                NewItem.ROOM_Rooms_ID = RoomID;

                Contacts.Add(NewItem);

                ShowContactSelection = false;
                StateHasChanged();
            }
        }
        private void CloseContact()
        {
            ShowContactSelection = false;
            StateHasChanged();            
        }
        private void RemoveContact(ROOM_Rooms_Contact Item)
        {
            if (Contacts != null)
            {
                Contacts.Remove(Item);
                StateHasChanged();
            }
        }
        private void ToggleEmail(ROOM_Rooms_Contact Item)
        {
            Item.SendEmail = !Item.SendEmail;
            StateHasChanged();
        }
        private void ToggleSMS(ROOM_Rooms_Contact Item)
        {
            Item.SendSMS = !Item.SendSMS;
            StateHasChanged();
        }
        private async void SetContactType(ROOM_Rooms_Contact Item)
        {
            if (Item != null)
            {
                CurrentContact = Item;
                SelectedContactTypeIDs = new string[] { };
                RoomContactTypes = await RoomProvider.GetRoomContactTypes(Item.ID);

                List<string> Types = new List<string>();

                foreach (var cont in RoomContactTypes)
                {
                    if (cont.ROOM_Contact_Type_ID != null)
                    {
                        Types.Add(cont.ROOM_Contact_Type_ID.Value.ToString());
                    }
                }

                SelectedContactTypeIDs = Types.ToArray();
                ShowContactTypeSelection = true;
                StateHasChanged();
            }
        }
        private async void SaveContactType()
        {
            if (RoomContactTypes != null && SelectedContactTypeIDs != null && CurrentContact != null)
            {
                foreach (var cont in RoomContactTypes)
                {
                    await RoomProvider.RemoveRoomContactType(cont);
                }

                foreach (var cont in SelectedContactTypeIDs)
                {
                    ROOM_Rooms_Contact_Type newCont = new ROOM_Rooms_Contact_Type();

                    newCont.ID = Guid.NewGuid();
                    newCont.ROOM_Rooms_Contact_ID = CurrentContact.ID;
                    newCont.ROOM_Contact_Type_ID = Guid.Parse(cont);

                    await RoomProvider.SetRoomContactType(newCont);
                }
            }

            if (Contacts != null && CurrentContact != null)
            {
                var listContact = Contacts.FirstOrDefault(p => p.ID == CurrentContact.ID);

                if (listContact != null)
                {
                    listContact.ShowOnline = CurrentContact.ShowOnline;
                }
            }

            CurrentContact = null;
            await OnContactChanged.InvokeAsync();
            ShowContactTypeSelection = false;
            StateHasChanged();
        }
        private void HideContactType()
        {
            CurrentContact = null;
            ShowContactTypeSelection = false;
            StateHasChanged();
        }
    }
}
