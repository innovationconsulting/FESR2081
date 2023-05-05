using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Pages.Canteen.Backend;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace ICWebApp.Components.Dashboard;

public partial class CanteenContainer
{
    [Inject] private ISessionWrapper SessionWrapper { get; set; }
    [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }
    [Inject] private IAUTHProvider AuthProvider { get; set; }
    [Inject] private IFORMApplicationProvider FormApplicationProvider { get; set; }
    [Inject] private IFORMDefinitionProvider FormDefinitionProvider { get; set; }
    [Inject] private ITEXTProvider TextProvider { get; set; }
    [Inject] private IFormAdministrationHelper FormAdministrationHelper { get; set; }
    [Parameter] public string AuthorityID { get; set; }
    private AUTH_Authority? Authority { get; set; }
    private V_AUTH_Authority_Statistik? AuthorityStatistik { get; set; }
    private List<FORM_Application_Status>? StatusList { get; set; }
    private List<FORM_Application_Priority>? PriorityList { get; set; }
    private Administration_Filter_Item Filter = new();
    private List<V_FORM_Application> Applications = new();
    private bool IsDataBusy { get; set; } = true;


    [Inject] private ICANTEENProvider CanteenProvider { get; set; }
    [Inject] private ICanteenAdministrationHelper FilterHelper { get; set; }
    private List<V_CANTEEN_Subscriber> Subscribers = new();
    private CANTEEN_SearchFilter searchFilter = new();
    private Administration_Filter_CanteenSubscriptions FilterCanteen = new();
    private int ApplicationOpenCount = 0;
    private int ApplicationWaitinglistCount = 0;

    protected override async Task OnParametersSetAsync()
    {
        IsDataBusy = true;
        StateHasChanged();

        Authority = await GetData();

        await GetApplications();

        IsDataBusy = false;
        StateHasChanged();

        await base.OnParametersSetAsync();
    }

    private async Task<AUTH_Authority?> GetData()
    {
        if (AuthorityID != null) return await AuthProvider.GetAuthority(Guid.Parse(AuthorityID));

        return null;
    }


    private async Task<List<V_CANTEEN_Subscriber>> GetApplications()
    {
        if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
        {
            if (FilterHelper.Filter != null)
            {
                FilterCanteen = FilterHelper.Filter;
            }
            else
            {
                FilterCanteen.Text = null;
                FilterCanteen.DeadlineFrom = null;
                FilterCanteen.DeadlineTo = null;
                FilterCanteen.SubmittedFrom = null;
                FilterCanteen.SubmittedTo = null;
                FilterCanteen.EskalatedTasks = null;
                FilterCanteen.ManualInput = null;
                FilterCanteen.AUTH_Authority_ID = null;
                FilterCanteen.Auth_User_ID = null;
                FilterCanteen.Subscription_Status_ID = new List<Guid>();
                FilterCanteen.Subscription_Status_ID.Add(CanteenStatus.Comitted);
                FilterCanteen.Subscription_Status_ID.Add(CanteenStatus.Waitlist);
            }

            Subscribers = await GetData(FilterCanteen);

            ApplicationOpenCount = Subscribers.Where(a => a.CANTEEN_Subscriber_Status_ID == CanteenStatus.Comitted).Count();
            ApplicationWaitinglistCount = Subscribers.Where(a => a.CANTEEN_Subscriber_Status_ID == CanteenStatus.Waitlist).Count();
        }


        return Subscribers;
    }

    private async Task<List<V_CANTEEN_Subscriber>> GetData(Administration_Filter_CanteenSubscriptions? Filter)
    {
        if (SessionWrapper.AUTH_Municipality_ID != null && SessionWrapper.CurrentUser != null)
        {
            var subscriptions = await CanteenProvider.GetVSubscriptions(SessionWrapper.AUTH_Municipality_ID.Value,
                SessionWrapper.CurrentUser.ID, Filter);


            FilterHelper.Filter = Filter;

            return subscriptions;
        }

        return new List<V_CANTEEN_Subscriber>();
    }


    private void ShowListByStatusGroup(int Group)
    {
        if (Authority != null)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/Canteen/Subscriptionlist");
            StateHasChanged();
        }
    }

    private void GetOrCreateFilter()
    {
        if (FormAdministrationHelper.Filter != null)
        {
            Filter = FormAdministrationHelper.Filter;
        }
        else
        {
            Filter.FORM_Application_Status_ID = new List<Guid>();

            var municipalStatusList =
                FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

            foreach (var item in municipalStatusList.Where(p => p.Selectable == true))
                Filter.FORM_Application_Status_ID.Add(item.ID);

            Filter.FORM_Application_Status_ID.Add(FORMStatus.Comitted);

            Filter.FORM_Application_Priority_ID = new List<Guid>();
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("f1af3d5a-7a02-4faa-8845-34d2a5a66785"));
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("4318613b-17f7-4e15-a613-8801d4d5ae65"));
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("80dadb40-fa35-43eb-b2d5-871b8dc40913"));
            Filter.FORM_Application_Priority_ID.Add(Guid.Parse("f47f39b8-bfaf-4b71-9fd0-f3c978a4d1a4"));
        }
    }

    private void ShowDetailPage(Guid ApplicationID)
    {
        BusyIndicatorService.IsBusy = true;
        NavManager.NavigateTo("/Backend/Canteen/Subscriptionlist");
        StateHasChanged();
    }

    private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
    {
        var item = (V_CANTEEN_Subscriber)Args.Item;
        ;

        if (item != null) ShowDetailPage(item.ID);

        return true;
    }
}