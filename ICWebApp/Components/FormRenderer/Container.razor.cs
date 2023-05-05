using DocumentFormat.OpenXml;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Formrenderer;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace ICWebApp.Components.FormRenderer
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
        public async Task<bool> Save()
        {
            foreach (var f in Fields)
            {
                if(f.FileValue != null && f.FileValue.Count() > 0)
                {
                    f.Value = null;
                    int count = 1;

                    foreach(var fileInfo in f.FileValue.ToList()) 
                    {
                        if (fileInfo != null)
                        {
                            var definitionField = DefinitionFields.FirstOrDefault(x => x.ID == f.FORM_Definition_Field_ID);

                            if (definitionField != null &&
                                definitionField.FORM_Definition_Field_Extended != null &&
                                definitionField.FORM_Definition_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage) != null &&
                                !string.IsNullOrEmpty(definitionField.FORM_Definition_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Name))
                            {
                                fileInfo.FileName = definitionField.FORM_Definition_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Name;

                                if(f.FileValue.Count() > 1)
                                {
                                    fileInfo.FileName += "_" + count;
                                    count++;
                                }
                            }

                            var fi = await FileProvider.SetFileInfo(fileInfo);

                            if (fi != null)
                            {
                                if (f.Value == null)
                                    f.Value = "";

                                f.Value += fi.ID.ToString() + ";";
                            }
                        }
                    }
                }

                await FormApplicationProvider.SetApplicationFieldData(f);

                await FormApplicationProvider.RemoveApplicationFieldSubData(f.ID);
                if (f.FORM_Application_Field_SubData != null && f.FORM_Application_Field_SubData.Count() > 0)
                {
                    foreach (var s in f.FORM_Application_Field_SubData)
                    {
                        await FormApplicationProvider.SetApplicationFieldSubData(s);
                    }
                }
            }

            return true;
        }
        public async Task<bool> Validate()
        {
            bool valid = true;

            foreach(var f in Fields)
            {
                f.ERROR_CODE = null;

                var definitionField = DefinitionFields.FirstOrDefault(x => x.ID == f.FORM_Definition_Field_ID);
                bool ReferenceCheck = true;

                //CHECK IF ELEMENT IS VISIBLE (CONTAINER ELEMENT)
                if (definitionField != null && definitionField.FORM_Definition_Field_Parent_ID != null && FormRendererHelper.GetApplicationFields() != null && DefinitionReferences != null)
                {
                    var parentDefinition = DefinitionFields.FirstOrDefault(x => x.ID == definitionField.FORM_Definition_Field_Parent_ID);

                    if (parentDefinition != null)
                    {
                        ReferenceCheck = CheckContainerReference(parentDefinition);
                    }
                }
                
                //CHECK DIFFERENCE FIELD
                if(definitionField != null && definitionField.FORM_Definition_Fields_Type_ID == Guid.Parse("bd95fb95-767e-4a7c-a6f7-bdbfdf51f1f7") && definitionField.DecimalReferenceValueLimit != null)
                {
                    var CurrentValue = f.DecimalValue;
                    var LimitValue = definitionField.DecimalReferenceValueLimit;

                    if(CurrentValue > LimitValue)
                    {
                        f.ERROR_CODE = "FORM_ERROR_CODE_DIFFERENCE";
                        valid = false;
                    }
                }

                //CHECK ALL ELEMENTS
                if (ReferenceCheck) 
                {
                    if (definitionField != null)
                    {
                        //REQUIRED CHECK
                        if (definitionField.Required)
                        {
                            if (string.IsNullOrEmpty(f.Value) && definitionField.FORM_Definition_Fields_Type_ID != FORMElements.FileUpload)
                            {
                                f.ERROR_CODE = "FORM_ERROR_CODE_REQUIRED";
                                valid = false;
                            }

                            if(definitionField.FORM_Definition_Fields_Type_ID == FORMElements.FileUpload && (f.FileValue == null || f.FileValue.Count() == 0))
                            {
                                f.ERROR_CODE = "FORM_ERROR_CODE_REQUIRED";
                                valid = false;
                            }

                            FormRendererHelper.SetApplicationField(f);
                        }

                        //SUBTYPE CHECK (Regex)
                        if (definitionField.FORM_Definition_Fields_SubType_ID != null)
                        {
                            var SubType = FormDefinitionProvider.GetDefinitionFieldSubType(definitionField.FORM_Definition_Fields_SubType_ID.Value);

                            if(SubType != null && SubType.Regex != null && !string.IsNullOrEmpty(f.Value))
                            {
                                Regex rgx = new Regex(SubType.Regex);

                                if (!rgx.IsMatch(f.Value))
                                {
                                    f.ERROR_CODE = "FORM_ERROR_CODE_WRONG_INPUT";
                                    valid = false;
                                }
                            }
                        }
                    }
                }
            }

            StateHasChanged();

            return valid;
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
        private async Task AddContainer(RepetitionField DefinitionField)
        {
            if (DefinitionField.DefinitionField != null && DefinitionField.ApplicationFieldID != null)
            {
                var NewItems = await FormApplicationProvider.GetAdditionalContainerFieldData(DefinitionField.ApplicationFieldID.Value, DefinitionField.DefinitionField, Application, DefinitionField.RepetitionCount);

                if (Fields != null)
                {
                    Fields.AddRange(NewItems);
                    FormRendererHelper.SetApplicationFields(Fields);
                }
            }

            StateHasChanged();

            return;
        }
        private async Task<bool> RemoveContainer(RepetitionField DefinitionField)
        {
            if (DefinitionField.DefinitionField != null && DefinitionField.ApplicationFieldID != null)
            {
                var MainFieldToDelete = Fields.Where(p => p.RepetitionParentID == DefinitionField.ApplicationFieldID && p.FORM_Definition_Field_ID == DefinitionField.DefinitionField.ID && p.RepetitionCount == DefinitionField.RepetitionCount).FirstOrDefault();

                if (MainFieldToDelete != null)
                {
                    await RemoveSubFields(MainFieldToDelete.ID);

                    await FormApplicationProvider.RemoveApplicationFieldData(MainFieldToDelete.ID);
                    Fields.Remove(MainFieldToDelete);

                    FormRendererHelper.SetApplicationFields(Fields);
                }
            }

            StateHasChanged();
            return true;
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
