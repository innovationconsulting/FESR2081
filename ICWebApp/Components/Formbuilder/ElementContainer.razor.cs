using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Navigations;
using Telerik.Blazor;

namespace ICWebApp.Components.Formbuilder
{
    public partial class ElementContainer
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        [Parameter] public FORM_Definition_Field Model { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveUp { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveDown { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnRemoveClicked { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnFieldRemoveClicked { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnFieldWithSubFieldsRemovedClicked { get; set; }
        [Parameter] public EventCallback<Formbuilder_DragAndDropItem> OnDropHandler { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnDragStartHandler { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnDragEndHandler { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnEditClicked { get; set; }
        [Parameter] public long MinSort { get; set; }
        [Parameter] public long MaxSort { get; set; }

        protected override void OnInitialized()
        {
            FormBuilderHelper.OnFormDefinitionChanged += FormBuilderHelper_OnFormDefinitionChanged;

            Model.OnChange += Model_OnChange;

            base.OnInitialized();
        }
        private void Model_OnChange()
        {
            StateHasChanged();
        }
        private void FormBuilderHelper_OnFormDefinitionChanged()
        {
            StateHasChanged();
        }
        protected override void OnParametersSet()
        {

            StateHasChanged();
            base.OnParametersSet();
        }
        private async void DragFieldStartHandler(FORM_Definition_Field f)
        {
            await JSRuntime.InvokeVoidAsync("SetElementContext", "builder");
            await OnDragStartHandler.InvokeAsync(f);
        }
        private async void DragFieldEndHandler(FORM_Definition_Field f)
        {
            await OnDragEndHandler.InvokeAsync(f);
            
        }
        private async void DropHandler(Formbuilder_DragAndDropItem DropItem)
        {
            await OnDropHandler.InvokeAsync(DropItem);
            StateHasChanged();
        }
        private async void MoveUp(FORM_Definition_Field f)
        {
            await OnMoveUp.InvokeAsync(f);
            StateHasChanged();
        }
        private async void MoveDown(FORM_Definition_Field f)
        {
            await OnMoveDown.InvokeAsync(f);
            StateHasChanged();
        }
        private async void OnRemoveField(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await OnFieldRemoveClicked.InvokeAsync(f);
                StateHasChanged();
            }
        }
        private async void OnRemoveContainer(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await OnRemoveClicked.InvokeAsync(f);
                StateHasChanged();
            }
        }
        private async void OnRemoveFieldWithSubFields(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await OnFieldWithSubFieldsRemovedClicked.InvokeAsync(f);
                StateHasChanged();
            }
        }
        private void OnEdit(FORM_Definition_Field Model)
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
                    OnEdit(Model);
                }
                else if (e.Item.Id == "delete")
                {
                    OnRemoveContainer(Model);
                }
            }
        }
    }
}
