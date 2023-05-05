using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace ICWebApp.Pages.Organziation.Backend
{
    public partial class ManagementList
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
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IRequestAdministrationHelper RequestAdministrationHelper { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<V_AUTH_Organizations> Organizations = new List<V_AUTH_Organizations>();
        private bool IsDataBusy { get; set; } = true;
        public Administration_Filter_Organization Filter { get; set; } = new Administration_Filter_Organization();
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_SUBSTITUTES_MANAGEMENT");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Organization/Backend/Management/List", "MAINMENU_BACKEND_SUBSTITUTES_MANAGEMENT", null, null, true);

            Organizations = await GetOrganizations();

            if(RequestAdministrationHelper.OrgFilter != null)
            {
                Filter = RequestAdministrationHelper.OrgFilter;
            }
            else
            {
                Filter = new Administration_Filter_Organization();
            }

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<List<V_AUTH_Organizations>> GetOrganizations()
        {
            var data = await AuthProvider.GetOrganizations(SessionWrapper.AUTH_Municipality_ID.Value, Filter);

            RequestAdministrationHelper.OrgFilter = Filter;

            return data;
        }
        private void ShowDetail(V_AUTH_Organizations Org)
        {
            if (Org != null && Org.AUTH_Users_ID != null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Backend/Management/Detail/" + Org.AUTH_Users_ID.Value);
                StateHasChanged();
            }
        }
        private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
        {
            var item = (V_AUTH_Organizations)Args.Item; ;

            if (item != null && item.AUTH_Users_ID != null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Backend/Management/Detail/" + item.AUTH_Users_ID.Value);
                StateHasChanged();
            }
            

            return true;
        }
        private async void FilterSearch()
        {
            IsDataBusy = true;
            StateHasChanged();

            Organizations = await GetOrganizations();

            IsDataBusy = false;
            StateHasChanged();
        }
    }
}
