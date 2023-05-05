using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Components.Canteen
{
    public partial class SubscriptionElementComponent
    {
        [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private ICANTEENProvider CanteenProvider { get; set; }
        [Inject] private ITEXTProvider TextProvider { get; set; }
        [Inject] private NavigationManager NavManager { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public CANTEEN_Subscriber SubscriberDetail { get; set; }
        [Parameter] public CANTEEN_Subscriber? PreviousDetail { get; set; } = null;
        public int CurrentTab { get; set; }

        private List<CANTEEN_Subscriber> Subscribers = new();
        private IEnumerable<CANTEEN_Subscriber> SelectedItems = new List<CANTEEN_Subscriber>();
        private IEnumerable<CANTEEN_Subscriber> SubscribersAccepted = new List<CANTEEN_Subscriber>();
        private List<CANTEEN_School> Schools = new();
        private List<CANTEEN_Canteen> Canteens = new();
        private List<CANTEEN_Subscriber_Status> Statuslist = new();
        private List<CANTEEN_Subscriber_Status> StatusActionlist = new();
        private List<V_CANTEEN_Schoolyear> SchoolyearList = new();
        private CANTEEN_Subscriber_Status CurrentStatus = new();
        private CANTEEN_School CurrentSchool = new();
        private CANTEEN_Canteen? CurrentCanteen = new();
        private CANTEEN_Subscriber_Status NewStatusAction = new();
        private decimal CurrentBalance = 0;

        //private List<CANTEEN_Subscriber_Movements> LatestMovements = new List<CANTEEN_Subscriber_Movements>();
        private List<V_CANTEEN_Subscriber_Movements> LatestMovements = new List<V_CANTEEN_Subscriber_Movements>();

        //private List<CANTEEN_Subscriber_Movements> NextMovements = new List<CANTEEN_Subscriber_Movements>();
        private List<V_CANTEEN_Subscriber_Movements> NextMovements = new List<V_CANTEEN_Subscriber_Movements>();

        private Guid? CanteenStartSelection;
        private bool ShowEditMensaWindow { get; set; } = false;
        private CANTEEN_Subscriber? SubscriberEditDetail = null;

        protected override async Task OnParametersSetAsync()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.ID == null ||
                SessionWrapper.CurrentUser.AUTH_Municipality_ID == null) NavManager.NavigateTo("/Canteen");

            SelectedItems = new List<CANTEEN_Subscriber>();

            Statuslist = await CanteenProvider.GetSubscriberStatuses();
            StatusActionlist = Statuslist.Where(a => a.ActionEnabled == true).ToList();

            Schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            var CanteenData = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

            if (SubscriberDetail.DayMo == true)
            {
                CanteenData = CanteenData.Where(p => p.DayMo == SubscriberDetail.DayMo).ToList();
            }
            if (SubscriberDetail.DayTue == true)
            {
                CanteenData = CanteenData.Where(p => p.DayTue == SubscriberDetail.DayTue).ToList();
            }
            if (SubscriberDetail.DayWed == true)
            {
                CanteenData = CanteenData.Where(p => p.DayWed == SubscriberDetail.DayWed).ToList();
            }
            if (SubscriberDetail.DayThu == true)
            {
                CanteenData = CanteenData.Where(p => p.DayThu == SubscriberDetail.DayThu).ToList();
            }
            if (SubscriberDetail.DayFri == true)
            {
                CanteenData = CanteenData.Where(p => p.DayFri == SubscriberDetail.DayFri).ToList();
            }

            Canteens = CanteenData;

            CurrentCanteen = Canteens.FirstOrDefault(p => p.ID == SubscriberDetail.CANTEEN_Canteen_ID);

            StateHasChanged();

            Subscribers = await CanteenProvider.GetSubscribersByStatusID(CurrentStatus.ID);

            CurrentBalance = CanteenProvider.GetUserBalance(SubscriberDetail.AUTH_Users_ID ?? Guid.Empty);

            LatestMovements = await CanteenProvider.GetPastVSubscriberMovementsBySubscriber(SubscriberDetail.ID);
            NextMovements = await CanteenProvider.GetFutureVSubscriberMovementsBySubscriber(SubscriberDetail.ID);

            SchoolyearList = await CanteenProvider.GetSchoolsyearAll(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

            CurrentStatus = Statuslist.FirstOrDefault();
            CurrentSchool = Schools.FirstOrDefault();

            BusyIndicatorService.IsBusy = false;

            CanteenStartSelection = SubscriberDetail.CANTEEN_Canteen_ID;

            if (SubscriberDetail.CreationDate == null) SubscriberDetail.CreationDate = DateTime.Today;

            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async void DailyCancellation(V_CANTEEN_Subscriber_Movements item)
        {
            if (item != null)
            {
                if (item.CancelDate != null)
                {
                    item.CancelDate = null;
                    var movement = await CanteenProvider.GetSubscriberMovementById(item.ID);
                    if (movement != null)
                    {
                        movement.CancelDate = null;
                        await CanteenProvider.SetSubscriberMovement(movement);
                    }
                }
                else
                {
                    if (item.Date == DateTime.Today && DateTime.Now.Hour >= 10)
                    {
                        return;
                    }
                    item.CancelDate = DateTime.Now;
                    var movement = await CanteenProvider.GetSubscriberMovementById(item.ID);
                    if (movement != null)
                    {
                        movement.CancelDate = DateTime.Now;
                        await CanteenProvider.SetSubscriberMovement(movement);
                    }
                }
            }
            


            StateHasChanged();
        }
        private async void SaveCanteenChange()
        {
            if (!await Dialogs.ConfirmAsync(TextProvider.Get("CHANGE_ARE_YOU_SURE_CANTEEN"), TextProvider.Get("WARNING")))
                return;

            if (SubscriberEditDetail != null && SubscriberEditDetail.CANTEEN_Canteen_ID != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await CanteenProvider.SetSubscriber(SubscriberEditDetail);

                CanteenStartSelection = SubscriberEditDetail.CANTEEN_Canteen_ID;

                SubscriberDetail = SubscriberEditDetail;
                SubscriberEditDetail = null;

                ShowEditMensaWindow = false;
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private void OnStepChanged()
        {
            StateHasChanged();
        }        
        private void EditCanteen()
        {
            SubscriberEditDetail = SubscriberDetail;

            ShowEditMensaWindow = true;
            StateHasChanged();
        }
        private void CancelEdit()
        {
            ShowEditMensaWindow = false;
            StateHasChanged();
        }
    }
}
