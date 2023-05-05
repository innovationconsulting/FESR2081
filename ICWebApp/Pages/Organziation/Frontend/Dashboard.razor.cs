using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Organziation.Frontend
{
    public partial class Dashboard
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        private bool IsDataBusy = true;
        private List<V_AUTH_Users_Organizations> Organizations = new List<V_AUTH_Users_Organizations>();

        protected override async Task OnInitializedAsync()
        {
            Organizations = await GetOrganizations();

            if(Organizations == null || Organizations.Count() == 0)
            {
                NavManager.NavigateTo("/Organization/Application");
                StateHasChanged();

                return;
            }

            SessionWrapper.PageTitle = TextProvider.Get("ORG_REQUEST_DASHBOARD_TITLE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Organization/Dashboard", "ORG_REQUEST_DASHBOARD_TITLE", null, null, true);

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }     
        private async Task<List<V_AUTH_Users_Organizations>> GetOrganizations()
        {
            return await AuthProvider.GetUsersOrganizations(SessionWrapper.CurrentUser.ID);
        }      
        private void CreateNewRequest()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Organization/Application");
        }
        private void GoToOrgManagement(V_AUTH_Users_Organizations Org)
        {
            if (Org != null && Org.ID != null && Org.ConfirmedAt != null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Management/" + Org.ORG_AUTH_Users_ID.Value);
                StateHasChanged();
            }
        }
    }
}
