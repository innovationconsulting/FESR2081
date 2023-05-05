using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Global
{
    public partial class BusyIndicator
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Parameter] public EventCallback<string> OnVisualChanged { get; set; }

        protected override void OnInitialized()
        {
            BusyIndicatorService.OnBusyIndicatorChanged += BusyIndicatorService_OnBusyIndicatorChanged;

            base.OnInitialized();
        }

        private async void BusyIndicatorService_OnBusyIndicatorChanged(bool IsBusy)
        {
            await OnVisualChanged.InvokeAsync();

            StateHasChanged();
        }
    }
}
