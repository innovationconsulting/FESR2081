using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class TaxReports
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        private List<V_CANTEEN_User_Tax_Reports> _reports = new List<V_CANTEEN_User_Tax_Reports>();
        private List<CANTEEN_Subscriber> _distinctChildren = new List<CANTEEN_Subscriber>();

        private List<V_CANTEEN_User_Tax_Reports> _reportsFiltered = new List<V_CANTEEN_User_Tax_Reports>();

        private List<int?> years = new List<int?>();

        private int? _selectedYear = null;

        private int? SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (value != _selectedYear)
                {
                    _selectedYear = value;
                    FilterReports();
                    StateHasChanged();
                }
            }
        }

        private int SelectedLanguage { get; set; }

        private string? _selectedChild = null;

        private string? SelectedChild
        {
            get => _selectedChild;
            set
            {
                if (value != _selectedChild)
                {
                    _selectedChild = value;
                    FilterReports();
                    StateHasChanged();
                }
            }
        }

        private Guid AccordionID = Guid.NewGuid();
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.GetOrCreate("CANTEEN_TAX_REPORT_PAGE_TITLE");
            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/TaxReports", "CANTEEN_DASHBOARD_TAXREPORTS", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/TaxReports", "CANTEEN_DASHBOARD_TAXREPORTS", null, null);
            }

            var currentUserId = SessionWrapper.CurrentUser.ID;
            var municipalityId = SessionWrapper.AUTH_Municipality_ID;
            if (municipalityId != null)
            {
                var canteenUser = CanteenProvider.GetCanteenUserByID(currentUserId, municipalityId.Value);
                if (canteenUser != null)
                {
                    _reports = await CanteenProvider.GetTaxReportsForCanteenUser(canteenUser.ID);
                    _reportsFiltered = GetDistinctChildYearCombinations();
                    years = _reports.Select(e => e.Year as int?).Distinct().OrderByDescending(e => e).ToList();
                    await GetDistinctChildren();
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }

        private async Task GetDistinctChildren()
        {
            var taxnumbers = _reports.Select(e => e.SubsriberTaxNumber).Distinct().ToList();
            foreach (var taxnumber in taxnumbers)
            {
                var subscriber = await CanteenProvider.GetLatestCanteenSubscriberByTaxNumber(taxnumber);
                if (subscriber != null)
                {
                    _distinctChildren.Add(subscriber);
                }
            }
        }

        private void FilterReports()
        {
            _reportsFiltered = GetDistinctChildYearCombinations();
            if (SelectedYear != null)
            {
                _reportsFiltered = _reportsFiltered.Where(e => e.Year == SelectedYear).ToList();
            }

            if (!string.IsNullOrEmpty(SelectedChild))
            {
                _reportsFiltered = _reportsFiltered
                    .Where(e => e.SubsriberTaxNumber == SelectedChild).ToList();
            }
        }

        private List<V_CANTEEN_User_Tax_Reports> GetDistinctChildYearCombinations()
        {
            var list = new List<V_CANTEEN_User_Tax_Reports>();
            foreach (var report in _reports)
            {
                if (!list.Any(e => e.Year == report.Year && e.SubsriberTaxNumber == report.SubsriberTaxNumber))
                {
                    list.Add(report);
                }
            }

            return list.OrderByDescending(e => e.Year).ThenBy(e => e.SubsriberTaxNumber).ToList();
        }

        private async void DownloadItem(Guid reportId)
        {
            var report = _reports.FirstOrDefault(e => e.ID == reportId);
            if (report != null)
            {
                await DownloadRessource(report.FILE_FileInfo_ID, report.Year + "_" + report.SubsriberTaxNumber);
            }
        }

        private async Task DownloadRessource(Guid FILE_Fileinfo_ID, string? Name)
        {
            var fileToDownload = await FileProvider.GetFileInfoAsync(FILE_Fileinfo_ID);

            if (fileToDownload != null)
            {
                FILE_FileStorage? blob = null;
                if (fileToDownload.FILE_FileStorage != null && fileToDownload.FILE_FileStorage.Count() > 0)
                {
                    blob = fileToDownload.FILE_FileStorage.FirstOrDefault();
                }
                else
                {
                    blob = await FileProvider.GetFileStorageAsync(fileToDownload.ID);
                }

                if (blob != null && blob.FileImage != null)
                {
                    if (string.IsNullOrEmpty(Name))
                    {
                        await EnviromentService.DownloadFile(blob.FileImage,
                            fileToDownload.FileName + fileToDownload.FileExtension);
                    }
                    else
                    {
                        await EnviromentService.DownloadFile(blob.FileImage, Name + fileToDownload.FileExtension, true);
                    }
                }
            }

            StateHasChanged();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Service");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Service");
            }
        }
    }
}