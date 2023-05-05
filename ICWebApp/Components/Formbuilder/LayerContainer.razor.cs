using DocumentFormat.OpenXml.InkML;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.Formbuilder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Formbuilder
{
    public partial class LayerContainer
    {
        [Inject] IJSRuntime JSRuntime { get; set; }

        [Parameter] public FORM_Definition? Definition { get; set; }
        [Parameter] public List<FORM_Definition_Field>? Data { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveUp { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveDown { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnEdit { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnDelete { get; set; }
        [Parameter] public EventCallback OnChange { get; set; }

        private FORM_Definition_Field? DraggedField { get; set; }

        private void MoveUp(FORM_Definition_Field Item)
        {
            OnMoveUp.InvokeAsync(Item);
        }
        private void MoveDown(FORM_Definition_Field Item)
        {
            OnMoveDown.InvokeAsync(Item);
        }
        private void Edit(FORM_Definition_Field Item)
        {
            OnEdit.InvokeAsync(Item);
        }
        private void Delete(FORM_Definition_Field Item)
        {
            OnDelete.InvokeAsync(Item);
        }
        private async void DragStart(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await JSRuntime.InvokeVoidAsync("SetElementContext", "layer");
                DraggedField = f;
            }
        }
        private async Task<bool> DragEnd(FORM_Definition_Field f)
        {
            if (f != null)
            {
                await JSRuntime.InvokeVoidAsync("Layer_clearDraggableClass");
                DraggedField = null;
            }

            return true;
        }
        private async Task<bool> Drop(LayerDropItem DropItem)
        {
            if (Definition != null && DraggedField != null)
            {
                Definition.FORM_Definition_Field.Where(x => x.ColumnPos == DropItem.ColumnPos && x.SortOrder >= DropItem.SortOrder && x.FORM_Definition_Field_Parent_ID == DropItem.ParentID).ToList().ForEach(x => x.SortOrder++);

                DraggedField.SortOrder = DropItem.SortOrder;
                DraggedField.ColumnPos = DropItem.ColumnPos;
                DraggedField.FORM_Definition_Field_Parent_ID = DropItem.ParentID;                

                ReorderList(DropItem.ParentID);

                await JSRuntime.InvokeVoidAsync("Layer_clearDraggableClass");
                await JSRuntime.InvokeVoidAsync("Layer_clearDraggableContainerClass");

                await OnChange.InvokeAsync();
                DraggedField = null;
                StateHasChanged();
            }

            return true;
        }
        private bool ReorderList(Guid? FORM_Definition_Field_Parent_ID)
        {
            if (Definition != null)
            {
                foreach (var col in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Parent_ID == FORM_Definition_Field_Parent_ID).Select(p => p.ColumnPos).Distinct().ToList())
                {
                    int Sort = 0;

                    foreach (var d in Definition.FORM_Definition_Field.Where(p => p.FORM_Definition_Field_Parent_ID == FORM_Definition_Field_Parent_ID && p.ColumnPos == col).ToList().OrderBy(p => p.SortOrder))
                    {
                        d.SortOrder = Sort;
                        Sort++;
                    }
                }
            }

            return true;
        }
    }
}
