using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Pages.Canteen.Backend;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using static Telerik.Blazor.ThemeConstants;
using Telerik.Blazor.Components;
using ICWebApp.Application.Services;
using Stripe;

namespace ICWebApp.Components.Canteen;

public partial class SubscriptionDetailComponent
{
    [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] private ISessionWrapper SessionWrapper { get; set; }
    [Inject] private ICANTEENProvider CanteenProvider { get; set; }
    [Inject] private ITEXTProvider TextProvider { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }
    [Inject] IFILEProvider FileProvider { get; set; }
    [Inject] IEnviromentService EnviromentService { get; set; }
    [CascadingParameter] public DialogFactory Dialogs { get; set; }
    [Parameter] public List<Guid> SubscriberDetailList { get; set; }

    private List<CANTEEN_Subscriber> Subscribers = new();
    private List<CANTEEN_Subscriber> SubscribersList = new();
    private IEnumerable<CANTEEN_Subscriber> SelectedItems = new List<CANTEEN_Subscriber>();
    private IEnumerable<CANTEEN_Subscriber> SubscribersAccepted = new List<CANTEEN_Subscriber>();
    private List<CANTEEN_School> Schools = new();
    private List<CANTEEN_Canteen> Canteens = new();
    private List<CANTEEN_Subscriber_Status> Statuslist = new();
    private List<CANTEEN_Subscriber_Status> StatusActionlist = new();
    private List<V_CANTEEN_Schoolyear> SchoolyearList = new();
    private CANTEEN_Subscriber_Status CurrentStatus = new();
    private CANTEEN_School CurrentSchool = new();
    private CANTEEN_Canteen CurrentCanteen = new();
    private CANTEEN_Subscriber_Status NewStatusAction = new();
    private decimal CurrentBalance = 0;
    private List<CANTEEN_Subscriber_Movements> LatestMovements = new List<CANTEEN_Subscriber_Movements>();
    private List<CANTEEN_Subscriber_Movements> NextMovements = new List<CANTEEN_Subscriber_Movements>();
    private Guid? CanteenStartSelection;  
    private int? CurrentTab { get; set; } = 0;
    private int CurrentWizardTab { get; set; } = 0;
    private CANTEEN_Subscriber? subscriberDetail { get; set; }
    public List<CANTEEN_Subscriber> subscriberDetailOldVersions { get; set; } = new List<CANTEEN_Subscriber>();

    protected override async Task OnInitializedAsync()
    {
        BusyIndicatorService.IsBusy = false;
        StateHasChanged();

        if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.ID == null ||
            SessionWrapper.CurrentUser.AUTH_Municipality_ID == null) NavManager.NavigateTo("/Canteen");

        SelectedItems = new List<CANTEEN_Subscriber>();

        Statuslist = await CanteenProvider.GetSubscriberStatuses();
        StatusActionlist = Statuslist.Where(a => a.ActionEnabled == true).ToList();

        Schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
        Canteens = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
        Subscribers = await CanteenProvider.GetSubscribersByStatusID(CurrentStatus.ID);

        if(SubscriberDetailList != null && SubscriberDetailList.Count > 0)
        {
            var subscriber = await CanteenProvider.GetSubscriber(SubscriberDetailList.FirstOrDefault());

            subscriberDetail = subscriber;
            await SubscriberDetailSelected();

            SubscribersList = new List<CANTEEN_Subscriber>();

            foreach(var ID in SubscriberDetailList)
            {
                var data = await CanteenProvider.GetSubscriber(ID);

                if (data != null)
                {
                    SubscribersList.Add(data);
                }
            }
        }

        StateHasChanged();

        await base.OnInitializedAsync();
    }
    private async Task<bool> SubscriberDetailSelected()
    {
        if (subscriberDetail != null)
        {
            CurrentBalance = CanteenProvider.GetUserBalance(subscriberDetail.AUTH_Users_ID ?? Guid.Empty);

            LatestMovements = await CanteenProvider.GetSubscriberMovementsBySubscriber(subscriberDetail.ID);
            LatestMovements = LatestMovements.Where(a => a.CANTEEN_Subscriber_ID == subscriberDetail.ID).ToList();
            NextMovements = LatestMovements;
            LatestMovements = LatestMovements.Where(a => a.Date <= DateTime.Today && (a.Fee < 0 || (a.PaymentTransaction != null && a.PaymentTransaction.PaymentDate != null))).ToList();

            if (DateTime.Now.Hour >= 10)
            {
                NextMovements = NextMovements.Where(a => a.Date > DateTime.Now).ToList();
            }
            else
            {
                NextMovements = NextMovements.Where(a => a.Date >= DateTime.Today).ToList();
            }

            NextMovements = NextMovements.OrderBy(o => o.Date).Take(30).ToList();


            SchoolyearList = await CanteenProvider.GetSchoolsyearAll(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

            CurrentStatus = Statuslist.FirstOrDefault();
            CurrentSchool = Schools.FirstOrDefault();
            CurrentCanteen = Canteens.FirstOrDefault();

            BusyIndicatorService.IsBusy = false;

            CanteenStartSelection = subscriberDetail.CANTEEN_Canteen_ID;

            if (subscriberDetail.PreviousSubscriptionID != null)
            {
                subscriberDetailOldVersions = new List<CANTEEN_Subscriber>();

                var subscriberDetailOldVersion = await CanteenProvider.GetSubscriber(subscriberDetail.PreviousSubscriptionID ?? Guid.Empty);

                if (subscriberDetailOldVersion != null)
                {
                    subscriberDetailOldVersions.Add(subscriberDetailOldVersion);

                    while (subscriberDetailOldVersion != null && subscriberDetailOldVersion.PreviousSubscriptionID != null)
                    {
                        var newVersion = await CanteenProvider.GetSubscriber(subscriberDetailOldVersion.PreviousSubscriptionID ?? Guid.Empty);

                        if (newVersion != null)
                        {
                            subscriberDetailOldVersions.Add(newVersion);

                            subscriberDetailOldVersion = newVersion;
                        }
                        else
                        {
                            subscriberDetailOldVersion = null;

                        }
                    }
                }
            }

            if (subscriberDetail.CreationDate == null) subscriberDetail.CreationDate = DateTime.Today;
        }

        StateHasChanged();
        return true;
    }
    private async void SelectSub(Guid SubscriberID)
    {
        var subscriber = await CanteenProvider.GetSubscriber(SubscriberDetailList.Where(p => p == SubscriberID).FirstOrDefault());

        subscriberDetail = subscriber;

        await SubscriberDetailSelected();

        StateHasChanged();
    }
}