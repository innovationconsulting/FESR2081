using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using Microsoft.JSInterop;

namespace ICWebApp.Components.MainMenu.Frontend
{
    public partial class NavMenu
    {
        [Inject] ICONFProvider ConfProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IJSRuntime JsRuntime { get; set; }
        public List<CONF_MainMenu> MainMenuList { get; set; }
        private AUTH_Municipality? Municipality { get; set; }

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.OnCurrentUserChanged += SessionWrapper_ValueChanged;
            SessionWrapper.OnAUTHMunicipalityIDChanged += SessionWrapper_OnAUTHMunicipalityIDChanged;

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            await LoadMenu();
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void SessionWrapper_OnAUTHMunicipalityIDChanged()
        {
            try
            {
                await LoadMenu();
                StateHasChanged();
            }
            catch { }
        }
        private async void SessionWrapper_ValueChanged()
        {
            try
            {
                await LoadMenu();
                StateHasChanged();
            }
            catch { }
        }

        private async void CloseNavMenu()
        {
            await JsRuntime.InvokeVoidAsync("navmenu_ToggleVisibility");
        }
        private async Task<bool> LoadMenu()
        {
            try
            {
                List<CONF_MainMenu> locMenu = new List<CONF_MainMenu>();
                var menu = await ConfProvider.GetPublicMainMenuElements();

                if (menu != null)
                {
                    if (NavManager.BaseUri.Contains("localhost"))
                    {
                        locMenu = menu;
                    }
                    else if (NavManager.BaseUri.Contains("192.168.77"))
                    {
                        locMenu = menu;
                    }
                    else
                    {
                        locMenu = menu.Where(p => p.InDevelopment == false).ToList();
                    }
                }

                if (SessionWrapper.CurrentUser != null && AUTHProvider.HasUserRole(AuthRoles.Citizen)) //CITIZEN
                {
                    var menuCitizen = await ConfProvider.GetCitizenMainMenuElements();

                    if (menuCitizen != null)
                    {
                        if (NavManager.BaseUri.Contains("localhost"))
                        {
                            locMenu = locMenu.Union(menuCitizen).ToList();
                        }
                        else if (NavManager.BaseUri.Contains("192.168.77"))
                        {
                            locMenu = locMenu.Union(menuCitizen).ToList();
                        }
                        else
                        {
                            locMenu = locMenu.Union(menuCitizen.Where(p => p.InDevelopment == false).ToList()).ToList();
                        }
                    }
                }

                MainMenuList = locMenu;
                StateHasChanged();
            }
            catch { }
            return true;
        }
    }
}
