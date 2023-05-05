using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Contacts
{
    public partial class SelectionComponent
    {
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }

        [Parameter] public List<Guid>? ExistingContacts { get; set; }
        [Parameter] public EventCallback<CONT_Contact> ContactSelected { get; set; }

        private List<V_CONT_Contact> ContactsToAdd = new List<V_CONT_Contact>();
        private Guid? SelectedContact = null;
        private bool OnShowQuickAdd = false;
        private CONT_Contact? NewContact = null;

        protected override async Task OnParametersSetAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var AllContacts = await ContProvider.GetContacts(SessionWrapper.AUTH_Municipality_ID.Value);

                ContactsToAdd = AllContacts.Where(p => ExistingContacts == null || !ExistingContacts.Contains(p.ID)).ToList();

                SelectedContact = null;

                StateHasChanged();
            }

            await base.OnParametersSetAsync();
        }

        private async void OnSelect()
        {
            if (SelectedContact != null) 
            {
                var cont = await ContProvider.GetContact(SelectedContact.Value);

                if(cont != null)
                {
                    await ContactSelected.InvokeAsync(cont);
                }
            }
        }
        private async void QuickEdit(Guid ID)
        {
            NewContact = await ContProvider.GetContact(ID);

            OnShowQuickAdd = true;
            StateHasChanged();
        }
        private void OnQuickAdd()
        {
            NewContact = new CONT_Contact();

            NewContact.ID = Guid.NewGuid();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                NewContact.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
            }

            OnShowQuickAdd = true;
            StateHasChanged();
        }
        private async void OnSaveQuickAdd()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var AllContacts = await ContProvider.GetContacts(SessionWrapper.AUTH_Municipality_ID.Value);

                ContactsToAdd = AllContacts.Where(p => ExistingContacts == null || !ExistingContacts.Contains(p.ID)).ToList();

                SelectedContact = null;

                OnShowQuickAdd = false;
                NewContact = null;
                StateHasChanged();
            }
        }
        private void HideQuickAdd()
        {
            OnShowQuickAdd = false;
            NewContact = null;
            StateHasChanged();
        }
    }
}
