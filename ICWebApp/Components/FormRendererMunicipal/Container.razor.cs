using DocumentFormat.OpenXml;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Formrenderer;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace ICWebApp.Components.FormRendererMunicipal
{
    public partial class Container
    {
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFormRendererHelper FormRendererHelper { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }

        [Parameter] public FORM_Application Application { get; set; }
        [Parameter] public FORM_Definition Definition { get; set; }
        [Parameter] public EventCallback OnContainerInitialized { get; set; }
        private List<FORM_Application_Field_Data> Fields { get; set; }
        private List<FORM_Definition_Field> DefinitionFields { get; set; }
        private List<FORM_Definition_Field_Reference> DefinitionReferences { get; set; }
        private Guid CurrentLanguage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            CurrentLanguage = LANGProvider.GetCurrentLanguageID();

            await GetData();

            if (Fields != null)
            {
                FormRendererHelper.SetApplicationFields(Fields);
                FormRendererHelper.SetDefinitionFields(DefinitionFields);
            }

            await OnContainerInitialized.InvokeAsync();
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        protected override void OnParametersSet()
        {
            StateHasChanged();

            base.OnParametersSet();
        }
        private async Task<bool> GetData()
        {
            if (Application != null && Application.FORM_Definition_ID != null)
            {
                DefinitionFields = await FormDefinitionProvider.GetDefinitionFieldList(Application.FORM_Definition_ID.Value);
                Fields = await FormApplicationProvider.GetOrCreateApplicationFieldData(Application);

                foreach(var f in Fields)
                {
                    var def = DefinitionFields.FirstOrDefault(p => p.ID == f.FORM_Definition_Field_ID);

                    if(def != null && def.FORM_Definition_Fields_Type_ID == FORMElements.FileUpload)
                    {
                        if (!string.IsNullOrEmpty(f.Value))
                        {
                            var ids = f.Value.Split(";");

                            foreach(var id in ids)
                            {
                                try
                                {
                                    var fi = await FileProvider.GetFileInfoAsync(Guid.Parse(id));

                                    if (fi != null && f.FileValue != null)
                                        f.FileValue.Add(fi);
                                }
                                catch { }
                            }
                        }
                    }
                }

                DefinitionReferences = await FormDefinitionProvider.GetDefinitionFieldReferenceListByDefinition(Application.FORM_Definition_ID.Value);
            }
            return true;
        }
        private bool CheckContainerReference(FORM_Definition_Field Field)
        {
            bool ReferenceCheck = true;

            foreach (var r in DefinitionReferences.Where(p => p.FORM_Definition_Field_ID == Field.ID))
            {
                var applicationRefField = FormRendererHelper.GetApplicationFields().FirstOrDefault(p => p.FORM_Definition_Field_ID == r.FORM_Definition_Field_Source_ID);

                if (applicationRefField != null)
                {
                    bool ValueOK = false;

                    var sourcefield = DefinitionFields.FirstOrDefault(p => p.ID == r.FORM_Definition_Field_Source_ID);

                    if (sourcefield != null && sourcefield.FORM_Definition_Fields_Type_ID == Guid.Parse("4f99845e-10a5-4e81-99f5-b79e715df647")) //CHECKBOX
                    {
                        if (r.TriggerValueBool != applicationRefField.BoolValue)
                        {
                            ValueOK = true;
                        }
                    }
                    else if (sourcefield != null && sourcefield.FORM_Definition_Fields_Type_ID == Guid.Parse("c28f27ff-3131-42fb-b328-bf487e8ba3e9"))   //RADIOBUTTON
                    {
                        if (r.TriggerValue != applicationRefField.Value)
                        {
                            ValueOK = true;
                        }
                    }
                    else if (sourcefield != null && sourcefield.FORM_Definition_Fields_Type_ID == FORMElements.Dropdown)   //DROPDOWN
                    {
                        if (r.TriggerValue != applicationRefField.Value)
                        {
                            ValueOK = true;
                        }
                    }

                    if (ValueOK)
                    {
                        ReferenceCheck = false;
                    }
                }
            }

            if(Field.FORM_Definition_Field_Parent_ID != null)
            {
                var parentDefinition = DefinitionFields.FirstOrDefault(x => x.ID == Field.FORM_Definition_Field_Parent_ID);

                if (parentDefinition != null)
                {
                    ReferenceCheck = CheckContainerReference(parentDefinition);
                }
            }

            return ReferenceCheck;
        }
       
        private async Task<bool> RemoveSubFields(Guid MainFieldToDelete)
        {
            var SubFieldsToDelete = Fields.Where(p => p.RepetitionParentID == MainFieldToDelete).ToList();

            foreach (var subField in SubFieldsToDelete)
            {
                await RemoveSubFields(subField.ID);

                await FormApplicationProvider.RemoveApplicationFieldData(subField.ID);
                Fields.Remove(subField);
            }

            return true;
        }
    }
}
