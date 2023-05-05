using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.MainMenu.Backend.Desktop
{
    public partial class NavMenu
    {
        [Parameter] public bool CollapsedSideBar { get; set; }
        [Inject] public ICONFProvider ConfProvider { get; set; }
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] public NavigationManager NavManager { get; set; }
        [Inject] public IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        private List<CONF_MainMenu>? MainMenuList;
        private AUTH_Municipality? Municipality { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await LoadMenu();

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            await base.OnInitializedAsync();
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
        private void LogoClicked()
        {
            NavManager.NavigateTo("/Backend/Landing");
        }
    }
}
