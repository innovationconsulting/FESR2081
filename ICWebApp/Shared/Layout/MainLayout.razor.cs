using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Shared.Layout
{
    public partial class MainLayout
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        private bool CollapseMenu { get; set; } = true;
        private string SideBarCssClass => CollapseMenu ? "SideBarCollapse" : null;
        private string MainCssClass => CollapseMenu ? "main-page-large" : null;
        private AUTH_Municipality? Municipality { get; set; }

        private System.Timers.Timer _healthCheckTimer;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            SessionWrapper.PageIsPublic = false;
            StateHasChanged();

            SessionWrapper.OnInitialized += SessionWrapper_OnInitialized; ;

            SessionWrapper.OnPageTitleChanged += SessionWrapper_OnPageTitleChanged;
            SessionWrapper.OnPageSubTitleChanged += SessionWrapper_OnPageTitleChanged;
            EnviromentService.OnIsMobileChanged += EnviromentService_OnIsMobileChanged;

            _healthCheckTimer = new System.Timers.Timer(120000);
            _healthCheckTimer.Elapsed += _healthCheckTimer_Elapsed; ;
            _healthCheckTimer.Enabled = true;
            _healthCheckTimer.AutoReset = true;

            _healthCheckTimer.Start();

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            StateHasChanged();

            await base.OnInitializedAsync();
        }

        private void SessionWrapper_OnInitialized()
        {
            StateHasChanged();
        }
        private void SessionWrapper_OnPageTitleChanged()
        {
            StateHasChanged();
        }
        private void _healthCheckTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            AccountService.HealthCheck();
            MessageService.CallRequestRefresh();
        }
        private void EnviromentService_OnIsMobileChanged()
        {
            if (EnviromentService.IsMobile && CollapseMenu)
            {
                CollapseMenu = false;
                StateHasChanged();
            }
        }
        private void OnScreenClicked()
        {
            EnviromentService.NotifyOnScreenClicked();
        }
        private async void HandleLogout()
        {
            await AccountService.Logout();
            NavManager.NavigateTo("/");
        }
        private void ReturnToStart()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/Landing");
            StateHasChanged();
        }
    }
}
