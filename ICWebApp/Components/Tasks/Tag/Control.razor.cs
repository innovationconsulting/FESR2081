using System.Drawing;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Tag
{
    public partial class Control
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TASKProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }

        [Parameter] public Guid TaskID { get; set; }
        [Parameter] public long? TaskContextID { get; set; } = null;
        [Parameter] public List<TASK_Task_Tag> TagList { get; set; }
        [Parameter] public EventCallback<TASK_Task_Tag> ItemAdded { get; set; }
        [Parameter] public EventCallback<TASK_Task_Tag> ItemRemoved { get; set; }
        [Parameter] public EventCallback OverlayClicked { get; set; }
        [Parameter] public bool ShowInputField { get; set; } = true;

        private List<V_TASK_Tag?> AllTags = new List<V_TASK_Tag>();
        private List<V_TASK_Tag?> TagsToAdd = new List<V_TASK_Tag>();
        private bool TagsDropdownVisibility = false;

        protected override async Task OnInitializedAsync()
        {
            if (TaskContextID == null)
                AllTags = await TaskService.GetTagList(TaskService.TASK_Context_ID, true);
            else
                AllTags = await TaskService.GetTagList(TaskContextID, true);
            
            if (TagList != null)
            {
                TagsToAdd = AllTags.Where(p => !TagList.Select(p => p.TASK_Tag_ID).Contains(p.ID)).ToList();
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async void AddTag(V_TASK_Tag Tag)
        {
            var item = new TASK_Task_Tag()
            {
                ID = Guid.NewGuid(),
                TASK_Tag_ID = Tag.ID,
                TASK_Task_ID = TaskID,
                SortOrder = Tag.SortOrder
            };

            TagList.Add(item);
            TagsToAdd = AllTags.Where(p => !TagList.Select(p => p.TASK_Tag_ID).Contains(p.ID)).ToList();

            HideTagsDropdown();

            await ItemAdded.InvokeAsync(item);
            StateHasChanged();
        }

        private async void RemoveTag(V_TASK_Tag Tag)
        {
            var tagToRemove = TagList.FirstOrDefault(p => p.TASK_Tag_ID == Tag.ID);

            if (tagToRemove != null)
            {
                TagList.Remove(tagToRemove);
            }

            TagsToAdd = AllTags.Where(p => !TagList.Select(p => p.TASK_Tag_ID).Contains(p.ID)).ToList();

            await ItemRemoved.InvokeAsync(tagToRemove);
            StateHasChanged();
        }

        private void ToggleTagsDropdown()
        {
            TagsDropdownVisibility = !TagsDropdownVisibility;
            StateHasChanged();
        }

        private void HideTagsDropdown()
        {
            TagsDropdownVisibility = false;
            StateHasChanged();
        }

        private void InvokeOverlayClicked()
        {
            OverlayClicked.InvokeAsync();
            StateHasChanged();
        }

        private string CalculateTagTextColor(string backgroundColor)
        {
            var color = ColorTranslator.FromHtml(backgroundColor);
            int r = Convert.ToInt16(color.R);
            int g = Convert.ToInt16(color.G);
            int b = Convert.ToInt16(color.B);
            var luminance = (r * 0.299f + g * 0.587f + b * 0.114f) / 256;
            return luminance < 0.3 ? "white" : "black";
        }
    }
}