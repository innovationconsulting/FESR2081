using DocumentFormat.OpenXml.Drawing.Charts;
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
using ICWebApp.Pages.Rooms.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Stripe;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Schedule;
using System.Diagnostics;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Scheduler.Models;

namespace ICWebApp.Components.Rooms.Calendar
{
    public partial class CalendarComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public CalendarFilter? Filter {get;set;}
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        SfSchedule<AppointmentData>? ScheduleRef;
        EditForm? editform;
        public int[] WorkingDays { get; set; } = { 0, 1, 2, 3, 4, 5, 6 };
        private List<AppointmentData> ExistingAppointments = new List<AppointmentData>();
        private List<ResourceData> Resources { get; set; }
        private DateTime CurrentDate = DateTime.Today;
        private DateTime MinDate = DateTime.Today.AddDays(-1);
        private DateTime MaxDate = DateTime.Today.AddYears(2);
        private List<V_ROOM_Booking_Status> StatusList = new List<V_ROOM_Booking_Status>();
        private List<V_ROOM_Rooms> RoomList = new List<V_ROOM_Rooms>();
        private List<Guid> SelectedRoomList = new List<Guid>();
        private View CurrentView = View.Month;
        private bool IsDataBusy = false;
        private IJSObjectReference? _module;
        private List<V_ROOM_Booking_Type> RoomBookingTypeList = new List<V_ROOM_Booking_Type>();
        private List<Guid> SelectedBookingTypeList = new List<Guid>();
        private int ExternalInputType = 0;
        private string? WrongDatesError;

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/RoomBooking/List");
                StateHasChanged();
                return;
            }

            var rooms = await RoomProvider.GetAllRooms(SessionWrapper.AUTH_Municipality_ID.Value);
            RoomList = rooms.Where(p => p.HasRooms == false).ToList();
            RoomBookingTypeList = await RoomProvider.GetRoomBookingTypes();

            StateHasChanged();

            var config = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

            if (config != null)
            {
                if (config.MaxRoomBookingDays != null)
                {
                    MaxDate = DateTime.Now.AddDays(config.MaxRoomBookingDays.Value);
                }
            }

            StatusList = await RoomProvider.GetBookingStatusList();

            if(Filter != null)
            {
                if(Filter.ROOM_Room_IDs != null && Filter.ROOM_Room_IDs.Any())
                {
                    foreach(var room in Filter.ROOM_Room_IDs)
                    {
                        SelectedRoomList.Add(room);
                    }
                }

                if(Filter.StartDate != null)
                {
                    CurrentDate = Filter.StartDate.Value;
                }
            }

            await base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/Scheduler/Scheduler.js");                

            }

            if (Resources == null)
            {
                GetRessources();
                await GetAppointments();
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private void GetRessources()
        {
            if (Resources == null)
            {
                Resources = new List<ResourceData>();

                int counter = 0;

                foreach (var room in RoomList)
                {
                    var res = new ResourceData()
                    {
                        ID = counter,
                        ExternalID = room.ID,
                        Color = room.RoomColor,
                        Text = room.BuildingName
                    };

                    if (string.IsNullOrEmpty(res.Text))
                    {
                        res.Text = room.Name;
                    }
                    else
                    {
                        res.Text += " - " + room.Name;
                    }

                    if (string.IsNullOrEmpty(res.Color))
                    {
                        res.Color = "#0F7173";
                    }

                    res.CSS = room.Name;

                    Resources.Add(res);
                    counter++;
                }
            }
        }
        private async Task<bool> GetAppointments()
        {
            ExistingAppointments.Clear();

            var roomstoFetch = SelectedRoomList;

            if (SelectedRoomList.Count() == 0)
                roomstoFetch = RoomList.Select(p => p.ID).Distinct().ToList();

            foreach (var room in roomstoFetch)
            {
                var dbRoom = RoomList.FirstOrDefault(p => p.ID == room);

                if (dbRoom != null)
                {
                    var existingDBAppointments = await RoomProvider.GetBookings(room);
                    List<V_ROOM_Booking> existingAppointments = new List<V_ROOM_Booking>();


                    if(SelectedBookingTypeList.Count() != 0)
                    {
                        foreach(var type in SelectedBookingTypeList)
                        {
                            existingAppointments.AddRange(existingDBAppointments.Where(p => p.ROOM_Booking_Type_ID == type).ToList());
                        }
                    }
                    else
                    {
                        existingAppointments = existingDBAppointments;
                    }

                    foreach (var app in existingAppointments)
                    {
                        if (app.StartDate != null && app.EndDate != null && app.ROOM_Room_ID != null)
                        {
                            var ownerID = Resources.FirstOrDefault(p => p.ExternalID == app.ROOM_Room_ID.Value);

                            var newApp = new AppointmentData()
                            {
                                IsReadonly = true,
                                Description = app.Description,
                                StartTime = app.StartDate.Value,
                                EndTime = app.EndDate.Value,
                                CssClass = "",
                                ExternalID = app.ID,
                                ExternalGroupID = app.ROOM_BookingGroup_ID,
                                StatusID = app.ROOM_BookingStatus_ID,
                                AUTH_User_ID = app.AUTH_User_ID,
                                FirstName = app.FirstName,
                                LastName = app.LastName,
                                Email = app.Email,
                                MobilePhone = app.MobilePhone,
                                RoomID = room,
                                IsNew = false,
                                Subject = app.Title,
                                OwnerID = ownerID.ID,
                                ROOMBookingTypeID = app.ROOM_Booking_Type_ID,
                                IsWholeDay = app.IsWholeDay
                            };

                            if (app.StartDate.Value.DayOfYear != app.EndDate.Value.DayOfYear)
                            {
                                newApp.AppointmentString = app.StartDate.Value.ToString("dd.MM.yyyy HH:mm") + " - " + app.EndDate.Value.ToString("dd.MM.yyyy HH:mm");
                            }
                            else
                            {
                                newApp.AppointmentString = app.StartDate.Value.ToString("HH:mm") + " - " + app.EndDate.Value.ToString("HH:mm");
                            }

                            if (!string.IsNullOrEmpty(dbRoom.BuildingName))
                            {
                                newApp.RoomName = dbRoom.BuildingName + " - " + dbRoom.Name;
                            }
                            else
                            {
                                newApp.RoomName = dbRoom.Name;
                            }

                            if (newApp.EndTime < DateTime.Now)
                            {
                                newApp.IsReadonly = true;
                            }

                            if (app.ROOM_BookingGroupBackend_ID != null)
                            {
                                newApp.ManualInput = true;
                            }

                            if (app.ROOM_BookingStatus_ID == ROOMStatus.Comitted)
                            {
                                newApp.Accepted = false;
                            }
                            else
                            {
                                newApp.Accepted = true;
                            }

                            if (app.ROOM_BookingStatus_ID == ROOMStatus.ToPay)
                            {
                                newApp.ToPay = true;
                            }

                            if (app.ROOM_BookingStatus_ID == ROOMStatus.ToPay)
                            {
                                newApp.ToSign = true;
                            }

                            if (app.AUTH_User_ID == null)
                            {
                                ExternalInputType = 2;
                            }
                            else if(app.IsOrganization == true)
                            {
                                ExternalInputType = 1;
                            }
                            else
                            {
                                ExternalInputType = 0;
                            }

                            ExistingAppointments.Add(newApp);
                        }
                    }
                }
            }

            StateHasChanged();

            if (ScheduleRef != null)
            {
                await ScheduleRef.RefreshEventsAsync();
            }

            return true;
        }
        private async void ClearTagFilter()
        {
            if (SelectedRoomList != null)
            {
                SelectedRoomList = new List<Guid>();
                await GetAppointments();
            }
        }
        private async void ClearTypeTagFilter()
        {
            if (SelectedRoomList != null)
            {
                SelectedBookingTypeList = new List<Guid>();
                await GetAppointments();
            }
        }
        private async void AddFilter(Guid ROOM_Rooms_ID)
        {

            if (SelectedRoomList == null)
                SelectedRoomList = new List<Guid>();

            if (SelectedRoomList.Contains(ROOM_Rooms_ID))
            {
                SelectedRoomList.Remove(ROOM_Rooms_ID);
            }
            else
            {
                SelectedRoomList.Add(ROOM_Rooms_ID);
            }

            await GetAppointments();
        }
        private async void AddTypeFilter(Guid Type_ID)
        {

            if (SelectedBookingTypeList == null)
                SelectedBookingTypeList = new List<Guid>();

            if (SelectedBookingTypeList.Contains(Type_ID))
            {
                SelectedBookingTypeList.Remove(Type_ID);
            }
            else
            {
                SelectedBookingTypeList.Add(Type_ID);
            }

            await GetAppointments();
        }
        private void GoToBooking(Guid ROOM_BookingGroup_ID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/RoomBooking/Detail/" + ROOM_BookingGroup_ID);
            StateHasChanged();
        }
        private async Task RemoveBooking(Guid ROOM_Booking_ID)
        {
            if (!await Dialogs.ConfirmAsync(TextProvider.Get("FRONTEND_BOOKING_REMOVE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                return;

            IsDataBusy = true;
            StateHasChanged();

            var dbBooking = await RoomProvider.GetBooking(ROOM_Booking_ID);

            if (dbBooking != null)
            {
                dbBooking.RemovedDate = DateTime.Now;

                await RoomProvider.SetBooking(dbBooking);

                if (SessionWrapper.AUTH_Municipality_ID != null)
                {
                    if (dbBooking.ROOM_BookingGroup_ID != null)
                    {
                        var dbBookingGroup = await RoomProvider.GetBookingGroup(dbBooking.ROOM_BookingGroup_ID.Value);

                        if (dbBookingGroup != null && dbBookingGroup.ROOM_Booking_Type_ID != ROOMBookingType.Blocked)
                        {
                            BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, dbBookingGroup.ID, dbBookingGroup.Title, true, dbBooking);
                        }
                    }
                    else if (dbBooking.ROOM_BookingGroupBackend_ID != null)
                    {
                        var dbBookingGroup = await RoomProvider.GetBookingGroupBackend(dbBooking.ROOM_BookingGroupBackend_ID.Value);

                        if (dbBookingGroup != null && dbBookingGroup.ROOM_Booking_Type_ID != ROOMBookingType.Blocked)
                        {
                            BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, dbBookingGroup.ID, dbBookingGroup.Title, true, dbBooking);
                        }
                    }
                }

                await BookingService.SendMessage("NOTIF_BOOKING_CANCELED_BODY", "NOTIF_BOOKING_CANCELED_TITLE", ROOM_Booking_ID, NavManager.BaseUri, Guid.Parse("42AD8B13-3C85-4BEC-8A9C-FD3F14159352"));
            }

            await GetAppointments();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task OnCellClick(CellClickEventArgs args)
        {
            if (CurrentView == View.Month)
            {
                args.Cancel = true;
            }
        }
        private async Task EditEvent(AppointmentData Appointment)
        {
            if (ScheduleRef != null)
            {
                await ScheduleRef.OpenEditorAsync(Appointment, CurrentAction.EditOccurrence);
            }
        }
        private async Task UserSelected(Guid? Auth_User_ID, AppointmentData Data)
        {
            if (Auth_User_ID != null)
            {
                var anagraficData = await AuthProvider.GetAnagraficByUserID(Auth_User_ID.Value);

                if (anagraficData != null)
                {
                    Data.AUTH_User_ID = Auth_User_ID;
                    Data.Email = anagraficData.Email;
                    Data.MobilePhone = anagraficData.MobilePhone;
                    Data.FirstName = anagraficData.FirstName;
                    Data.LastName = anagraficData.LastName;

                    StateHasChanged();
                }
            }
        }
        public void OnActionBegin(PopupOpenEventArgs<AppointmentData> args)
        {
            if (args.Type == PopupType.Editor)
            {
                if(args.Data != null && args.Data.IsNew == true)
                {
                    args.Data.StartTime = new DateTime(args.Data.StartTime.Year, args.Data.StartTime.Month, args.Data.StartTime.Day, DateTime.Now.Hour, 0, 0);
                    args.Data.EndTime = args.Data.StartTime.AddHours(1);
                    args.Data.ROOMBookingTypeID = ROOMBookingType.External;
                }
            }

            IsDataBusy = false;
            StateHasChanged();
        }
        public async void OnActionCompleted(AppointmentData Item)
        {
            if (editform != null && editform.EditContext != null)
            {
                var result = editform.EditContext.Validate();

                if (Item.ROOMBookingTypeID == ROOMBookingType.External)
                {
                    if (!result)
                    {
                        StateHasChanged();
                        return;
                    }
                }

                if(Item.StartTime >= Item.EndTime)
                {
                    WrongDatesError = TextProvider.Get("BACKEND_BOOKING_DATE_WRONG");
                    StateHasChanged();
                    return;
                }
                else
                {
                    WrongDatesError = null;
                }

                if(Item.RoomID == null)
                {
                    return;
                }
            }

            if (Item.IsWholeDay == true)
            {
                Item.StartTime = new DateTime(Item.StartTime.Year, Item.StartTime.Month, Item.StartTime.Day, 0, 0, 0);
                Item.EndTime = new DateTime(Item.EndTime.Year, Item.EndTime.Month, Item.EndTime.Day, 23, 59, 0);
            }

            HideAppointmentEditWindow();
            IsDataBusy = true;
            StateHasChanged();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var existingItem = await RoomProvider.GetBooking(Item.ExternalID);

                if (existingItem == null)
                {
                    ROOM_BookingGroupBackend newBGroup = new ROOM_BookingGroupBackend();

                    newBGroup.ID = Guid.NewGuid();
                    newBGroup.CreatedAt = DateTime.Now;
                    newBGroup.Description = Item.Description;
                    newBGroup.Title = Item.Subject;
                    newBGroup.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                    newBGroup.FirstName = Item.FirstName;
                    newBGroup.LastName = Item.LastName;
                    newBGroup.MobilePhone = Item.MobilePhone;
                    newBGroup.EMail = Item.Email;
                    newBGroup.ROOM_BookingStatus_ID = ROOMStatus.Accepted;
                    newBGroup.IsWholeDay = Item.IsWholeDay;

                    if (ExternalInputType == 0 || ExternalInputType == 1)
                    {
                        newBGroup.AUTH_Users_ID = Item.AUTH_User_ID;
                    }
                    else
                    {
                        newBGroup.FirstName = Item.FirstName;
                        newBGroup.LastName = Item.LastName;
                        newBGroup.EMail = Item.Email;
                        newBGroup.MobilePhone = Item.MobilePhone;
                    }

                    newBGroup.ROOM_Booking_Type_ID = Item.ROOMBookingTypeID;

                    await RoomProvider.SetBookingGroupBackend(newBGroup);

                    ROOM_Booking booking = new ROOM_Booking();

                    booking.ID = Guid.NewGuid();
                    booking.ROOM_BookingGroupBackend_ID = newBGroup.ID;
                    booking.CreationDate = DateTime.Now;
                    booking.StartDate = Item.StartTime;
                    booking.EndDate = Item.EndTime;
                    booking.Price = 0;
                    booking.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                    booking.ROOM_Room_ID = Item.RoomID;

                    await RoomProvider.SetBooking(booking);

                    await BookingService.SendMessage("NOTIF_BOOKING_CREATED_BODY", "NOTIF_BOOKING_CREATED_TITLE", booking.ID, NavManager.BaseUri, Guid.Parse("DCD04015-C1BD-4AD5-99E6-AEEF7F35BFA4"));

                    if (newBGroup.ROOM_Booking_Type_ID != ROOMBookingType.Blocked)
                    {
                        BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, newBGroup.ID, newBGroup.Title, false);
                    }
                }
                else
                {
                    existingItem.StartDate = Item.StartTime;
                    existingItem.EndDate = Item.EndTime;
                    existingItem.ROOM_Room_ID = Item.RoomID;

                    await RoomProvider.SetBooking(existingItem);

                    if (existingItem.ROOM_BookingGroupBackend_ID != null)
                    {
                        var existingGroupItem = await RoomProvider.GetBookingGroupBackend(existingItem.ROOM_BookingGroupBackend_ID.Value);

                        if (existingGroupItem != null)
                        {
                            existingGroupItem.Description = Item.Description;
                            existingGroupItem.Title = Item.Subject;
                            existingGroupItem.FirstName = Item.FirstName;
                            existingGroupItem.LastName = Item.LastName;
                            existingGroupItem.MobilePhone = Item.MobilePhone;
                            existingGroupItem.EMail = Item.Email;
                            existingGroupItem.AUTH_Users_ID = Item.AUTH_User_ID;
                            existingGroupItem.IsWholeDay = Item.IsWholeDay;

                            if (ExternalInputType == 0 || ExternalInputType == 1)
                            {
                                existingGroupItem.AUTH_Users_ID = Item.AUTH_User_ID;
                            }
                            else
                            {
                                existingGroupItem.FirstName = Item.FirstName;
                                existingGroupItem.LastName = Item.LastName;
                                existingGroupItem.EMail = Item.Email;
                                existingGroupItem.MobilePhone = Item.MobilePhone;

                                existingGroupItem.AUTH_Users_ID = null;
                            }

                            await RoomProvider.SetBookingGroupBackend(existingGroupItem);

                            if (existingGroupItem.ROOM_Booking_Type_ID != ROOMBookingType.Blocked)
                            {
                                BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, existingGroupItem.ID, existingGroupItem.Title, false, existingItem);
                            }
                        }
                    }
                    else if (existingItem.ROOM_BookingGroup_ID != null)
                    {
                        var existingGroupItem = await RoomProvider.GetBookingGroup(existingItem.ROOM_BookingGroup_ID.Value);

                        if (existingGroupItem != null)
                        {
                            existingGroupItem.Description = Item.Description;
                            existingGroupItem.Title = Item.Subject;

                            await RoomProvider.SetBookingGroup(existingGroupItem);
                        }
                    }


                    await BookingService.SendMessage("NOTIF_BOOKING_CHANGED_BODY", "NOTIF_BOOKING_CHANGED_TITLE", existingItem.ID, NavManager.BaseUri, Guid.Parse("DCD04015-C1BD-4AD5-99E6-AEEF7F35BFA4"));

                }

                await GetAppointments();
            }
            IsDataBusy = false;
            StateHasChanged();
        }
        public void HideAppointmentEditWindow()
        {
            if (ScheduleRef != null)
            {
                ScheduleRef.CloseEditor();
                StateHasChanged();
            }
        }
        public void OnRenderCell(RenderCellEventArgs args)
        {
            if (args.Date <= MinDate)
            {
                args.CssClasses = new List<string>(){ "e-disable-dates" };
            }
        }       
    }
}
