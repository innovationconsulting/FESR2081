using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor;
using ICWebApp.Pages.Canteen.Frontend;
using Telerik.Reporting;
using ClosedXML.Excel;
using ICWebApp.Application.Services;

namespace ICWebApp.Pages.Canteen.Backend
{

    public partial class DailyStudentList
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
        private List<V_CANTEEN_Daily_Movements> MovementsList = new List<V_CANTEEN_Daily_Movements>();
        private List<CANTEEN_MealMenu?> MenuTypes = new List<CANTEEN_MealMenu?>();
        private List<Guid> SelectedCanteen = new List<Guid>();
        private List<Guid> SelectedSchools = new List<Guid>();
        private bool IsDataBusy { get; set; } = true;
        private bool ShowSendStudent { get; set; } = false;
        private long AdditionalMeals = 0;
        private CANTEEN_Studentlist_Email Data = new CANTEEN_Studentlist_Email();

        private DateTime _currentDate = DateTime.Now;

        private DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (_currentDate != value)
                {
                    _currentDate = value;
                    DateChanged();
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            if (SessionWrapper.CurrentUser == null || SessionWrapper.AUTH_Municipality_ID == null)
            {
                NavManager.NavigateTo("/Admin/Students/Dashboard");
            }

            SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_STUDENTLIST_DAILY");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Admin/Canteen/CanteenList", "MAINMENU_CANTEEN", null, null, true);
            CrumbService.AddBreadCrumb("/Admin/Students/Dashboard", "MAINMENU_BACKEND_CANTEEN_STUDENTS_DASHBOARD", null, null);
            CrumbService.AddBreadCrumb("/Canteen/Subscribe", "CANTEEN_STUDENTLIST_DAILY", null, null, true);

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                MenuTypes = await CanteenProvider.GetCANTEEN_MealMenuList(SessionWrapper.AUTH_Municipality_ID.Value);
                Schools = await CanteenProvider.GetSchools(SessionWrapper.AUTH_Municipality_ID.Value);
                Canteens = await CanteenProvider.GetCanteens(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            if(!string.IsNullOrEmpty(SchoolID))
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
                var data = await CanteenProvider.GetSubscriberMovementsDaily(SessionWrapper.AUTH_Municipality_ID.Value, CurrentDate);

                if (SelectedCanteen != null && SelectedCanteen.Count() != 0)
                {
                    data = data.Where(p => p.CANTEEN_Canteen_ID != null && SelectedCanteen.Contains(p.CANTEEN_Canteen_ID.Value)).ToList();
                }

                if (SelectedSchools != null && SelectedSchools.Count() != 0)
                {
                    data = data.Where(p => p.CANTEEN_School_ID != null && SelectedSchools.Contains(p.CANTEEN_School_ID.Value)).ToList();
                }

                AdditionalMeals = 0;

                foreach (var school in data.Select(p => p.CANTEEN_School_ID).Distinct().ToList())
                {
                    if (school != null) 
                    {
                        var add = await CanteenProvider.GetAdditionalPersonalList(school.Value);

                        if (add != null && add.FirstOrDefault() != null)
                        {
                            if (CurrentDate.DayOfWeek == DayOfWeek.Monday) 
                            {
                                if (add.FirstOrDefault().MO != null)
                                {
                                    AdditionalMeals += add.FirstOrDefault().MO.Value;
                                }
                                if (add.FirstOrDefault().MOPP != null && add.FirstOrDefault().MOPP > 0)
                                {
                                    AdditionalMeals += long.Parse(Math.Ceiling(decimal.Parse(data.Count().ToString())/decimal.Parse(add.FirstOrDefault().MOPP.ToString())).ToString());
                                }
                            }
                            else if (CurrentDate.DayOfWeek == DayOfWeek.Tuesday)
                            {
                                if (add.FirstOrDefault().DI != null)
                                {
                                    AdditionalMeals += add.FirstOrDefault().DI.Value;
                                }
                                if (add.FirstOrDefault().DIPP != null && add.FirstOrDefault().DIPP > 0)
                                {
                                    AdditionalMeals += long.Parse(Math.Ceiling(decimal.Parse(data.Count().ToString()) / decimal.Parse(add.FirstOrDefault().DIPP.ToString())).ToString());
                                }
                            }
                            else if (CurrentDate.DayOfWeek == DayOfWeek.Wednesday)
                            {
                                if (add.FirstOrDefault().MI != null)
                                {
                                    AdditionalMeals += add.FirstOrDefault().MI.Value;
                                }
                                if (add.FirstOrDefault().MIPP != null && add.FirstOrDefault().MIPP > 0)
                                {
                                    AdditionalMeals += long.Parse(Math.Ceiling(decimal.Parse(data.Count().ToString()) / decimal.Parse(add.FirstOrDefault().MIPP.ToString())).ToString());
                                }
                            }
                            else if (CurrentDate.DayOfWeek == DayOfWeek.Thursday)
                            {
                                if (add.FirstOrDefault().DO != null)
                                {
                                    AdditionalMeals += add.FirstOrDefault().DO.Value;
                                }
                                if (add.FirstOrDefault().DOPP != null && add.FirstOrDefault().DOPP > 0)
                                {
                                    AdditionalMeals += long.Parse(Math.Ceiling(decimal.Parse(data.Count().ToString()) / decimal.Parse(add.FirstOrDefault().DOPP.ToString())).ToString());
                                }
                            }
                            else if (CurrentDate.DayOfWeek == DayOfWeek.Friday)
                            {
                                if (add.FirstOrDefault().FR != null)
                                {
                                    AdditionalMeals += add.FirstOrDefault().FR.Value;
                                }
                                if (add.FirstOrDefault().FRPP != null && add.FirstOrDefault().FRPP > 0)
                                {
                                    AdditionalMeals += long.Parse(Math.Ceiling(decimal.Parse(data.Count().ToString()) / decimal.Parse(add.FirstOrDefault().FRPP.ToString())).ToString());
                                }
                            }
                        } 
                    }
                }

                MovementsList = data;
            }

            return true;
        }
        private void ShowSendStudentList()
        {
            Data = new CANTEEN_Studentlist_Email();

            ShowSendStudent = true;
            StateHasChanged();
        }
        private void HideSendStudentList()
        {
            ShowSendStudent = false;
            StateHasChanged();
        }
        private async Task<bool> SendStudentList()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            HideSendStudentList();

            await Task.Delay(1);

            if (Data != null && !string.IsNullOrEmpty(Data.Email))
            {
                
                List<Guid> CanteensToFetch = new List<Guid>();

                if(SelectedCanteen == null || SelectedCanteen.Count == 0)
                {
                    CanteensToFetch = Canteens.Select(p => p.ID).ToList();
                }
                else
                {
                    CanteensToFetch = SelectedCanteen;
                }

                var mailsToSend = Data.Email.Split(";");

                foreach (var mail in mailsToSend)
                {
                    MSG_Mailer msg = new MSG_Mailer();

                    msg.ID = Guid.NewGuid();
                    msg.ToAdress = mail;

                    msg.Subject = TextProvider.Get("CANTEEN_DAILY_STUNDENTLIST");
                    msg.Body = TextProvider.Get("CANTEEN_DAILY_STUNDENTLIST_BODY");

                    List<MSG_Mailer_Attachment> attachments = new List<MSG_Mailer_Attachment>();

                    foreach (var canteenID in CanteensToFetch)
                    {
                        var canteen = Canteens.FirstOrDefault(p => p.ID == canteenID);


                        if (canteen != null && canteen.Name != null)
                        {
                            var DE = CanteenProvider.GetStundentListFile(Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"), canteenID, SelectedSchools);
                            var IT = CanteenProvider.GetStundentListFile(Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"), canteenID, SelectedSchools);

                            if (DE != null && DE.Length > 0)
                            {
                                MSG_Mailer_Attachment attachment = new MSG_Mailer_Attachment();

                                attachment.ID = Guid.NewGuid();
                                attachment.MSG_MailerID = msg.ID;
                                attachment.FileName = TextProvider.Get("CANTEEN_DAILY_STUNDENTLIST_FILENAME", Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075")) + "_" + canteen.Name.Replace(" ", "_") + ".pdf";
                                attachment.FileData = DE;

                                attachments.Add(attachment);
                            }
                            if (IT != null && IT.Length > 0)
                            {
                                MSG_Mailer_Attachment attachment = new MSG_Mailer_Attachment();

                                attachment.ID = Guid.NewGuid();
                                attachment.MSG_MailerID = msg.ID;
                                attachment.FileName = TextProvider.Get("CANTEEN_DAILY_STUNDENTLIST_FILENAME", Guid.Parse("e450421a-baff-493e-a390-71b49be6485f")) + "_" + canteen.Name.Replace(" ", "_") + ".pdf";
                                attachment.FileData = IT;

                                attachments.Add(attachment);
                            }
                        }
                    }

                    await MailerService.SendMail(msg, attachments, SessionWrapper.AUTH_Municipality_ID.Value);
                }                
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

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
        private async Task<bool> ExportToExcel()
        {
            var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add("Data");

            ws.Cell(1, 1).Value = TextProvider.Get("CANTEEN_STUDENT_CANTEEN");
            ws.Cell(1, 2).Value = TextProvider.Get("CANTEEN_STUDENT_SCHOOL");
            ws.Cell(1, 3).Value = TextProvider.Get("CANTEEN_STUDENT_SCHOOL_CLASS");
            ws.Cell(1, 4).Value = TextProvider.Get("CANTEEN_DASHBOARD_NAME");
            ws.Cell(1, 5).Value = TextProvider.Get("CANTEEN_TAXNUMBER");
            ws.Cell(1, 6).Value = TextProvider.Get("CANTEEN_MEALMENU");
            ws.Cell(1, 7).Value = TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_PATHOLOGIES");

            int rowCount = 1;

            foreach (var column in MovementsList.OrderBy(p => p.CanteenName).ThenBy(p => p.SchoolName).ThenBy(p => p.SchoolClass).ThenBy(p => p.FullName).ToList())
            {
                ws.Cell(rowCount + 1, 1).Value = column.CanteenName;                
                ws.Cell(rowCount + 1, 2).Value = column.SchoolName;
                ws.Cell(rowCount + 1, 3).Value = column.SchoolClass;
                ws.Cell(rowCount + 1, 4).Value = column.FullName;
                ws.Cell(rowCount + 1, 5).Value = column.TaxNumber;
                ws.Cell(rowCount + 1, 6).Value = TextProvider.Get(column.MenuTextCode);

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

                ws.Cell(rowCount + 1, 7).Value = result;

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
        private async Task<bool> DecreaseDate()
        {
            IsDataBusy = true;
            StateHasChanged();

            CurrentDate = CurrentDate.AddDays(-1);

            await GetData();

            IsDataBusy = false;
            StateHasChanged();

            return true;
        }
        private async Task<bool> IncreaseDate()
        {
            IsDataBusy = true;
            StateHasChanged();

            CurrentDate = CurrentDate.AddDays(1);

            await GetData();

            IsDataBusy = false;
            StateHasChanged();

            return true;
        }
        private async Task<bool> DateChanged()
        {
            IsDataBusy = true;
            StateHasChanged();

            await GetData();

            IsDataBusy = false;
            StateHasChanged();

            return true;
        }
    }
}