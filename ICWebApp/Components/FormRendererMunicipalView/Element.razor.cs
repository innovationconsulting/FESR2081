using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.FormRendererMunicipalView
{
    public partial class Element
    {
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IFormRendererHelper FormRendererHelper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public FORM_Definition_Field? DefinitionField { get; set; }
        [Parameter] public FORM_Application_Field_Data? ApplicationField { get; set; }
        private Guid CurrentLanguage { get; set; }
        private List<FORM_Definition_Field_Option> FieldOptions = new List<FORM_Definition_Field_Option>();
       
        protected override void OnInitialized()
        {
            CurrentLanguage = LANGProvider.GetCurrentLanguageID();
            GetOptions();
            StateHasChanged();
            base.OnInitialized();
        }       
        private async void GetOptions()
        {
            if (DefinitionField != null && 
                (DefinitionField.FORM_Definition_Fields_Type_ID == FORMElements.Dropdown
                 || DefinitionField.FORM_Definition_Fields_Type_ID == FORMElements.Radiobutton)
               )
            {
                FieldOptions = await FormDefinitionProvider.GetDefinitionFieldOptionList(DefinitionField.ID);

                StateHasChanged();
            }
        }       
    }
}
