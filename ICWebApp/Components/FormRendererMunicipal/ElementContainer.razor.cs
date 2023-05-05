using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.Formrenderer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Telerik.Blazor;

namespace ICWebApp.Components.FormRendererMunicipal
{
    public partial class ElementContainer
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] public ILANGProvider LANGProvider { get; set; }
        [Inject] IFormRendererHelper FormRendererHelper { get; set; }
        [Inject] IFORMApplicationProvider FORMApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Parameter] public FORM_Application_Field_Data? ApplicationField { get; set; }
        [Parameter] public FORM_Definition_Field? DefinitionField { get; set; }
        [Parameter] public List<FORM_Definition_Field_Reference>? ReferenceList { get; set; }
        [Parameter] public FORM_Definition Definition { get; set; }
        [Parameter] public long? RepetitionItem { get; set; }

        public Guid? CurrentLanguage { get; set; }
        private bool ReferenceCheck { get; set; } = true;
        private bool HasReference = false;

        protected override void OnInitialized()
        {
            CurrentLanguage = LANGProvider.GetCurrentLanguageID();
            
            InitializeReferences();

            if(HasReference)
            {
                CheckReference();
            }

            StateHasChanged();
            base.OnInitialized();
        }

        private bool InitializeReferences()
        {
            if (ApplicationField != null && ReferenceList != null)
            {
                var reference = ReferenceList.FirstOrDefault(p => p.FORM_Definition_Field_ID == ApplicationField.FORM_Definition_Field_ID);

                if (reference != null)
                {
                    FormRendererHelper.OnChange += FormRendererHelper_OnChange;
                    HasReference = true;
                }
            }

            return true;
        }
        private void FormRendererHelper_OnChange()
        {
            CheckReference();
            StateHasChanged();
        }
        private bool HasMunicipalViewDisplayedElements()
        {
            if (DefinitionField == null)
                return false;

            return IsMunicipalDisplayedField(DefinitionField);
        }
        private bool IsMunicipalDisplayedField(FORM_Definition_Field field)
        {
            var subFields = FormRendererHelper.GetDefinitionFields().Where(p => p.FORM_Definition_Field_Parent_ID == field.ID).ToList();
            bool displayed = false;
            foreach(var subField in subFields)
            {
                if (subField.ShowOnMunicipalSite && subField.OnlyShowInFormRenderer != true)
                {
                    return true;
                }
                displayed = displayed || IsMunicipalDisplayedField(subField);
            }
            return displayed;
        }
        private void CheckReference()
        {
            ReferenceCheck = true;

            if (DefinitionField != null && FormRendererHelper.GetApplicationFields() != null && ReferenceList != null)
            {
                foreach(var f in ReferenceList.Where(p => p.FORM_Definition_Field_ID == DefinitionField.ID))
                {
                    var applicationRefField = FormRendererHelper.GetApplicationFields().FirstOrDefault(p => p.FORM_Definition_Field_ID == f.FORM_Definition_Field_Source_ID);

                    if(applicationRefField != null)
                    {
                        bool ValueOK = false;

                        var sourcefield = FormRendererHelper.GetDefinitionFields().FirstOrDefault(p => p.ID == f.FORM_Definition_Field_Source_ID);

                        if (sourcefield != null && sourcefield.FORM_Definition_Fields_Type_ID == FORMElements.Checkbox) //CHECKBOX
                        {
                            if (f.Negate != true)
                            {
                                if (f.TriggerValueBool != applicationRefField.BoolValue)
                                {
                                    ValueOK = true;
                                }
                            }
                            else
                            {
                                if (f.TriggerValueBool == applicationRefField.BoolValue)
                                {
                                    ValueOK = true;
                                }
                            }
                        }
                        else if (sourcefield != null && sourcefield.FORM_Definition_Fields_Type_ID == FORMElements.Radiobutton)   //RADIOBUTTON
                        {
                            if (f.Negate != true)
                            {
                                if (f.TriggerValue != applicationRefField.Value)
                                {
                                    ValueOK = true;
                                }
                            }
                            else
                            {
                                if (f.TriggerValue == applicationRefField.Value)
                                {
                                    ValueOK = true;
                                }
                            }
                        }
                        else if (sourcefield != null && sourcefield.FORM_Definition_Fields_Type_ID == FORMElements.Dropdown)   //DROPDOWN
                        {
                            if (f.Negate != true)
                            {
                                if (f.TriggerValue != applicationRefField.Value)
                                {
                                    ValueOK = true;
                                }
                            }
                            else
                            {
                                if (f.TriggerValue == applicationRefField.Value)
                                {
                                    ValueOK = true;
                                }
                            }
                        }

                        if (ValueOK)
                        {
                            ReferenceCheck = false;
                        }

                        DefinitionField.ReferenceOk = ReferenceCheck;
                    }
                }
            }
            StateHasChanged();
        }
    }
}
