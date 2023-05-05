using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.Models.Rooms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace ICWebApp.Components.Rooms.Frontend
{
    public partial class TimeFilterComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }

        [Parameter] public TimeFilter? Filter { get; set; }
        [Parameter] public EventCallback OnSearch { get; set; }

        IJSObjectReference? _module;
        private List<WeekDayOfMonth> WeekDayOfMonthList = new List<WeekDayOfMonth>();
        private List<WeekDay> WeekDayList = new List<WeekDay>();
        private DateTime MinDate = DateTime.Today;
        private DateTime MaxDate = DateTime.Today.AddYears(2);

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var config = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

                if (config != null)
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
            }

            WeekDayOfMonthList.Add(WeekDayOfMonth.First);
            WeekDayOfMonthList.Add(WeekDayOfMonth.Second);
            WeekDayOfMonthList.Add(WeekDayOfMonth.Third);
            WeekDayOfMonthList.Add(WeekDayOfMonth.Fourth);
            WeekDayOfMonthList.Add(WeekDayOfMonth.Last);

            WeekDayList.Add(WeekDay.Monday);
            WeekDayList.Add(WeekDay.Tuesday);
            WeekDayList.Add(WeekDay.Wednesday);
            WeekDayList.Add(WeekDay.Thursday);
            WeekDayList.Add(WeekDay.Friday);
            WeekDayList.Add(WeekDay.Saturday);
            WeekDayList.Add(WeekDay.Sunday);

            if(Filter != null)
            {
                Filter.OnSeriesEndDateChanged += Filter_OnSeriesEndDateChanged;
                Filter.OnSeriesRepetitionCountChanged += Filter_OnSeriesRepetitionCountChanged;
            }

            BookingService.OnValuesChanged += BookingService_OnValuesChanged;

            await base.OnInitializedAsync();
        }
        private async void BookingService_OnValuesChanged()
        {
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
        private void Filter_OnSeriesRepetitionCountChanged()
        {
            if (Filter != null)
            {
                Filter = BookingService.UpdateEndDate(Filter);
                StateHasChanged();
            }
        }
        private void Filter_OnSeriesEndDateChanged()
        {
            if (Filter != null)
            {
                Filter = BookingService.UpdateRepetitionCount(Filter);
                StateHasChanged();
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            if (Filter != null)
            {
                if (SessionWrapper.AUTH_Municipality_ID != null && Filter.Meeting_FromDate == null && Filter.Meeting_ToDate == null)
                {
                    var config = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

                    if (Filter.Meeting_FromDate == null)
                        Filter.Meeting_FromDate = DateTime.Now;

                    if (Filter.Meeting_FromHour == null)
                        Filter.Meeting_FromHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

                    if (Filter.Meeting_ToDate == null)
                        Filter.Meeting_ToDate = DateTime.Now.AddHours(1);

                    if (Filter.Meeting_ToHour == null)
                        Filter.Meeting_ToHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).AddHours(1);

                    if (config != null && config.MinRoomBookingDays != null)
                    {
                        Filter.Meeting_FromDate = Filter.Meeting_FromDate.Value.AddDays(config.MinRoomBookingDays.Value);
                        Filter.Meeting_ToDate = Filter.Meeting_FromDate;
                    }
                }
            }

            StateHasChanged();
            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_module == null)
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/Rooms/RoomHelper.js");
            }

            if (_module != null && firstRender == true && Filter != null && Filter.ShowSeries == true)
            {
                await _module.InvokeVoidAsync("HideElement", "room-meeting-window");
                await _module.InvokeVoidAsync("ShowElement", "room-series-window");
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private async void CreateSeries()
        {
            if(Filter != null && _module != null)
            {
                Filter.ShowSeries = !Filter.ShowSeries;

                if (Filter.ShowSeries == true)
                {
                    await _module.InvokeVoidAsync("HideElement", "room-meeting-window");
                    await _module.InvokeVoidAsync("ShowElement", "room-series-window");

                    Filter.NotifySeriesRepetitionCountChanged();
                    Filter.NotifySeriesEndDateChanged();
                }
                else
                {
                    await _module.InvokeVoidAsync("HideElement", "room-series-window");
                    await _module.InvokeVoidAsync("ShowElement", "room-meeting-window");
                }

                Filter.Series_StartDate = Filter.Meeting_FromDate;
                Filter.Series_EndDate = Filter.Meeting_ToDate;

                BookingService.NotifyValuesChanged();

                StateHasChanged();
            }
        }
        private void OnWeekDayChanged(WeekDay WeekDay)
        {
            if(Filter != null)
            {
                if (Filter.Weekly_Weekdays.Contains(WeekDay))
                    Filter.Weekly_Weekdays.Remove(WeekDay);
                else
                    Filter.Weekly_Weekdays.Add(WeekDay);

                Filter.NotifySeriesRepetitionCountChanged();
                Filter.NotifySeriesEndDateChanged();

                BookingService.NotifyValuesChanged();
            }
        }
        private bool GetWeekDayValue(WeekDay WeekDay)
        {
            if (Filter != null)
            {
                if (Filter.Weekly_Weekdays.Contains(WeekDay))
                {
                    return true;
                }
            }

            return false;
        }
        private void Search()
        {
            OnSearch.InvokeAsync();
        }
    }
}
