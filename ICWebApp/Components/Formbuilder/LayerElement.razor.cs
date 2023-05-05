using ICWebApp.Application.Interface.Helper;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Formbuilder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Syncfusion.Blazor.Navigations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Formbuilder
{
    public partial class LayerElement
    {
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }

        [Parameter] public FORM_Definition_Field? Item { get; set; }
        [Parameter] public List<FORM_Definition_Field>? Data { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveUp { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnMoveDown { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnEdit { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnDelete { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnDragStart { get; set; }
        [Parameter] public EventCallback<FORM_Definition_Field> OnDragEnd{ get; set; }
        [Parameter] public EventCallback<LayerDropItem> OnDrop{ get; set; }
        [Parameter] public long MinSort { get; set; }
        [Parameter] public long MaxSort { get; set; }

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
        private void SelectedHandler(MenuEventArgs<MenuItem> e)
        {
            if (Item != null)
            {
                if (e.Item.Id == "edit")
                {
                    Edit(Item);
                }
                else if (e.Item.Id == "delete")
                {
                    Delete(Item);
                }
            }
        }
        private void DragStart(FORM_Definition_Field f)
        {
            OnDragStart.InvokeAsync(f);            
        }
        private void DragEnd(FORM_Definition_Field f)
        {
            OnDragEnd.InvokeAsync(f);
        }
        private void Drop(LayerDropItem DropItem)
        {
            OnDrop.InvokeAsync(DropItem);
        }
    }
}
