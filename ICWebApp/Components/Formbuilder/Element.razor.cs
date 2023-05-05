using DocumentFormat.OpenXml.InkML;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Syncfusion.Blazor.Navigations;

namespace ICWebApp.Components.Formbuilder
{
    public partial class Element
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }

        [Parameter] public FORM_Definition_Field Model { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveUp { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveDown { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnRemoveClicked { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnEditClicked { get; set; }
        [Parameter] public long MinSort { get; set; }
        [Parameter] public long MaxSort { get; set; }

        private bool DummyBool = false;
        private FORM_Application_Field_Data DummyList = new FORM_Application_Field_Data()
        {
            ID = Guid.NewGuid(),
            Value = "0"
        };
        private List<FORM_Definition_Field_SubType> SubTypes = new List<FORM_Definition_Field_SubType>();
        private FORM_Definition_Field_Type? Type;

        protected override void OnInitialized()
        {
            if (Model != null) 
            {
                Model.OnChange += Model_OnChange;
            }

            FormBuilderHelper.OnCurrentLanguageChanged += FormBuilderHelper_OnCurrentLanguageChanged;

            StateHasChanged();

            base.OnInitialized();
        }
        private void Model_OnChange()
        {
            if (Model != null && Model.FORM_Definition_Fields_Type_ID != null)
            {
                Type = FormBuilderHelper.GetDefintionFieldType(Model.FORM_Definition_Fields_Type_ID.Value);

                if (Type != null && Type.FORM_Definition_Field_SubType != null && SubTypes.Count() == 0)
                {
                    SubTypes = FormBuilderHelper.GetDefintionFieldSubTypeList();
                }

                StateHasChanged();
            }
        }
        private void FormBuilderHelper_OnCurrentLanguageChanged()
        {
            StateHasChanged();
        }
        protected override void OnParametersSet()
        {
            if (Model != null && Model.FORM_Definition_Fields_Type_ID != null)
            {
                Type = FormBuilderHelper.GetDefintionFieldType(Model.FORM_Definition_Fields_Type_ID.Value);

                if (Type != null && Type.FORM_Definition_Field_SubType != null && SubTypes.Count() == 0)
                {
                    SubTypes = FormBuilderHelper.GetDefintionFieldSubTypeList();
                }
            }

            StateHasChanged();

            base.OnParametersSet();
        }
        private async void MoveUp(FORM_Definition_Field Model)
        {
            await OnMoveUp.InvokeAsync(Model);
            StateHasChanged();
        }
        private async void MoveDown(FORM_Definition_Field Model)
        {
            await OnMoveDown.InvokeAsync(Model);
            StateHasChanged();
        }
        private async void OnRemove()
        {
            await OnRemoveClicked.InvokeAsync(Model);
        }
        private void OnEdit()
        {
            OnEditClicked.InvokeAsync(Model);

            StateHasChanged();
        }
        private void SelectedHandler(MenuEventArgs<MenuItem> e)
        {
            if (Model != null)
            {
                if (e.Item.Id == "edit")
                {
                    OnEdit();
                }
                else if (e.Item.Id == "delete")
                {
                    OnRemove();
                }
            }
        }
    }
}
