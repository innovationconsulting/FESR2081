using System.Diagnostics;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Stripe;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using ICWebApp.Domain.Models.Textvorlagen;

namespace ICWebApp.Pages.Canteen.Backend;


public partial class SubscriptionsManagementList
{
    [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] IMessageService MessageService { get; set; }
    [Inject] ISessionWrapper SessionWrapper { get; set; }
    [Inject] ICANTEENProvider CanteenProvider { get; set; }
    [Inject] ITEXTProvider TextProvider { get; set; }
    [Inject] NavigationManager NavManager { get; set; }
    [Inject] IBreadCrumbService CrumbService { get; set; }
    [Inject] ICanteenAdministrationHelper FilterHelper { get; set; }
    [Inject] IAUTHProvider AuthProvider { get; set; }
    [Inject] IINFO_PAGEProvider InfoPageProvider { get; set; }
    [CascadingParameter] public DialogFactory Dialogs { get; set; }

    private List<V_CANTEEN_Subscriber> Subscribers = new List<V_CANTEEN_Subscriber>();
    private IEnumerable<V_CANTEEN_Subscriber> SelectedItems = Enumerable.Empty<V_CANTEEN_Subscriber>();
    private IEnumerable<V_CANTEEN_Subscriber> SubscribersAccepted = new List<V_CANTEEN_Subscriber>();
    private List<CANTEEN_School> Schools = new List<CANTEEN_School>();
    private List<CANTEEN_Canteen> Canteens = new List<CANTEEN_Canteen>();
    private List<CANTEEN_Subscriber_Status> Statuslist = new List<CANTEEN_Subscriber_Status>();
    private List<CANTEEN_Subscriber_Status> StatusActionlist = new List<CANTEEN_Subscriber_Status>();
    private List<V_CANTEEN_Schoolyear> SchoolyearList = new List<V_CANTEEN_Schoolyear>();
    private CANTEEN_Subscriber_Status CurrentStatus = new CANTEEN_Subscriber_Status();
    private CANTEEN_School CurrentSchool = new CANTEEN_School();
    private CANTEEN_Canteen CurrentCanteen = new CANTEEN_Canteen();
    private Guid ReferenceID;
    private long? FirstSubscriberNumber;
    private bool IsDataBusy = true;
    private CANTEEN_SearchFilter searchFilter = new CANTEEN_SearchFilter();
    private bool ChangeStatusWindowVisible;
    private bool BulkEnabled;
    private Administration_Filter_CanteenSubscriptions Filter = new Administration_Filter_CanteenSubscriptions();
    private bool ChangeCanteenWindowVisible;
    private List<CANTEEN_Canteen> PossibleCanteensList = new List<CANTEEN_Canteen>();
    private bool BulkReminderEnabled = false;
    private bool ReminderWindowVisible;
    private V_CANTEEN_Schoolyear_Current? CurrentSchoolyear;
    private TextItem? TextItem = new TextItem();
    protected override async Task OnInitializedAsync()
    {
        BusyIndicatorService.IsBusy = false;
        StateHasChanged();

        if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.ID == null ||
            SessionWrapper.CurrentUser.AUTH_Municipality_ID == null) NavManager.NavigateTo("/Canteen");

        SelectedItems = new List<V_CANTEEN_Subscriber>();
        searchFilter = new CANTEEN_SearchFilter();

        SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_TITLE");
        SessionWrapper.PageSubTitle = TextProvider.Get("");


        CrumbService.ClearBreadCrumb();
        CrumbService.AddBreadCrumb("/Backend/Canteen/Subscriptionlist", "MAINMENU_CANTEEN", null, null);
        CrumbService.AddBreadCrumb("/Canteen/Subscribe", "CANTEEN_CREATE_SUBSCRIPTION_TITLE", null, null);


        ReferenceID = Guid.NewGuid();

        Statuslist = await CanteenProvider.GetSubscriberStatuses();
        StatusActionlist = Statuslist.Where(a => a.ActionEnabled == true).OrderBy(o => o.SortOrder).ToList();

        Schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
        Canteens = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
        Subscribers = await CanteenProvider.GetVSubscribersByStatusID(CurrentStatus.ID);


        SchoolyearList = await CanteenProvider.GetSchoolsyearAll(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
        if (SchoolyearList.Count() > 0)
        {
            searchFilter.CurrentSchoolYearID = SchoolyearList.OrderByDescending(o => o.BeginYear).FirstOrDefault().id;
        }

        searchFilter.CurrentStatusID = Statuslist.OrderBy(o => o.SortOrder).FirstOrDefault().ID;
        searchFilter.CurrentStatusActionID = searchFilter.CurrentStatusID;

        SubscribersAccepted = await CanteenProvider.GetVSubscribersByStatusID(CanteenStatus.Accepted);
        SubscribersAccepted = SubscribersAccepted.Where(a => a.SchoolyearID == searchFilter.CurrentSchoolYearID).ToList();

        CurrentStatus = Statuslist.FirstOrDefault();
        CurrentSchool = Schools.FirstOrDefault();
        CurrentCanteen = Canteens.FirstOrDefault();

        if (FilterHelper.Filter != null)
        {
            Filter = FilterHelper.Filter;
        }
        else
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
        }

        Subscribers = await GetData(this.Filter);
        CurrentSchoolyear = (await CanteenProvider.GetSchoolsyearsCurrent(SessionWrapper.AUTH_Municipality_ID.Value, true)).FirstOrDefault();

        BulkEnabled = false;
        BusyIndicatorService.IsBusy = false;
        IsDataBusy = false;
        StateHasChanged();

        await base.OnInitializedAsync();
    }
    private async void FilterSearch(Administration_Filter_CanteenSubscriptions Filter)
    {
        IsDataBusy = true;
        StateHasChanged();

        this.Filter = Filter;

        Subscribers = await GetData(this.Filter);

        IsDataBusy = false;
        StateHasChanged();
    }
    private async Task<List<V_CANTEEN_Subscriber>> GetData(Administration_Filter_CanteenSubscriptions? Filter)
    {
        if (SessionWrapper.AUTH_Municipality_ID != null && SessionWrapper.CurrentUser != null)
        {
            var subscriptions = await CanteenProvider.GetVSubscriptions(SessionWrapper.AUTH_Municipality_ID.Value, SessionWrapper.CurrentUser.ID, Filter);


            FilterHelper.Filter = Filter;

            return subscriptions;
        }

        return new List<V_CANTEEN_Subscriber>();
    }
    private void ReturnToPreviousPage()
    {
        ReminderWindowVisible = false;
        ChangeCanteenWindowVisible = false;
        ChangeStatusWindowVisible = false;
        StateHasChanged();
    }
    private void CheckBulkOperationGrid(GridStateEventArgs<V_CANTEEN_Subscriber> args)
    {
        CheckBulkOperation();
    }
    private void CheckBulkOperation()
    {
        BulkEnabled = true;

        if (SelectedItems == null || SelectedItems.Count() == 0)
        {
            BulkEnabled = false;
        }

        if (SelectedItems != null && SelectedItems.Where(p => p.CANTEEN_Subscriber_Status_ID == CanteenStatus.Incomplete).Any() &&
           CurrentSchoolyear != null && CurrentSchoolyear.RegisterBeginDate <= DateTime.Now && (CurrentSchoolyear.RegisterEndDate.Value.AddDays(1)) >= DateTime.Now)
        {
            BulkReminderEnabled = true;
        }
        else
        {
            BulkReminderEnabled = false;
        }

        StateHasChanged();
    }
    private int GetCanteenChartData(Guid canteenID, int dayNo)
    {
        var archivedID = CanteenStatus.Archived;

        var canteensubscribersAccepted = SubscribersAccepted.Where(a => a.CANTEEN_Canteen_ID == canteenID && a.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted && a.Archived == null);
        var canteensubscribersSelected = SelectedItems.Where(a => a.CANTEEN_Subscriber_Status_ID != CanteenStatus.Accepted && a.SuccessorSubscriptionID == null && a.CANTEEN_Subscriber_Status_ID != archivedID && a.CANTEEN_Canteen_ID == canteenID && a.Archived == null);

        if (dayNo == 1)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DayMo == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DayMo == true);
        }
        if (dayNo == 2)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DayTue == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DayTue == true);
        }
        if (dayNo == 3)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DayWed == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DayWed == true);
        }
        if (dayNo == 4)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DayThu == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DayThu == true);
        }
        if (dayNo == 5)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DayFri == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DayFri == true);
        }
        if (dayNo == 6)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DaySat == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DaySat == true);
        }
        if (dayNo == 7)
        {
            canteensubscribersAccepted = canteensubscribersAccepted.Where(a => a.DaySun == true);
            canteensubscribersSelected = canteensubscribersSelected.Where(a => a.DaySun == true);
        }

        var resultlist = canteensubscribersAccepted.Where(p => p.SuccessorSubscriptionID == null || canteensubscribersSelected.FirstOrDefault(x => x.ID == p.SuccessorSubscriptionID) == null).ToList();

        foreach (var canteenItem in canteensubscribersSelected)
        {
            if (!resultlist.Select(p => p.TaxNumber).Contains(canteenItem.TaxNumber))
            {
                resultlist.Add(canteenItem);
            }
        }

        return resultlist.Count();
    }
    private async void SetNewStatus()
    {
        ChangeStatusWindowVisible = false;
        BusyIndicatorService.IsBusy = true;
        StateHasChanged();

        foreach (var sub in SelectedItems)
        {
            if (sub.ID != null)
            {
                var subscription = await CanteenProvider.GetSubscriberWithoutInclude(sub.ID);

                var previousStatus = subscription.CANTEEN_Subscriber_Status_ID;

                if (subscription != null)
                {
                    if (sub.CANTEEN_Subscriber_Status_ID == CanteenStatus.Archived) //CANTEEN_SUBSCRIBER_STATUS_ARCHIVED
                    {
                        continue;
                    }
                    if (sub.CANTEEN_Subscriber_Status_ID == CanteenStatus.Disabled) //CANTEEN_SUBSCRIBER_STATUS_DISABLED
                    {
                        continue;
                    }

                    if (sub.CANTEEN_Subscriber_Status_ID == searchFilter.CurrentStatusActionID)
                    {
                        continue;
                    }

                    var auth_municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID ?? Guid.NewGuid());

                    subscription.CANTEEN_Subscriber_Status_ID = searchFilter.CurrentStatusActionID;

                    if (subscription.CreationDate == null)
                        subscription.CreationDate = DateTime.Today;

                    if (subscription.SchoolyearID != null)
                    {
                        var period = await CanteenProvider.GetSchoolyear(subscription.SchoolyearID.Value);

                        if (period != null)
                        {
                            if (DateTime.Now > period.BeginDate)
                            {
                                subscription.Begindate = DateTime.Today.AddDays(3);
                            }

                            subscription.Enddate = period.EndDate;
                        }
                    }

                    if (subscription.PreviousSubscriptionID != null && subscription.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted)
                    {
                        var previous = await CanteenProvider.GetSubscriber(subscription.PreviousSubscriptionID ?? Guid.Empty);
                        
                        if (DateTime.Now.Hour >= 10)
                            subscription.Begindate = DateTime.Today.AddDays(1);
                        else
                            subscription.Begindate = DateTime.Today;

                        if (previous != null && previous.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted)
                        {
                            previous.CANTEEN_Subscriber_Status_ID = CanteenStatus.Archived;
                            previous.Archived = DateTime.Now;
                            var futureMovements = await CanteenProvider.GetSubscriberMovementsBySubscriber(previous.ID);
                            futureMovements = futureMovements.Where(a => a.Date >= DateTime.Now).ToList();
                            foreach (var futureMovement in futureMovements)
                            {
                                await CanteenProvider.RemoveSubscriberMovement(futureMovement.ID);
                            }
                            await CanteenProvider.SetSubscriber(previous);
                        }

                        if (subscription.AUTH_Users_ID != null && SessionWrapper.AUTH_Municipality_ID != null)
                        {
                            var canteenUser = CanteenProvider.GetCanteenUserByID(subscription.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value);

                            if (canteenUser != null)
                            {
                                canteenUser.ServiceDisabledDate = null;
                                StateHasChanged();
                            }
                        }
                    }

                    if (subscription.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted && subscription.ProgressivNumber == null)    //Set Progressiv Number if not existant
                    {
                        var lastNumber = CanteenProvider.GetLatestProgressivNumber(SessionWrapper.AUTH_Municipality_ID.Value, DateTime.Now.Year);
                        subscription.SignedDate = DateTime.Now;

                        if (subscription.ProgressivNumber == null || subscription.ProgressivNumber == 0)
                        {
                            subscription.ProgressivYear = DateTime.Now.Year;

                            subscription.ProgressivNumber = lastNumber + 1;
                        }

                        await CanteenProvider.SetSubscriber(subscription);
                    }

                    var canteen = await CanteenProvider.GetCanteen(subscription.CANTEEN_Canteen_ID.Value);
                    var subscriptionUser = CanteenProvider.GetCanteenUserByID(subscription.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID ?? Guid.NewGuid());
                    string subject = " " + subscription.FirstName + " " + subscription.LastName + " " + subscription.SchoolyearDescription;

                    await CanteenProvider.SetSubscriber(subscription);

                    if (searchFilter.CurrentStatusActionID == CanteenStatus.Accepted)
                    {
                        await CanteenProvider.CreateSubscriber_Movements(subscription.ID);
                    }
                    else
                    {
                        var futureMovements = await CanteenProvider.GetSubscriberMovementsBySubscriber(subscription.ID);
                        futureMovements = futureMovements.Where(a => a.Date >= DateTime.Now).ToList();
                        foreach (var futureMovement in futureMovements)
                        {
                            await CanteenProvider.RemoveSubscriberMovement(futureMovement.ID);
                        }
                    }

                    //MSG
                    if (TextItem != null && subscription.AUTH_Users_ID != null)
                    {
                        string text = "";
                        Guid userLangID = LanguageSettings.German;

                        var userLang = await AuthProvider.GetSettings(subscription.AUTH_Users_ID.Value);

                        if (userLang != null && userLang.LANG_Languages_ID == LanguageSettings.Italian)
                        {
                            if (!string.IsNullOrEmpty(TextItem.Italian))
                            {
                                text = await TextProvider.ReplaceGeneralKeyWords(subscription.AUTH_Users_ID.Value, TextItem.Italian);
                            }

                            text = await CanteenProvider.ReplaceKeywords(subscription.ID, LanguageSettings.Italian, text, previousStatus);

                            userLangID = LanguageSettings.Italian;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(TextItem.German))
                            {
                                text = await TextProvider.ReplaceGeneralKeyWords(subscription.AUTH_Users_ID.Value, TextItem.German);
                            }

                            text = await CanteenProvider.ReplaceKeywords(subscription.ID, LanguageSettings.German, text, previousStatus);
                        }

                        if (subscription.AUTH_Users_ID != null && subscription.AUTH_Municipality_ID != null)
                        {
                            var msg = await MessageService.GetMessage(subscription.AUTH_Users_ID.Value, subscription.AUTH_Municipality_ID.Value, text, 
                                                                      TextProvider.Get("NOTIF_CANTEEN_STATUS_CHANGED_SHORTTEXT", userLangID),
                                                                      TextProvider.Get("NOTIF_CANTEEN_STATUS_CHANGED_TITLE", userLangID), 
                                                                      Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), false, null);

                            if (msg != null)
                            {
                                await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                            }
                        }
                        
                    }
                }
            }
        }

        SubscribersAccepted = await CanteenProvider.GetVSubscribersByStatusID(CanteenStatus.Accepted);
        Subscribers = await GetData(Filter);
        SelectedItems = new List<V_CANTEEN_Subscriber>();
        BulkEnabled = false;

        BusyIndicatorService.IsBusy = false;
        StateHasChanged();
    }
    private async void ShowCanteenChangeWindow()
    {
        if (SessionWrapper.AUTH_Municipality_ID != null)
        {
            var MON = SelectedItems.Where(p => p.DayMo == true).Select(p => p.DayMo).Distinct().FirstOrDefault();
            var TUE = SelectedItems.Where(p => p.DayTue == true).Select(p => p.DayTue).Distinct().FirstOrDefault();
            var WED = SelectedItems.Where(p => p.DayWed == true).Select(p => p.DayWed).Distinct().FirstOrDefault();
            var THU = SelectedItems.Where(p => p.DayThu == true).Select(p => p.DayThu).Distinct().FirstOrDefault();
            var FRI = SelectedItems.Where(p => p.DayFri == true).Select(p => p.DayFri).Distinct().FirstOrDefault();
            var SAT = SelectedItems.Where(p => p.DaySat == true).Select(p => p.DaySat).Distinct().FirstOrDefault();
            var SUN = SelectedItems.Where(p => p.DaySun == true).Select(p => p.DaySun).Distinct().FirstOrDefault();

            var startList = await CanteenProvider.GetCanteens(SessionWrapper.AUTH_Municipality_ID.Value);
            PossibleCanteensList = new List<CANTEEN_Canteen>();
            bool CanteenOk = true;

            foreach (var item in startList)
            {
                if (MON == true && item.DayMo == false)
                    CanteenOk = false;
                if (TUE == true && item.DayTue == false)
                    CanteenOk = false;
                if (WED == true && item.DayWed == false)
                    CanteenOk = false;
                if (THU == true && item.DayThu == false)
                    CanteenOk = false;
                if (FRI == true && item.DayFri == false)
                    CanteenOk = false;
                if (SAT == true && item.DaySat == false)
                    CanteenOk = false;
                if (SUN == true && item.DaySun == false)
                    CanteenOk = false;

                if (CanteenOk == true)
                    PossibleCanteensList.Add(item);

                CanteenOk = true;
            }

            ChangeCanteenWindowVisible = true;
            StateHasChanged();
        }
    }
    private async void SetCanteen()
    {
        if (searchFilter.CurrentCanteenID != null)
        {
            ChangeCanteenWindowVisible = false;
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var sub in SelectedItems)
            {
                //MSG
                var subscription = await CanteenProvider.GetSubscriberWithoutInclude(sub.ID);

                if (subscription != null)
                {
                    var startCanteen = await CanteenProvider.GetCanteen(sub.CANTEEN_Canteen_ID.Value);
                    var targetCanteen = await CanteenProvider.GetCanteen(searchFilter.CurrentCanteenID.Value);

                    if (startCanteen != null && targetCanteen != null && startCanteen.ID != targetCanteen.ID)
                    {
                        subscription.CANTEEN_Canteen_ID = searchFilter.CurrentCanteenID;

                        await CanteenProvider.SetSubscriber(subscription);

                        if (sub.AUTH_Users_ID != null)
                        {
                            var msgParameter = new List<MSG_Message_Parameters>()
                            {
                                new MSG_Message_Parameters()
                                {
                                    Code = "{StartCanteen}",
                                    Message = startCanteen.Name
                                },
                                new MSG_Message_Parameters()
                                {
                                    Code = "{TargetCanteen}",
                                    Message = targetCanteen.Name
                                }
                            };

                            if (sub.AUTH_Users_ID != null && sub.AUTH_Municipality_ID != null)
                            {
                                var msg = await MessageService.GetMessage(sub.AUTH_Users_ID.Value, sub.AUTH_Municipality_ID.Value, "NOTIF_CANTEEN_CANTEEN_CHANGED_TEXT", "NOTIF_CANTEEN_CANTEEN_CHANGED_SHORTTEXT", "NOTIF_CANTEEN_CANTEEN_CHANGED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, msgParameter);

                                if (msg != null)
                                {
                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                                }
                            }
                        }
                    }

                }
            }

            SubscribersAccepted = await CanteenProvider.GetVSubscribersByStatusID(CanteenStatus.Accepted);
            Subscribers = await GetData(Filter);

            SelectedItems = new List<V_CANTEEN_Subscriber>();
            BulkEnabled = false;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
    }
    private void ShowReminderWindow()
    {
        ReminderWindowVisible = true;
        StateHasChanged();
    }
    private async void SetReminder()
    {
        ReminderWindowVisible = false;
        BusyIndicatorService.IsBusy = true;
        StateHasChanged();

        var alreadySendNotifications = new List<Guid>();

        foreach (var sub in SelectedItems)
        {
            //MSG
            var subscription = await CanteenProvider.GetSubscriberWithoutInclude(sub.ID);

            if (subscription != null)
            {
                if (subscription.CANTEEN_Subscriber_Status_ID == CanteenStatus.Incomplete)
                {
                    if (CurrentSchoolyear != null && CurrentSchoolyear.RegisterEndDate != null && CurrentSchoolyear.RegisterBeginDate <= DateTime.Now && (CurrentSchoolyear.RegisterEndDate.Value.AddDays(1)) >= DateTime.Now)
                    {
                        subscription.ReminderDate = DateTime.Now;

                        await CanteenProvider.SetSubscriber(subscription);

                        if (sub.AUTH_Users_ID != null && !alreadySendNotifications.Contains(sub.AUTH_Users_ID.Value))
                        {
                            if (sub.AUTH_Users_ID != null && sub.AUTH_Municipality_ID != null)
                            {
                                var msgParameter = new List<MSG_Message_Parameters>()
                                {
                                    new MSG_Message_Parameters()
                                    {
                                        Code = "{Datum}",
                                        Message = CurrentSchoolyear.RegisterEndDate.Value.ToString("dd.MM.yyyy")
                                    },
                                    new MSG_Message_Parameters()
                                    {
                                        Code = "{Link}",
                                        Message = NavManager.BaseUri + "/Canteen/Service"
                                    }
                                };

                                var msg = await MessageService.GetMessage(sub.AUTH_Users_ID.Value, sub.AUTH_Municipality_ID.Value, "NOTIF_CANTEEN_INCOMPLETE_REMINDER_TEXT", "NOTIF_CANTEEN_INCOMPLETE_REMINDER_SHORTTEXT", "NOTIF_CANTEEN_INCOMPLETE_REMINDER_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, msgParameter);

                                if (msg != null)
                                {
                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                                }

                                alreadySendNotifications.Add(sub.AUTH_Users_ID.Value);
                            }
                        }
                    }
                }
            }
        }

        SubscribersAccepted = await CanteenProvider.GetVSubscribersByStatusID(CanteenStatus.Accepted);
        Subscribers = await GetData(Filter);

        SelectedItems = new List<V_CANTEEN_Subscriber>();
        BulkEnabled = false;

        BusyIndicatorService.IsBusy = false;
        StateHasChanged();
    }
    private void ShowChangeStatusWindow()
    {
        TextItem = new TextItem();
        ChangeStatusWindowVisible = true;
        StateHasChanged();
    }
    private void StatusActionChanged()
    {
        StateHasChanged();
    }
}