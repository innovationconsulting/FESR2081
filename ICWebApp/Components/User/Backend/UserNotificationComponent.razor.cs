using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.User.Backend
{
    public partial class UserNotificationComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IMessageService Messageservice { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IMSGProvider MSGProvider { get; set; }

        private bool ShowPopupMenu = false;
        private bool PopUpAktivated = false;
        private int MessagesToRead { get; set; }
        private List<MSG_Message> Messages = new List<MSG_Message>();
        private MSG_Message? CurrentMessage;
        private string ShowPopupMenuCSS
        {
            get
            {
                if (ShowPopupMenu)
                    return "nav-item-backend-active";

                return "";
            }
        }
        private bool WindowVisible { get; set; }

        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            await LoadData();

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private void ToggleMenu()
        {
            ShowPopupMenu = !ShowPopupMenu;
            StateHasChanged();
        }
        private void HideMenu()
        {
            ShowPopupMenu = false;
            StateHasChanged();
        }
        private async void EnviromentService_OnScreenClicked()
        {
            if (ShowPopupMenu && PopUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("notification-popup-menu");

                if (!onScreen)
                {
                    HideMenu();
                }
            }
            else
            {
                PopUpAktivated = true;
            }
        }
        private async Task<bool> LoadData()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                var m = await Messageservice.GetMessagesToReadCount(SessionWrapper.CurrentUser.ID);
                MessagesToRead = m;

                var MessageList = await Messageservice.GetMessages(SessionWrapper.CurrentUser.ID, 10);
                Messages = MessageList.ToList();
            }

            return true;
        }
        private void ShowAllMessages()
        {
            ShowPopupMenu = false;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/MessageCommunications");
        }
        private void GoToMessage(MSG_Message? Message)
        {
            if (Message != null)
            {
                Message.FirstReadDate = DateTime.Now;

                MSGProvider.SetMessage(Message);

                if (Message.Link != null)
                {
                    ShowPopupMenu = false;

                    if (NavManager.BaseUri.Contains("localhost"))
                    {
                        Message.Link = Message.Link.Replace("https://test.comunix.bz.it/", "https://localhost:7149/");
                    }

                    if (NavManager.Uri != Message.Link)
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo(Message.Link);
                    }

                    StateHasChanged();
                }
                else
                {
                    ShowPopupMenu = false;
                    if (!NavManager.Uri.Contains("/Backend/MessageCommunications"))
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo("/Backend/MessageCommunications");
                    }
                    StateHasChanged();
                }
            }
        }
        private void ShowMessage(MSG_Message? Message)
        {
            if (Message != null)
            {
                CurrentMessage = Message;
                WindowVisible = true;
                StateHasChanged();
            }
        }
        private async void SetAllasRead()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                var messages = await Messageservice.GetMessagesToRead(SessionWrapper.CurrentUser.ID, null);

                foreach (var m in messages)
                {
                    m.FirstReadDate = DateTime.Now;

                    await MSGProvider.SetMessage(m);
                }

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
    }
}
