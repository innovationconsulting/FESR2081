using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Organization.Filter
{
    public partial class FilterComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IRequestAdministrationHelper RequestAdministrationHelper { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public EventCallback<Administration_Filter_Request> OnSearch { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public Administration_Filter_Request Filter { get; set; }
        [Parameter] public bool Modal { get; set; } = false;

        private List<V_ORG_Request_Status> StatusList = new List<V_ORG_Request_Status>();
        private List<V_ORG_Request_Users> UserList = new List<V_ORG_Request_Users>();
        private List<V_AUTH_Company_Type> CompanyTypeList = new List<V_AUTH_Company_Type>();
        
        private bool FilterWindowVisible { get; set; } = false;
        private bool PopUpAktivated = false;

        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            StatusList = await OrgProvider.GetStatusList();
            CompanyTypeList = await AuthProvider.GetVCompanyType();

            await GetFilterValues();
            StateHasChanged();
        }
        private async Task<bool> GetFilterValues()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                UserList = await OrgProvider.GetRequestUserList(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return true;
        }
        private void AddFilter(Guid AUTH_Company_Type_ID)
        {
            if (Filter.Company_Type_ID == null)
                Filter.Company_Type_ID = new List<Guid>();

            if (Filter.Company_Type_ID.Contains(AUTH_Company_Type_ID))
            {
                Filter.Company_Type_ID.Remove(AUTH_Company_Type_ID);

            }
            else
            {
                Filter.Company_Type_ID.Add(AUTH_Company_Type_ID);
            }

            OnSearch.InvokeAsync(Filter);

            StateHasChanged();
        }
        private void ClearTagFilter()
        {
            if (Filter != null && Filter.Company_Type_ID != null)
            {
                Filter.Company_Type_ID = null;
                OnSearch.InvokeAsync(Filter);
                StateHasChanged();
            }
        }
        private void FilterClear()
        {
            Filter.Text = null;
            Filter.SubmittedFrom = null;
            Filter.SubmittedTo = null;
            Filter.Auth_User_ID = null;
            Filter.Company_Type_ID = new List<Guid>();
            Filter.Request_Status_ID = new List<Guid>();
            Filter.Request_Status_ID.Add(Guid.Parse("d09bfdf6-406b-44b8-9def-d37481b0828a"));   //REQUEST

            StateHasChanged();
        }
        private void FilterSearch()
        {
            OnSearch.InvokeAsync(Filter);
            FilterWindowVisible = false;
            StateHasChanged();
        }
        private void ToggleFilter()
        {
            FilterWindowVisible = !FilterWindowVisible;

            if (FilterWindowVisible)
            {
                PopUpAktivated = false;
            }

            StateHasChanged();
        }
        private async void EnviromentService_OnScreenClicked()
        {
            if (FilterWindowVisible && PopUpAktivated)
            {
                var onScreen = await EnviromentService.MouseOverDiv("filter-popup");

                if (!onScreen)
                {
                    ToggleFilter();
                }
            }
            else
            {
                PopUpAktivated = true;
            }
        }
        private void ClearSearchBar()
        {
            if (Filter != null)
            {
                FilterClear();

                FilterSearch();
            }
        }
    }
}
