using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Reporting;
using Telerik.Reporting.Processing;

namespace ICWebApp.Pages.Organziation.Frontend
{
    public partial class Application_ComittedView
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Parameter] public string ID { get; set; }
        private List<FORM_Definition_Additional_FORM>? FORM_AdditionalForms { get; set; }
        private List<FORM_Definition> FORM_AdditionalDefinitions { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                if (ID == null)
                {
                    NavManager.NavigateTo("/");
                    StateHasChanged();
                    return;
                }

                CrumbService.ClearBreadCrumb();
                CrumbService.AddBreadCrumb("/Organization/Dashboard", "ORG_REQUEST_DASHBOARD_TITLE", null);
                CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), "ORG_REQUEST_NEW_TITLE", null, null, true);

              
                SessionWrapper.PageTitle = TextProvider.Get("ORG_REQUEST_NEW_TITLE");

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }

            base.OnAfterRender(firstRender);
        }
              
        private void BackToData()
        {
            NavManager.NavigateTo("/User/Services");
            StateHasChanged();
        }
    }
}
