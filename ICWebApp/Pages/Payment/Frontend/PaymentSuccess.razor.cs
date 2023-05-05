using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Payment.Frontend
{
    public partial class PaymentSuccess
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Parameter] public string Family_ID { get; set; }
        [Parameter] public string ReturnUrl { get; set; }
        private bool ShowCloseTag = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (Family_ID == null)
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

                        await Task.Delay(4000);

                        NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
                    }

                    StateHasChanged();
                    return;
                }

                await PayProvider.SetTransactionsPayedByFamilyID(Guid.Parse(Family_ID));

                if(SessionWrapper.AUTH_Municipality_ID == null)
                {
                    SessionWrapper.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();
                }

                if (!EnviromentService.IsMobile)
                {
                    if (ReturnUrl != null)
                    {
                        NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowCloseTag = true;

                    await Task.Delay(2000);

                    NavManager.NavigateTo(Uri.UnescapeDataString(ReturnUrl));
                }

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
