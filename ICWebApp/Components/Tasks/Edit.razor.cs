using ICWebApp.Application.Cache.Tasks;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Pages.Form.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ICWebApp.Components.Tasks
{
    public partial class Edit
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }   
        [Inject] NavigationManager NavManager { get; set; }

        [Parameter] public string? ID { get; set; }
        [Parameter] public Guid? BucketID { get; set; }
        [Parameter] public EventCallback<Guid> Saved { get; set; }
        [Parameter] public EventCallback Cancelled { get; set; }

        private TASK_Task Task { get; set; }
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
        private bool DataIsBusy = false;
        EditForm InputForm;
        
        //Change Tracking variables
        //CheckItems
        private List<Guid> addedCheckItems = new List<Guid>();
        private List<Guid> editedCheckItems = new List<Guid>();
        private List<Guid> checkedCheckItems = new List<Guid>();
        private List<Guid> uncheckedCheckItems = new List<Guid>();

        private List<TASK_Task_CheckItems> removedCheckItemsObjects = new List<TASK_Task_CheckItems>();

        //Tags
        private List<Guid> addedTags = new List<Guid>();
        
        //Comments
        private List<Guid> addedComments = new List<Guid>();
        
        //Files
        private List<Guid> addedFiles = new List<Guid>();

        //Responsible
        private List<Guid> addedResponsibles = new List<Guid>();

        protected override async Task OnInitializedAsync()
        {
            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, true);
            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, true);
            TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, true);
            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, true);

            if (string.IsNullOrEmpty(ID))
            {
                Task = new TASK_Task();

                Task.ID = Guid.NewGuid();
                Task.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID;
                Task.TASK_Context_ID = TaskService.TASK_Context_ID;
                Task.ContextElementID = TaskService.ContextElementID;
                Task.Url = NavManager.Uri;
                Task.CreatedByAUTH_UserID = SessionWrapper.CurrentUser.ID;
                Task.NotifyCreator = TaskService.TaskNotifyCreator;
                Task.Deadline = TaskService.TaskDefaultDeadline;

                if (!string.IsNullOrEmpty(TaskService.ContextName))
                {
                    Task.ContextName = TaskService.ContextName;
                }
                else
                {
                    Task.ContextName = TextProvider.Get("TASK_ADD_DEFAULT_CONEXT");
                }

                Task.CreatedAt = DateTime.Now;

                var defaultPrio = PriorityList.FirstOrDefault(p => p.Default == true);

                if (defaultPrio != null)
                {
                    Task.TASK_Priority_ID = defaultPrio.ID;
                }

                var defaultStatus = StatusList.FirstOrDefault(p => p.Default == true);

                if (defaultStatus != null)
                {
                    Task.TASK_Status_ID = defaultStatus.ID;
                }

                if (BucketID != null)
                {
                    Task.TASK_Bucket_ID = BucketID;
                }
                else
                {
                    var defaultBucket = BucketList.FirstOrDefault(p => p.Default == true);

                    if (defaultBucket != null)
                    {
                        Task.TASK_Bucket_ID = defaultBucket.ID;
                    }
                }
            }
            else
            {
                var task = await TaskProvider.GetTask(Guid.Parse(ID));

                if (task != null)
                {
                    Task = task;
                }
            }

            if(Task == null)
            {
                await Cancelled.InvokeAsync();
                return;
            }

            TaskTagList = (await TaskProvider.GetTaskTagList(Task.ID)).ToList();
            TaskCheckItemsList = (await TaskProvider.GetTaskCheckItemsList(Task.ID)).ToList();
            TaskCommentList = (await TaskProvider.GetTaskCommentList(Task.ID)).ToList();
            TaskResponsibleList = (await TaskProvider.GetTaskResponsibleList(Task.ID)).ToList();
            TaskEskalationList = (await TaskProvider.GetTaskEskalationList(Task.ID)).ToList();

            if(TaskResponsibleList == null)
            {
                TaskResponsibleList = new List<TASK_Task_Responsible?>();
            }
            if (TaskCheckItemsList == null)
            {
                TaskCheckItemsList = new List<TASK_Task_CheckItems?>();
            }
            if (TaskCommentList == null)
            {
                TaskCommentList = new List<TASK_Task_Comment?>();
            }
            if (TaskTagList == null)
            {
                TaskTagList = new List<TASK_Task_Tag?>();
            }
            if (TaskEskalationList == null)
            {
                TaskEskalationList = new List<TASK_Task_Eskalation?>();
            }

            foreach (var esk in TaskEskalationList)
            {
                if (esk != null)
                {
                    esk.TASK_Task_Eskalation_Responsible = (await TaskProvider.GetTaskEskalationResponsibleList(esk.ID)).ToList();
                }
            }

            TaskFilesList = (await TaskProvider.GetTaskFilesList(Task.ID)).ToList();

            FileList = new List<FILE_FileInfo?>();

            foreach(var f in TaskFilesList)
            {
                var file = FileProvider.GetFileInfo(f.FILE_FileInfo_ID.Value);

                if (file != null)
                {
                    FileList.Add(file);
                }
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async void CancelForm()
        {
            await Cancelled.InvokeAsync();
            StateHasChanged();
        }
        private async void SaveForm()
        {
            if (InputForm != null && InputForm.EditContext != null && InputForm.EditContext.Validate())
            {

                DataIsBusy = true;
                StateHasChanged();

                //Task Changed Email Stuff -----------------------------------------------
                //On Save Only
                TASK_Task? original = null;
                if (!string.IsNullOrEmpty(ID))
                {
                    original = await TaskProvider.GetTask(Guid.Parse(ID));
                }
                if (original != null)
                {
                    if(original.Title != Task.Title)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Title, new List<string>(){Task.Title}), false);
                    if(original.Description != Task.Description)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Description, new List<string>(){Task.Description}), false);
                    if(original.TASK_Bucket_ID != Task.TASK_Bucket_ID)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Bucket, new List<string>(){Task.TASK_Bucket_ID.ToString() ?? "Err"}), false);
                    if (original.TASK_Status_ID != Task.TASK_Status_ID)
                    {
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Status, new List<string>() { Task.TASK_Status_ID.ToString() ?? "Err" }), false);
                        V_TASK_Status? _selectedStatus = StatusList.FirstOrDefault(p => p.ID == Task.TASK_Status_ID);
                        if (_selectedStatus != null && _selectedStatus.CompleteTask)
                        {
                            Task.CompletedAt = DateTime.Now;
                        }
                    }
                    if (original.CompletedAt == null && Task.CompletedAt != null)
                    {
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.TaskChecked, new List<string>() { Task.CompletedAt.ToString() ?? "Err" }), false);

                        V_TASK_Status? _actualStatus = null;
                        if (Task.TASK_Status_ID != null)
                        {
                            _actualStatus = StatusList.FirstOrDefault(p => p.ID == Task.TASK_Status_ID.Value);
                        }
                        if (Task.TASK_Status_ID == null || _actualStatus == null || !_actualStatus.CompleteTask)
                        {
                            V_TASK_Status? _completationStatus = StatusList.FirstOrDefault(p => p.DefaultCompleteTask);
                            if (_completationStatus != null)
                            {
                                Task.TASK_Status_ID = _completationStatus.ID;
                                TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Status, new List<string>() { Task.TASK_Status_ID.ToString() }), false);
                            }
                        }
                    }
                    if(original.CompletedAt != null && Task.CompletedAt == null)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.TaskUnchecked, new List<string>(){""}), false);
                    if(original.TASK_Priority_ID != Task.TASK_Priority_ID)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Priority, new List<string>(){Task.TASK_Priority_ID.ToString() ?? "Err"}), false);
                    if(original.Deadline != Task.Deadline)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.Deadline, new List<string>(){Task.Deadline.ToString() ?? ""}), false);
                }
                //-----------------------------------------------------------------------
                
                if (Task.SortOrder == null || Task.SortOrder == 0)
                {
                    Task.SortOrder = await TaskService.GetTaskPosition(Task.TASK_Bucket_ID);
                }
                
                await TaskProvider.SetTask(Task);

                //behaviour of the component when data changes
                //Tags are instantly removed but only added on Save
                foreach (var tag in TaskTagList)
                {
                    if (tag != null)
                    {
                        await TaskProvider.SetTaskTag(tag);
                    }
                }
                
                //responsible employees are instantly removed but only added on 
                //Also they are disregarded for notification emails
                foreach (var responsible in TaskResponsibleList)
                {
                    if (responsible != null)
                    {
                        await TaskService.SetResponsible(responsible);
                    }
                }
                
                //Subtasks are instantly removed but only added on save
                foreach (var checkItem in TaskCheckItemsList)
                {
                    if (checkItem != null)
                    {
                        await TaskProvider.SetTaskCheckItem(checkItem);
                    }
                }
                
                //Comments are only added on save and cannot be removed
                foreach (var Item in TaskCommentList)
                {
                    if (Item != null)
                    {
                        await TaskProvider.SetTaskComment(Item);
                    }
                }
                
                //files removed immediately and added on save
                foreach (var file in FileList)
                {
                    if (TaskFilesList == null || !TaskFilesList.Select(p => p.FILE_FileInfo_ID).Contains(file.ID))
                    {
                        TASK_Task_Files newItem = new TASK_Task_Files()
                        {
                            ID = Guid.NewGuid(),
                            TASK_Task_ID = Task.ID,
                            FILE_FileInfo_ID = file.ID,
                            CreatedAt = DateTime.Now
                        };

                        await FileProvider.SetFileInfo(file);
                        await TaskProvider.SetTaskFiles(newItem);
                    }
                }

                //Escalations are disregarded for notification emails
                foreach (var esk in TaskEskalationList)
                {
                    if (esk != null)
                    {
                        await TaskProvider.SetEskalation(esk);

                        if (esk.TASK_Task_Eskalation_Responsible != null)
                        {
                            foreach (var resp in esk.TASK_Task_Eskalation_Responsible)
                            {
                                await TaskProvider.SetEskalationResponsible(resp);
                            }
                        }

                        await TaskProvider.SetEskalation(esk);
                    }
                }
                
                //Task Change Email Notification Stuff ----------------
                if (original != null)
                {
                    if(addedComments.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.CommentsAdded, addedComments.Select(e => e.ToString()).ToList()), false);
                    if(addedCheckItems.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.CheckItemsAdded, addedCheckItems.Select(e => e.ToString()).ToList()), false);
                    if(editedCheckItems.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.CheckItemsEdited, editedCheckItems.Select(e => e.ToString()).ToList()), false);
                    if(checkedCheckItems.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.CheckItemsChecked, checkedCheckItems.Select(e => e.ToString()).ToList()), false);
                    if(uncheckedCheckItems.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.CheckItemsUnchecked, uncheckedCheckItems.Select(e => e.ToString()).ToList()), false);
                    if(addedTags.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.TagsAdded, addedTags.Select(e => e.ToString()).ToList()), false);
                    if(addedResponsibles.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.ResponsibleAdded, addedResponsibles.Select(e => e.ToString()).ToList()), false);
                    if(addedFiles.Count > 0)
                        TaskService.ChangesCache.AddChange(original, new TaskChangedValue(ChangeType.FilesAdded, addedFiles.Select(e => e.ToString()).ToList()), false);
                }
                
                //-----------------------------------------------------
                await Saved.InvokeAsync(Task.ID);
            }

            StateHasChanged();
        }

        private async Task<bool> TagAddedEvent(TASK_Task_Tag tag)
        {
            addedTags.Add(tag.TASK_Tag_ID.Value);
            StateHasChanged();
            return true;
        }
        private async Task<bool> TagRemovedEvent(TASK_Task_Tag tag)
        {            
            bool removed = await TaskProvider.RemoveTaskTag(tag.ID);
            if (addedTags.All(e => e != tag.TASK_Tag_ID))
            {
                TaskService.ChangesCache.AddChange(Task, 
                    new TaskChangedValue(ChangeType.TagsRemoved, new List<string>(){tag.TASK_Tag_ID.Value.ToString()}), true);
            }
            else
            {
                addedTags.Remove(tag.TASK_Tag_ID.Value);
            }
            StateHasChanged();
            return true;
        }
        private async Task<bool> ResponsibleRemovedEvent(TASK_Task_Responsible Item)
        {
            await TaskProvider.RemoveTaskResponsible(Item.ID);
            if (addedResponsibles.All(e => e != Item.AUTH_Users_ID.Value))
            {
                TaskService.ChangesCache.AddChange(Task, 
                    new TaskChangedValue(ChangeType.ResponsibleRemoved, new List<string>(){Item.AUTH_Users_ID.Value.ToString()}), true);
            }
            else
            {
                addedResponsibles = addedResponsibles.Where(e => e != Item.ID).ToList();
            }
            StateHasChanged();

            return true;
        }

        private async Task<bool> ResponsibleAddedEvent(TASK_Task_Responsible Item)
        {
            addedResponsibles.Add(Item.AUTH_Users_ID.Value);
            return true;
        }
        private async Task<bool> EskalationRemovedEvent(TASK_Task_Eskalation Item)
        {
            await TaskProvider.RemoveTaskEskalation(Item.ID);
            StateHasChanged();

            return true;
        }
        private async Task<bool> EskalationAddEvent(TASK_Task_Eskalation Item)
        {
            StateHasChanged();
            return true;
        }
        
        private async Task<bool> CheckItemAddedEvent(TASK_Task_CheckItems Item)
        {
            addedCheckItems.Add(Item.ID);
            StateHasChanged();
            return true;
        }

        private async Task<bool> CheckItemChecked(TASK_Task_CheckItems item)
        {
            if (uncheckedCheckItems.All(e => e != item.ID))
            {
                checkedCheckItems.Add(item.ID);
            }
            else
            {
                uncheckedCheckItems.Remove(item.ID);
            }

            StateHasChanged();
            return true;
        }
        private async Task<bool> CheckItemUnchecked(TASK_Task_CheckItems item)
        {
            if (checkedCheckItems.All(e => e != item.ID))
            {
                uncheckedCheckItems.Add(item.ID);
            }
            else
            {
                checkedCheckItems.Remove(item.ID);
            }
            
            StateHasChanged();
            return true;
        }
        private async Task<bool> CheckItemEditedEvent(TASK_Task_CheckItems Item)
        {
            if (addedCheckItems.All(e => e != Item.ID) && !editedCheckItems.Any(e => e == Item.ID))
            {
                editedCheckItems.Add(Item.ID);
            }
            StateHasChanged();
            return true;
        }
        private async Task<bool> CheckItemRemovedEvent(TASK_Task_CheckItems Item)
        {
            if (editedCheckItems.Any(e => e == Item.ID))
            {
                editedCheckItems.Remove(Item.ID);
            }
            if (checkedCheckItems.Any(e => e == Item.ID))
            {
                checkedCheckItems.Remove(Item.ID);
            }
            if (uncheckedCheckItems.Any(e => e == Item.ID))
            {
                uncheckedCheckItems.Remove(Item.ID);
            }
            
            if (addedCheckItems.Any(e => e == Item.ID))
            {
                addedCheckItems.Remove(Item.ID);
            }
            else
            {
                TaskService.ChangesCache.AddChange(Task, new TaskChangedValue(ChangeType.CheckItemsRemoved, new List<string>(){Item.Description.ToString()})
                {
                    __ids = new List<string>(){Item.ID.ToString()}
                }, true);
            }
            await TaskProvider.RemoveTaskCheckItem(Item.ID);
            StateHasChanged();

            return true;
        }
        
        private async Task<bool> CommentAddedEvent(TASK_Task_Comment Item)
        {
            StateHasChanged();
            addedComments.Add(Item.ID);
            return true;
        }

        private void OnUploadFile(Guid id)
        {
            addedFiles.Add(id);
        }
        private async Task<bool> OnRemoveFile(Guid ID)
        {
            if (addedFiles.Contains(ID))
            {
                addedFiles.Remove(ID);
            }
            else
            {
                TaskService.ChangesCache.AddChange(Task, new TaskChangedValue(ChangeType.FilesRemoved, new List<string>(){ID.ToString()}), true);
            }
            
            if (TaskFilesList != null)
            {
                var taskFile = TaskFilesList.FirstOrDefault(p => p.FILE_FileInfo_ID != null && p.FILE_FileInfo_ID == ID);

                if (taskFile != null)
                {
                    await FileProvider.RemoveFileInfo(ID);
                    await TaskProvider.RemoveTaskFile(taskFile.ID);

                    TaskFilesList.Remove(taskFile);
                }
            }

            return true;
        }
    }
}
