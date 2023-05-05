using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor;
using Telerik.Reporting.Configuration;

namespace ICWebApp.Pages.Canteen.Frontend.MyCivis
{
    public partial class LandingPageCanteen
    {
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        public string Infopage { get; set; }

        protected override void OnInitialized()
        {
            if (NavManager.Uri.Contains("MyCivisInfo"))
            {
                Infopage = "info";
            }

            MyCivisService.Enabled = true;
            StateHasChanged();

            base.OnInitialized();
        }
    }
}
