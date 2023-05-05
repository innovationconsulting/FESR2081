using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Bollo.Developer
{
    public partial class BolloPay
    {
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndiactorService { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private ITEXTProvider TextProvider { get; set; }

        private List<V_PAY_Bollo_To_Pay> BolloList = new List<V_PAY_Bollo_To_Pay>();

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.GetOrCreate("DEV_BOLLOLIST");
            BolloList = await PayProvider.GetVPayBolloToPay();

            BusyIndiactorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
    }
}
