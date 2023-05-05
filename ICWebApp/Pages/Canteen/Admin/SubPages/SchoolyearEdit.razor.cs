using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Common.Dropdowns;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Canteen.Admin.SubPages
{
    public partial class SchoolyearEdit
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string ID { get; set; }

        private CANTEEN_Schoolyear? Data { get; set; }
        private List<StringDropDownItem> YearList = new List<StringDropDownItem>();
        private List<CANTEEN_Schoolyear_Base_Informations> SchoolYear_BaseInformations = new List<CANTEEN_Schoolyear_Base_Informations>();
        private List<CANTEEN_Schoolyear_RegistrationPeriods> Periods = new List<CANTEEN_Schoolyear_RegistrationPeriods>();
        private bool ShowPeriodWindow = false;
        private CANTEEN_Schoolyear_RegistrationPeriods? PeriodToEdit;
        protected override async Task OnInitializedAsync()
        {
            if (ID == "New")
            {
                SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_CANTEEN_SCHOOLSYEAR_ADD");

                Data = new CANTEEN_Schoolyear();
                int _actualYear = DateTime.Today.Year;

                Data.ID = Guid.NewGuid();
                Data.DisplayText = _actualYear.ToString() + "-" + (_actualYear + 1).ToString();
                Data.BeginDate = DateTime.Now;
                Data.EndDate = DateTime.Now.AddYears(1);


                if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
                {
                    Data.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value;
                }
            }
            else
            {
                SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_CANTEEN_SCHOOLSYEAR_EDIT");

                Data = await CanteenProvider.GetSchoolyear(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                    return;
                }
            }

            SchoolYear_BaseInformations = await CanteenProvider.GetSchoolyear_BaseInformations();

            var startdate = DateTime.Parse("01.01.2021");

            while (startdate < DateTime.Now.AddYears(2))
            {
                YearList.Add(new StringDropDownItem()
                {
                    Value = startdate.Year.ToString() + "-" + (startdate.Year + 1).ToString(),
                    Text = startdate.Year.ToString() + "-" + (startdate.Year + 1).ToString()
                });

                startdate = startdate.AddYears(1);
            }

            if (ID == "New")
            {
                SetStartEndDate(Data);
            }
            Periods = await CanteenProvider.GetRegistrationPeriodList(Data.ID);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void SetStartEndDate(CANTEEN_Schoolyear? _schoolYear)
        {
            if (_schoolYear != null)
            {
                CANTEEN_Schoolyear_Base_Informations? _matchingItem = SchoolYear_BaseInformations.FirstOrDefault(p => p.SchoolYears == _schoolYear.DisplayText);
                if (_matchingItem != null)
                {
                    if (_matchingItem.BeginYear != null)
                    {
                        _schoolYear.BeginYear = _matchingItem.BeginYear.Value;
                    }
                    if (_matchingItem.Startdate != null)
                    {
                        _schoolYear.BeginDate = _matchingItem.Startdate;
                    }
                    if (_matchingItem.EndYear != null)
                    {
                        _schoolYear.EndYear = _matchingItem.EndYear.Value;
                    }
                    if (_matchingItem.Enddate != null)
                    {
                        _schoolYear.EndDate = _matchingItem.Enddate;
                    }
                }
            }
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Admin/Canteen/SchoolyearManagement");
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (Data != null)
            {
                if (!string.IsNullOrEmpty(Data.DisplayText))
                {
                    var beginyear = Data.DisplayText.Split("-")[0];
                    var endyear = Data.DisplayText.Split("-")[1];

                    Data.BeginYear = int.Parse(beginyear);
                    Data.EndYear = int.Parse(endyear);
                }

                await CanteenProvider.SetSchoolyear(Data);
            }

            NavManager.NavigateTo("/Admin/Canteen/SchoolyearManagement");
        }
        private void AddPeriod()
        {
            if (Data != null)
            {
                PeriodToEdit = new CANTEEN_Schoolyear_RegistrationPeriods();

                PeriodToEdit.ID = Guid.NewGuid();
                PeriodToEdit.BeginDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                PeriodToEdit.EndDate = new DateTime(DateTime.Now.Year + 1, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

                PeriodToEdit.CANTEEN_Schoolyear_ID = Data.ID;

                ShowPeriodWindow = true;
                StateHasChanged();
            }
        }
        private void EditPeriod(CANTEEN_Schoolyear_RegistrationPeriods Item)
        {
            PeriodToEdit = Item;

            ShowPeriodWindow = true;
            StateHasChanged();
        }
        private async void SavePeriod()
        {
            if(PeriodToEdit != null)
            {

                Data = await CanteenProvider.SetSchoolyear(Data);

                await CanteenProvider.SetRegistrationPeriod(PeriodToEdit);
                HidePeriodWindow();
            }
        }
        private async void RemovePeriod(CANTEEN_Schoolyear_RegistrationPeriods Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE_PERIOD"), TextProvider.Get("WARNING")))
                    return;

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await CanteenProvider.RemoveRegistrationPeriod(Item.ID);
                Periods = await CanteenProvider.GetRegistrationPeriodList(Data.ID);

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private async void HidePeriodWindow()
        {
            PeriodToEdit = null;

            Periods = await CanteenProvider.GetRegistrationPeriodList(Data.ID);

            ShowPeriodWindow = false;
            StateHasChanged();
        }
    }
}
