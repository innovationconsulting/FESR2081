using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.DataStore.MSSQL.Repositories;
using ICWebApp.Domain.DBModels;
using ICWebApp.Pages.Canteen.Backend;
using ICWebApp.Pages.Form.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Canteen.Admin.SubPages
{
    public partial class CanteenPeriod
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }

        private CANTEEN_Canteen? Data { get; set; }
        private List<V_CANTEEN_Schoolyear> Schoolyears { get; set; }
        private List<CANTEEN_Period> PeriodsDetail { get; set; }

        private int actualYear = DateTime.Now.Year;
        private DateTime startDate1 = DateTime.Today;
        private DateTime startDate2 = DateTime.Today.AddMonths(1);
        private DateTime startDate3 = DateTime.Today.AddMonths(2);
        private DateTime startDate4 = DateTime.Today.AddMonths(3);
        private DateTime startDate5 = DateTime.Today.AddMonths(4);
        private DateTime startDate6 = DateTime.Today.AddMonths(5);
        private DateTime startDate7 = DateTime.Today.AddMonths(6);
        private DateTime startDate8 = DateTime.Today.AddMonths(7);
        private DateTime startDate9 = DateTime.Today.AddMonths(8);
        private DateTime startDate10 = DateTime.Today.AddMonths(9);
        private DateTime startDate11 = DateTime.Today.AddMonths(10);
        private DateTime startDate12 = DateTime.Today.AddMonths(11);
        private List<DateTime> DisabledDatesList { get; set; } = new List<DateTime>();

        protected override async Task OnInitializedAsync()
        {
            if (ID == "New")
            {
                ReturnToPreviousPage();
                return;
            }
            else
            {
                Data = await CanteenProvider.GetCanteen(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                    return;
                }
            }

            SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_CANTEENMANAGEMENT");
            SessionWrapper.PageSubTitle = TextProvider.Get("");


            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Canteen/Subscriptionlist", "MAINMENU_CANTEEN", null, null);
            CrumbService.AddBreadCrumb("/Admin/Canteen/CanteenManagement", "CANTEEN_CANTEENMANAGEMENT", null, null, true);

            DisabledDatesList = new List<DateTime>();

            actualYear = DateTime.Now.Year;
            SetYearCalendar(actualYear);

            await GetData();

            StateHasChanged();

            if (DisabledDatesList == null)
            {
                DisabledDatesList = new List<DateTime>();
            }

            for (var startdate = new DateTime(actualYear, 1, 1); startdate <= new DateTime(actualYear, 12, 31); startdate = startdate.AddDays(1))
            {
                if (Schoolyears == null || Schoolyears.Count() == 0)
                {
                    DisabledDatesList.Add(startdate);
                    continue;
                }

                bool Contained = false;

                foreach (var schoolyear in Schoolyears.ToList())
                {
                    if ((startdate >= schoolyear.BeginDate || schoolyear.BeginDate == null) && (startdate < schoolyear.EndDate || schoolyear.EndDate == null))
                    {
                        Contained = true;
                        break;
                    }
                }

                if (Contained)
                {
                    continue;
                }

                DisabledDatesList.Add(startdate);
            }


            BusyIndicatorService.IsBusy = false;

            SetYearCalendar(actualYear);

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                StateHasChanged();
            }

            base.OnAfterRender(firstRender);
        }
        private void SetYearCalendar(int newactualYear)
        {
            actualYear = newactualYear;
            startDate1 = new DateTime(actualYear, 1, 1);
            startDate2 = new DateTime(actualYear, 2, 1);
            startDate3 = new DateTime(actualYear, 3, 1);
            startDate4 = new DateTime(actualYear, 4, 1);
            startDate5 = new DateTime(actualYear, 5, 1);
            startDate6 = new DateTime(actualYear, 6, 1);
            startDate7 = new DateTime(actualYear, 7, 1);
            startDate8 = new DateTime(actualYear, 8, 1);
            startDate9 = new DateTime(actualYear, 9, 1);
            startDate10 = new DateTime(actualYear, 10, 1);
            startDate11 = new DateTime(actualYear, 11, 1);
            startDate12 = new DateTime(actualYear, 12, 1);
            
        }
        private async Task<bool> GetData()
        {
            Schoolyears = await CanteenProvider.GetSchoolsyearAll(SessionWrapper.AUTH_Municipality_ID.Value);

            if (Data != null)
            {
                DisabledDatesList = new List<DateTime>();

                PeriodsDetail = await CanteenProvider.GetPeriods(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
                PeriodsDetail = PeriodsDetail.Where(a => a.CANTEEN_Canteen_ID == Data.ID).ToList();
            }

            return true;
        }
        private string GetDayStatusClass(DateTime date)
        {
            if (Schoolyears == null || Schoolyears.Count() == 0)
            {
                DisabledDatesList.Add(date);
                return "disabled";
            }

            bool Contained = false;

            foreach(var schoolyear in Schoolyears.ToList())
            {                 
                if((date >= schoolyear.BeginDate || schoolyear.BeginDate == null) && (date < schoolyear.EndDate || schoolyear.EndDate == null))
                {
                    Contained = true;
                }
            }

            if (!Contained)
            {
                DisabledDatesList.Add(date);
                return "disabled";
            }

            if (date < DateTime.Now)
            {
                DisabledDatesList.Add(date);
            }

            var dayoftheWeek = date.DayOfWeek;
            if (Data != null && (
                (Data.DayMo == true && dayoftheWeek == DayOfWeek.Monday) ||
                (Data.DayTue == true && dayoftheWeek == DayOfWeek.Tuesday) ||
                (Data.DayWed == true && dayoftheWeek == DayOfWeek.Wednesday) ||
                (Data.DayThu == true && dayoftheWeek == DayOfWeek.Thursday) ||
                (Data.DayFri == true && dayoftheWeek == DayOfWeek.Friday) ||
                (Data.DaySat == true && dayoftheWeek == DayOfWeek.Saturday) ||
                (Data.DaySun == true && dayoftheWeek == DayOfWeek.Sunday))
               )
            {

                string status = "open";

                if (PeriodsDetail != null &&  PeriodsDetail.Where(a => a.FromDate == date).Count() > 0)
                {
                    status = "holliday";
                }

                return status;
            }
            else
            {
                if (DisabledDatesList == null)
                {
                    DisabledDatesList = new List<DateTime>();
                }

                var disableddate = DisabledDatesList.FirstOrDefault(a => a.Date == date);
                if (disableddate != null)
                {
                    DisabledDatesList.Add(date);
                }

                return "disabled";
            }

            return "";

        }
        private async void  SingleSelectionChangeHandler(DateTime newValue)
        {
            if (newValue < DateTime.Now)
            {
                return;                
            }

            var currentSchoolyear = Schoolyears.Where(p => newValue >= p.BeginDate && newValue < p.EndDate).FirstOrDefault();

            if(currentSchoolyear == null)
            {
                return;
            }

            if (currentSchoolyear != null && (currentSchoolyear.EndDate) < newValue)
            {
                return;
            }

            PeriodsDetail = await CanteenProvider.GetPeriods(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
            PeriodsDetail = PeriodsDetail.Where(a => a.CANTEEN_Canteen_ID == Data.ID).ToList();

            var day = PeriodsDetail.FirstOrDefault(a => a.FromDate == newValue);

            if (day == null)
            {
                var schoolyearList = await CanteenProvider.GetSchoolsyearsCurrent(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

                var item = new CANTEEN_Period();
                item.CANTEEN_Canteen_ID = Data.ID;
                item.Active = true;
                item.FromDate = newValue;
                item.ToDate = newValue;
                item.PeriodType = "CLOSED";
                item.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID;
                await CanteenProvider.SetPeriod(item);
                StateHasChanged();

                var movements = await CanteenProvider.GetSubscriberMovementsByCanteen(Data.ID, true);
                movements = movements.Where(a => a.Date == newValue && a.Fee <=0).ToList();
                foreach (var movement in movements)
                {
                    movement.RemovedDate  = DateTime.Now;
                    movement.Info = "Holliday";
                    await CanteenProvider.SetSubscriberMovement(movement);
                }
            }
            else
            {
                await CanteenProvider.RemovePeriod(day.ID);
                StateHasChanged();

                var movements = await CanteenProvider.GetSubscriberMovementsByCanteen(Data.ID, true);
                movements = movements.Where(a => a.Date == newValue && a.Fee <= 0 && a.Info== "Holliday").ToList();
                foreach (var movement in movements)
                {
                    movement.RemovedDate = null;
                    movement.Info = null;
                    await CanteenProvider.SetSubscriberMovement(movement);
                }
            }

            PeriodsDetail = await CanteenProvider.GetPeriods(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
            PeriodsDetail = PeriodsDetail.Where(a => a.CANTEEN_Canteen_ID == Data.ID).ToList();
            StateHasChanged();
        }
        public void Next()
        {
            SetYearCalendar(actualYear + 1);
            StateHasChanged();
        }
        public void Prior()
        {
            SetYearCalendar(actualYear - 1);
            StateHasChanged();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Admin/Canteen/CanteenManagement");
        }
    }
}
