using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Backend
{
    public partial class CreateSubscriptionsBackend
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<CANTEEN_Subscriber> Subscribers = new List<CANTEEN_Subscriber>();
        private List<CANTEEN_School> Schools = new List<CANTEEN_School>();
        private List<CANTEEN_Canteen> Canteens = new List<CANTEEN_Canteen>();
        private List<V_CANTEEN_Schoolyear_Current> SchoolyearList = new List<V_CANTEEN_Schoolyear_Current>();
        private List<CANTEEN_MealMenu> MealList = new List<CANTEEN_MealMenu>();
        private List<CANTEEN_SchoolClass> SchoolClassList = new List<CANTEEN_SchoolClass>();
        private List<FILE_FileInfo> FileList { get; set; } = new List<FILE_FileInfo>();

        private Guid ReferenceID;
        private long? FirstSubscriberNumber { get; set; }
        private bool IsDataBusy { get; set; } = true;
        private bool ContinueNotFinished { get; set; } = false;

        private async void OnRemoveFile(Guid File_Info_ID)
        {
            await FileProvider.RemoveFileInfo(File_Info_ID);
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.ID == null || SessionWrapper.CurrentUser.AUTH_Municipality_ID == null)
            {
                NavManager.NavigateTo("/Canteen");
            }

            UserSelectionEnabled = false;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Canteen/Subscriptionlist", "MAINMENU_CANTEEN", null, null);
            CrumbService.AddBreadCrumb("/Canteen/Subscribe", "CANTEEN_CREATE_SUBSCRIPTION_TITLE", null, null);

            ReferenceID = Guid.NewGuid();


            Schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            Canteens = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            SchoolyearList = await CanteenProvider.GetSchoolsyearsCurrent(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, true);
            MealList = await CanteenProvider.GetCANTEEN_MealMenuList(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
            SchoolClassList = await CanteenProvider.GetSchoolClassList();


            Guid statusNotFinishedID = CanteenStatus.Incomplete;
            var subList = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);
            subList = subList.Where(a => a.CANTEEN_Subscriber_Status_ID == statusNotFinishedID).ToList();

            if (subList.Count == 0)
            {
                var sub = new CANTEEN_Subscriber();

                sub.ID = Guid.NewGuid();

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
                    sub.IsManualInput = true;

                }

                Subscribers.Add(sub);

            }
            else
            {


                Subscribers = subList;
                foreach (var sub in subList)
                {
                    sub.MedicalFiles = new List<FILE_FileInfo>();
                }

                ContinueNotFinished = true;
            }



            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
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
                    SchoolClassList = await CanteenProvider.GetSchoolClassList();
                    SchoolClassList = SchoolClassList.Where(a => a.SchoolTypeID == CurrentSchool.SchoolTypeID).ToList();

                    if (Canteens.Count() == 1)
                    {
                        Data.CANTEEN_Canteen_ID = Canteens.FirstOrDefault().ID;
                        Data.CANTEEN_Canteen_IDReq = Canteens.FirstOrDefault().ID;
                        CanteenChanged(Data);
                    }

                    StateHasChanged();
                }
            }
        }
        private async void CanteenChanged(CANTEEN_Subscriber Data)
        {
            if (Data != null && Data.CANTEEN_Canteen_ID != null)
            {
                var currentCanteen = await CanteenProvider.GetCanteen(Data.CANTEEN_Canteen_ID ?? Guid.Empty);

                if (Data.DayMo == true && currentCanteen.DayMo == false) { Data.DayMo = false; }
                if (Data.DayTue == true && currentCanteen.DayTue == false) { Data.DayTue = false; }
                if (Data.DayThu == true && currentCanteen.DayThu == false) { Data.DayThu = false; }
                if (Data.DayWed == true && currentCanteen.DayWed == false) { Data.DayWed = false; }
                if (Data.DayFri == true && currentCanteen.DayFri == false) { Data.DayFri = false; }
                if (Data.DaySat == true && currentCanteen.DaySat == false) { Data.DaySat = false; }
                if (Data.DaySun == true && currentCanteen.DaySun == false) { Data.DaySun = false; }

                Data.EnableDayMo = currentCanteen.DayMo;
                Data.EnableDayTue = currentCanteen.DayTue;
                Data.EnableDayThu = currentCanteen.DayThu;
                Data.EnableDayWed = currentCanteen.DayWed;
                Data.EnableDayFri = currentCanteen.DayFri;
                Data.EnableDaySat = currentCanteen.DaySat;
                Data.EnableDaySun = currentCanteen.DaySun;

                StateHasChanged();
            }
        }


        private async void MealChanged(CANTEEN_Subscriber Data)
        {
            StateHasChanged();
        }
        public bool UserSelectionEnabled { get; set; } = true;


        private async void AddSubscriber()
        {
            IsDataBusy = true;
            StateHasChanged();

            var sub = new CANTEEN_Subscriber();

            sub.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

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
                sub.IsManualInput = true;

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

            Subscribers.Add(sub);

            IsDataBusy = false;
            StateHasChanged();
        }
        private void RemoveSubscriber(CANTEEN_Subscriber Data)
        {
            IsDataBusy = true;
            StateHasChanged();

            Subscribers.Remove(Data);

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ContinueToSecondStep()
        {
            bool valid = true;

            Guid subfamilyID = Guid.Empty;
            foreach (var sub in Subscribers)
            {
                subfamilyID = sub.SubscriptionFamilyID ?? Guid.Empty;

                if (sub.HTMLReference != null && sub.HTMLReference.EditContext != null)
                {
                    valid = sub.HTMLReference.EditContext.Validate();
                    StateHasChanged();
                }

                if (SessionWrapper.CurrentUser.AUTH_Municipality_ID != null &&
                    sub.CANTEEN_Period_ID != null &&
                    ContinueNotFinished == false &&
                    await CanteenProvider.TaxNumberExists(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, sub.CANTEEN_Period_ID.Value, sub.TaxNumber)
                   )
                {
                    valid = false;
                    sub.TaxNumberError = TextProvider.Get("CANTEEN_TAXNUMBER_EXISTANT_ERROR");
                    StateHasChanged();
                }
                else
                {
                    sub.TaxNumberError = "";
                }

                if (sub.SchoolClassID != null)
                {
                    var schoolClassList = await CanteenProvider.GetSchoolClassList();
                    var currentschoolClass = SchoolClassList.Where(a => a.ID == sub.SchoolClassID).FirstOrDefault();
                    sub.SchoolClass = TextProvider.Get(currentschoolClass.TEXT_SystemText_Code);
                }

                if (sub.SchoolClass == null || sub.SchoolClass == "")
                {
                    sub.SchoolClassError = TextProvider.Get("CANTEEN_NO_SCOOLCLASS_ERROR");
                    valid = false;
                    StateHasChanged();
                }
                else
                {
                    sub.SchoolClassError = "";
                }

                if (MealList != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID) != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID).Special == true && (sub.MedicalFiles == null || sub.MedicalFiles.Count() == 0)) //SpecialMenu
                {
                    sub.MedicalFileError = TextProvider.Get("CANTEEN_NO_MEDICAL_FILE_ERROR");
                    valid = false;
                    StateHasChanged();
                }
                else
                {
                    sub.MedicalFileError = "";
                }

                if (MealList != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID) != null && MealList.FirstOrDefault(p => p.ID == sub.CanteenMenuID).Special == true && (sub.IsGlutenIntolerance == false && sub.IsLactoseIntolerance == false && (sub.AdditionalIntolerance == null || sub.AdditionalIntolerance == ""))) //SpecialMenu
                {
                    sub.MedicalFileError = TextProvider.Get("CANTEEN_NO_INTOLERANCE_SELECTED");
                    valid = false;
                    StateHasChanged();
                }
                else
                {
                    sub.MedicalFileError = "";
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
            }

            if (valid)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("CANTEEN_CREATE_SUBSCRIPTION_ARE_YOU_SURE"), TextProvider.Get("INFORMATION")))
                    return false;

                foreach (var sub in Subscribers)
                {
                    sub.CANTEEN_Subscriber_Status_ID = CanteenStatus.Incomplete; //Incomplete status

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
                        sub.FILE_FileInfo_SpecialMenu_ID = sub.MedicalFiles.FirstOrDefault().ID;
                    }

                    await CanteenProvider.SetSubscriber(sub);
                    //await CanteenProvider.CreateSubscriber_Movements(sub.ID);
                }

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                //NavManager.NavigateTo("/Canteen/Sign/" + subfamilyID);
                NavManager.NavigateTo("/Backend/Canteen/Subscriptionlist");
            }

            return valid;
        }
        private void ReturnToPreviousPage()
        {
            NavManager.NavigateTo("/Backend/Canteen/Subscriptionlist");
        }

        private async void UserSelected(Guid? AUTH_Users_ID)
        {

            foreach (var sub in Subscribers)
            {
                var anagrafic = await AUTHProvider.GetAnagraficByUserID(AUTH_Users_ID ?? Guid.Empty);

                sub.AUTH_Users_ID = AUTH_Users_ID;
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
                    sub.IsManualInput = true;


                }
            }

            StateHasChanged();
        }

    }


}