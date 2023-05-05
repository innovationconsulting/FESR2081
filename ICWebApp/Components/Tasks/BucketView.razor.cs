using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Components.Tasks
{
    public partial class BucketView
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        [Parameter] public EventCallback Cancelled { get; set; }

        private List<V_TASK_Task?> TaskList;
        private List<V_TASK_Bucket?> BucketList;
        private bool EditVisibility = false;
        private string? ItemToEditID = null;
        private Guid? BucketID = null;
        private V_TASK_Task? DraggingTask = null;
        private bool BusyIndicator = true;
        private BucketView_Search Filter = new BucketView_Search();
        private List<V_TASK_Status?> StatusList;
        private List<V_TASK_Priority?> PriorityList;
        private bool BoardFiltered { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {   
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                TaskList = (await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, LangProvider.GetCurrentLanguageID(), TaskService.TASK_Context_ID, TaskService.ContextElementID)).ToList();
            }

            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, true);
            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, true);
            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, true);

            BusyIndicator = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private void EditTask(V_TASK_Task Task)
        {
            ItemToEditID = Task.ID.ToString();

            EditVisibility = true;
            StateHasChanged();
        }
        private void AddTask(V_TASK_Bucket Bucket)
        {
            BucketID = Bucket.ID;
            ItemToEditID = null;
            
            EditVisibility = true;
            StateHasChanged();
        }
        private void CloseTask()
        {
            EditVisibility = false;
            StateHasChanged();
        }
        private async Task<bool> TaskSaved(Guid TaskID)
        {
            if (TaskList != null)
            {
                var taskToUpdate = TaskList.FirstOrDefault(p => p.ID == TaskID);

                if (taskToUpdate != null)
                {
                    TaskList.Remove(taskToUpdate);
                }

                taskToUpdate = await TaskProvider.GetVTask(TaskID, LangProvider.GetCurrentLanguageID());

                TaskList.Add(taskToUpdate);
            }

            EditVisibility = false;
            StateHasChanged();

            return true;
        }
        private async Task<bool> TaskRemoved(Guid TaskID)
        {
            if (TaskList != null)
            {
                var taskToUpdate = TaskList.FirstOrDefault(p => p.ID == TaskID);

                if (taskToUpdate != null)
                {
                    TaskList.Remove(taskToUpdate);
                }
            }

            StateHasChanged();

            return true;
        }
        private void ToggleFinishedTasks(V_TASK_Bucket Bucket)
        {
            Bucket.ShowCompleted = !Bucket.ShowCompleted;
            StateHasChanged();
        }
        private void DragStart(V_TASK_Task? Item)
        {
            DraggingTask = Item;
            StateHasChanged();
        }
        private async void DragEnd(V_TASK_Task? Item)
        {
            await JSRuntime.InvokeVoidAsync("formBuilder_clearDraggableClass");
            DraggingTask = null;
            StateHasChanged();
        }
        private async void DropHandler(Guid TASK_Bucket_ID, V_TASK_Task? Item = null, bool Completed = false)
        {
            if(DraggingTask != null)
            {
                await JSRuntime.InvokeVoidAsync("formBuilder_clearDraggableClass");
                BusyIndicator = true;
                StateHasChanged();

                var task = await TaskProvider.GetTask(DraggingTask.ID);

                if (task != null)
                {
                    task.TASK_Bucket_ID = TASK_Bucket_ID;

                    if(Completed == true)
                    {
                        task.CompletedAt = DateTime.Now;
                    }
                    else
                    {
                        task.CompletedAt = null;
                    }

                    if(Item != null)
                    {
                        task.SortOrder = Item.SortOrder - 1;
                    }
                    else
                    {
                        task.SortOrder = await TaskService.GetTaskPosition(task.TASK_Bucket_ID.Value);
                    }

                    await TaskProvider.SetTask(task);

                    await ReorderBucket(TASK_Bucket_ID);


                    TaskList = (await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, LangProvider.GetCurrentLanguageID(), TaskService.TASK_Context_ID, TaskService.ContextElementID)).ToList();
                }

                BusyIndicator = false;
                StateHasChanged();
            }
        }
        private async Task<bool> ReorderBucket(Guid? TASK_Bucket_ID)
        {
            int Sort = 0;

            var itemsToUpdate = await TaskProvider.GetTaskListToReorder(TASK_Bucket_ID, SessionWrapper.AUTH_Municipality_ID.Value, TaskService.TASK_Context_ID, TaskService.ContextElementID);

            foreach (var d in itemsToUpdate.OrderBy(p => p.SortOrder).ToList())
            {
                var task = await TaskProvider.GetTask(d.ID);

                if (task != null) 
                { 
                    if (d.SortOrder != 9999)
                    {
                        d.SortOrder = Sort;

                        Sort++;
                    }
                }
            }

            await TaskProvider.BulkUpdateTask(itemsToUpdate);
            return true;
        }
        private bool OnFilterChanged()
        {
            FilterData();
            StateHasChanged();

            return true;
        }
        private bool OnFilterCleared()
        {
            if(Filter != null)
            {
                Filter.TASK_Priorities = null;
                Filter.TASK_Status = null;

                if (Filter.TASK_Tags != null)
                {
                    Filter.TASK_Tags.Clear();
                }
                if (Filter.AUTH_Users != null)
                {
                    Filter.AUTH_Users.Clear();
                }
            }
            else
            {
                Filter = new BucketView_Search();
            }

            FilterData();

            StateHasChanged();
            return true;
        }
        private bool FilterData()
        {
            bool Filtered = false;

            if(Filter != null)
            {
                if (Filter.TASK_Status != null)
                    Filtered = true;
                if (Filter.TASK_Priorities != null)
                    Filtered = true;
                if (Filter.TASK_Tags != null && Filter.TASK_Tags.Count() > 0)
                    Filtered = true;
                if (Filter.AUTH_Users != null && Filter.AUTH_Users.Count() > 0)
                    Filtered = true;
            }

            foreach(var task in TaskList)
            {
                if (Filter != null && task != null)
                {
                    if (Filter.TASK_Status != null && Filter.TASK_Status.Value == task.TASK_Status_ID)
                    {
                        task.Filtered = true;
                        continue;
                    }
                    if (Filter.TASK_Priorities != null && Filter.TASK_Priorities.Value == task.TASK_Priority_ID)
                    {
                        task.Filtered = true;
                        continue;
                    }
                    if (Filter.TASK_Tags != null && Filter.TASK_Tags.Count() > 0)
                    {
                        var tagFiltered = false;

                        if (task.SelectedTags != null)
                        {
                            foreach (var tag in Filter.TASK_Tags)
                            {
                                if (task.SelectedTags.ToLower().Contains(tag.ToString().ToLower()))
                                {
                                    tagFiltered = true;
                                    continue;
                                }
                            }

                            if (tagFiltered == true) 
                            {
                                task.Filtered = tagFiltered;
                                continue;
                            }
                        }
                    }
                    if (Filter.AUTH_Users != null && Filter.AUTH_Users.Count() > 0)
                    {
                        var userFiltered = false;

                        if (task.ResponsibleUser != null)
                        {
                            foreach (var user in Filter.AUTH_Users)
                            {
                                if (task.ResponsibleUser.ToLower().Contains(user.ToString().ToLower()))
                                {
                                    userFiltered = true;
                                    continue;
                                }
                            }

                            if (userFiltered == true)
                            {
                                task.Filtered = userFiltered;
                                continue;
                            }
                        }
                    }

                    task.Filtered = false;
                }                
            }

            BoardFiltered = Filtered;

            return true;
        }
    }
}
