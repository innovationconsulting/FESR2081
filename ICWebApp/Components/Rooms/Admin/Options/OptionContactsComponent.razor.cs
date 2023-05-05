using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Admin.Options
{
    public partial class OptionContactsComponent
    {
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public List<ROOM_RoomOptions_Contact>? Contacts { get; set; }
        [Parameter] public Guid OptionID { get; set; }

        private bool ShowContactSelection = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();
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
                ROOM_RoomOptions_Contact NewItem = new ROOM_RoomOptions_Contact();

                NewItem.ID = Guid.NewGuid();
                NewItem.CONT_Contact = Item;
                NewItem.CONT_Contact_ID = Item.ID;
                NewItem.ROOM_RoomOptions_ID = OptionID;

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

        private void RemoveContact(ROOM_RoomOptions_Contact Item)
        {
            if (Contacts != null)
            {
                Contacts.Remove(Item);
                StateHasChanged();
            }
        }
        private void ToggleEmail(ROOM_RoomOptions_Contact Item)
        {
            Item.SendEmail = !Item.SendEmail;
            StateHasChanged();
        }
        private void ToggleSMS(ROOM_RoomOptions_Contact Item)
        {
            Item.SendSMS = !Item.SendSMS;
            StateHasChanged();
        }
    }
}
