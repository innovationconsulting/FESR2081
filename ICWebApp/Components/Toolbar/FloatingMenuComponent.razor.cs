using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Toolbar
{
    public partial class FloatingMenuComponent
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ILANGProvider LanguageProvider { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        private bool MenuVisibility = false;
        private bool FastAddVisibility = false;
        private bool BucketWindowVisibility = false;
        private int AllTasksCount = 0;
        private int MyTasksCount = 0;

        protected override void OnInitialized()
        {
            TaskService.OnContextChanged += TaskService_OnContextChanged;

            StateHasChanged();
            base.OnInitialized();
        }

        private async void TaskService_OnContextChanged()
        {
            await CheckVisibility();

            if (MenuVisibility && SessionWrapper.AUTH_Municipality_ID != null && TaskService.TASK_Context_ID != null && TaskService.ContextElementID != null)
            {
                var tasks = await TaskProvider.GetTaskList(SessionWrapper.AUTH_Municipality_ID.Value, LanguageProvider.GetCurrentLanguageID(), TaskService.TASK_Context_ID, TaskService.ContextElementID);

                if (tasks != null)
                {
                    AllTasksCount = tasks.Where(p => p.CompletedAt == null).Count();
                    MyTasksCount = tasks.Where(p => p.CompletedAt == null && p.ResponsibleUser != null && p.ResponsibleUser.ToLower().Contains(SessionWrapper.CurrentUser.ID.ToString().ToLower())).Count();

                }
            }

            StateHasChanged();
        }
        private async Task<bool> CheckVisibility()
        {
            bool visibility = true;

            if (TaskService.TASK_Context_ID == null)
            {
                visibility = false;
            }

            if (string.IsNullOrEmpty(TaskService.ContextElementID))
            {
                visibility = false;
            }

            if(TaskService.Context == null)
            {
                visibility = false;
            }

            if(TaskService.Context != null && TaskService.Context.Enabled == false)
            {
                visibility = false;
            }

            var municipalApps = await AUTHProvider.GetMunicipalityApps();

            if (!municipalApps.Select(p => p.APP_Application_ID).Contains(Applications.Tasks))
            {
                visibility = false;
            }

            if(TaskService.ShowToolbar == false)
            {
                visibility = false;
            }

            MenuVisibility = visibility;
            StateHasChanged();

            return true;
        }
        private void ShowFastAdd()
        {
            FastAddVisibility = true;
            StateHasChanged();
        }
        private void HideFastAdd()
        {
            FastAddVisibility = false;
            StateHasChanged();
        }
        private void FastAddSaved()
        {
            FastAddVisibility = false;
            StateHasChanged();
        }
        private void ShowSettings()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Tasks/Admin/Dashboard/" + TaskService.TASK_Context_ID.ToString());            
            StateHasChanged();
        }
        private void ShowBucketWindow()
        {
            BucketWindowVisibility = true;
            StateHasChanged();
        }
        private void HideBucketWindow()
        {
            BucketWindowVisibility = false;
            StateHasChanged();
        }
        private void GoToDashboard()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Tasks/Backend/Dashboard");
            StateHasChanged();
        }
    }
}
