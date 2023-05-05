using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Shared.Layout
{
    public partial class FrontendLayout
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
        private List<AUTH_MunicipalityApps>? AktiveApps = new List<AUTH_MunicipalityApps>();

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            SessionWrapper.PageIsPublic = true;
            SessionWrapper.OnInitialized += SessionWrapper_OnInitialized;
            StateHasChanged();

            SessionWrapper.OnPageTitleChanged += OnValueChanged;
            SessionWrapper.OnPageSubTitleChanged += OnValueChanged;
            SessionWrapper.OnAUTHMunicipalityIDChanged += SessionWrapper_OnAUTHMunicipalityIDChanged1;
            AnchorService.OnAnchorChanged += AnchorService_OnAnchorChanged;
            AnchorService.OnFoceShowChanged += AnchorService_OnAnchorChanged;
            EnviromentService.OnIsMobileChanged += EnviromentService_OnIsMobileChanged;
            EnviromentService.OnShowPanoramicChanged += EnviromentService_OnShowPanoramicChanged;
            EnviromentService.OnLayoutAdditionalCSSChanged += EnviromentService_OnShowPanoramicChanged;
            NavManager.LocationChanged += NavManager_LocationChanged;
            RoomBookingHelper.OnShowBookingChanged += RoomBookingHelper_OnShowBookingChanged;

            _healthCheckTimer = new System.Timers.Timer(120000);
            _healthCheckTimer.Elapsed += _healthCheckTimer_Elapsed;
            _healthCheckTimer.Enabled = true;
            _healthCheckTimer.AutoReset = true;

            _healthCheckTimer.Start();

            SessionWrapper.OnAUTHMunicipalityIDChanged += SessionWrapper_OnAUTHMunicipalityIDChanged;

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);         
            }

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                AktiveApps = await AuthProvider.GetMunicipalityApps();
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private void RoomBookingHelper_OnShowBookingChanged()
        {
            StateHasChanged();
        }
        private void EnviromentService_OnShowPanoramicChanged()
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
                SessionWrapper.PageButtonAction = null;
                SessionWrapper.PageButtonActionTitle = null;
                SessionWrapper.PageSubTitle = null;
                SessionWrapper.ShowTitleSepparation = true;
                await EnviromentService.ScrollToTop();
            }
            catch { }
        }
        private void SessionWrapper_OnAUTHMunicipalityIDChanged1()
        {
            StateHasChanged();
        }
        private async void SessionWrapper_OnAUTHMunicipalityIDChanged()
        {
            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                if (Municipality != null && Municipality.PanoramicImage != null)
                {
                    if (!Directory.Exists("D:/Comunix/NewsImages"))
                    {
                        Directory.CreateDirectory("D:/Comunix/NewsImages");
                    }

                    if (!File.Exists("D:/Comunix/NewsImages/Panormaic_" + Municipality.Name + ".webp"))
                    {
                        File.WriteAllBytes("D:/Comunix/NewsImages/Panormaic_" + Municipality.Name + ".webp", Municipality.PanoramicImage);
                    }
                }
            }
        }
        private void SessionWrapper_OnInitialized()
        {
            StateHasChanged();
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
