using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor;
using ICWebApp.Pages.Canteen.Frontend;
using ICWebApp.Application.Settings;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace ICWebApp.Pages.Canteen.Backend
{

    public partial class StudentList
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IMailerService MailerService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string? CanteenID { get; set; }
        [Parameter] public string? SchoolID { get; set; }

        private List<CANTEEN_School> Schools = new List<CANTEEN_School>();
        private List<CANTEEN_Canteen> Canteens = new List<CANTEEN_Canteen>();
        private List<V_CANTEEN_Students> MovementsList = new List<V_CANTEEN_Students>();
        private List<CANTEEN_MealMenu?> MenuTypes = new List<CANTEEN_MealMenu?>();
        private List<Guid> SelectedCanteen = new List<Guid>();
        private List<Guid> SelectedSchools = new List<Guid>();
        private List<string> WeekDays = new List<string>();
        private List<string> SelectedWeekDays = new List<string>();
        private bool IsDataBusy { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            if (SessionWrapper.CurrentUser == null || SessionWrapper.AUTH_Municipality_ID == null)
            {
                NavManager.NavigateTo("/Admin/Students/Dashboard");
            }

            SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_STUDENTLIST");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Admin/Canteen/CanteenList", "MAINMENU_CANTEEN", null, null, true);
            CrumbService.AddBreadCrumb("/Admin/Students/Dashboard", "MAINMENU_BACKEND_CANTEEN_STUDENTS_DASHBOARD", null, null);
            CrumbService.AddBreadCrumb("/Canteen/Subscribe", "CANTEEN_STUDENTLIST", null, null, true);

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                MenuTypes = await CanteenProvider.GetCANTEEN_MealMenuList(SessionWrapper.AUTH_Municipality_ID.Value);
                Schools = await CanteenProvider.GetSchools(SessionWrapper.AUTH_Municipality_ID.Value);
                Canteens = await CanteenProvider.GetCanteens(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            WeekDays = new List<string>();
            WeekDays.Add(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_MO"));
            WeekDays.Add(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_TUE"));
            WeekDays.Add(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_WED"));
            WeekDays.Add(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_THU"));
            WeekDays.Add(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_FRI"));

            if (!string.IsNullOrEmpty(SchoolID))
            {
                SelectedSchools.Add(Guid.Parse(SchoolID));
            }

            if (!string.IsNullOrEmpty(CanteenID))
            {
                SelectedCanteen.Add(Guid.Parse(CanteenID));
            }

            IsDataBusy = true;
            StateHasChanged();

            await GetData();



            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var data = await CanteenProvider.GetStudents(SessionWrapper.AUTH_Municipality_ID.Value);

                if (SelectedCanteen != null && SelectedCanteen.Count() != 0)
                {
                    data = data.Where(p => p.CANTEEN_Canteen_ID != null && SelectedCanteen.Contains(p.CANTEEN_Canteen_ID.Value)).ToList();
                }

                if (SelectedSchools != null && SelectedSchools.Count() != 0)
                {
                    data = data.Where(p => p.CANTEEN_School_ID != null && SelectedSchools.Contains(p.CANTEEN_School_ID.Value)).ToList();
                }

                if(SelectedWeekDays != null && SelectedWeekDays.Count() != 0)
                {

                    if (SelectedWeekDays.Contains(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_MO")))
                    {
                        data = data.Where(p => p.DayMo == true).ToList();
                    }
                    if (SelectedWeekDays.Contains(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_TUE")))
                    {
                        data = data.Where(p => p.DayTue == true).ToList();
                    }
                    if (SelectedWeekDays.Contains(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_WED")))
                    {
                        data = data.Where(p => p.DayWed == true).ToList();
                    }
                    if (SelectedWeekDays.Contains(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_THU")))
                    {
                        data = data.Where(p => p.DayThu == true).ToList();
                    }
                    if (SelectedWeekDays.Contains(TextProvider.GetOrCreate("CANTEEN_DASHBOARD_SHORT_FRI")))
                    {
                        data = data.Where(p => p.DayFri == true).ToList();
                    }
                }

                MovementsList = data;
            }

            return true;
        }
        private async void AddCanteenFilter(Guid ID)
        {
            IsDataBusy = true;
            StateHasChanged();

            if (SelectedCanteen == null)
                SelectedCanteen = new List<Guid>();

            if (SelectedCanteen.Contains(ID))
            {
                SelectedCanteen.Remove(ID);

            }
            else
            {
                SelectedCanteen.Add(ID);
            }

            await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async void AddSchoolFilter(Guid ID)
        {
            IsDataBusy = true;
            StateHasChanged();

            if (SelectedSchools == null)
                SelectedSchools = new List<Guid>();

            if (SelectedSchools.Contains(ID))
            {
                SelectedSchools.Remove(ID);

            }
            else
            {
                SelectedSchools.Add(ID);
            }

            await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async void ClearCanteenFilter()
        {
            IsDataBusy = true;
            StateHasChanged();

            SelectedCanteen = new List<Guid>();

            await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async void ClearSchoolFilter()
        {
            IsDataBusy = true;
            StateHasChanged();

            SelectedSchools = new List<Guid>();

            await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async void AddWeekDayFilter(string value)
        {
            IsDataBusy = true;
            StateHasChanged();

            if (SelectedWeekDays == null)
                SelectedWeekDays = new List<string>();

            if (SelectedWeekDays.Contains(value))
            {
                SelectedWeekDays.Remove(value);
            }
            else
            {
                SelectedWeekDays.Add(value);
            }

            await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async void ClearWeekDayFilter()
        {
            IsDataBusy = true;
            StateHasChanged();

            SelectedWeekDays = new List<string>();

            await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ExportToExcel()
        {
            var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add("Data");

            ws.Cell(1, 1).Value = TextProvider.Get("CANTEEN_STUDENT_CANTEEN");
            ws.Cell(1, 2).Value = TextProvider.Get("CANTEEN_STUDENT_SCHOOL");
            ws.Cell(1, 3).Value = TextProvider.Get("CANTEEN_STUDENT_SCHOOL_CLASS");
            ws.Cell(1, 4).Value = TextProvider.Get("CANTEEN_DASHBOARD_NAME");
            ws.Cell(1, 5).Value = TextProvider.Get("CANTEEN_TAXNUMBER");
            ws.Cell(1, 6).Value = TextProvider.Get("CANTEEN_DASHBOARD_SHORT_MO");
            ws.Cell(1, 7).Value = TextProvider.Get("CANTEEN_DASHBOARD_SHORT_TUE");
            ws.Cell(1, 8).Value = TextProvider.Get("CANTEEN_DASHBOARD_SHORT_WED");
            ws.Cell(1, 9).Value = TextProvider.Get("CANTEEN_DASHBOARD_SHORT_THU");
            ws.Cell(1, 10).Value = TextProvider.Get("CANTEEN_DASHBOARD_SHORT_FRI");
            ws.Cell(1, 11).Value = TextProvider.Get("CANTEEN_MEALMENU");
            ws.Cell(1, 12).Value = TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_PATHOLOGIES");

            int rowCount = 1;

            foreach (var column in MovementsList.OrderBy(p => p.CanteenName).ThenBy(p => p.SchoolName).ThenBy(p => p.SchoolClass).ThenBy(p => p.FullName).ToList())
            {
                ws.Cell(rowCount + 1, 1).Value = column.CanteenName;
                ws.Cell(rowCount + 1, 2).Value = column.SchoolName;
                ws.Cell(rowCount + 1, 3).Value = column.SchoolClass;
                ws.Cell(rowCount + 1, 4).Value = column.FullName;
                ws.Cell(rowCount + 1, 5).Value = column.TaxNumber;
                if (column.DayMo == true)
                {
                    ws.Cell(rowCount + 1, 6).Value = "x";
                }
                if (column.DayTue == true)
                {
                    ws.Cell(rowCount + 1, 7).Value = "x";
                }
                if (column.DayWed == true)
                {
                    ws.Cell(rowCount + 1, 8).Value = "x";
                }
                if (column.DayThu == true)
                {
                    ws.Cell(rowCount + 1, 9).Value = "x";
                }
                if (column.DayFri == true)
                {
                    ws.Cell(rowCount + 1, 10).Value = "x";
                }
                ws.Cell(rowCount + 1, 11).Value = TextProvider.Get(column.MenuTextCode);

                string result = "";
                if (column.IsLactoseIntolerance)
                {
                    result = result + TextProvider.GetOrCreate("CANTEEN_CREATE_SUBSCRIPTION_LACTOSE") + ",";
                }
                if (column.IsGlutenIntolerance)
                {
                    result = result + TextProvider.GetOrCreate("CANTEEN_CREATE_SUBSCRIPTION_GLUTEN") + ",";
                }
                if (column.AdditionalIntolerance != null)
                {
                    result = result + column.AdditionalIntolerance;
                }
                result = result.TrimEnd(',');

                ws.Cell(rowCount + 1, 12).Value = result;

                rowCount++;
            }

            var ms = new MemoryStream();

            wb.SaveAs(ms);

            await EnviromentService.DownloadFile(ms.ToArray(), TextProvider.Get("CANTEEN_CREATE_STUDENTLIST") + ".xlsx");

            return true;
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Admin/Students/Dashboard");
            StateHasChanged();
        }
    }
}