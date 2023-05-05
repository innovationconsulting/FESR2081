using Microsoft.AspNetCore.Components;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.DataStore;
using ICWebApp.Application.Interface.Services;
using System.Globalization;
using ICWebApp.Domain.DBModels;

namespace ICWebApp.Components.Localization.Backend
{
    public partial class LanguageComponent
    {
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Parameter] public string? PopUpBackgroundColor { get; set; }
        [Parameter] public bool CollapsedSideBar { get; set; } = false;
        private string IsCollapsedCSS => CollapsedSideBar ? "nav-item-collapsed" : null;
        private string IsCollapsedPopupCSS => CollapsedSideBar ? "user-popup-menu-collapsed" : null;

        List<LANG_Languages> LanguageList = new List<LANG_Languages>();
        private LANG_Languages CurrentLanguage;
        private bool ShowLanguageList = false;
        private bool PopUpAktivated = false;

        protected override async Task OnInitializedAsync()
        {
            LanguageList = await LangProvider.GetAll();

            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            CurrentLanguage = LangProvider.GetLanguageByCode(CultureInfo.CurrentCulture.Name);

            StateHasChanged();

            await base.OnInitializedAsync();
        }

        private async void EnviromentService_OnScreenClicked()
        {
            if (ShowLanguageList && PopUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("language-popup-menu");

                if (!onScreen)
                {
                    ToggleMenu();
                }
            }
            else
            {
                PopUpAktivated = true;
            }
        }
        private void ToggleMenu()
        {
            ShowLanguageList = !ShowLanguageList;

            if (ShowLanguageList)
            {
                PopUpAktivated = false;
            }

            StateHasChanged();

        }
        private async Task<bool> SetLanguage(string LanguageCode)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await LangProvider.SetLanguage(LanguageCode);

            return true;
        }
    }
}
