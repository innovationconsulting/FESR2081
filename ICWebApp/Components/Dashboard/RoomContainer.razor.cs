using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Pages.Canteen.Backend;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace ICWebApp.Components.Dashboard;

public partial class RoomContainer
{
    [Inject] private ISessionWrapper SessionWrapper { get; set; }
    [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }
    [Inject] private IAUTHProvider AuthProvider { get; set; }
    [Inject] private IFORMApplicationProvider FormApplicationProvider { get; set; }
    [Inject] private IFORMDefinitionProvider FormDefinitionProvider { get; set; }
    [Inject] private ITEXTProvider TextProvider { get; set; }
    [Inject] private IFormAdministrationHelper FormAdministrationHelper { get; set; }
    [Parameter] public string AuthorityID { get; set; }
    private AUTH_Authority? Authority { get; set; }
    private V_AUTH_Authority_Statistik? AuthorityStatistik { get; set; }
    private List<FORM_Application_Status>? StatusList { get; set; }
    private List<FORM_Application_Priority>? PriorityList { get; set; }

    private List<V_FORM_Application> Applications = new();
    private bool IsDataBusy { get; set; } = true;



    private int ApplicationOpenCount = 0;
    private int ApplicationWaitinglistCount = 0;
    private int ApplicationWaitingPaymentCount = 0;

    [Inject] IRoomProvider RoomProvider { get; set; }
    [Inject] IRoomAdministrationHelper RoomAdministrationHelper { get; set; }
    private List<V_ROOM_Booking_Group> Bookinggroups = new List<V_ROOM_Booking_Group>();
    private Administration_Filter_RoomBookingGroup Filter = new Administration_Filter_RoomBookingGroup();

    protected override async Task OnParametersSetAsync()
    {
        IsDataBusy = true;
        StateHasChanged();

        Authority = await GetData();

        await GetApplications();

        IsDataBusy = false;
        StateHasChanged();

        await base.OnParametersSetAsync();
    }

    private async Task<AUTH_Authority?> GetData()
    {
        if (AuthorityID != null) return await AuthProvider.GetAuthority(Guid.Parse(AuthorityID));

        return null;
    }


    private async Task<List<V_ROOM_Booking_Group>> GetApplications()
    {
        if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
        {
            if (RoomAdministrationHelper.Filter != null)
            {
                Filter = RoomAdministrationHelper.Filter;
            }
            else
            {
                Filter.Room_ID = new List<Guid>();
                Filter.Booking_Status_ID = new List<Guid>();
                Filter.Booking_Status_ID.Add(Guid.Parse("B99595E0-B4E1-4F46-A10A-7D42F80C491E"));   //REQUEST
                Filter.Booking_Status_ID.Add(Guid.Parse("000FB92E-3BAF-483D-9FA7-08C79A4DAF8B"));   //TO_PAY
             //   Filter.Booking_Status_ID.Add(Guid.Parse("159A9EBC-5E27-4AA6-992F-6E1B35502F04"));   //Accepted with Changes
             //   Filter.Booking_Status_ID.Add(Guid.Parse("55DFD0BE-E6D9-447D-B5CC-7F2013181C75"));   //Accepted
            }

            Bookinggroups = await GetRoomData();
            //Bookinggroups = Bookinggroups.Where(a => a.LastDate >= DateTime.Today).ToList();

            ApplicationOpenCount = Bookinggroups.Where(a =>
                a.ROOM_BookingStatus_ID == Guid.Parse("B99595E0-B4E1-4F46-A10A-7D42F80C491E")).Count();
            ApplicationWaitingPaymentCount = Bookinggroups.Where(a =>
                a.ROOM_BookingStatus_ID == Guid.Parse("000FB92E-3BAF-483D-9FA7-08C79A4DAF8B")).Count();
        }


        return Bookinggroups;
    }

    private async Task<List<V_ROOM_Booking_Group>> GetRoomData()
    {
        if (SessionWrapper.AUTH_Municipality_ID != null)
        {
            var data = await RoomProvider.GetBookingGroups(SessionWrapper.AUTH_Municipality_ID.Value, Filter);

            RoomAdministrationHelper.Filter = Filter;

            return data;
        }

        return new List<V_ROOM_Booking_Group>();
    }


    private void ShowListByStatusGroup(int Group)
    {
        if (Authority != null)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/RoomBooking/List");
            StateHasChanged();
        }
    }



    private void ShowDetailPage(Guid ApplicationID)
    {
        BusyIndicatorService.IsBusy = true;
        NavManager.NavigateTo("/RoomBooking/Detail/" +ApplicationID);
        StateHasChanged();
    }

    private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
    {
        var item = (V_ROOM_Booking_Group)Args.Item;

        if (item != null) ShowDetailPage(item.ID);

        return true;
    }
}