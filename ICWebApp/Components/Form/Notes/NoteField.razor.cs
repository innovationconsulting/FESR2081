using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Form.Notes
{
    public partial class NoteField
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Parameter] public FORM_Application_Municipal_Field_Data Data {get;set;}
        [Parameter] public V_FORM_Definition_Municipal_Field Definition { get; set; }
        private bool InEdit { get; set; } = false;

        protected override void OnInitialized()
        {
            InEdit = false;
            StateHasChanged();

            base.OnInitialized();
        }
        private void Edit()
        {
            InEdit = true;
            StateHasChanged();
        }
        private void Cancel()
        {
            InEdit = false;
            StateHasChanged();
        }
        private async void Save()
        {
            await FormApplicationProvider.SetFormApplicationMunicipalField(Data);

            InEdit = false;
            StateHasChanged();
        }
    }
}
