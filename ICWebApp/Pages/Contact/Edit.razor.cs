using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Pages.Contact
{
    public partial class Edit
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }

        CONT_Contact? Data = null;

        protected override async Task OnInitializedAsync()
        {
            if(ID == "New")
            {
                SessionWrapper.PageTitle = TextProvider.Get("CONTACT_TITLE_ADD");

                Data = new CONT_Contact();

                Data.ID = Guid.NewGuid();

                if (SessionWrapper.AUTH_Municipality_ID != null)
                {
                    Data.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                }

            }
            else
            {
                SessionWrapper.PageTitle = TextProvider.Get("CONTACT_TITLE_EDIT");

                Data = await ContProvider.GetContact(Guid.Parse(ID));
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private void Save()
        {
            ReturnToPrevious();
        }

        private void ReturnToPrevious()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Contacts/List");
            StateHasChanged();
        }
    }
}
