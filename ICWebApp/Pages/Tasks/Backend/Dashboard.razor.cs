using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Tasks.Backend
{
    public partial class Dashboard
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        protected override void OnInitialized()
        {
            SessionWrapper.PageTitle = TextProvider.Get("TASK_BACKEND_DASHBOARD");

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            base.OnInitialized();
        }
    }
}
