using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Tasks
{
    public partial class RowTask
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }

        [Parameter] public EventCallback<Guid> Saved { get; set; }
        [Parameter] public EventCallback Onclick { get; set; }
        [Parameter] public Guid TaskID { get; set; }
        [Parameter] public string Class { get; set; }

        private V_TASK_Task? Task { get; set; }
        private List<V_TASK_Bucket?> BucketList { get; set; }
        private List<V_TASK_Status?> StatusList { get; set; }
        private List<V_TASK_Tag?> TagList { get; set; }
        private List<V_TASK_Priority?> PriorityList { get; set; }
        private List<TASK_Task_Tag?> TaskTagList { get; set; }
        private List<TASK_Task_Comment?> TaskCommentList { get; set; }
        private List<TASK_Task_Responsible?> TaskResponsibleList { get; set; }
        private List<TASK_Task_Eskalation?> TaskEskalationList { get; set; }
        private List<TASK_Task_CheckItems?> TaskCheckItemsList { get; set; }
        private List<TASK_Task_Files?> TaskFilesList { get; set; }
        private List<FILE_FileInfo?> FileList { get; set; }
        private bool HasComments = false;
        private bool HasFiles = false;
        private bool HasTags = false;
        private bool HasCheckItems = false;
        private bool HasResponsible = false;
        private bool HasEskalation = false;

        protected override async Task OnInitializedAsync()
        {
            Task = await TaskProvider.GetVTask(TaskID, LangProvider.GetCurrentLanguageID());

            if (Task != null)
            {
                BucketList = await TaskService.GetBucketList(Task.TASK_Context_ID, true);
                StatusList = await TaskService.GetStatusList(Task.TASK_Context_ID, true);
                TagList = await TaskService.GetTagList(Task.TASK_Context_ID, true);
                PriorityList = await TaskService.GetPriorityList(Task.TASK_Context_ID, true);

                await GetData();
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            TaskTagList = (await TaskProvider.GetTaskTagList(Task.ID)).ToList();
            TaskCheckItemsList = (await TaskProvider.GetTaskCheckItemsList(Task.ID)).ToList();
            TaskCommentList = (await TaskProvider.GetTaskCommentList(Task.ID)).ToList();
            TaskResponsibleList = (await TaskProvider.GetTaskResponsibleList(Task.ID)).ToList();
            TaskEskalationList = (await TaskProvider.GetTaskEskalationList(Task.ID)).ToList();

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
                    item.CompletedAt = null;

                    await TaskProvider.SetTask(item);

                    await Saved.InvokeAsync(Task.ID);
                }

                StateHasChanged();
            }

            StateHasChanged();
        }
    }
}
