using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Telerik.Blazor;

namespace ICWebApp.Components.Formbuilder
{
    public partial class Container
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LanguageProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }

        [Parameter] public FORM_Definition Definition { get; set; }
        [Parameter] public Guid? CurrentLanguage { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        public bool HasChanges { get; set; } = false;
        private List<FORM_Definition_Field_Type> FieldTypes { get; set; }
        private FORM_Definition_Field_Type? DraggedFieldType { get; set; }
        private FORM_Definition_Field? DraggedField { get; set; }
        private bool IsBusy { get; set; } = true;
        private FORM_Definition_Field? EditModel { get; set; }

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            IsBusy = true;
            StateHasChanged();

            if (Definition.FORM_Definition_Field == null || Definition.FORM_Definition_Field.Count() == 0)
            {
                Definition.FORM_Definition_Field = await GetFields();

                if (Definition.FORM_Definition_Field == null)
                {
                    Definition.FORM_Definition_Field = new List<FORM_Definition_Field>();
                }
            }

            FieldTypes = FormDefinitionProvider.GetDefinitionFieldTypeList();
            var subTypes = FormDefinitionProvider.GetDefinitionFieldSubTypeList();

            foreach (var s in subTypes)
            {
                s.Name = TextProvider.Get(s.TEXT_SystemTEXT_Code);
            }

            FormBuilderHelper.SetDefinitionFieldType(FieldTypes);
            FormBuilderHelper.SetDefinitionFieldSubType(subTypes);

            FormBuilderHelper.FormElementType = TextProvider.Get("FORM_ELEMENT_TYPE");
            FormBuilderHelper.FormElementRequired = TextProvider.Get("FORM_ELEMENT_REQUIRED");
            FormBuilderHelper.LayoutElementColumn = TextProvider.Get("LAYOUT_ELEMENT_COLUMN");
            FormBuilderHelper.Edit = TextProvider.Get("LAYOUT_ELEMENT_EDIT");
            FormBuilderHelper.Delete = TextProvider.Get("LAYOUT_ELEMENT_REMOVE");
            FormBuilderHelper.FormElementSignatureDetail = TextProvider.Get("FORM_ELEMENT_SIGNATURE_DETAIL");
            FormBuilderHelper.CanBeRepeated = TextProvider.Get("FORM_ELEMENT_CAN_BE_REPEATED");

            FormBuilderHelper.Languages = await LanguageProvider.GetAll();

            await base.OnInitializedAsync();
        }
        protected override void OnParametersSet()
        {
            if (CurrentLanguage != null)
            {
                FormBuilderHelper.CurrentLanguage = CurrentLanguage.Value;
            }

            if(Definition != null)
            {
                FormBuilderHelper.FormDefinition = Definition;
            }

            base.OnParametersSet();
        }
        public async Task<List<FORM_Definition_Field>> GetFields()
        {
            return await FormDefinitionProvider.GetDefinitionFieldList(Definition.ID);
        }
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                BusyIndicatorService.IsBusy = false;
                IsBusy = false;
                StateHasChanged();
            }

            base.OnAfterRender(firstRender);
        }
        private async Task<bool> AddElement(FORM_Definition_Field_Type Element, Formbuilder_DragAndDropItem? DropItem = null)
        {
            IsBusy = true;
            StateHasChanged();

            var definitionField = new FORM_Definition_Field();

            definitionField.IsNew = true;
            definitionField.FORM_Definition_Field_Extended = new List<FORM_Definition_Field_Extended>();
            definitionField.ID = Guid.NewGuid();
            definitionField.FORM_Definition_ID = Definition.ID;

            definitionField.FORM_Definition_Fields_Type_ID = Element.ID;

            definitionField.DatabaseName = Element.Description + "-" + (Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Fields_Type_ID == definitionField.FORM_Definition_Fields_Type_ID).Count() + 1).ToString();

            if (DropItem != null && DropItem.ParentID != null)
            {
                definitionField.ColumnPos = DropItem.ColumnPos;
                definitionField.FORM_Definition_Field_Parent_ID = DropItem.ParentID;
            }
            else
            {
                definitionField.ColumnPos = null;
                definitionField.FORM_Definition_Field_Parent_ID = null;
            }

            if (definitionField != null)
            {
                if (FormBuilderHelper.Languages != null)
                {
                    foreach (var l in FormBuilderHelper.Languages)
                    {
                        if (definitionField.FORM_Definition_Field_Extended == null)
                        {
                            definitionField.FORM_Definition_Field_Extended = new List<FORM_Definition_Field_Extended>();
                        }

                        if (definitionField.FORM_Definition_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == l.ID) == null)
                        {
                            var ext = new FORM_Definition_Field_Extended()
                            {
                                ID = Guid.NewGuid(),
                                FORM_Definition_Field_ID = definitionField.ID,
                                LANG_Languages_ID = l.ID
                            };

                            definitionField.FORM_Definition_Field_Extended.Add(ext);
                        }
                    }
                }

                if (DropItem == null || DropItem.Count == null)
                {
                    definitionField.SortOrder = Definition.FORM_Definition_Field.Where(p => p.ColumnPos == null).Count() + 1;
                }
                else
                {
                    long? columnPos = null;
                    long? count = null;

                    if (DropItem != null)
                    {
                        columnPos = DropItem.ColumnPos;
                        count = DropItem.Count;
                    }

                    Definition.FORM_Definition_Field.Where(x => x.ColumnPos == columnPos && x.SortOrder >= count).ToList().ForEach(x => x.SortOrder++);
                    definitionField.SortOrder = count;
                }

                if(Element.IsContainer == true)
                {
                    definitionField.ColumnCount = 2;
                }

                definitionField.Placeholder = TextProvider.Get("FORM_ELEMENT_PLACEHOLDER");

                if (Element.ID == FORMElements.Textbox)
                {
                    definitionField.FORM_Definition_Fields_SubType_ID = Guid.Parse("4b212e9c-b804-4ed4-9550-c31aad019b4d"); //Subtype Text
                }

                Definition.FORM_Definition_Field.Add(definitionField);

                if (Element.IsEditable == true || Element.IsContainer == true)
                {
                    EditModel = definitionField;
                }
            }

            HasChanges = true;
            IsBusy = false;
            StateHasChanged();

            return true;
        }
        private async Task<bool> DropHandler(Formbuilder_DragAndDropItem DropItem)
        {
            IsBusy = true;
            StateHasChanged();

            if (DraggedFieldType != null)
            {
                await AddElement(DraggedFieldType, DropItem);
            }

            if (DraggedField != null)
            {
                Definition.FORM_Definition_Field.Where(x => x.ColumnPos == DropItem.ColumnPos && x.SortOrder >= DropItem.Count && x.FORM_Definition_Field_Parent_ID == DropItem.ParentID).ToList().ForEach(x => x.SortOrder++);

                DraggedField.SortOrder = DropItem.Count;
                DraggedField.ColumnPos = DropItem.ColumnPos;
                DraggedField.FORM_Definition_Field_Parent_ID = DropItem.ParentID;
            }

            await JSRuntime.InvokeVoidAsync("formBuilder_clearDraggableClass");
            await JSRuntime.InvokeVoidAsync("formBuilder_clearDraggableContainerClass");


            DraggedFieldType = null;
            DraggedField = null;

            if (DraggedFieldType == null)
            {
                ReorderList(DropItem.ParentID);
            }

            HasChanges = true;
            IsBusy = false;
            StateHasChanged();

            return true;
        }
        private async void DragStartHandler(FORM_Definition_Field_Type t)
        {
            await JSRuntime.InvokeVoidAsync("SetElementContext", "builder");
            DraggedFieldType = t;
            StateHasChanged();
        }
        private async Task<bool> DragStopHandler()
        {
            await JSRuntime.InvokeVoidAsync("formBuilder_clearDraggableClass");
            DraggedFieldType = null;
            StateHasChanged();

            return true;
        }
        private async void DragFieldStartHandler(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await JSRuntime.InvokeVoidAsync("SetElementContext", "builder");
                DraggedField = f;
            }
        }
        private async Task<bool> DragFieldEndHandler(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await JSRuntime.InvokeVoidAsync("formBuilder_clearDraggableClass");
                DraggedField = null;
            }
            return true;
        }
        private bool MoveUp(FORM_Definition_Field f)
        {
            StateHasChanged();
            IsBusy = true;

            var previousItem = Definition.FORM_Definition_Field.FirstOrDefault(p => p.FORM_Definition_Field_Parent_ID == f.FORM_Definition_Field_Parent_ID && p.ColumnPos == f.ColumnPos && p.SortOrder == f.SortOrder - 1);

            if (previousItem != null)
            {
                previousItem.SortOrder = previousItem.SortOrder + 1;
                f.SortOrder = f.SortOrder - 1;

            }

            ReorderList(f.FORM_Definition_Field_Parent_ID);

            f.NotifyOnChanged();

            HasChanges = true;
            IsBusy = false;
            StateHasChanged();
            return true;
        }
        private bool MoveDown(FORM_Definition_Field f)
        {
            IsBusy = true;
            StateHasChanged();

            var previousItem = Definition.FORM_Definition_Field.FirstOrDefault(p => p.FORM_Definition_Field_Parent_ID == f.FORM_Definition_Field_Parent_ID && p.ColumnPos == f.ColumnPos && p.SortOrder == f.SortOrder + 1);
            if (previousItem != null)
            {
                previousItem.SortOrder = previousItem.SortOrder - 1;
                f.SortOrder = f.SortOrder + 1;

            }

            ReorderList(f.FORM_Definition_Field_Parent_ID);

            f.NotifyOnChanged();

            HasChanges = true;
            IsBusy = false;
            StateHasChanged();
            return true;
        }
        private async Task<bool> OnRemoveField(FORM_Definition_Field f)
        {
            if (f != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("FORM_ELEMENT_DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return false;


                IsBusy = true;
                StateHasChanged();

                RemoveFields(f);
                f.ToRemove = true;

                ReorderList(null);

                HasChanges = true;
                IsBusy = false;
                StateHasChanged();
            }
            return true;
        }
        private async Task<bool> OnRemoveFieldWithSubFields(FORM_Definition_Field f)
        {
            if (f != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("FORM_ELEMENT_DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return false;

                IsBusy = true;
                StateHasChanged();

                RemoveFields(f);
                f.ToRemove = true;

                ReorderList(null);

                HasChanges = true;
                IsBusy = false;
                StateHasChanged();
            }
            return true;
        }
        private bool ReorderList(Guid? FORM_Definition_Field_Parent_ID)
        {

            foreach (var col in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Parent_ID == FORM_Definition_Field_Parent_ID).OrderBy(p => p.SortOrder).Select(p => p.ColumnPos).Distinct().ToList())
            {
                int Sort = 0;

                foreach (var d in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Parent_ID == FORM_Definition_Field_Parent_ID && p.ColumnPos == col).ToList().OrderBy(p => p.SortOrder))
                {
                    d.SortOrder = Sort;
                    Sort++;                    
                }
            }

            return true;
        }
        public async Task<bool> SaveForm()
        {
            IsBusy = true;
            StateHasChanged();

            await Task.Delay(1);

            foreach(var referenceToReset in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_ReferenceFORM_Definition_Field != null && 
                                                                                        p.FORM_Definition_Field_ReferenceFORM_Definition_Field.FirstOrDefault(p => p.FORM_Definition_Field_Source != null 
                                                                                        && p.FORM_Definition_Field_Source.ToRemove == true) != null).ToList()
                   )
            {
                var valuesToReset = referenceToReset.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.FORM_Definition_Field_Source != null && p.FORM_Definition_Field_Source.ToRemove == true);

                foreach(var reset in valuesToReset)
                {
                    reset.FORM_Definition_Field_Source_ID = null;
                    reset.FORM_Definition_Field_Source = null;
                }
            }

            foreach (var subf in Definition.FORM_Definition_Field.Where(p => p.ToRemove != true).ToList())
            {
                await FormDefinitionProvider.SetDefinitionField(subf);
            }

            foreach (var subf in Definition.FORM_Definition_Field.Where(p => p.ToRemove != true).ToList())
            {
                foreach (var ext in subf.FORM_Definition_Field_Extended)
                {
                    await FormDefinitionProvider.SetDefinitionFieldExtended(ext);
                }

                foreach (var option in subf.FORM_Definition_Field_Option.Where(p => p.ToRemove != true).ToList())
                {
                    await FormDefinitionProvider.SetDefinitionFieldOption(option);

                    foreach (var ext in option.FORM_Definition_Field_Option_Extended)
                    {
                        await FormDefinitionProvider.SetDefinitionFieldOptionExtended(ext);
                    }
                }
                
                foreach (var reference in subf.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove != true).ToList())
                {
                    await FormDefinitionProvider.SetDefinitionFieldReference(reference);
                }
            }

            foreach (var subf in Definition.FORM_Definition_Field.Where(p => p.ToRemove == true).ToList())
            {
                foreach (var reference in subf.FORM_Definition_Field_ReferenceFORM_Definition_Field_Source.ToList())
                {
                    await FormDefinitionProvider.RemoveDefinitionFieldReference(reference.ID);
                }

                foreach (var reference in subf.FORM_Definition_Field_ReferenceFORM_Definition_Field.ToList())
                {
                    await FormDefinitionProvider.RemoveDefinitionFieldReference(reference.ID);
                }

                foreach (var option in subf.FORM_Definition_Field_Option.ToList())
                {
                    await FormDefinitionProvider.RemoveDefinitionFieldOption(option.ID);
                }
            }

            foreach (var subf in Definition.FORM_Definition_Field.Where(p => p.ToRemove == true).ToList())
            {
                await FormDefinitionProvider.RemoveDefinitionField(subf.ID);
            }

            foreach (var reference in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Option.FirstOrDefault(p => p.ToRemove == true) != null).ToList())
            {
                foreach (var option in reference.FORM_Definition_Field_Option.Where(p => p.ToRemove == true).ToList())
                {
                    await FormDefinitionProvider.RemoveDefinitionFieldOption(option.ID);
                }
            }

            foreach (var reference in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_ReferenceFORM_Definition_Field.FirstOrDefault(p => p.ToRemove == true) != null).ToList())
            {
                foreach (var option in reference.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == true).ToList())
                {
                    await FormDefinitionProvider.RemoveDefinitionFieldOption(option.ID);
                }
            }

            Definition.FORM_Definition_Field = await GetFields();

            HasChanges = false;
            IsBusy = false;
            StateHasChanged();
            return true;
        }
        private void RemoveFields(FORM_Definition_Field ParentField)
        {
            foreach (var subF in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Parent_ID == ParentField.ID).ToList())
            {
                if(Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Parent_ID == subF.ID).Count() > 0)
                {
                    RemoveFields(subF);
                }

                subF.ToRemove = true;
            }
        }
        private void OnEdit(FORM_Definition_Field Model)
        {
            EditModel = Model;
            StateHasChanged();
        }
        private void OnEditClose()
        {
            HasChanges = true;
            EditModel = null;
            StateHasChanged();
        }
        private void Refresh()
        {
            StateHasChanged();
        }
    }
}
