using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Canteen.Frontend.Default;

public partial class TaxReports
{
    [Inject] IMyCivisService MyCivisService { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }

    protected override void OnInitialized()
    {
        MyCivisService.Enabled = false;
        StateHasChanged();

        base.OnInitialized();
    }
}