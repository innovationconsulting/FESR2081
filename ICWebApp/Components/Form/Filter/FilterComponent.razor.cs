using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Form.Filter
{
    public partial class FilterComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFormAdministrationHelper FormAdministrationHelper { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public EventCallback<Administration_Filter_Item> OnSearch { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public Administration_Filter_Item Filter { get; set; }
        [Parameter] public bool Modal { get; set; } = false;

        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private List<V_FORM_Application_Users> UserList = new List<V_FORM_Application_Users>();
        private List<FORM_Application_Status> StatusList = new List<FORM_Application_Status>();
        private List<FORM_Application_Priority> PriorityList = new List<FORM_Application_Priority>();
        private List<Guid> AllowedAuthorities = new List<Guid>();
        private bool FilterWindowVisible { get; set; } = false;
        private bool PopUpAktivated = false;
        private bool HasCommitteeRights = false;

        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            StatusList = GetStatus();
            var prios = await FormApplicationProvider.GetPriorities();

            PriorityList.AddRange(prios);

            HasCommitteeRights = AuthProvider.HasUserRole(AuthRoles.Committee) || AuthProvider.HasUserRole(AuthRoles.Developer);

            AllowedAuthorities = await GetAuthorities();
            await GetFilterValues();
            StateHasChanged();
        }
        private List<FORM_Application_Status> GetStatus()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var statusList = FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                return statusList.ToList();
            }

            return new List<FORM_Application_Status>();
        }
        private async Task<List<Guid>> GetAuthorities()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                var userAuthorities = await AuthProvider.GetUserAuthorities(SessionWrapper.CurrentUser.ID);

                return userAuthorities.Where(p => p.AUTH_Authority_ID != null).Select(p => p.AUTH_Authority_ID.Value).ToList();
            }

            return new List<Guid>();
        }
        private async Task<bool> GetFilterValues()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var authlocList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null);

                AuthorityList = authlocList.Where(p => p.AUTH_Municipality_ID != null && AllowedAuthorities.Contains(p.ID)).ToList();

                var userLoclist = await AuthProvider.GetUserList(SessionWrapper.AUTH_Municipality_ID.Value);

                UserList = await FormApplicationProvider.GetApplicationUsers(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return true;
        }
        private void AddFilter(Guid Auth_Authority_ID)
        {
            if (Filter.AUTH_Authority_ID == null)
                Filter.AUTH_Authority_ID = new List<Guid>();

            if (Filter.AUTH_Authority_ID.Contains(Auth_Authority_ID))
            {
                Filter.AUTH_Authority_ID.Remove(Auth_Authority_ID);

            }
            else
            {
                Filter.AUTH_Authority_ID.Add(Auth_Authority_ID);
            }

            OnSearch.InvokeAsync(Filter);

            StateHasChanged();
        }
        private void ClearTagFilter()
        {
            if (Filter != null && (Filter.AUTH_Authority_ID != null))
            {
                Filter.AUTH_Authority_ID = null;

                OnSearch.InvokeAsync(Filter);
                StateHasChanged();
            }
        }
        private void ClearMantainenceFilter()
        {
            if (Filter != null && (Filter.OnlyMunicipal != false || Filter.OnlyMunicipalCommittee != false || Filter.OnlyPublic))
            {
                Filter.OnlyPublic = false;
                Filter.OnlyMunicipal = false;
                Filter.OnlyMunicipalCommittee = false;

                OnSearch.InvokeAsync(Filter);
                StateHasChanged();
            }
        }
        private void FilterClear()
        {
            Filter.Text = null;
            Filter.DeadlineFrom = null;
            Filter.DeadlineTo = null;
            Filter.SubmittedFrom = null;
            Filter.SubmittedTo = null;
            Filter.EskalatedTasks = null;
            Filter.ManualInput = null;
            Filter.AUTH_Authority_ID = null;
            Filter.Auth_User_ID = null;
            Filter.FORM_Application_Status_ID = new List<Guid>();
            Filter.FORM_Application_Status_ID.Add(FORMStatus.Accepted);
            Filter.FORM_Application_Status_ID.Add(FORMStatus.Comitted);
            Filter.FORM_Application_Status_ID.Add(Guid.Parse("d500ea5e-5cc7-4284-bec7-3959a0564d39"));

            Filter.FORM_Application_Priority_ID = new List<Guid>(); 
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("f1af3d5a-7a02-4faa-8845-34d2a5a66785"));
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("4318613b-17f7-4e15-a613-8801d4d5ae65"));
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("80dadb40-fa35-43eb-b2d5-871b8dc40913"));
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("f47f39b8-bfaf-4b71-9fd0-f3c978a4d1a4"));

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
        private async void ToggleOnlyMunicipal()
        {
            Filter.OnlyPublic = false;
            Filter.OnlyMunicipalCommittee = false;

            Filter.OnlyMunicipal = !Filter.OnlyMunicipal;

            await OnSearch.InvokeAsync(Filter);    

            StateHasChanged();
        }
        private async void ToggleOnlyPublic()
        {
            Filter.OnlyMunicipal = false;
            Filter.OnlyMunicipalCommittee = false;

            Filter.OnlyPublic = !Filter.OnlyPublic;

            await OnSearch.InvokeAsync(Filter);

            StateHasChanged();
        }
        private async void ToggleMunicipalCommittee()
        {
            Filter.OnlyPublic = false;
            Filter.OnlyMunicipal = false;

            Filter.OnlyMunicipalCommittee = !Filter.OnlyMunicipalCommittee;

            await OnSearch.InvokeAsync(Filter);

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
