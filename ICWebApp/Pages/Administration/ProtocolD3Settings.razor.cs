using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Telerik.Blazor;

namespace ICWebApp.Pages.Administration
{
    
    public partial class ProtocolD3Settings
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IUnitOfWork UnitOfWork { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private D3SettingEditModel _model = new D3SettingEditModel();
        private AUTH_Municipality? _municipality;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("D3_SETTINGS_PAGE_TITLE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Admin/ProtocolSettings", "BREADCRUMB_D3_SETTINGS", null, null, true);

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                _municipality = await UnitOfWork.Repository<AUTH_Municipality>()
                    .FirstOrDefaultAsync(e => e.ID == SessionWrapper.AUTH_Municipality_ID.Value);
                if(_municipality != null)
                    _model.Mail = _municipality.D3ProtocolEmail;
            }
            
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }

        private async Task Save()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            
            if (_municipality != null)
            {
                _municipality.D3ProtocolEmail = _model.Mail;
                await UnitOfWork.Repository<AUTH_Municipality>().InsertOrUpdateAsync(_municipality);
            }
            
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }

        private class D3SettingEditModel
        {
            [Required]
            [DataType(DataType.EmailAddress)]
            public string Mail { get; set; } = "";
        }
    }
}
