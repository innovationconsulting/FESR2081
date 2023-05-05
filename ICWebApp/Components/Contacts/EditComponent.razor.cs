using DocumentFormat.OpenXml.Office2010.Excel;
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
    public partial class EditComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        [Parameter] public CONT_Contact? Data { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        protected override void OnInitialized()
        {

            StateHasChanged();
            base.OnInitialized();
        }
        private async void Save()
        {
            if (Data != null)
            {
                await ContProvider.SetContact(Data);              
            }

            await OnSave.InvokeAsync();
        }

        private async void Cancel()
        {
            await OnCancel.InvokeAsync();
        }
    }
}
