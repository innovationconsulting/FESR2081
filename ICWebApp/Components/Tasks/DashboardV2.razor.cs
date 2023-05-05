using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Syncfusion.Blazor.DropDowns;


namespace ICWebApp.Components.Tasks
{
    public partial class DashboardV2
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        
        private V_TASK_Context? CurrentContext;
        private List<AUTH_Authority> SelectedAuthorities = new List<AUTH_Authority>();

        private List<V_TASK_Context?> ContextList = new List<V_TASK_Context?>();
        private List<V_TASK_Statistik_Dashboard> TaskList = new List<V_TASK_Statistik_Dashboard?>();
        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private bool BucketWindowVisibility = false;
        private Guid Language = LanguageSettings.German;
        private bool ShowCompleted = false;
        
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("TASK_BACKEND_DASHBOARD");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Tasks/Backend/Dashboard", "BREADCRUMB_TASK_DASHBOARD", null, null, true);

            TaskService.ShowToolbar = false;
            Language = LangProvider.GetCurrentLanguageID();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                ContextList = (await TaskProvider.GetTaskContextList(LangProvider.GetCurrentLanguageID(), SessionWrapper.AUTH_Municipality_ID.Value)).ToList();
                AuthorityList = await AuthProvider.GetAlllowedAuthoritiesByUser(SessionWrapper.CurrentUser.ID);
                
                if (ContextList != null)
                {
                    await OnContextChange(ContextList.OrderBy(p => p.SortOrder).FirstOrDefault(), true);
                }
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        
        private async Task<bool> OnContextChange(V_TASK_Context item, bool onInitialized = false)
        {
            CurrentContext = item;

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                if (CurrentContext.ID == 1)
                {
                    if (onInitialized)
                    {
                        SelectedAuthorities.AddRange(AuthorityList);
                    }

                    var authorities = SelectedAuthorities.Select(e => e.ID).ToList();
                    TaskList = await TaskProvider.GetTaskDashboardForAuthorities(item.ID, authorities,
                        SessionWrapper.AUTH_Municipality_ID.Value, false);
                }
                else
                {
                    TaskList = (await TaskProvider.GetTaskDashboard(item.ID, SessionWrapper.AUTH_Municipality_ID.Value)).ToList();
                }
            }

            ShowCompleted = false;

            StateHasChanged();

            return true;
        }

        private async Task<bool> OnAuthorityChanged(AUTH_Authority? authority)
        {
            if (SelectedAuthorities.Count == AuthorityList.Count)
            {
                SelectedAuthorities.Clear();
            }
            if (CurrentContext != null && authority != null)
            {
                if (SelectedAuthorities.Any(e => e.ID == authority.ID))
                {
                    SelectedAuthorities = SelectedAuthorities.Where(e => e.ID != authority.ID).ToList();
                }
                else
                {
                    SelectedAuthorities.Add(authority);
                }
                TaskList = await TaskProvider.GetTaskDashboardForAuthorities(CurrentContext.ID, SelectedAuthorities.Select(e => e.ID).ToList(),
                    SessionWrapper.AUTH_Municipality_ID.Value, false);
            }
            StateHasChanged();
            return true;
        }
        private async Task OnAuthorityDropdownChanged(ChangeEventArgs<AUTH_Authority?, AUTH_Authority?> args)
        {
            await OnAuthorityChanged(args.ItemData);
        }
        
        private async void ShowBucketWindow(V_TASK_Statistik_Dashboard? item)
        {
            if (item != null)
            {
                item.TaskList = (await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, Language, item.TASK_Context_ID, item.ContextElementID)).ToList();
                var task = item.TaskList.FirstOrDefault();
                var defaultDeadline = await TaskProvider.DeferTaskDefaultDeadlineFromContextElementId(task.TASK_Context_ID.Value, task.ContextElementID);
                await TaskService.SetContext(task.TASK_Context_ID, task.ContextElementID, task.ContextName, task.NotifyCreator ?? false, defaultDeadline);

                BucketWindowVisibility = true;
                StateHasChanged();
            }
        }
        private void HideBucketWindow()
        {
            BucketWindowVisibility = false;
            StateHasChanged();
        }

        private string GetContextElementColumnTitle()
        {
            if (CurrentContext != null)
            {
                switch (CurrentContext.ID)
                {
                    case 1:
                        return TextProvider.GetOrCreate("GRID_COLUMN_FORM");
                    case 2:
                        return TextProvider.GetOrCreate("GRID_COLUMN_MAINTENANCE");
                }
            }
            return TextProvider.Get("GRID_COLUMN_FORM");
        }

        private void OnTaskDeleted(Tuple<string, int> contextElementAndTaskIds)
        {
            if (TaskList != null && contextElementAndTaskIds.Item2 == 0)
            {
                TaskList = TaskList.Where(e => e.ContextElementID != contextElementAndTaskIds.Item1).ToList();
            }
            StateHasChanged();
        }

        private async void ToggleAllAuthorities()
        {
            if (CurrentContext != null && CurrentContext.ID == 1)
            {
                if (SelectedAuthorities.Count == AuthorityList.Count)
                {
                    SelectedAuthorities.Clear();
                }
                else
                {
                    SelectedAuthorities.Clear();
                    SelectedAuthorities.AddRange(AuthorityList);
                }
                TaskList = await TaskProvider.GetTaskDashboardForAuthorities(CurrentContext.ID, SelectedAuthorities.Select(e => e.ID).ToList(),
                    SessionWrapper.AUTH_Municipality_ID.Value, false);
                StateHasChanged();
            }
            
        }
    }
}
