using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace ICWebApp.Shared.Layout
{
    public partial class MyCivisLayout
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IRoomBookingHelper RoomBookingHelper { get; set; }

        private System.Timers.Timer _healthCheckTimer;
        private AUTH_Municipality? Municipality { get; set; }

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            SessionWrapper.PageIsPublic = true;

            StateHasChanged();

            SessionWrapper.OnPageTitleChanged += OnValueChanged;
            SessionWrapper.OnPageSubTitleChanged += OnValueChanged;
            AnchorService.OnAnchorChanged += AnchorService_OnAnchorChanged;
            AnchorService.OnFoceShowChanged += AnchorService_OnAnchorChanged;
            EnviromentService.OnIsMobileChanged += EnviromentService_OnIsMobileChanged;
            NavManager.LocationChanged += NavManager_LocationChanged;
            BusyIndicatorService.OnBusyIndicatorChanged += BusyIndicatorService_OnBusyIndicatorChanged;

            _healthCheckTimer = new System.Timers.Timer(120000);
            _healthCheckTimer.Elapsed += _healthCheckTimer_Elapsed;
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

        private void BusyIndicatorService_OnBusyIndicatorChanged(bool obj)
        {
            StateHasChanged();
        }

        private async void NavManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            try
            {
                RoomBookingHelper.ShowBookingSideBar = false;
                EnviromentService.ShowMunicipalPanoramic = false;
                EnviromentService.LayoutAdditionalCSS = "";
                await EnviromentService.ScrollToTop();
            }
            catch { }
        }
        private void EnviromentService_OnIsMobileChanged()
        {
            StateHasChanged();
        }
        private void AnchorService_OnAnchorChanged()
        {
            StateHasChanged();
        }
        private void _healthCheckTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            AccountService.HealthCheck();
            MessageService.CallRequestRefresh();
        }
        private void OnValueChanged()
        {
            StateHasChanged();
        }
    }
}
