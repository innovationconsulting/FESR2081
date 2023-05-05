using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Breadcrumb.Frontend
{
    public partial class Breadcrumb
    {
        [Inject] IBreadCrumbService BreadCrumbService { get; set; }
        protected override void OnInitialized()
        {
            BreadCrumbService.OnBreadCrumbDataChanged += BreadCrumbService_OnBreadCrumbDataChanged;

            StateHasChanged();
            base.OnInitialized();
        }

        private void BreadCrumbService_OnBreadCrumbDataChanged()
        {
            StateHasChanged();
        }
    }
}
