using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Canteen
{
    public partial class CanteenFilterComponent
    { 
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] IFORMApplicationProvider Form_ApplicationProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public EventCallback<Administration_Filter_CanteenSubscriptions> OnSearch { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public EventCallback OnClear{ get; set; }
        [Parameter] public Administration_Filter_CanteenSubscriptions Filter { get; set; }
        [Parameter] public bool Modal { get; set; } = false;

        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private List<V_FORM_Application_Users> UserList = new List<V_FORM_Application_Users>();
        private List<CANTEEN_Subscriber_Status> StatusList = new List<CANTEEN_Subscriber_Status>();
        private List<V_CANTEEN_Schoolyear> SchoolyearList = new();
        private List<CANTEEN_School> SchoolList = new();
        private List<Guid> AllowedAuthorities = new List<Guid>();
        private List<CANTEEN_Canteen> CanteenList = new();
        private bool FilterWindowVisible { get; set; } = false;
        private bool PopUpAktivated = false;

        public int StartDistanceValue { get; set; } = 0;
        public int EndDistanceValue { get; set; } =0;
        public int MinDistance { get; set; } = 0;
        public int MaxDistance { get; set; } = 100;




        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnScreenClicked += EnviromentService_OnScreenClicked;

            StatusList = await GetStatus();
            SchoolList = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            CanteenList = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            SchoolyearList = await CanteenProvider.GetSchoolsyearAll(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

            AllowedAuthorities = await GetAuthorities();
            await GetFilterValues();
            FilterClear();
            StateHasChanged();
        }
        private async Task<List<CANTEEN_Subscriber_Status>> GetStatus()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var statusList = await CanteenProvider.GetSubscriberStatuses();

                return statusList.ToList();
            }

            return new List<CANTEEN_Subscriber_Status>();
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

                UserList = await Form_ApplicationProvider.GetApplicationUsers(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return true;
        }
        private void AddFilter(Guid StatusID)
        {
            if (Filter.Subscription_Status_ID == null)
                Filter.Subscription_Status_ID = new List<Guid>();

            if (Filter.Subscription_Status_ID.Contains(StatusID))
            {
                Filter.Subscription_Status_ID.Remove(StatusID);

            }
            else
            {
                Filter.Subscription_Status_ID.Add(StatusID);
            }

            OnSearch.InvokeAsync(Filter);

            StateHasChanged();
        }
        private void ClearTagFilter()
        {
            if (Filter != null && Filter.Subscription_Status_ID != null)
            {
                Filter.Subscription_Status_ID = null;
                OnSearch.InvokeAsync(Filter);
                StateHasChanged();
            }
        }
        private async void FilterClear()
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
            Filter.Subscription_Status_ID = new List<Guid>();
            Filter.Subscription_Status_ID.Add(CanteenStatus.Comitted);
            Filter.Subscription_Status_ID.Add(CanteenStatus.Waitlist);

            await OnClear.InvokeAsync();

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
