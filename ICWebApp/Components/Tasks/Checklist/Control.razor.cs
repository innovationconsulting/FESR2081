using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Checklist
{
    public partial class Control
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TASKProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }

        [Parameter] public Guid TaskID { get; set; }
        [Parameter] public List<TASK_Task_CheckItems> List { get; set; }
        [Parameter] public EventCallback<TASK_Task_CheckItems> ItemAdded { get; set; }
        [Parameter] public EventCallback<TASK_Task_CheckItems> ItemRemoved { get; set; }
        [Parameter] public EventCallback<TASK_Task_CheckItems> ItemEdited { get; set; }
        [Parameter] public EventCallback<TASK_Task_CheckItems> ItemChecked { get; set; }
        [Parameter] public EventCallback<TASK_Task_CheckItems> ItemUnchecked { get; set; }
        [Parameter] public bool ReadOnly { get; set; } = false;
        [Parameter] public bool SmallStyle { get; set; } = false;

        protected override void OnInitialized()
        {
            StateHasChanged();
            base.OnInitialized();
        }
        private async void AddCheckItem()
        {
            var item = new TASK_Task_CheckItems()
            {
                ID = Guid.NewGuid(),
                TASK_Task_ID = TaskID,
                CreatedAt = DateTime.Now,
                SortOrder = List.Count() + 1,
                InEdit = true
            };

            List.Add(item);

            await ItemAdded.InvokeAsync(item);
            StateHasChanged();
        }
        private async void SaveCheckItem(TASK_Task_CheckItems item)
        {
            item.InEdit = false;

            await ItemEdited.InvokeAsync(item);

            StateHasChanged();
        }
        private void EditCheckItem(TASK_Task_CheckItems item)
        {
            if (!ReadOnly)
            {
                item.InEdit = true;
                ItemEdited.InvokeAsync(item);
                StateHasChanged();
            }
            else
            {
                if(item.CompletedAt != null)
                {
                    UnCheckCheckItem(item);
                }
                else
                {
                    CheckCheckItem(item);
                }
            }
        }
        private async void RemoveCheckItem(TASK_Task_CheckItems Item)
        {
            var tagToRemove = List.FirstOrDefault(p => p.ID == Item.ID);

            if(tagToRemove != null)
            {
                List.Remove(tagToRemove);
            }

            await ItemRemoved.InvokeAsync(tagToRemove);
            StateHasChanged();
        }
        private async void CheckCheckItem(TASK_Task_CheckItems item)
        {
            item.CompletedAt = DateTime.Now;

            await ItemChecked.InvokeAsync(item);

            StateHasChanged();
        }
        private async void UnCheckCheckItem(TASK_Task_CheckItems item)
        {
            item.CompletedAt = null;

            await ItemUnchecked.InvokeAsync(item);

            StateHasChanged();
        }
    }
}
