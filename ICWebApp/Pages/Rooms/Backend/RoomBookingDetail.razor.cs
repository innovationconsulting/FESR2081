using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.Rooms;
using ICWebApp.Domain.Models.Textvorlagen;
using ICWebApp.Pages.Form.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Telerik.Blazor;

namespace ICWebApp.Pages.Rooms.Backend
{
    public partial class RoomBookingDetail
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IRoomAdministrationHelper RoomAdministrationHelper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IMailerService MailerService { get; set; }
        [Inject] ISMSService SMSService { get; set; }
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ICONTProvider ContProvider { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string ID { get; set; }

        private List<V_ROOM_Rooms> RoomList = new List<V_ROOM_Rooms>();
        private List<V_ROOM_Booking_Group> Bookinggroups = new List<V_ROOM_Booking_Group>();
        private Administration_Filter_RoomBookingGroup Filter = new Administration_Filter_RoomBookingGroup();
        private List<V_ROOM_Booking_Status_Log> CurrentStatusLogList = new List<V_ROOM_Booking_Status_Log>();
        private List<FILE_FileInfo> CurrentBookingUserRessource = new List<FILE_FileInfo>();
        private List<ROOM_Booking_Ressource> CurrentBookingRessource = new List<ROOM_Booking_Ressource>();
        private List<V_ROOM_Booking_Status> StatusList = new List<V_ROOM_Booking_Status>();
        private List<V_ROOM_Booking_Status> AllStatusList = new List<V_ROOM_Booking_Status>();
        private TextItem? TextItem = new TextItem();
        private List<V_ROOM_Booking> CurrentBookings = new List<V_ROOM_Booking>();
        private List<V_ROOM_Booking_Bookings> CurrentBookingPositions = new List<V_ROOM_Booking_Bookings>();
        private int _applicationTabIndex = 0;
        private int ApplicationTabIndex
        {
            get
            {
                return _applicationTabIndex;
            }
            set
            {
                _applicationTabIndex = value;
                OnTabIndexChanged(_applicationTabIndex);
            }
        }
        private bool IsDataBusy = true;
        private bool IsWizardBusy { get; set; } = true;
        private bool FilterWindowVisible { get; set; } = false;
        public V_ROOM_Booking_Group? CurrentBookingGroup;
        private AUTH_Municipality? Municipality { get; set; }
        private string? CurrentWizardTitle { get; set; }
        private List<Guid?> CurrenTransactionList = new List<Guid?>();
        private Guid? StartStatus { get; set; }
        private Guid SelectedRoomID { get; set; } = Guid.Empty;
        private CalendarFilter? calendarFilter;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("ROOMBOOKING_ROOM_BOOKING_DETAIL");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/RoomBooking/List", "ROOMBOOKING_ROOM_BOOKING_LIST", null, null);
            CrumbService.AddBreadCrumb(NavManager.Uri, "ROOMBOOKING_ROOM_BOOKING_DETAIL", null, null, true);

            if (RoomAdministrationHelper.Filter != null)
            {
                Filter = RoomAdministrationHelper.Filter;
            }
            else
            {
                Filter.Room_ID = new List<Guid>();
                Filter.Booking_Status_ID = new List<Guid>
                {
                    Guid.Parse("b99595e0-b4e1-4f46-a10a-7d42f80c491e")   //REQUEST
                };
            }

            Bookinggroups = await GetData();
            Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            StatusList = await GetStatusList();
            AllStatusList = await RoomProvider.GetBookingStatusList();

            RoomList = new List<V_ROOM_Rooms>();
            var roomsfromDB = await RoomProvider.GetAllRooms(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
            foreach (var booking in CurrentBookings)
            {
                var roomfromDB = roomsfromDB.FirstOrDefault(a => a.ID == booking.ROOM_Room_ID);
                RoomList.Add(roomfromDB);
            }

            IsDataBusy = false;
            IsWizardBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            if (ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/RoomBooking/List");
                StateHasChanged();
                return;
            }

            IsDataBusy = true;
            IsWizardBusy = true;
            StateHasChanged(); 
            
            CurrentBookingGroup = await GetCurrentBookingGroup();

            if(CurrentBookingGroup != null && CurrentBookingGroup.AUTH_MunicipalityID != SessionWrapper.AUTH_Municipality_ID)
            {
                BackToPreviousPage();
                return;
            }

            if (CurrentBookingGroup != null)
            {
                CurrenTransactionList = await GetCurrentBookingTransactionList();
                CurrentStatusLogList = await GetCurrentBookingStatusLog();
                CurrentBookingUserRessource = await GetCurrentBookingUserRessource();
                CurrentBookingRessource = await GetCurrentBookingRessource();
                CurrentBookings = await GetCurrentBookings();
                CurrentBookingPositions = await RoomProvider.GetBookingPositions(CurrentBookingGroup.ID);

                StartStatus = CurrentBookingGroup.ROOM_BookingStatus_ID;

                RoomList = new List<V_ROOM_Rooms>();
                var roomsfromDB = await RoomProvider.GetAllRooms(SessionWrapper.AUTH_Municipality_ID ?? Guid.Empty);
                foreach (var booking in CurrentBookings)
                {
                    var roomfromDB = roomsfromDB.FirstOrDefault(a => a.ID == booking.ROOM_Room_ID);
                    RoomList.Add(roomfromDB);
                }
            }

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var settings = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

                if (settings != null && settings.HasTasks == true)
                {
                    await TaskService.SetContext(3, ID, "");    //ROOM CONTEXT

                    if (CurrentBookingGroup != null)
                    {
                        TaskService.ContextName = CurrentBookingGroup.FirstName + " " + CurrentBookingGroup.LastName;
                    }
                }
            }

            calendarFilter = new CalendarFilter();

            var startItem = CurrentBookings.FirstOrDefault(p => p.StartDate >= DateTime.Now);

            if(startItem != null && startItem.StartDate != null)
            {
                calendarFilter.StartDate = startItem.StartDate.Value;
            }

            var rooms = CurrentBookings.Where(p => p.ROOM_Room_ID != null).Select(p => p.ROOM_Room_ID.Value).Distinct().ToList();

            if (rooms != null && rooms.Any())
            {
                calendarFilter.ROOM_Room_IDs = rooms;
            }


            IsDataBusy = false;
            IsWizardBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async Task<V_ROOM_Booking_Group?> GetCurrentBookingGroup()
        {
            return await RoomProvider.GetVBookingGroup(Guid.Parse(ID));
        }
        private async Task<List<V_ROOM_Booking_Group>> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var data = await RoomProvider.GetBookingGroups(SessionWrapper.AUTH_Municipality_ID.Value, Filter);

                RoomAdministrationHelper.Filter = Filter;

                return data;
            }

            return new List<V_ROOM_Booking_Group>();
        }
        private async Task<List<Guid?>> GetCurrentBookingTransactionList()
        {
            var data = await RoomProvider.GetBookingTransactionList(CurrentBookingGroup.ID);

            return data.Select(p => p.PAY_Transaction_ID).ToList();
        }
        private async Task<List<V_ROOM_Booking_Status_Log>> GetCurrentBookingStatusLog()
        {
            var locList = await RoomProvider.GetRoomStatusLogList(CurrentBookingGroup.ID);

            if (CurrentBookingGroup.CreationDate != null)
            {
                locList.Add(new V_ROOM_Booking_Status_Log()
                {
                    AUTH_Users_ID = CurrentBookingGroup.AUTH_User_ID,
                    User = CurrentBookingGroup.FirstName + " " + CurrentBookingGroup.LastName,
                    ChangeDate = CurrentBookingGroup.CreationDate,
                    IconCSS = "fa-regular fa-file-plus",
                    Status = TextProvider.Get("FORM_USER_DETAIL_CREATED_STATUS")
                });
            }

            return locList;
        }
        private async Task<List<FILE_FileInfo>> GetCurrentBookingUserRessource()
        {
            var ressources = await RoomProvider.GetRoomRessourceList(CurrentBookingGroup.ID);

            if (ressources != null)
            {
                var locFiles = new List<FILE_FileInfo>();

                foreach (var r in ressources.Where(p => p.UserUpload == true))
                {
                    if (r.FILE_FileInfo_ID != null)
                    {
                        var f = await FileProvider.GetFileInfoAsync(r.FILE_FileInfo_ID.Value);

                        if (f != null)
                        {
                            locFiles.Add(f);
                        }
                    }
                }

                return locFiles;
            }

            return new List<FILE_FileInfo>();
        }
        private async Task<List<ROOM_Booking_Ressource>> GetCurrentBookingRessource()
        {
            var ressources = await RoomProvider.GetRoomRessourceList(CurrentBookingGroup.ID);

            if (ressources != null)
            {
                return ressources.Where(p => p.UserUpload == false).ToList();
            }

            return new List<ROOM_Booking_Ressource>();
        }
        private async Task<List<V_ROOM_Booking_Status>> GetStatusList()
        {
            var data = await RoomProvider.GetBookingStatusList();

            if (CurrentBookingGroup != null)
            { 
                data = data.Where(e => e.UserSelectable == true || e.ID == CurrentBookingGroup.ROOM_BookingStatus_ID).ToList();
            }
            else
            {
                data = data.Where(e => e.UserSelectable == true).ToList();
            }

            return data;
        }
        private async Task<List<V_ROOM_Booking>> GetCurrentBookings()
        {
            var data = await RoomProvider.GetVBookingsByGroupID(CurrentBookingGroup.ID, SessionWrapper.AUTH_Municipality_ID.Value);

            return data.ToList();
        }
        private async void FilterSearch(Administration_Filter_RoomBookingGroup Filter)
        {
            IsDataBusy = true;
            StateHasChanged();

            Bookinggroups = await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private void FilterClose()
        {
            FilterWindowVisible = false;
            StateHasChanged();
        }
        private void ShowFilter()
        {
            FilterWindowVisible = true;
            StateHasChanged();
        }
        private void BackToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/RoomBooking/List");
        }
        private void SelectBooking(Guid? ID)
        {
            if (ID != null && (CurrentBookingGroup == null || ID != CurrentBookingGroup.ID))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/RoomBooking/Detail/" + ID.ToString());
                StateHasChanged();
            }
        }
        private void OnTabIndexChanged(int Index)
        {
            if (Index == 0)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_OVERVIEW");
            }
            else if (Index == 1)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_ANAGRAFIC");
            }
            else if (Index == 2)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_DETAILS");
            }
            else if (Index == 3)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_PREVIEW");
            }
            else if (Index == 4)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_STATUS_LOG");
            }
            else if (Index == 5)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_PAYMENTS");
            }

            IsWizardBusy = false;
            StateHasChanged();
        }
        private void OnStepChanged()
        {
            IsWizardBusy = true;
            StateHasChanged();
        }
        private async Task<bool> TransactionCreated(Guid PAY_Transcation_ID)
        {
            if (CurrentBookingGroup != null)
            {
                var newItem = new ROOM_BookingTransactions()
                {
                    ID = Guid.NewGuid(),
                    ROOM_BookingGroupID = CurrentBookingGroup.ID,
                    PAY_Transaction_ID = PAY_Transcation_ID
                };

                await RoomProvider.SetBookingTransaction(newItem);
                CurrenTransactionList = await GetCurrentBookingTransactionList();
                StateHasChanged();
            }

            return true;
        }
        private async void DownloadRessource(Guid FILE_Fileinfo_ID, string? Name)
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
                        await EnviromentService.DownloadFile(blob.FileImage, fileToDownload.FileName + fileToDownload.FileExtension);
                    }
                    else
                    {
                        await EnviromentService.DownloadFile(blob.FileImage, Name + fileToDownload.FileExtension);
                    }
                }
            }

            StateHasChanged();
        }
        private async Task<bool> FileRemoved(Guid FILE_Fileinfo_ID)
        {
            if (CurrentBookingGroup != null)
            {
                var ressources = await RoomProvider.GetRoomRessourceList(CurrentBookingGroup.ID);

                if (ressources != null)
                {
                    var locFile = ressources.FirstOrDefault(p => p.FILE_FileInfo_ID == FILE_Fileinfo_ID);

                    if (locFile != null)
                    {
                        await RoomProvider.RemoveRoomRessource(locFile.ID);
                        await FileProvider.RemoveFileInfo(FILE_Fileinfo_ID);
                    }
                }

                CurrentBookingUserRessource = await GetCurrentBookingUserRessource();
                StateHasChanged();
            }
            return true;
        }
        private async Task<bool> SaveRessources()
        {
            if (CurrentBookingGroup != null)
            {
                var ressources = await RoomProvider.GetRoomRessourceList(CurrentBookingGroup.ID);

                foreach (var f in CurrentBookingUserRessource)
                {
                    await FileProvider.SetFileInfo(f);

                    ROOM_Booking_Ressource? existing = null;

                    if (ressources != null)
                    {
                        existing = ressources.FirstOrDefault(p => p.FILE_FileInfo_ID == f.ID);
                    }

                    if (existing == null)
                    {
                        existing = new ROOM_Booking_Ressource();
                        existing.ID = Guid.NewGuid();
                        existing.ROOM_BookingGroup_ID = CurrentBookingGroup.ID;
                        existing.CreationDate = DateTime.Now;
                        existing.FILE_FileInfo_ID = f.ID;
                        existing.UserUpload = true;
                    }

                    await RoomProvider.SetRoomRessource(existing);
                }

                CurrentBookingUserRessource = await GetCurrentBookingUserRessource();
                StateHasChanged();
            }

            return true;
        }
        private async Task<bool> ChangeStatus()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (CurrentBookingGroup != null && CurrentBookingGroup.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("APPLICATION_STATUS_CHANGE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                {
                    BusyIndicatorService.IsBusy = false;
                    await InvokeAsync(() => StateHasChanged());
                    StateHasChanged();
                    return false;
                }

                var StatusLog = new ROOM_Booking_Status_Log();

                StatusLog.ID = Guid.NewGuid();

                StatusLog.ROOM_BookingGroup_ID = CurrentBookingGroup.ID;
                StatusLog.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                StatusLog.ROOM_BookingStatus = CurrentBookingGroup.ROOM_BookingStatus_ID;
                StatusLog.ChangeDate = DateTime.Now;

                var dbBookingGroup = await RoomProvider.GetBookingGroup(CurrentBookingGroup.ID);

                if (dbBookingGroup != null)
                {
                    dbBookingGroup.ROOM_BookingStatus_ID = CurrentBookingGroup.ROOM_BookingStatus_ID;

                    await RoomProvider.SetBookingGroup(dbBookingGroup);
                    await RoomProvider.SetRoomStatusLog(StatusLog);

                    if (TextItem != null)
                    {
                        var Languages = await LangProvider.GetAll();

                        if (Languages != null)
                        {
                            foreach (var l in Languages)
                            {
                                var dataE = new ROOM_Booking_Status_Log_Extended()
                                {
                                    ID = Guid.NewGuid(),
                                    ROOM_Booking_Status_Log_ID = StatusLog.ID,
                                    LANG_Languages_ID = l.ID
                                };

                                if (l.ID == Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"))  //DE
                                {
                                    dataE.Reason = TextItem.German;
                                }
                                else if (l.ID == Guid.Parse("e450421a-baff-493e-a390-71b49be6485f")) //IT
                                {
                                    dataE.Reason = TextItem.Italian;
                                }
                                else
                                {
                                    dataE.Reason = TextItem.German;
                                }

                                await RoomProvider.SetRoomStatusLogExtended(dataE);
                            }
                        }

                        CurrentStatusLogList = await GetCurrentBookingStatusLog();

                        if (dbBookingGroup.AUTH_User_ID != null)
                        {
                            string text = "";
                            Guid userLangID = LanguageSettings.German;

                            var userLang = await AuthProvider.GetSettings(dbBookingGroup.AUTH_User_ID.Value);

                            if (userLang != null && userLang.LANG_Languages_ID == LanguageSettings.Italian)
                            {
                                if (!string.IsNullOrEmpty(TextItem.Italian))
                                {
                                    text = await TextProvider.ReplaceGeneralKeyWords(dbBookingGroup.AUTH_User_ID.Value, TextItem.Italian);
                                }

                                text = await RoomProvider.ReplaceKeywords(dbBookingGroup.ID, LanguageSettings.Italian, text, StartStatus);

                                userLangID = LanguageSettings.Italian;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(TextItem.German))
                                {
                                    text = await TextProvider.ReplaceGeneralKeyWords(dbBookingGroup.AUTH_User_ID.Value, TextItem.German);
                                }

                                text = await RoomProvider.ReplaceKeywords(dbBookingGroup.ID, LanguageSettings.German, text, StartStatus);
                            }

                            if (dbBookingGroup.AUTH_User_ID != null && dbBookingGroup.AUTH_MunicipalityID != null)
                            {
                                var msg = await MessageService.GetMessage(dbBookingGroup.AUTH_User_ID.Value, dbBookingGroup.AUTH_MunicipalityID.Value, text,
                                                                          TextProvider.Get("NOTIF_ROOM_STATUS_CHANGED_SHORTTEXT", userLangID),
                                                                          TextProvider.Get("NOTIF_ROOM_STATUS_CHANGED_TITLE", userLangID),
                                                                          Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), false, null);

                                if (msg != null)
                                {
                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Room/BookingUserDetails/" + dbBookingGroup.ID);
                                }
                            }
                        }
                    }

                    if (SessionWrapper.AUTH_Municipality_ID != null)
                    {
                        if (CurrentBookingGroup.ROOM_BookingStatus_ID == ROOMStatus.Accepted || CurrentBookingGroup.ROOM_BookingStatus_ID == ROOMStatus.AcceptedWithChanges)
                        {
                            BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, CurrentBookingGroup.ID, CurrentBookingGroup.Title, false);
                        }
                        if ((StartStatus == ROOMStatus.Accepted || StartStatus == ROOMStatus.AcceptedWithChanges)
                            && (CurrentBookingGroup.ROOM_BookingStatus_ID == ROOMStatus.Cancelled || CurrentBookingGroup.ROOM_BookingStatus_ID == ROOMStatus.Declined))
                        {
                            BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, CurrentBookingGroup.ID, CurrentBookingGroup.Title, true);
                        }
                    }

                    StartStatus = StatusLog.ROOM_BookingStatus;
                    StateHasChanged();

                    TextItem = new TextItem();

                    StateHasChanged();
                    await InvokeAsync(() => StateHasChanged());
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            return true;
        }
        private void GoToCalendar()
        {
            if(CurrentBookingGroup != null)
            {
                if (RoomAdministrationHelper.CalendarFilter == null)
                {
                    RoomAdministrationHelper.CalendarFilter = new Administration_Filter_RoomCalendar();
                }

                if (CurrentBookings != null)
                {
                    RoomAdministrationHelper.CalendarFilter.Room_ID = CurrentBookings.Where(p => p.ROOM_Room_ID != null).Select(p => p.ROOM_Room_ID.Value).Distinct().ToList();

                    RoomAdministrationHelper.CalendarFilter.StartDate = CurrentBookings.Min(p => p.StartDate);
                }
                

                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/RoomBooking/Calendar");
                StateHasChanged();
            }
        }
    }
}
