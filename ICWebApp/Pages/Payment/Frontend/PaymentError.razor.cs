using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Payment.Frontend
{
    public partial class PaymentError
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public string Family_ID { get; set; }
        [Parameter] public string ReturnUrl { get; set; }
        private bool ShowCloseTag = false;

        protected override void OnInitialized()
        {
            if(Family_ID == null)
            {
                if (!EnviromentService.IsMobile)
                {
                    if (ReturnUrl != null)
                    {
                        NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
                    }
                    else
                    {
                        NavManager.NavigateTo("/");
                    } 
                }
                else
                {
                    ShowCloseTag = true;
                    StateHasChanged();
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            base.OnInitialized();
        }

        private async void ReturnToStart()
        {
            if (!EnviromentService.IsMobile) {
                
                if (ReturnUrl != null)
                {
                    NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
                    StateHasChanged();
                }
                else
                {
                    NavManager.NavigateTo("/");
                }
            }
            else
            {
                ShowCloseTag = true;
                StateHasChanged();

                await Task.Delay(2000);

                NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
    }
}
