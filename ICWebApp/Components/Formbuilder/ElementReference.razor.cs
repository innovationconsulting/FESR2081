using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Formbuilder
{
    public partial class ElementReference
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }
        [Parameter] public FORM_Definition_Field Model { get; set; }

        private List<FORM_Definition_Field> PossibleReferenceList = new List<FORM_Definition_Field>();

        protected override async Task OnInitializedAsync()
        {
            await LoadReferenceList();

            FormBuilderHelper.OnCurrentLanguageChanged += FormBuilderHelper_OnCurrentLanguageChanged; ;

            if (Model.FORM_Definition_Field_ReferenceFORM_Definition_Field == null)
                Model.FORM_Definition_Field_ReferenceFORM_Definition_Field = new List<FORM_Definition_Field_Reference>();

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private void FormBuilderHelper_OnCurrentLanguageChanged()
        {
            StateHasChanged();
        }
        private async Task LoadReferenceList(Guid? Fref_ID = null, bool newItem = false)
        {
            foreach(var r in Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false))
            {
                if (r.FORM_Definition_Field_Source_ID != null)
                {
                    var def = FormBuilderHelper.FormDefinition.FORM_Definition_Field.FirstOrDefault(p => p.ID == r.FORM_Definition_Field_Source_ID.Value);
                    
                    if (def != null)
                    {
                        r.FORM_Definition_Field_Source = def;
                    }

                    if (def != null && def.FORM_Definition_Fields_Type_ID == FORMElements.Radiobutton)
                    {
                        r.OptionList = def.FORM_Definition_Field_Option.ToList();

                        foreach(var opt in r.OptionList)
                        {
                            if (opt.FORM_Definition_Field_Option_Extended != null && 
                                opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage) != null)
                            {
                                opt.Description = opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage).Description;
                            }
                        }
                    }
                    else if (def != null && def.FORM_Definition_Fields_Type_ID == FORMElements.Dropdown)
                    {
                        r.OptionList = def.FORM_Definition_Field_Option.ToList();

                        foreach (var opt in r.OptionList)
                        {
                            if (opt.FORM_Definition_Field_Option_Extended != null &&
                                opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage) != null)
                            {
                                opt.Description = opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage).Description;
                            }
                        }
                    }
                }
            }

            if (Model.FORM_Definition_ID != null)
            {
                var formFields = FormBuilderHelper.FormDefinition.FORM_Definition_Field.ToList();

                if (formFields != null)
                {
                    PossibleReferenceList = new List<FORM_Definition_Field>();

                    foreach(var f in formFields)
                    {
                        if (f.FORM_Definition_Fields_Type_ID != null)
                        {
                            var type = FormBuilderHelper.GetDefintionFieldType(f.FORM_Definition_Fields_Type_ID.Value);

                            if (type != null && type.CanBeReferenced == true && Model.FORM_Definition_Fields_Type_ID == FORMElements.ColumnContainer && type.ID == FORMElements.Checkbox)     
                            {
                                PossibleReferenceList.Add(f);
                            }
                            else if (type != null && type.CanBeReferenced == true && Model.FORM_Definition_Fields_Type_ID == FORMElements.ColumnContainer && type.ID == FORMElements.Radiobutton)        //RADIO
                            {
                                PossibleReferenceList.Add(f);
                            }
                            else if (type != null && type.CanBeReferenced == true && Model.FORM_Definition_Fields_Type_ID == FORMElements.ColumnContainer && type.ID == FORMElements.Dropdown)        //DROPDOWN
                            {
                                PossibleReferenceList.Add(f);
                            }
                            else if (type != null && type.CanBeReferenced == true && Model.FORM_Definition_Fields_Type_ID == FORMElements.Difference && type.ID == FORMElements.List) // LIST
                            {
                                PossibleReferenceList.Add(f);
                            }
                            else if(type != null && Model.FORM_Definition_Fields_Type_ID == FORMElements.Signature && type.ID == FORMElements.Textbox)
                            {
                                PossibleReferenceList.Add(f);
                            }
                        }
                    }

                    foreach (var f in PossibleReferenceList)
                    {
                        if (f.FORM_Definition_Field_Extended != null && f.FORM_Definition_Field_Extended.Count() > 0
                            && f.FORM_Definition_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage) != null)
                        {
                            var text = f.FORM_Definition_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage).Name;

                            if(text != null && text.Length > 30)
                            {
                                text = text.Substring(0, 30);
                            }

                            if (text != null)
                            {
                                f.Name = f.DatabaseName + " - " + text;
                            }
                        }

                        if (string.IsNullOrEmpty(f.Name))
                        {
                            f.Name = f.DatabaseName;
                        }
                    }
                }

                if (Fref_ID != null && newItem)
                {
                    var fref = Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false).FirstOrDefault(p => p.ID == Fref_ID);

                    if (fref != null)
                    {
                        fref.InEdit = true;
                        StateHasChanged();
                    }
                }
            }

            StateHasChanged();

            return;
        }
        private async Task<bool> AddReference()
        {
            FORM_Definition_Field_Reference fref = new FORM_Definition_Field_Reference();

            fref.ID = Guid.NewGuid();
            fref.FORM_Definition_Field_ID = Model.ID;
            
            long NextNumber = 0;

            if(Model.FORM_Definition_Field_ReferenceFORM_Definition_Field != null && Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false).Count() > 0 && Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false).Max(p => p.SortOrder) != null)
            {
                NextNumber = Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false).Max(p => p.SortOrder).Value + 1;
            }

            fref.SortOrder = NextNumber;

            Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Add(fref);

            await LoadReferenceList(fref.ID, true);
            StateHasChanged();

            return true;
        }
        private async Task<bool> RemoveReference(FORM_Definition_Field_Reference fref)
        {
            fref.ToRemove = true;
            await LoadReferenceList();
            StateHasChanged();

            return true;
        }
        private async void Save(FORM_Definition_Field_Reference fref)
        {
            await LoadReferenceList();
            fref.InEdit = false;

            StateHasChanged();
        }
        private async void Edit(FORM_Definition_Field_Reference fref)
        {
            fref.InEdit = true;
            StateHasChanged();
        }
        private async void MoveUp(FORM_Definition_Field_Reference fref)
        {
            var previousItem = Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false).FirstOrDefault(p => p.SortOrder == fref.SortOrder - 1);
            if (previousItem != null)
            {
                previousItem.SortOrder = previousItem.SortOrder + 1;
                fref.SortOrder = fref.SortOrder - 1;

                await LoadReferenceList(fref.ID);
            }

            StateHasChanged();
        }
        private async void MoveDown(FORM_Definition_Field_Reference fref)
        {
            var previousItem = Model.FORM_Definition_Field_ReferenceFORM_Definition_Field.Where(p => p.ToRemove == false).FirstOrDefault(p => p.SortOrder == fref.SortOrder + 1);
            if (previousItem != null)
            {
                previousItem.SortOrder = previousItem.SortOrder - 1;
                fref.SortOrder = fref.SortOrder + 1;

                await LoadReferenceList(fref.ID);
            }

            StateHasChanged();
        }
        private async void ReferenceItemChanged(FORM_Definition_Field_Reference fref)
        {
            if (fref != null && fref.FORM_Definition_Field_Source_ID != null)
            {
                var sourceItem = FormBuilderHelper.FormDefinition.FORM_Definition_Field.FirstOrDefault(p => p.ID == fref.FORM_Definition_Field_Source_ID.Value);

                if (sourceItem != null)
                {
                    fref.OptionList = sourceItem.FORM_Definition_Field_Option.ToList();

                    foreach (var opt in fref.OptionList)
                    {
                        if (opt.FORM_Definition_Field_Option_Extended != null &&
                            opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage) != null)
                        {
                            opt.Description = opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == FormBuilderHelper.CurrentLanguage).Description;
                        }
                    }
                }
            }

            StateHasChanged();
        }
    }
}