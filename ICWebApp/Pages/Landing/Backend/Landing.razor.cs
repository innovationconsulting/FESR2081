using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Landing.Backend
{
    public partial class Landing : IDisposable
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IDASHProvider DashProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ISETTProvider SettProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        private System.Timers.Timer? _timer;
        private List<V_DASH_UserChat> UserChat = new List<V_DASH_UserChat>();
        private Guid? CurrentChatApplicationID = null;
        private bool ChatWindowVisible = false;
        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private List<Guid> AllowedAuthorities = new List<Guid>();
        private SETT_Municipal_Dashboard? DashboardSettings = new SETT_Municipal_Dashboard();
        private TelerikTileLayout TileLayoutInstance { get; set; }
        private bool TaskWindowVisible = false;
        private List<IEditorTool> Tools { get; set; } = new List<IEditorTool>()
        {
            new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
            new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
            new UnorderedList(),
            new EditorButtonGroup(new CreateLink(), new Unlink()),
            new InsertTable(),
            new DeleteTable(),
            new EditorButtonGroup(new AddRowBefore(), new AddRowAfter(), new DeleteRow(), new MergeCells(), new SplitCell())
        };
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font" };
        public List<V_TASK_Statistik_User_General?> UserStatistikGeneralList = new List<V_TASK_Statistik_User_General?>();
        public List<V_TASK_Statistik_User_Bucket?> UserStatistikBucketList = new List<V_TASK_Statistik_User_Bucket?>();
        public List<V_TASK_Statistik_User_Status?> UserStatistikStatusList = new List<V_TASK_Statistik_User_Status?>();
        public List<V_TASK_Statistik_User_Priority?> UserStatistikPriorityList = new List<V_TASK_Statistik_User_Priority?>();
        public List<V_TASK_Statistik_User?> UserStatistikList = new List<V_TASK_Statistik_User?>();
        public List<V_TASK_Context?> ContextList = new List<V_TASK_Context>();
        public V_TASK_Context? SelectedContext = null;
        private Guid Language = LanguageSettings.German;
        private bool BucketWindowVisibility = false;
        private List<V_TASK_Task>? UserTasks = null;


        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_LANDING");
            CrumbService.ClearBreadCrumb();

            UserChat = await GetChatData();

            Language = LangProvider.GetCurrentLanguageID();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                AuthorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null, null);
                AllowedAuthorities = await GetAuthorities();
                await GetAllStatistik();
                ContextList = (await TaskProvider.GetTaskContextList(Language, SessionWrapper.AUTH_Municipality_ID.Value)).ToList();
                SelectedContext = ContextList.OrderBy(p => p.SortOrder).FirstOrDefault();
                UserTasks = await TaskProvider.GetUncompletedVTasksForUser(SessionWrapper.CurrentUser.ID, LangProvider.GetCurrentLanguageID());
            }

            TaskProvider.OnStatistikChanged += Statistik_StatusChanged;
            TaskService.ShowToolbar = false;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async void Statistik_StatusChanged()
        {
            await GetAllStatistik().ConfigureAwait(false);
        }
        private async Task<bool> GetAllStatistik()
        {
            UserStatistikGeneralList = (await TaskProvider.GetStatistikByUserGeneral(Language, SessionWrapper.CurrentUser.ID)).ToList();
            UserStatistikBucketList = (await TaskProvider.GetStatistikByUserBucket(Language, SessionWrapper.CurrentUser.ID)).ToList();
            UserStatistikStatusList = (await TaskProvider.GetStatistikByUserStatus(Language, SessionWrapper.CurrentUser.ID)).ToList();
            UserStatistikPriorityList = (await TaskProvider.GetStatistikByUserPriority(Language, SessionWrapper.CurrentUser.ID)).ToList();
            UserStatistikList = (await TaskProvider.GetStatistikByUser(SessionWrapper.AUTH_Municipality_ID.Value, SessionWrapper.CurrentUser.ID, Language)).ToList();

            StateHasChanged();

            return true;
        }
        private async Task<bool> SetSettings(string ID)
        {
            if (DashboardSettings != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (DashboardSettings.SelectedAreasFilter != null && DashboardSettings.SelectedAreasFilter.Contains(ID))
                {
                    DashboardSettings.SelectedAreasFilter = DashboardSettings.SelectedAreasFilter.Replace(ID + ";", "");
                }
                else
                {
                    DashboardSettings.SelectedAreasFilter += ID + ";";
                }

                await SettProvider.SetDashboard(DashboardSettings);
                SaveState();
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            return true;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                _timer = new System.Timers.Timer(40000);
                _timer.Elapsed += CheckMessages;
                _timer.Enabled = true;
                _timer.AutoReset = true;

                if (SessionWrapper.CurrentUser != null)
                {
                    DashboardSettings = await SettProvider.GetDashboard(SessionWrapper.CurrentUser.ID);

                    if (DashboardSettings == null)
                    {
                        DashboardSettings = new SETT_Municipal_Dashboard();

                        DashboardSettings.ID = Guid.NewGuid();
                        DashboardSettings.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

                        string AuthoritiesIDs = "";

                        foreach (var auth in AllowedAuthorities)
                        {
                            AuthoritiesIDs += auth.ToString() + ";";
                        }

                        DashboardSettings.SelectedAreasFilter = AuthoritiesIDs;

                        if (TileLayoutInstance != null)
                        {
                            var state = TileLayoutInstance.GetState();
                            DashboardSettings.TileLayoutConf = JsonSerializer.Serialize(state);
                        }

                        await SettProvider.SetDashboard(DashboardSettings);
                    }
                }

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
            else
            {
                if (DashboardSettings != null && DashboardSettings.TileLayoutConf != null && TileLayoutInstance != null)
                {
                    TileLayoutState? storedState = JsonSerializer.Deserialize<TileLayoutState>(DashboardSettings.TileLayoutConf);
                    if (storedState != null)
                    {
                        TileLayoutInstance.SetState(storedState);
                    }
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private async void SaveState()
        {
            if (DashboardSettings != null && TileLayoutInstance != null)
            {
                var state = TileLayoutInstance.GetState();
                DashboardSettings.TileLayoutConf = JsonSerializer.Serialize(state);

                await SettProvider.SetDashboard(DashboardSettings);
            }

            StateHasChanged();
        }
        private async void CheckMessages(object? sender, System.Timers.ElapsedEventArgs e)
        {
            UserChat = await GetChatData();
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
        private async Task<List<V_DASH_UserChat>> GetChatData()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                return await DashProvider.GetDashUserChat(SessionWrapper.CurrentUser.ID);
            }

            return new List<V_DASH_UserChat>();
        }
        private async Task<List<Guid>> GetAuthorities()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                var userAuthorities = await AuthProvider.GetUserAuthorities(SessionWrapper.CurrentUser.ID);

                return userAuthorities.Where(p => p.AUTH_Authority_ID != null).Select(p => p.AUTH_Authority_ID.Value).ToList();
            }

            return new List<Guid>();
        }
        private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
        {
            var item = (V_DASH_UserChat)Args.Item; ;

            if (item != null && item.Application_ID != null)
            {
                ShowChat(item.Application_ID);
            }

            return true;
        }
        private void ShowChat(Guid ApplicationID)
        {
            CurrentChatApplicationID = ApplicationID;
            ChatWindowVisible = true;
            StateHasChanged();
        }
        private void HideChat()
        {
            CurrentChatApplicationID = null;
            ChatWindowVisible = false;
            StateHasChanged();
        }
        private void GoToApplication(Guid ApplicationID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/Form/Detail/" + ApplicationID);
            StateHasChanged();
        }
        private void ContextShowSubMenu(V_TASK_Context Context)
        {
            SelectedContext = Context;
            StateHasChanged();
        }
        private void UserShowSubMenu(V_TASK_Statistik_User Context)
        {
            Context.ShowSubContent = !Context.ShowSubContent;
            StateHasChanged();
        }
        private async void ShowBucketWindow(long? TASK_Context_ID, string ContextElementID, string ContextName)
        {
            //Method never used
            //SET DEFAULT DATE AND NOTIFYCREATOR (if used)
            await TaskService.SetContext(TASK_Context_ID, ContextElementID, ContextName, false, null);

            BucketWindowVisibility = true;
            StateHasChanged();
        }
        private void HideBucketWindow()
        {
            BucketWindowVisibility = false;
            StateHasChanged();
        }
        private async void TaskSaved(Guid TaskID)
        {
            if (UserStatistikList != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                UserStatistikList = (await TaskProvider.GetStatistikByUser(SessionWrapper.AUTH_Municipality_ID.Value, SessionWrapper.CurrentUser.ID, Language)).ToList();
                StateHasChanged();
            }
        }
        private void GoToTaskSeeAll()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Tasks/Backend/Dashboard");
            StateHasChanged();
        }
        public void Dispose()
        {
            TaskProvider.OnStatistikChanged -= Statistik_StatusChanged;
        }
    }   
}