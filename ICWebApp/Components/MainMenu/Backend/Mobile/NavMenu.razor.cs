using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace ICWebApp.Components.MainMenu.Backend.Mobile
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
        public List<CONF_MainMenu> MainMenuList { get; set; }

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.OnCurrentUserChanged += SessionWrapper_ValueChanged;

            await LoadMenu();

            await base.OnInitializedAsync();
        }

        private async void SessionWrapper_ValueChanged()
        {
            await LoadMenu();
            StateHasChanged();
        }

        private async Task<bool> LoadMenu()
        {
            var menu = await ConfProvider.GetLoggedInMainMenuElements();

            if (menu != null)
            {
                if (NavManager.BaseUri.Contains("localhost"))
                {
                    MainMenuList = menu;
                }
                else if (NavManager.BaseUri.Contains("192.168.77"))
                {
                    MainMenuList = menu;
                }
                else
                {
                    MainMenuList = menu.Where(p => p.InDevelopment == false).ToList();
                }
            }

            StateHasChanged();
            return true;
        }
    }
}
