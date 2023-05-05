using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Payment.Frontend
{
    public partial class Payment
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string ReturnUrl { get; set; }
        private Guid? TransactionID {get;set;}

        protected override void OnInitialized()
        {
            if(ID == null)
            {
                if (ReturnUrl != null)
                {
                    NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
                }
                else
                {
                    NavManager.NavigateTo("/");
                }
                StateHasChanged();
                return;
            }

            TransactionID = Guid.Parse(ID);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            base.OnInitialized();
        }

        private void BackToPreviousPayment()
        {
            NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
            StateHasChanged();
            return;
        }
    }
}
