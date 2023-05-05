using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Rooms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Frontend
{
    public partial class CalendarComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IRoomBookingHelper RoomBookingHelper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }

        [Parameter] public V_ROOM_Rooms Room { get; set; }
        [Parameter] public EventCallback<object> OnTimeSelected { get; set; }

        SfSchedule<AppointmentData>? ScheduleRef;
        EditForm? editform;
        public int[] WorkingDays { get; set; } = { 0, 1, 2, 3, 4, 5, 6 };
        private List<AppointmentData> ExistingAppointments = new List<AppointmentData>();
        private DateTime CurrentDate = DateTime.Today;
        private DateTime MinDate = DateTime.Today;
        private DateTime MaxDate = DateTime.Today.AddYears(2);

        ValidationRules RequiredRules = new ValidationRules { Required = true };

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var config = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

                if(config != null)
                {
                    if (config.MinRoomBookingDays != null)
                    {
                        MinDate = MinDate.AddDays(config.MinRoomBookingDays.Value);
                    }

                    if (config.MaxRoomBookingDays != null)
                    {
                        MaxDate = DateTime.Now.AddDays(config.MaxRoomBookingDays.Value);
                    }
                }

                var existingAppointments = await RoomProvider.GetBookings(Room.ID);

                foreach (var app in existingAppointments)
                {
                    if (app.StartDate != null && app.EndDate != null)
                    {
                        var newApp = new AppointmentData()
                        {
                            IsReadonly = true,
                            Description = "",
                            StartTime = app.StartDate.Value,
                            EndTime = app.EndDate.Value,
                            Subject = "",
                            CssClass = "scheduler-existing"
                        };

                        if(app.StartDate.Value.DayOfYear != app.EndDate.Value.DayOfYear)
                        {
                            newApp.AppointmentString = null;
                        }
                        else
                        {
                            newApp.AppointmentString = app.StartDate.Value.ToString("HH:mm") + " - " + app.EndDate.Value.ToString("HH:mm");
                        }

                        if (app.ROOM_BookingStatus_ID == ROOMStatus.Comitted || app.ROOM_BookingStatus_ID == ROOMStatus.ToPay || app.ROOM_BookingStatus_ID == ROOMStatus.ToSign)
                        {
                            newApp.CssClass = "scheduler-reserved";
                        }

                        ExistingAppointments.Add(newApp);
                    }
                }
            }

            if(RoomBookingHelper != null && RoomBookingHelper.TimeFilter != null && RoomBookingHelper.TimeFilter.ShowSeries == false
                && RoomBookingHelper.TimeFilter.Meeting_From_Combined != null && RoomBookingHelper.TimeFilter.Meeting_To_Combined != null)
            {
                var myApp = new AppointmentData()
                {
                    IsReadonly = false,
                    Description = "",
                    StartTime = RoomBookingHelper.TimeFilter.Meeting_From_Combined.Value,
                    EndTime = RoomBookingHelper.TimeFilter.Meeting_To_Combined.Value,
                    CssClass = "scheduler-user-appointment",
                    Subject = TextProvider.Get("ROOM_SCHEDULER_DEFAULT_APPOINTMENT_TITLE")
                };

                if (myApp.StartTime.DayOfYear != myApp.EndTime.DayOfYear)
                {
                    myApp.AppointmentString = null;
                }
                else
                {
                    myApp.AppointmentString = myApp.StartTime.ToString("HH:mm") + " - " + myApp.EndTime.ToString("HH:mm");
                }

                ExistingAppointments.Add(myApp);
            }

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void OnActionCompleted(AppointmentData record)
        {
            if (record != null) 
            {
                RoomBookingHelper.TimeFilter.ShowSeries = false;
                RoomBookingHelper.TimeFilter.Meeting_FromDate = record.StartTime;
                RoomBookingHelper.TimeFilter.Meeting_FromHour = record.StartTime;
                RoomBookingHelper.TimeFilter.Meeting_ToDate = record.EndTime;
                RoomBookingHelper.TimeFilter.Meeting_ToHour = record.EndTime;
            }

            if(ScheduleRef != null)
            {
                ScheduleRef.CloseEditor();
                StateHasChanged();
            }

            await OnTimeSelected.InvokeAsync();            
        }
        private async Task OnCellClick(CellClickEventArgs args)
        {
            args.Cancel = true;

            var startTime = args.StartTime.AddHours(DateTime.Now.Hour); 
            var endTime = args.StartTime.AddHours(DateTime.Now.Hour + 1);

            args.StartTime = startTime;
            args.EndTime = endTime;

            await ScheduleRef.OpenEditorAsync(args, CurrentAction.Add);
        }
        private async Task OnEventClick(EventClickArgs<AppointmentData> args)
        {
            args.Cancel = true;

            if (args.Event != null && args.Event.IsReadonly == false)
            {
                CurrentAction action = CurrentAction.Save;

                await ScheduleRef.OpenEditorAsync(args.Event, action);
            }
        }
        private void HideAppointmentEditor()
        {
            if (ScheduleRef != null)
            {
                ScheduleRef.CloseEditor();
                StateHasChanged();
            }
        }
    }
}
