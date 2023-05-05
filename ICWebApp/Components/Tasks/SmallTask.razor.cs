using System.Drawing;
using ICWebApp.Application.Cache.Tasks;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Components.Tasks
{
    public partial class SmallTask
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public V_TASK_Task? Task { get; set; }
        [Parameter] public EventCallback<Guid> Saved { get; set; }
        [Parameter] public EventCallback<Guid> Removed { get; set; }
        [Parameter] public EventCallback Cancelled { get; set; }
        [Parameter] public EventCallback<V_TASK_Task> Edit { get; set; }
        [Parameter] public bool RefreshElement { get; set; } = false;
        [Parameter] public bool AllowDelete { get; set; } = true;
        [Parameter] public bool DisplayContext { get; set; } = false;

        private List<V_TASK_Bucket?> BucketList { get; set; }
        private List<V_TASK_Status?> StatusList { get; set; }
        private List<V_TASK_Tag?> TagList { get; set; }
        private List<V_TASK_Priority?> PriorityList { get; set; }
        private List<TASK_Task_Tag?> TaskTagList { get; set; }
        private List<TASK_Task_CheckItems?> TaskCheckItemsList { get; set; }
        private List<TASK_Task_Files?> TaskFilesList { get; set; }
        private List<FILE_FileInfo?> FileList { get; set; }
        private List<TASK_Task_Comment?> TaskCommentList { get; set; }
        private List<TASK_Task_Responsible?> TaskResponsibleList { get; set; }
        private List<TASK_Task_Eskalation?> TaskEskalationList { get; set; }

        private string? _contextSystemTextCode { get; set; }

        private bool HasComments = false;
        private bool HasFiles = false;
        private bool HasTags = false;
        private bool HasCheckItems = false;
        private bool HasResponsible = false;
        private bool HasEskalation = false;
        private bool Refreshing = false;
        private bool Initialized = false;
        private bool ResponsibleSelectionVisibile = false;
        private bool TagSelectionVisible = false;
        private bool ShowPriorityList = false;
        private bool ShowStatusList = false;

        private bool ShowCalendar = false;
        private DateTime? pickedDate = null;

        protected override async Task OnInitializedAsync()
        {
            if (Task == null)
            {
                await Cancelled.InvokeAsync();
                return;
            }
            
            BucketList = await TaskService.GetBucketList(Task.TASK_Context_ID, true);
            StatusList = await TaskService.GetStatusList(Task.TASK_Context_ID, true);
            TagList = await TaskService.GetTagList(Task.TASK_Context_ID, true);
            PriorityList = await TaskService.GetPriorityList(Task.TASK_Context_ID, true);


            await GetData();
            pickedDate = Task.Deadline;
            Initialized = true;

            StateHasChanged();
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (RefreshElement == true && Refreshing == false && Initialized == true)
            {
                Refreshing = true;

                await GetData();
                StateHasChanged();

                RefreshElement = false;
                Refreshing = false;
            }

            
            StateHasChanged();
            await base.OnParametersSetAsync();
        }

        private async Task<bool> GetData()
        {
            TaskTagList = (await TaskProvider.GetTaskTagList(Task.ID)).ToList();
            TaskCheckItemsList = (await TaskProvider.GetTaskCheckItemsList(Task.ID)).ToList();
            TaskCommentList = (await TaskProvider.GetTaskCommentList(Task.ID)).ToList();
            TaskResponsibleList = (await TaskProvider.GetTaskResponsibleList(Task.ID)).ToList();
            TaskEskalationList = (await TaskProvider.GetTaskEskalationList(Task.ID)).ToList();

            if (Task.TASK_Context_ID != null)
            {
                _contextSystemTextCode = await TaskProvider.GetContextSyetemTextCode(Task.TASK_Context_ID.Value);
            }


            var TaskFilesList = await TaskProvider.GetTaskFilesList(Task.ID);

            FileList = new List<FILE_FileInfo?>();

            foreach (var f in TaskFilesList)
            {
                var file = FileProvider.GetFileInfo(f.FILE_FileInfo_ID.Value);

                if (file != null)
                {
                    FileList.Add(file);
                }
            }

            if (TaskTagList != null && TaskTagList.Count() > 0)
            {
                HasTags = true;
            }
            else
            {
                HasTags = false;
            }

            if (TaskCheckItemsList != null && TaskCheckItemsList.Where(p => p.CompletedAt == null).Count() > 0)
            {
                HasCheckItems = true;
            }
            else
            {
                HasCheckItems = false;
            }

            if (TaskCommentList != null && TaskCommentList.Count() > 0)
            {
                HasComments = true;
            }
            else
            {
                HasComments = false;
            }

            if (TaskResponsibleList != null && TaskResponsibleList.Count() > 0)
            {
                HasResponsible = true;
            }
            else
            {
                HasResponsible = false;
            }

            if (TaskEskalationList != null && TaskEskalationList.Count() > 0)
            {
                HasEskalation = true;
            }
            else
            {
                HasEskalation = false;
            }

            if (FileList != null && FileList.Count() > 0)
            {
                HasFiles = true;
            }
            else
            {
                HasFiles = false;
            }

            return true;
        }

        private async void CheckCheckItem()
        {
            if (Task != null)
            {
                var item = await TaskProvider.GetTask(Task.ID);

                if (item != null)
                {

                    //Check Status
                    List<V_TASK_Status> _statusList = await TaskService.GetStatusList(item.TASK_Context_ID, true);

                    if (_statusList != null && _statusList.Any())
                    {
                        V_TASK_Status? _actualStatus = null;
                        if (item.TASK_Status_ID != null)
                        {
                            _actualStatus = _statusList.FirstOrDefault(p => p.ID == item.TASK_Status_ID.Value);
                        }
                        if (item.TASK_Status_ID == null || _actualStatus == null || !_actualStatus.CompleteTask)
                        {
                            V_TASK_Status? _completationStatus = _statusList.FirstOrDefault(p => p.DefaultCompleteTask);
                            if(_completationStatus != null)
                            {
                                item.TASK_Status_ID = _completationStatus.ID;
                                TaskService.ChangesCache.AddChange(item, new TaskChangedValue(ChangeType.Status, new List<string>() { item.TASK_Status_ID.ToString() }), false);
                            }
                        }
                    }
                    //Update Change Cache for this task
                    TaskService.ChangesCache.AddChange(item, new TaskChangedValue(ChangeType.TaskChecked, new List<string>() { item.CompletedAt.ToString() }), false);

                    item.CompletedAt = DateTime.Now;
                    await TaskProvider.SetTask(item);
                    
                    await Saved.InvokeAsync(Task.ID);
                }

                StateHasChanged();
            }
        }

        private async void UnCheckCheckItem()
        {
            if (Task != null)
            {
                var item = await TaskProvider.GetTask(Task.ID);

                if (item != null)
                {
                    //Update Change Cache for this task
                    TaskService.ChangesCache.AddChange(item, new TaskChangedValue(ChangeType.TaskUnchecked, new List<string>(){""}), false);
                    
                    item.CompletedAt = null;

                    await TaskProvider.SetTask(item);
                    
                    await Saved.InvokeAsync(Task.ID);
                }

                StateHasChanged();
            }

            StateHasChanged();
        }

        private async Task<bool> CheckItemEdited(TASK_Task_CheckItems Item)
        {
            await TaskProvider.SetTaskCheckItem(Item);
            StateHasChanged();

            return true;
        }

        private async Task<bool> CheckItemChecked(TASK_Task_CheckItems Item)
        {
            await TaskProvider.SetTaskCheckItem(Item);
            
            var task = await TaskProvider.GetTask(Task.ID);
            //Update Change Cache for this task
            TaskService.ChangesCache.AddChange(task!, new TaskChangedValue(ChangeType.CheckItemsChecked, new List<string>(){Item.ID.ToString()}), false);
            StateHasChanged();
            return true;
        }

        private async Task<bool> CheckItemUnchecked(TASK_Task_CheckItems Item)
        {
            await TaskProvider.SetTaskCheckItem(Item);
            var task = await TaskProvider.GetTask(Task.ID);
            //Update Change Cache for this task
            TaskService.ChangesCache.AddChange(task!, new TaskChangedValue(ChangeType.CheckItemsUnchecked, new List<string>(){Item.ID.ToString()}), false);
            StateHasChanged();
            return true;
        }

        private async Task<bool> EditTask()
        {
            await Edit.InvokeAsync(Task);
            StateHasChanged();

            return true;
        }

        private async Task<bool> DeleteTask()
        {
            if (Task != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("TASK_DELETE_ARE_YOUR_SURE"),
                        TextProvider.Get("WARNING")))
                    return false;

                await TaskProvider.RemoveTask(Task.ID);
                await Removed.InvokeAsync(Task.ID);
                TaskService.ChangesCache.TaskWasDeleted(Task.ID);
                return true;
            }

            return false;
        }

        private async Task<bool> ResponsibleQuickAdd(TASK_Task_Responsible Responsible)
        {
            //Update Change Cache for this task
            TaskService.ChangesCache.AddChange(Task.ID, new TaskChangedValue(ChangeType.ResponsibleAdded, new List<string>(){Responsible.AUTH_Users_ID.ToString()}));
            
            await TaskService.SetResponsible(Responsible);

            ResponsibleSelectionVisibile = false;
            StateHasChanged();

            return true;
        }

        private async Task<bool> ResponsibleQuickRemove(TASK_Task_Responsible Responsible)
        {
            //Update Change Cache for this task
            TaskService.ChangesCache.AddChange(Task.ID, new TaskChangedValue(ChangeType.ResponsibleRemoved, new List<string>(){Responsible.AUTH_Users_ID.ToString()}));
            
            await TaskProvider.RemoveTaskResponsible(Responsible.ID);
            ResponsibleSelectionVisibile = false;
            StateHasChanged();
            return true;
        }

        private void ResponsibleOverlayClicked()
        {
            ResponsibleSelectionVisibile = false;
            StateHasChanged();
        }

        private void ShowTagSelection()
        {
            TagSelectionVisible = !TagSelectionVisible;
            StateHasChanged();
        }

        private async Task<bool> TagQuickAdd(TASK_Task_Tag Tag)
        {
            //Update Change Cache for this task
            TaskService.ChangesCache.AddChange(Task.ID, new TaskChangedValue(ChangeType.TagsAdded, new List<string>(){Tag.TASK_Tag_ID.ToString()}));
            
            await TaskProvider.SetTaskTag(Tag);
            TagSelectionVisible = false;
            StateHasChanged();

            return true;
        }

        private async Task<bool> TagQuickRemove(TASK_Task_Tag Tag)
        {
            //Update Change Cache for this task
            TaskService.ChangesCache.AddChange(Task.ID, new TaskChangedValue(ChangeType.TagsRemoved, new List<string>(){Tag.TASK_Tag_ID.ToString()}));
            
            await TaskProvider.RemoveTaskTag(Tag.ID);
            TagSelectionVisible = false;
            StateHasChanged();
            return true;
        }

        private void TagOverlayClicked()
        {
            TagSelectionVisible = false;
            StateHasChanged();
        }

        private void OnShowPriorityList()
        {
            ShowPriorityList = !ShowPriorityList;
            StateHasChanged();
        }

        private async Task<bool> SelectPriority(V_TASK_Priority Prio)
        {
            if (Task != null)
            {
                var dbTask = await TaskProvider.GetTask(Task.ID);

                if (dbTask != null)
                {
                    //Update Change Cache for this task
                    TaskService.ChangesCache.AddChange(dbTask, new TaskChangedValue(ChangeType.Priority, new List<string>(){Prio.ID.ToString()}), false);
                    
                    Task.TASK_Priority_ID = Prio.ID;
                    dbTask.TASK_Priority_ID = Prio.ID;
                    await TaskProvider.SetTask(dbTask);
                }
            }

            ShowPriorityList = false;
            StateHasChanged();
            return true;
        }

        private void OnShowStatusList()
        {
            ShowStatusList = !ShowStatusList;
            StateHasChanged();
        }

        private async Task<bool> SelectStatus(V_TASK_Status Status)
        {
            if (Task != null)
            {
                var dbTask = await TaskProvider.GetTask(Task.ID);

                if (dbTask != null)
                {
                    //Update Change Cache for this task
                    TaskService.ChangesCache.AddChange(dbTask, new TaskChangedValue(ChangeType.Status, new List<string>(){Status.ID.ToString()}), false);
                    
                    Task.TASK_Status_ID = Status.ID;
                    dbTask.TASK_Status_ID = Status.ID;
                    await TaskProvider.SetTask(dbTask);
                }
            }

            ShowStatusList = false;
            StateHasChanged();
            return true;
        }

        private void DueDateChanged(DateTime newValue)
        {
            pickedDate = newValue;
        }

        private async Task SaveDate()
        {
            if (Task != null && pickedDate != null && pickedDate.Value != Task.Deadline)
            {
                var item = await TaskProvider.GetTask(Task.ID);

                if (item != null)
                {
                    //Update Change Cache for this task
                    TaskService.ChangesCache.AddChange(item, new TaskChangedValue(ChangeType.Deadline, new List<string>(){pickedDate.Value.ToString()}), false);

                    item.Deadline = pickedDate;
                    await TaskProvider.SetTask(item);
                    await Saved.InvokeAsync(Task.ID);
                }
                
            }
            ShowCalendar = false;
        }
        private void CloseallPopups()
        {
            ShowPriorityList = false;
            ShowStatusList = false;
            ShowCalendar = false;

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
        private void ShowResponsibleSelection()
        {
            ResponsibleSelectionVisibile = !ResponsibleSelectionVisibile;
            StateHasChanged();
        }
    }
}