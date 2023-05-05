using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;

namespace ICWebApp.Components.Messaging.Backend
{
    public partial class SystemNotificationComponent
    {
        [Inject] public IMSGProvider MsgProvider { get; set; }
        [Inject] public ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }

        private string ShowMenuCSS
        {
            get
            {
                if (_showMenu)
                    return "nav-item-backend-active";

                return "";
            }
        }

        public ObservableCollection<MSG_SystemNotifications> SystemNotifications = new ObservableCollection<MSG_SystemNotifications>();
        private System.Timers.Timer? _timer;
        private bool _showMenu = true;
        private bool _popUpAktivated = true;
        private bool _timerRunning = false;
        private string _priorityClass = "";

        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            await GetData();

            StateHasChanged();

            _timer = new System.Timers.Timer(60000);
            _timer.Elapsed += CheckNotifications;
            _timer.Enabled = true;
            _timer.AutoReset = true;

            await base.OnInitializedAsync();
        }
        private async void EnviromentService_OnScreenClicked()
        {
            if (_showMenu && _popUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("user-popup-menu");

                if (!onScreen)
                {
                    ToggleMenu();
                }
            }
            else
            {
                _popUpAktivated = true;
            }
        }
        private void ToggleMenu()
        {
            _showMenu = !_showMenu;

            if (_showMenu)
            {
                _popUpAktivated = false;
            }

            StateHasChanged();
        }
        private async void CheckNotifications(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_timerRunning)
            {
                try
                {
                    _timerRunning = true;

                    await GetData();
                    await InvokeAsync(StateHasChanged);

                    _timerRunning = false;
                }
                catch { }
            }
        }
        private async Task<bool> GetData()
        {
            var remoteData = await MsgProvider.GetSystemNotifications();

            if (remoteData != null)
            {
                foreach (var remoteNotification in remoteData)
                {
                    if (!SystemNotifications.Select(p => p.ID).Contains(remoteNotification.ID))
                    {
                        SystemNotifications.Add(remoteNotification);
                    }
                }

                var ItemsToRemove = new List<MSG_SystemNotifications>();

                foreach (var localNotification in SystemNotifications)
                {
                    if (!remoteData.Select(p => p.ID).Contains(localNotification.ID))
                    {
                        ItemsToRemove.Add(localNotification);
                    }
                }

                foreach (var itemToRemove in ItemsToRemove)
                {
                    SystemNotifications.Remove(itemToRemove);
                }

            }
            else
            {
                SystemNotifications.Clear();
            }
            await UpdateNotificationDropdown();

            return true;
        }
        private async Task UpdateNotificationDropdown()
        {
            MSG_SystemNotifications? _SystemNotifications = SystemNotifications.OrderBy(p => p.MSG_SystemMessageTypes.Priority).FirstOrDefault();
            if (_SystemNotifications != null && _SystemNotifications.MSG_SystemMessageTypes != null)
            {
                _priorityClass = "ntf_" + _SystemNotifications.MSG_SystemMessageTypes.Name.Trim().ToLower();
                StateHasChanged();
            }
            else
            {
                _priorityClass = String.Empty;
            }
        }
    }
}