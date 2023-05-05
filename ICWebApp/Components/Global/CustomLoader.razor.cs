using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Global;

public partial class CustomLoader
{
    [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] IMyCivisService MyCivisService { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public bool IgnoreGlobalLoader { get; set; } = false;
    [Parameter] public string? Text { get; set; } = "";
    [Parameter] public string? Class { get; set; } = "";

    private void BusyIndicatorService_OnBusyIndicatorChanged(bool IsBusy)
    {
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        BusyIndicatorService.OnBusyIndicatorChanged += BusyIndicatorService_OnBusyIndicatorChanged;

        StateHasChanged();
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        StateHasChanged();
        base.OnParametersSet();
    }
}