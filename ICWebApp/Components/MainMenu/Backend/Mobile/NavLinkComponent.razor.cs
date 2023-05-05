using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace ICWebApp.Components.MainMenu.Backend.Mobile
{
    public partial class NavLinkComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICONFProvider ConfProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] INavMenuHelper NavMenuHelper { get; set; }
        [Parameter] public CONF_MainMenu MenuItem { get; set; }
        private List<CONF_MainMenu> SubMenuItems { get; set; }
        private bool SubMenuVisible { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            if (MenuItem != null)
            {
                await LoadSubMenu();
            }

            NavMenuHelper.OnChange += NavMenuHelper_OnChange;
            NavManager.LocationChanged += NavManager_LocationChanged;

            await base.OnInitializedAsync();
            StateHasChanged();
        }

        private void NavMenuHelper_OnChange()
        {
            SubMenuVisible = false;
            StateHasChanged();
        }

        private void NavManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            SubMenuVisible = false;
            StateHasChanged();
        }
        private async Task<bool> LoadSubMenu()
        {
            List<CONF_MainMenu> locMenu = new List<CONF_MainMenu>();

            var menuBackend = await ConfProvider.GetLoggedInSubMenuElements(MenuItem.ID);

            if (menuBackend != null)
            {
                if (NavManager.BaseUri.Contains("localhost"))
                {
                    locMenu = locMenu.Union(menuBackend).ToList();
                }
                else if (NavManager.BaseUri.Contains("192.168.77"))
                {
                    locMenu = locMenu.Union(menuBackend).ToList();
                }
                else
                {
                    locMenu = locMenu.Union(menuBackend.Where(p => p.InDevelopment == false).ToList()).ToList();
                }
            }

            if (MenuItem.ID == Guid.Parse("bdc54b2a-b6a1-49bc-b960-5f23d84f7731"))  //Dynamic Menu from Authorities
            {
                var list = await ConfProvider.GetBackendAuthorityList(MenuItem.ID);

                if (list != null && list.Count() > 0)
                {
                    locMenu = locMenu.Union(list).ToList();
                }
            }

            SubMenuItems = locMenu;

            StateHasChanged();

            return true;
        }
        private void ToggleVisbility()
        {
            SubMenuVisible = !SubMenuVisible;
            NavMenuHelper.NotifyStateChanged();
            StateHasChanged();
        }
        private void NavigateTo(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (!NavManager.Uri.EndsWith(url))
                {
                    JSRuntime.InvokeVoidAsync("NavMenu_Mobile_OnHide", "mobile-menu-popup-container");

                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo(url);
                    StateHasChanged();
                }
                else
                {
                    StateHasChanged();
                }
            }
        }
    }
}
