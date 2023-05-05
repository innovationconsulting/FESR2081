using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;


namespace ICWebApp.Components.Tasks.DashboardComponents
{
    public partial class TaskListDetailTemplate
    {
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private ITASKProvider TaskProvider { get; set; }
        [Inject] private ILANGProvider LangProvider { get; set; }
        [Inject] private ITEXTProvider TextProvider { get; set; }
        [Inject] private ITASKService TaskService { get; set; }
        [Parameter] public V_TASK_Statistik_Dashboard? TaskContext { get; set; }
        [Parameter] public List<V_TASK_Task>? Tasks { get; set; }
        [Parameter] public bool StartInListView { get; set; } = false;
        [Parameter] public bool ShowCompletedAtBeginning { get; set; } = false;
        [Parameter] public bool DisplayContextInSmallTasks { get; set; } = false;
        [Parameter] public EventCallback<Tuple<string,int>> OnTaskDeleted { get; set; }

        private bool _asListView = false;
        private bool _showCompleted = false;

        //Edit window
        private bool _editWindowVisible = false;
        private string _itemToEditId = "";
        
        protected override async Task OnInitializedAsync()
        {
            if (TaskContext != null)
            {
                Tasks = (await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, LangProvider.GetCurrentLanguageID(), TaskContext.TASK_Context_ID, TaskContext.ContextElementID)).ToList();
            }
            _asListView = StartInListView;
            _showCompleted = ShowCompletedAtBeginning;
            StateHasChanged();
        }

        private async Task<bool> EditTask(V_TASK_Task task)
        {
            //Setup TaskService Context
            await TaskService.SetContext(task.TASK_Context_ID, task.ContextElementID, task.ContextName, task.NotifyCreator ?? false, null);
            _itemToEditId = task.ID.ToString();
            _editWindowVisible = true;
            return true;
        }

        private async Task<bool> TaskDeleted(Guid id)
        {
            if (Tasks != null)
            {
                var task = Tasks.FirstOrDefault(e => e.ID == id);
                Tasks = Tasks.Where(e => e.ID != id).ToList();
                if (task != null)
                {
                    await OnTaskDeleted.InvokeAsync(new Tuple<string, int>(task.ContextElementID, Tasks.Count));
                }
                StateHasChanged();
            }
            return true;
        }

        private async Task<bool> TaskSaved(Guid id)
        {
            _editWindowVisible = false;
            var index = Tasks.IndexOf(Tasks.FirstOrDefault(e => e.ID == id));
            if (index >= 0)
            {
                Tasks[index] = await TaskProvider.GetVTask(id, LangProvider.GetCurrentLanguageID());
            }
            StateHasChanged();
            return true;
        }

        private async Task<bool> EditCanceled()
        {
            _editWindowVisible = false;
            StateHasChanged();
            return true;
        }
    }
}
