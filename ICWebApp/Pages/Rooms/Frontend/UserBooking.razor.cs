using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;


namespace ICWebApp.Pages.Rooms.Frontend
{
    public partial class UserBooking
    {
        [Inject] ICONFProvider ConfProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }

        [Parameter] public string? BookingID { get; set; }

        private bool IsDataBusy = true;
        private V_ROOM_Booking_Group? BookingGroup;
        private List<V_ROOM_Booking_Status> StatusList = new List<V_ROOM_Booking_Status>();
        private List<V_ROOM_Booking_Status_Log> CurrentStatusLogList = new List<V_ROOM_Booking_Status_Log>();
        private List<Guid?> CurrenTransactionList = new List<Guid?>();
        private List<V_ROOM_Booking_Bookings> Bookings = new List<V_ROOM_Booking_Bookings>();

        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrEmpty(BookingID))
            {
                ReturnToPreviousPage();
                return;
            }

            BookingGroup = await RoomProvider.GetVBookingGroup(Guid.Parse(BookingID));

            if(BookingGroup == null)
            {
                ReturnToPreviousPage();
                return;
            }

            StatusList = await RoomProvider.GetBookingStatusList();
            CurrentStatusLogList = await GetCurrentBookingStatusLog();
            CurrenTransactionList = await GetTransactionList();
            Bookings = await RoomProvider.GetBookingPositions(BookingGroup.ID);

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Services", "FRONTEND_USER_BOOKINGS_TITLE", null, null);
            CrumbService.AddBreadCrumb("/Rooms", "BOOKING_DETAILS", null, null, true);

            SessionWrapper.PageTitle = BookingGroup.Title;

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<List<Guid?>> GetTransactionList()
        {
            if (BookingGroup != null) 
            {             
                var data = await RoomProvider.GetBookingTransactionList(BookingGroup.ID);

                return data.Select(p => p.PAY_Transaction_ID).ToList();
            }

            return new List<Guid?>();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/User/Services");
            StateHasChanged();
        }
        private async Task<List<V_ROOM_Booking_Status_Log>> GetCurrentBookingStatusLog()
        {
            if (BookingGroup != null)
            {
                var locList = await RoomProvider.GetRoomStatusLogList(BookingGroup.ID);

                if (BookingGroup.CreationDate != null)
                {
                    locList.Add(new V_ROOM_Booking_Status_Log()
                    {
                        AUTH_Users_ID = BookingGroup.AUTH_User_ID,
                        User = BookingGroup.FirstName + " " + BookingGroup.LastName,
                        ChangeDate = BookingGroup.CreationDate,
                        IconCSS = "fa-regular fa-file-plus",
                        Status = TextProvider.Get("FORM_USER_DETAIL_CREATED_STATUS")
                    });
                }
                return locList;
            }

            return new List<V_ROOM_Booking_Status_Log>();
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
    }
}
