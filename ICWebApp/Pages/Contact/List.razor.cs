using DocumentFormat.OpenXml.Office2016.Excel;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor;

namespace ICWebApp.Pages.Contact
{
    public partial class List
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        private List<V_CONT_Contact> Data = new List<V_CONT_Contact>();
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Data = await ContProvider.GetContacts(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_CONTACTS_LIST");

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void Add()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Contacts/Edit/New");
            StateHasChanged();
        }
        private void Edit(V_CONT_Contact Item)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Contacts/Edit/" + Item.ID.ToString());
            StateHasChanged();
        }
        private async void Remove(V_CONT_Contact Item)
        {
            if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE_CONTACT"),
                    TextProvider.Get("WARNING")))
                return;

            await ContProvider.RemoveContact(Item.ID);

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Data = await ContProvider.GetContacts(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            StateHasChanged();

        }
    }
}
