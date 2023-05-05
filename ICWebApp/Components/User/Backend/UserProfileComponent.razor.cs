using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace ICWebApp.Components.User.Backend
{
    public partial class UserProfileComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IAUTHSettingsProvider AuthSettingsProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
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

            CurrentLanguage = LangProvider.GetLanguageByCode(CultureInfo.CurrentCulture.Name);

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async void EnviromentService_OnScreenClicked()
        {
            if (ShowUserMenu && PopUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("user-popup-menu");

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
        private async void ToggleLanguage()
        {
            if (CurrentLanguage != null && CurrentLanguage.Code == "de-DE")
            {
                await LangProvider.SetLanguage("it-IT");
            }
            else
            {
                await LangProvider.SetLanguage("de-DE");
            }

            StateHasChanged();
        }
    }
}
