using Microsoft.AspNetCore.Components;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.DataStore;
using ICWebApp.Application.Interface.Services;
using System.Globalization;
using ICWebApp.Domain.DBModels;

namespace ICWebApp.Components.Localization.Frontend
{
    public partial class LanguageComponent
    {
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }

        List<LANG_Languages> LanguageList = new List<LANG_Languages>();
        private LANG_Languages CurrentLanguage;

        protected override async Task OnInitializedAsync()
        {
            CurrentLanguage = LangProvider.GetLanguageByCode(CultureInfo.CurrentCulture.Name);
            LanguageList = await LangProvider.GetAll();

            StateHasChanged();

            await base.OnInitializedAsync();
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
