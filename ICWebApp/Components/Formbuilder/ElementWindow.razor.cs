using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Formbuilder
{
    public partial class ElementWindow
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Parameter] public FORM_Definition_Field Model { get; set; }
        [Parameter] public bool Visibile { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }

        private List<FORM_Definition_Field_SubType> SubTypes = new List<FORM_Definition_Field_SubType>();
        private FORM_Definition_Field_Type? Type;
        private List<FORM_Definition_Field_Type> ChangeableFieldTypes;
        private List<long> ColumnCountList = new List<long>
        {
            1, 2, 3, 4
        }; 
        private bool Italian
        {
            get
            {
                if (FormBuilderHelper != null && FormBuilderHelper.CurrentLanguage == Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (FormBuilderHelper != null &&  value == true)
                {
                    FormBuilderHelper.CurrentLanguage = Guid.Parse("e450421a-baff-493e-a390-71b49be6485f");
                    FormBuilderHelper.NotifySingleLanguageChanged();
                    StateHasChanged();
                }
            }
        }
        private bool German
        {
            get
            {
                if (FormBuilderHelper != null && FormBuilderHelper.CurrentLanguage == Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (FormBuilderHelper != null && value == true)
                {
                    FormBuilderHelper.CurrentLanguage = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");
                    FormBuilderHelper.NotifySingleLanguageChanged();
                    StateHasChanged();
                }
            }
        }

        protected override void OnParametersSet()
        {
            if (Model != null && Model.FORM_Definition_Fields_Type_ID != null)
            {
                Type = FormBuilderHelper.GetDefintionFieldType(Model.FORM_Definition_Fields_Type_ID.Value);

                if (Type != null && Type.FORM_Definition_Field_SubType != null)
                {
                    var locSubTypes = FormBuilderHelper.GetDefintionFieldSubTypeList();

                    SubTypes = locSubTypes.Where(p => p.FORM_Definition_Field_Type_ID == Type.ID).ToList();
                }

                if (Type != null && Type.IsChangeable)
                {
                    var types = FormBuilderHelper.GetDefintionFieldTypeList();
                    ChangeableFieldTypes = types.Where(p => p.IsChangeable == true).ToList();
                }

                if (Model.UploadMultiple == null)
                {
                    Model.UploadMultiple = true;
                }
                StateHasChanged();
            }

            StateHasChanged();
            base.OnParametersSet();
        }
        private async void OnSave()
        {
            if (string.IsNullOrEmpty(Model.DatabaseName) && Type != null)
            {
                Model.DatabaseName = Type.Description + "-" + (FormBuilderHelper.FormDefinition.FORM_Definition_Field.Where(p => p.FORM_Definition_Fields_Type_ID == Type.ID).Count() + 1).ToString();
            }
            Model.NotifyOnChanged();
            await OnClose.InvokeAsync();

            StateHasChanged();
        }
        private async void AddOption()
        {
            var opt = new FORM_Definition_Field_Option();
            var Languages = FormBuilderHelper.Languages;

            opt.ID = Guid.NewGuid();
            opt.FORM_Definition_Field_ID = Model.ID;
            opt.IsNew = true;

            long NextItem = 1;

            if (Model.FORM_Definition_Field_Option != null && Model.FORM_Definition_Field_Option.Count > 0)
            {
                var MaxItem = Model.FORM_Definition_Field_Option.Max(p => p.SortOrder);

                if (MaxItem != null)
                {
                    NextItem = MaxItem.Value + 1;
                }
            }

            opt.SortOrder = NextItem;

            if (Languages != null)
            {
                foreach (var l in Languages)
                {
                    if (opt.FORM_Definition_Field_Option_Extended == null)
                    {
                        opt.FORM_Definition_Field_Option_Extended = new List<FORM_Definition_Field_Option_Extended>();
                    }

                    if (opt.FORM_Definition_Field_Option_Extended.FirstOrDefault(p => p.LANG_Languages_ID == l.ID) == null)
                    {
                        var opte = new FORM_Definition_Field_Option_Extended()
                        {
                            ID = Guid.NewGuid(),
                            FORM_Definition_Field_Option_ID = opt.ID,
                            LANG_Languages_ID = l.ID
                        };

                        opt.FORM_Definition_Field_Option_Extended.Add(opte);
                    }
                }
            }

            Model.FORM_Definition_Field_Option.Add(opt);

            StateHasChanged();
        }
        private async void RemoveOption(FORM_Definition_Field_Option opt)
        {
            opt.ToRemove = true;
            StateHasChanged();
        }
        private void MoveOptionUp(FORM_Definition_Field_Option opt)
        {
            if (Model != null && Model.FORM_Definition_Field_Option != null
               && Model.FORM_Definition_Field_Option.Count() > 0)
            {
                ReOrderOptions();
                var newPos = Model.FORM_Definition_Field_Option.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                }
            }

            StateHasChanged();
        }
        private void MoveOptionDown(FORM_Definition_Field_Option opt)
        {
            if (Model != null && Model.FORM_Definition_Field_Option != null
               && Model.FORM_Definition_Field_Option.Count() > 0)
            {
                ReOrderOptions();
                var newPos = Model.FORM_Definition_Field_Option.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                }
            }

            StateHasChanged();
        }
        private void ReOrderOptions()
        {
            int count = 1;

            foreach (var opt in Model.FORM_Definition_Field_Option.OrderBy(p => p.SortOrder))
            {
                opt.SortOrder = count;

                count++;
            }
        }
        private void TypeChanged()
        {
            if (Model != null && Model.FORM_Definition_Fields_Type_ID != null)
            {
                Type = FormDefinitionProvider.GetDefinitionFieldType(Model.FORM_Definition_Fields_Type_ID.Value);

                if (Type != null && Type.FORM_Definition_Field_SubType != null && SubTypes.Count() == 0)
                {
                    SubTypes = FormBuilderHelper.GetDefintionFieldSubTypeList().Where(p => p.FORM_Definition_Field_Type_ID == Type.ID).ToList();
                }

                StateHasChanged();
            }
        }

    }
}
