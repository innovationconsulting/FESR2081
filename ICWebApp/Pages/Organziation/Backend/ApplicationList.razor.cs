using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace ICWebApp.Pages.Organziation.Backend
{
    public partial class ApplicationList
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IRequestAdministrationHelper RequestAdministrationHelper { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        private bool IsDataBusy = true;
        private List<V_ORG_Requests> Data = new List<V_ORG_Requests>();
        private Administration_Filter_Request Filter = new Administration_Filter_Request();

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("ORG_BACKEND_APPLICATION_TITLE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/RoomBooking/List", "ROOMBOOKING_ROOM_BOOKING_LIST", null, null, true);

            if (RequestAdministrationHelper.Filter != null)
            {
                Filter = RequestAdministrationHelper.Filter;
            }
            else
            {
                Filter.Company_Type_ID = new List<Guid>();
                Filter.Request_Status_ID = new List<Guid>();
                Filter.Request_Status_ID.Add(Guid.Parse("d09bfdf6-406b-44b8-9def-d37481b0828a"));   //REQUEST
            }

            Data = await GetData();

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<V_ORG_Requests>> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var data = await OrgProvider.GetRequestList(Filter);

                RequestAdministrationHelper.Filter = Filter;

                return data;
            }

            return new List<V_ORG_Requests>();
        }
        private void ShowDetails(V_ORG_Requests Item)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Organization/Backend/Application/Detail/" + Item.ID);
            StateHasChanged();
        }
        private async void FilterSearch(Administration_Filter_Request Filter)
        {
            IsDataBusy = true;
            StateHasChanged();

            Data = await GetData();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
        {
            var item = (V_ORG_Requests)Args.Item; ;

            if (item != null)
            {
                ShowDetails(item);
            }

            return true;
        }
    }
}
