using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class CreateSubscriptions
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Inject] IMETAProvider MetaProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string? FamilyID { get; set; }
        [Parameter] public string? Edit { get; set; }

        private List<CANTEEN_Subscriber> Subscribers = new List<CANTEEN_Subscriber>();
        private List<V_CANTEEN_Subscriber_Previous> PreviousSubscribers = new List<V_CANTEEN_Subscriber_Previous>();
        private List<CANTEEN_School> Schools = new List<CANTEEN_School>();
        private List<CANTEEN_Canteen> Canteens = new List<CANTEEN_Canteen>();
        private List<V_CANTEEN_Schoolyear_Current> SchoolyearList = new List<V_CANTEEN_Schoolyear_Current>();
        private List<CANTEEN_MealMenu> MealList = new List<CANTEEN_MealMenu>();
        private List<CANTEEN_SchoolClass> SchoolClassList = new List<CANTEEN_SchoolClass>();
        private List<FILE_FileInfo> FileList = new List<FILE_FileInfo>();
        private List<META_IstatComuni>? MunicipalitiesList { get; set; }
        private PRIV_Privacy? Privacy { get; set; }
        private Guid ReferenceID;
        private long? FirstSubscriberNumber { get; set; }
        private bool IsDataBusy { get; set; } = true;
        private bool ContinueNotFinished { get; set; } = false;
        private bool PrivacyAccept { get; set; } = false;
        private bool MultipleInsert { get; set; } = true;
        public string? PrivacyError { get; set; }
        private bool Editable { get; set; } = true;
        private bool ShowPreviousWindow { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.ID == null || SessionWrapper.CurrentUser.AUTH_Municipality_ID == null)
            {
                if (MyCivisService.Enabled == true)
                {
                    NavManager.NavigateTo("/Canteen/MyCivis");
                }
                else
                {
                    NavManager.NavigateTo("/Canteen");
                }
            }

            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Subscribe", "CANTEEN_CREATE_SUBSCRIPTION_TITLE", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/Subscribe", "CANTEEN_CREATE_SUBSCRIPTION_TITLE", null, null);
            }

            ReferenceID = Guid.NewGuid();

            Schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            Canteens = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            SchoolyearList = await CanteenProvider.GetSchoolsyearsCurrent(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, true);
            MealList = await CanteenProvider.GetCANTEEN_MealMenuList(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
            SchoolClassList = await CanteenProvider.GetSchoolClassList();

            if (FamilyID != null)
            {
                SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_TITLE");

                var subList = await CanteenProvider.GetSubscribersByFamilyID(Guid.Parse(FamilyID));

                if (subList.Count() > 0)
                {
                    foreach (var oldsub in subList)
                    {
                        if (Subscribers.Count() > 0)
                        {
                            oldsub.OrderCount = Subscribers.Max(p => p.OrderCount) + 10;
                        }
                        else
                        {
                            oldsub.OrderCount = 0;
                        }

                        Subscribers.Add(oldsub);
                    }

                    MultipleInsert = false;

                    if (subList.Where(p => p.PrivacyDate != null).Any())
                    {
                        PrivacyAccept = true;
                    }

                    foreach (var sub in Subscribers)
                    {
                        sub.SchoolClasses = SchoolClassList.Where(p => p.SchoolTypeID == sub.CANTEEN_School.SchoolTypeID).ToList();

                        if (sub.MedicalFiles == null)
                        {
                            sub.MedicalFiles = new List<FILE_FileInfo>();
                        }

                        if (sub.FILE_FileInfo_SpecialMenu_ID != null)
                        {
                            var fileInfo = FileProvider.GetFileInfo(sub.FILE_FileInfo_SpecialMenu_ID.Value);

                            if (fileInfo != null)
                            {
                                sub.MedicalFiles.Add(fileInfo);
                            }
                        }

                        CanteenChanged(sub);
                    }
                }
            }
            else
            {
                SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_TITLE");

                var sub = new CANTEEN_Subscriber();

                sub.ID = Guid.NewGuid();

                sub.OrderCount = 0;
                sub.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                sub.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID;
                sub.CreationDate = DateTime.Now;
                sub.SubscriptionFamilyID = ReferenceID;

                FirstSubscriberNumber = await CanteenProvider.GetNextSubscriberNummer();

                if (FirstSubscriberNumber != null)
                {
                    sub.ReferenceID = FirstSubscriberNumber.Value;
                }
                else
                {
                    sub.ReferenceID = 0;
                }

                sub.MedicalFiles = new List<FILE_FileInfo>();
                sub.SchoolyearID = SchoolyearList.FirstOrDefault().id;
                sub.SchoolyearDescription = SchoolyearList.FirstOrDefault().DisplayText;

                var anagrafic = await AUTHProvider.GetAnagraficByUserID(sub.AUTH_Users_ID ?? Guid.Empty);

                if (anagrafic != null)
                {
                    sub.UserAdress = anagrafic.Address;
                    sub.UserCountryOfBirth = anagrafic.CountyOfBirth;
                    sub.UserDateOfBirth = anagrafic.DateOfBirth;
                    sub.UserDomicileMunicipality = anagrafic.DomicileMunicipality;
                    sub.UserDomicileNation = anagrafic.DomicileNation;
                    sub.UserDomicilePostalCode = anagrafic.DomicilePostalCode;
                    sub.UserDomicileProvince = anagrafic.DomicileProvince;
                    sub.UserDomicileStreetAdress = anagrafic.DomicileStreetAddress;
                    sub.UserEmail = anagrafic.Email;
                    sub.UserFirstName = anagrafic.FirstName;
                    sub.UserGender = anagrafic.Gender;
                    sub.UserLastName = anagrafic.LastName;
                    sub.UserMobilePhone = anagrafic.MobilePhone;
                    sub.UserTaxNumber = anagrafic.FiscalNumber;
                    sub.UserPlaceOfBirth = anagrafic.PlaceOfBirth;
                }

                Subscribers.Add(sub);
            }

            if (!string.IsNullOrEmpty(Edit))
            {
                Editable = false;
            }

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Privacy = await PrivProvider.GetPrivacy(SessionWrapper.AUTH_Municipality_ID.Value);
            }


            if (string.IsNullOrEmpty(FamilyID) && SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                PreviousSubscribers = await CanteenProvider.GetPreviousSubscriber(SessionWrapper.CurrentUser.ID);
            }

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void SessionWrapper_OnCurrentUserChanged()
        {
            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                BusyIndicatorService.IsBusy = true;

                if (MyCivisService.Enabled == true)
                {
                    NavManager.NavigateTo("/Canteen/MyCivis");
                }
                else
                {
                    NavManager.NavigateTo("/Canteen");
                }

                StateHasChanged();
            }
        }
        private async void OnRemoveFile(Guid File_Info_ID)
        {
            await FileProvider.RemoveFileInfo(File_Info_ID);
            StateHasChanged();
        }
        private async void SchoolChanged(CANTEEN_Subscriber Data)
        {
            if (Data != null && Data.CANTEEN_School_ID != null)
            {
                if (SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
                {
                    var CurrentSchool = await CanteenProvider.GetSchool(Data.CANTEEN_School_ID.Value);
                    Canteens = await CanteenProvider.GetCanteensBySchool(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, Data.CANTEEN_School_ID.Value);
                    Canteens = Canteens.Where(a => a.IsActive == true).ToList();

                    Data.SchoolClasses = await CanteenProvider.GetSchoolClassList();
                    Data.SchoolClasses = SchoolClassList.Where(a => a.SchoolTypeID == CurrentSchool.SchoolTypeID).ToList();

                    if (Canteens.Count() == 1)
                    {
                        Data.CANTEEN_Canteen_ID = Canteens.FirstOrDefault().ID;
                        Data.CANTEEN_Canteen_IDReq = Canteens.FirstOrDefault().ID;
                    }

                    StateHasChanged();

                }
            }

            CanteenChanged(Data);
        }
        private async void CanteenChanged(CANTEEN_Subscriber Data)
        {
            if (Data != null && Data.CANTEEN_Canteen_ID != null && Data.SchoolyearID != null && Data.CANTEEN_School_ID != null)
            {
                var period = await CanteenProvider.GetSchoolyear(Data.SchoolyearID.Value);
                var currentCanteen = await CanteenProvider.GetCanteen(Data.CANTEEN_Canteen_ID ?? Guid.Empty);
                var currentSchool = await CanteenProvider.GetSchool(Data.CANTEEN_School_ID.Value);

                if (period != null)
                {
                    Data.Begindate = period.BeginDate;
                    Data.Enddate = period.EndDate;
                }

                if (Data.DayMo == true && (currentCanteen.DayMo == false || currentSchool.DayMo == false)) { Data.DayMo = false; }
                if (Data.DayTue == true && (currentCanteen.DayTue == false || currentSchool.DayTue == false)) { Data.DayTue = false; }
                if (Data.DayThu == true && (currentCanteen.DayThu == false || currentSchool.DayThu == false)) { Data.DayThu = false; }
                if (Data.DayWed == true && (currentCanteen.DayWed == false || currentSchool.DayWed == false)) { Data.DayWed = false; }
                if (Data.DayFri == true && (currentCanteen.DayFri == false || currentSchool.DayFri == false)) { Data.DayFri = false; }
                if (Data.DaySat == true && (currentCanteen.DaySat == false || currentSchool.DaySat == false)) { Data.DaySat = false; }
                if (Data.DaySun == true && (currentCanteen.DaySun == false || currentSchool.DaySun == false)) { Data.DaySun = false; }

                if (currentCanteen != null && currentSchool != null)
                {
                    if (currentCanteen.DayMo && currentSchool.DayMo)
                    {
                        Data.EnableDayMo = true;
                    }
                    else
                    {
                        Data.EnableDayMo = false;
                    }
                    if (currentCanteen.DayTue && currentSchool.DayTue)
                    {
                        Data.EnableDayTue = true;
                    }
                    else
                    {
                        Data.EnableDayTue = false;
                    }
                    if (currentCanteen.DayWed && currentSchool.DayWed)
                    {
                        Data.EnableDayWed = true;
                    }
                    else
                    {
                        Data.EnableDayWed = false;
                    }
                    if (currentCanteen.DayThu && currentSchool.DayThu)
                    {
                        Data.EnableDayThu = true;
                    }
                    else
                    {
                        Data.EnableDayThu = false;
                    }
                    if (currentCanteen.DayFri && currentSchool.DayFri)
                    {
                        Data.EnableDayFri = true;
                    }
                    else
                    {
                        Data.EnableDayFri = false;
                    }
                    if (currentCanteen.DaySat && currentSchool.DaySat)
                    {
                        Data.EnableDaySat = true;
                    }
                    else
                    {
                        Data.EnableDaySat = false;
                    }
                    if (currentCanteen.DaySun && currentSchool.DaySun)
                    {
                        Data.EnableDaySun = true;
                    }
                    else
                    {
                        Data.EnableDaySun = false;
                    }
                }

                //Data.CANTEEN_Canteen = currentCanteen;


                StateHasChanged();
            }
        }
        private async void MealChanged(CANTEEN_Subscriber Data)
        {
            StateHasChanged();
        }
        private async void AddSubscriber(V_CANTEEN_Subscriber_Previous? CopyFrom = null)
        {
            IsDataBusy = true;
            StateHasChanged();

            var sub = new CANTEEN_Subscriber();

            if (Subscribers.Count() > 0) 
            { 
                sub.OrderCount = Subscribers.Max(p => p.OrderCount) + 10;
            }
            else
            {
                sub.OrderCount = 0;
            }

            sub.ID = Guid.NewGuid();
            sub.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
            sub.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID;
            sub.CreationDate = DateTime.Now;
            sub.SchoolyearID = SchoolyearList.FirstOrDefault().id;
            sub.SchoolyearDescription = SchoolyearList.FirstOrDefault().DisplayText;

            var anagrafic = await AUTHProvider.GetAnagraficByUserID(sub.AUTH_Users_ID ?? Guid.Empty);

            if (anagrafic != null)
            {
                sub.UserAdress = anagrafic.Address;
                sub.UserCountryOfBirth = anagrafic.CountyOfBirth;
                sub.UserDateOfBirth = anagrafic.DateOfBirth;
                sub.UserDomicileMunicipality = anagrafic.DomicileMunicipality;
                sub.UserDomicileNation = anagrafic.DomicileNation;
                sub.UserDomicilePostalCode = anagrafic.DomicilePostalCode;
                sub.UserDomicileProvince = anagrafic.DomicileProvince;
                sub.UserDomicileStreetAdress = anagrafic.DomicileStreetAddress;
                sub.UserEmail = anagrafic.Email;
                sub.UserFirstName = anagrafic.FirstName;
                sub.UserGender = anagrafic.Gender;
                sub.UserLastName = anagrafic.LastName;
                sub.UserMobilePhone = anagrafic.MobilePhone;
                sub.UserTaxNumber = anagrafic.FiscalNumber;
                sub.UserPlaceOfBirth = anagrafic.PlaceOfBirth;

            }

            sub.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID;
            sub.CreationDate = DateTime.Now;

            if (FirstSubscriberNumber != null)
            {
                sub.ReferenceID = FirstSubscriberNumber.Value + Subscribers.Count;
            }
            else
            {
                sub.ReferenceID = 0;
            }
            sub.SubscriptionFamilyID = ReferenceID;
            sub.MedicalFiles = new List<FILE_FileInfo>();

            if (CopyFrom != null)
            {
                sub.FirstName = CopyFrom.FirstName;
                sub.LastName = CopyFrom.LastName;
                sub.TaxNumber = CopyFrom.TaxNumber;

                if (CopyFrom.IsGlutenIntolerance == true)
                {
                    sub.IsGlutenIntolerance = true;
                }
                if (CopyFrom.IsLactoseIntolerance == true)
                {
                    sub.IsLactoseIntolerance = true;
                }
                if (CopyFrom.DistanceFromSchool != null)
                {
                    sub.DistanceFromSchool = CopyFrom.DistanceFromSchool;
                }
                if (CopyFrom.IsBothParentEmployed == true)
                {
                    sub.IsBothParentEmployed = true;
                }

                sub.AdditionalIntolerance = CopyFrom.AdditionalIntolerance;
                sub.CanteenMenuID = CopyFrom.CanteenMenuID;

                if (sub.MedicalFiles == null)
                {
                    sub.MedicalFiles = new List<FILE_FileInfo>();
                }

                if (CopyFrom.FILE_FileInfo_SpecialMenu_ID != null)
                {
                    var fileInfo = FileProvider.GetFileInfo(CopyFrom.FILE_FileInfo_SpecialMenu_ID.Value);

                    if (fileInfo != null)
                    {
                        fileInfo.ID = Guid.NewGuid();

                        sub.MedicalFiles.Add(fileInfo);
                    }
                }

                sub.Child_PlaceOfBirth = CopyFrom.Child_PlaceOfBirth;
                sub.Child_DateOfBirth = CopyFrom.Child_DateOfBirth;
                sub.Child_Domicile_Municipal_ID = CopyFrom.Child_Domicile_Municipal_ID;
                sub.Child_DomicileMunicipality = CopyFrom.Child_DomicileMunicipality;
                sub.Child_DomicileNation = CopyFrom.Child_DomicileNation;
                sub.Child_DomicilePostalCode = CopyFrom.Child_DomicilePostalCode;
                sub.Child_DomicileProvince = CopyFrom.Child_DomicileProvince;
                sub.Child_DomicileStreetAddress = CopyFrom.Child_DomicileStreetAddress;
            }

            Subscribers.Add(sub);

            IsDataBusy = false;
            StateHasChanged();
        }
        private void RemoveSubscriber(CANTEEN_Subscriber Data)
        {
            IsDataBusy = true;
            StateHasChanged();


            Subscribers.Remove(Data);

            for (int i = Data.OrderCount; i < Data.OrderCount + 9; i++) 
            {
                AnchorService.RemoveAnchorByOrder(i);
            }

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ContinueToSecondStep()
        {
            bool valid = true;

            Guid subfamilyID = Guid.Empty;

            foreach (var sub in Subscribers)
            {
                sub.TaxNumberError = null;

                subfamilyID = sub.SubscriptionFamilyID ?? Guid.Empty;

                if (sub.HTMLReference != null && sub.HTMLReference.EditContext != null)
                {
                    valid = sub.HTMLReference.EditContext.Validate();
                    StateHasChanged();
                }

                if (SessionWrapper.CurrentUser.AUTH_Municipality_ID != null && sub.SchoolyearID != null && sub.PreviousSubscriptionID == null && FamilyID == null)
                {
                    var result = await CanteenProvider.TaxNumberExists(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, sub.SchoolyearID.Value, sub.TaxNumber);

                    valid = !result;

                    if (Subscribers.FirstOrDefault(p => p.ID != sub.ID && p.TaxNumber == sub.TaxNumber) != null)
                    {
                        valid = false;
                    }

                    if (valid == false)
                    {
                        sub.TaxNumberError = TextProvider.Get("CANTEEN_TAXNUMBER_EXISTANT_ERROR");
                    }

                    StateHasChanged();
                }

                if (sub.SchoolClassID != null)
                {
                    var schoolClassList = await CanteenProvider.GetSchoolClassList();
                    var currentschoolClass = SchoolClassList.Where(a => a.ID == sub.SchoolClassID).FirstOrDefault();
                    sub.SchoolClass = TextProvider.GetOrCreate(currentschoolClass.TEXT_SystemText_Code);
                }

                if (sub.SchoolClass == null || sub.SchoolClass == "")
                {
                    sub.SchoolClassError = TextProvider.GetOrCreate("CANTEEN_NO_SCOOLCLASS_ERROR");
                    valid = false;
                    StateHasChanged();
                }
                else
                {
                    sub.SchoolClassError = "";
                }

                sub.MedicalFileError = "";

                if (MealList != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID) != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID).Special == true && sub.FILE_FileInfo_SpecialMenu_ID == null && sub.MedicalFiles.Count() == 0) //SpecialMenu
                {
                    sub.MedicalFileError = TextProvider.GetOrCreate("CANTEEN_NO_MEDICAL_FILE_ERROR");
                    valid = false;
                    StateHasChanged();
                }

                if (MealList != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID) != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID).Special == true && sub.IsGlutenIntolerance == false && sub.IsLactoseIntolerance == false && (sub.AdditionalIntolerance == null || sub.AdditionalIntolerance == "")) //SpecialMenu
                {
                    sub.MedicalFileError = TextProvider.GetOrCreate("CANTEEN_NO_INTOLERANCE_SELECTED");
                    valid = false;
                    StateHasChanged();
                }


                if (PrivacyAccept == false)
                {
                    PrivacyError = "Missing Privacy Check";
                    valid = false;
                    StateHasChanged();
                }
                else
                {
                    sub.PrivacyDate = DateTime.Now;
                    PrivacyError = null;
                }

                if (sub.TaxNumberError == null && Application.Helper.CodiceFiscaleHelper.IsCheckDigitValid(sub.TaxNumber) == false)
                {
                    sub.TaxNumberError = TextProvider.Get("CANTEEN_TAXNUMBER_CHECK_ERROR");
                    valid = false;
                }

                if (!sub.DayMo && !sub.DayTue && !sub.DayWed && !sub.DayThu && !sub.DayFri)
                {
                    valid = false;
                    sub.DaySelectionError = TextProvider.Get("CANTEEN_DAY_SELECTION_ERROR");
                    StateHasChanged();
                }
                else
                {
                    sub.DaySelectionError = "";
                    string Days = "";

                    if (sub.DayMo)
                    {
                        Days += @TextProvider.Get("MONDAY");
                    }

                    if (sub.DayTue)
                    {
                        if (!string.IsNullOrEmpty(Days))
                        {
                            Days += " ";
                        }

                        Days += @TextProvider.Get("TUESDAY");
                    }
                    if (sub.DayWed)
                    {
                        if (!string.IsNullOrEmpty(Days))
                        {
                            Days += " ";
                        }

                        Days += @TextProvider.Get("WEDNESDAY");
                    }
                    if (sub.DayThu)
                    {
                        if (!string.IsNullOrEmpty(Days))
                        {
                            Days += " ";
                        }

                        Days += @TextProvider.Get("THURSDAY");
                    }
                    if (sub.DayFri)
                    {
                        if (!string.IsNullOrEmpty(Days))
                        {
                            Days += " ";
                        }

                        Days += @TextProvider.Get("FRIDAY");
                    }

                    sub.DayString = Days;
                }

                if (sub.CanteenMenuID == null || sub.CanteenMenuID == Guid.Empty)
                {
                    sub.MealTypeMessage = TextProvider.Get("CAMTEEN_NO_MENU_SELECTED_ERROR");
                    valid = false;
                }
                else
                {
                    sub.MealTypeMessage = null;
                }
            }

            if (valid)
            {
                if (FamilyID == null)
                {
                    if (!await Dialogs.ConfirmAsync(TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_ARE_YOU_SURE"), TextProvider.Get("INFORMATION")))
                        return false;
                }
                if (!string.IsNullOrEmpty(Edit))
                {
                    if (!await Dialogs.ConfirmAsync(TextProvider.Get("CANTEEN_EDIT_SUBSCRIPTION_ARE_YOU_SURE"), TextProvider.Get("INFORMATION")))
                        return false;
                }

                foreach (var sub in Subscribers)
                {
                    sub.CANTEEN_Subscriber_Status_ID = CanteenStatus.Incomplete;

                    if (sub.CanteenMenuID != null)
                    {
                        var menu = await CanteenProvider.GetCANTEEN_MealMenuByID(sub.CanteenMenuID ?? Guid.Empty);
                        sub.MenuName = TextProvider.GetOrCreate(menu.TEXT_SystemTexts_Code);
                    }

                    if (sub.CANTEEN_School_IDReq != null)
                    {
                        var school = await CanteenProvider.GetSchool(sub.CANTEEN_School_IDReq ?? Guid.Empty);
                        sub.SchoolName = school.Name;
                    }

                    if (sub.MedicalFiles != null && sub.MedicalFiles.Count() > 0)
                    {
                        var medicalFile = sub.MedicalFiles.FirstOrDefault();

                        if (medicalFile != null)
                        {
                            await FileProvider.SetFileInfo(medicalFile);

                            sub.FILE_FileInfo_SpecialMenu_ID = medicalFile.ID;
                        }
                    }

                    await CanteenProvider.SetSubscriber(sub);
                }

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (MyCivisService.Enabled == true)
                {
                    NavManager.NavigateTo("/Canteen/MyCivis/Sign/" + subfamilyID);
                }
                else
                {
                    NavManager.NavigateTo("/Canteen/Sign/" + subfamilyID);
                }
            }

            return valid;
        }
        private void ReturnToPreviousPage()
        {
            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Service");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Service");
            }
        }
        private void ImportPreviousSub(V_CANTEEN_Subscriber_Previous ImportSub)
        {
            if (Subscribers != null && Subscribers.Count() == 1)
            {
                var firstSub = Subscribers.FirstOrDefault();

                if (firstSub != null && string.IsNullOrEmpty(firstSub.TaxNumber))
                {
                    Subscribers.Remove(firstSub);
                }
            }

            AddSubscriber(ImportSub);
            HideImportWindow();
        }
        private void ShowImportWindow()
        {
            ShowPreviousWindow = true;
            StateHasChanged();
        }
        private void HideImportWindow()
        {
            ShowPreviousWindow = false;
            StateHasChanged();
        }
        private async Task<DataEnvelope<META_IstatComuni>> GetRemoteMunicipalities(ComboBoxReadEventArgs args)
        {
            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            var result = await MunicipalitiesList.ToDataSourceResultAsync(args.Request);

            var dataToReturn = new DataEnvelope<META_IstatComuni>
            {
                Data = result.Data.Cast<META_IstatComuni>().ToList(),
                Total = result.Total
            };

            args.Data = result.Data;
            args.Total = result.Total;

            return await Task.FromResult(dataToReturn);
        }
        private async Task<META_IstatComuni> GetModelFromValue(Guid selectedValue)
        {
            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            return MunicipalitiesList.FirstOrDefault(p => p.ID == selectedValue);
        }
        private async void UpdateAddressData(CANTEEN_Subscriber subscriber)
        {
            if (subscriber.SelectedMunicipality != null)
            {
                var data = await MetaProvider.GetMunicipality(subscriber.SelectedMunicipality);

                if (data != null)
                {
                    subscriber.Child_DomicilePostalCode = data.Cap;
                    subscriber.Child_DomicileMunicipality = data.NameDE;
                    subscriber.Child_DomicileProvince = data.RegionCity;
                    subscriber.Child_DomicileNation = "IT";

                    StateHasChanged();
                }
            }
        }
        private void AddressNotFoundClick(CANTEEN_Subscriber subscriber)
        {
            subscriber.AddressNotFound = true;
            StateHasChanged();
        }
        private void SearchAddressClick(CANTEEN_Subscriber subscriber)
        {
            subscriber.AddressNotFound = false;
            StateHasChanged();
        }
    }
}
