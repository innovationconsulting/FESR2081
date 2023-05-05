using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Canteen.Frontend.MyCivis;

public partial class TaxReports
{
    [Inject] IMyCivisService MyCivisService { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }

    protected override void OnInitialized()
    {
        MyCivisService.Enabled = true;
        StateHasChanged();

        base.OnInitialized();
    }
}