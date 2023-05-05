using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Components.Global;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace ICWebApp.Components.User.Backend
{
    public partial class UserSupportComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IAUTHSettingsProvider AuthSettingsProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IFreshDeskService FreshDeskService { get; set; }
        [Inject] ICONFProvider ConfProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public bool IsCollapsed { get; set; }

        private AUTH_Municipality? Municipality { get; set; }
        private bool PopUpAktivated = false;
        private LANG_Languages CurrentLanguage;
        private AUTH_UserSettings? UserSettings { get; set; }
        private bool ShowUserMenu = false;
        private string ShowUserMenuCSS
        {
            get
            {
                if (ShowUserMenu)
                    return "nav-item-backend-active";

                return "";
            }
        }
        private FreshDeskTicket Ticket = new FreshDeskTicket();
        private string? TicketMessageError { get; set; }
        private string? TicketMessageSuccess { get; set; }
        private List<V_CONF_Freshdesk_Priority>? FreshdeskPriority { get; set; }

        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                UserSettings = await AuthSettingsProvider.GetSettings(SessionWrapper.CurrentUser.ID);

                if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
                {
                    Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
                }
            }

            FreshdeskPriority = await ConfProvider.GetVPriorityList();
            CurrentLanguage = LangProvider.GetLanguageByCode(CultureInfo.CurrentCulture.Name);

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                Ticket.Email = SessionWrapper.CurrentUser.Email;
            }

            Ticket.Priority = 1;

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async void EnviromentService_OnScreenClicked()
        {
            if (ShowUserMenu && PopUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("user-support-menu");

                if (!onScreen)
                {
                    ToggleUserMenu();
                }
            }
            else
            {
                PopUpAktivated = true;
            }
        }
        private void ToggleUserMenu()
        {
            ShowUserMenu = !ShowUserMenu;

            if (ShowUserMenu)
            {
                PopUpAktivated = false;
            }

            StateHasChanged();
        }
        private async void SendTicket()
        {
            TicketMessageError = null;
            TicketMessageSuccess = null;

            if (Ticket != null)
            {
                var result = await FreshDeskService.CreateTicket(Ticket);

                if (result != null)
                {
                    TicketMessageSuccess = TextProvider.Get("TICKET_COMMIT_SUCCESS");
                    Ticket = new FreshDeskTicket();

                    if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
                    {
                        Ticket.Email = SessionWrapper.CurrentUser.Email;
                    }

                    Ticket.Priority = 1;
                }
                else
                {
                    TicketMessageError = TextProvider.Get("TICKET_COMMIT_ERROR");
                }
            }

            StateHasChanged();
        }
        private void ShowPrivacy()
        {
            if (!NavManager.Uri.Contains("/Backend/Privacy"))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Backend/Privacy");
                StateHasChanged();
            }
        }
    }
}
