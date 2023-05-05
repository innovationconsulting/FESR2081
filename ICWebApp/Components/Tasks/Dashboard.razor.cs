using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Tasks
{
    public partial class Dashboard
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }

        private V_TASK_Context? CurrentContext;
        private List<V_TASK_Context?> ContextList = new List<V_TASK_Context?>();
        private List<V_TASK_Statistik_Dashboard?> TaskList = new List<V_TASK_Statistik_Dashboard?>();
        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private List<Guid> AllowedAuthorities = new List<Guid>();
        private bool BucketWindowVisibility = false;
        private Guid Language = LanguageSettings.German;
        private bool ShowCompleted = false;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("TASK_BACKEND_DASHBOARD");

            CrumbService.ClearBreadCrumb();

            TaskService.ShowToolbar = false;
            Language = LangProvider.GetCurrentLanguageID();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                ContextList = (await TaskProvider.GetTaskContextList(LangProvider.GetCurrentLanguageID(), SessionWrapper.AUTH_Municipality_ID.Value)).ToList();
                AuthorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null, null);

                var userAuthorities = await AuthProvider.GetUserAuthorities(SessionWrapper.CurrentUser.ID);

                AllowedAuthorities = userAuthorities.Where(p => p.AUTH_Authority_ID != null).Select(p => p.AUTH_Authority_ID.Value).ToList();

                if (ContextList != null)
                {
                    await OnContextChange(ContextList.OrderBy(p => p.SortOrder).FirstOrDefault());
                }
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> OnContextChange(V_TASK_Context item)
        {
            CurrentContext = item;

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                TaskList = (await TaskProvider.GetTaskDashboard(item.ID, SessionWrapper.AUTH_Municipality_ID.Value)).ToList();
            }

            ShowCompleted = false;

            StateHasChanged();

            return true;
        }
        private async void ShowBucketWindow(V_TASK_Task task)
        {
            //SET DEFAULT DATE (this dashboard is no longer used anyway)
            await TaskService.SetContext(task.TASK_Context_ID, task.ContextElementID, task.ContextName, task.NotifyCreator ?? false, null);

            BucketWindowVisibility = true;
            StateHasChanged();
        }
        private async void TaskSaved(V_TASK_Statistik_Dashboard Item, Guid TaskID)
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Item.TaskList = (await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, Language, Item.TASK_Context_ID, Item.ContextElementID)).ToList();
                StateHasChanged();
            }
        }
        private async Task<bool> ShowTaskList(V_TASK_Statistik_Dashboard Item)
        {
            if (Item.TaskList == null && SessionWrapper.AUTH_Municipality_ID != null) 
            {
                Item.TaskList = (await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, Language, Item.TASK_Context_ID, Item.ContextElementID)).ToList();
            }

            Item.ShowSubContent = !Item.ShowSubContent;

            StateHasChanged();
            return true;
        }
        private void HideBucketWindow()
        {
            BucketWindowVisibility = false;
            StateHasChanged();
        }
        private void OnShowCompleted()
        {
            ShowCompleted = true;
            StateHasChanged();
        }
    }
}
