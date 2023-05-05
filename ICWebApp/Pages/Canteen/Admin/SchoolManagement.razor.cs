using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Admin
{
    public partial class SchoolManagement
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Inject] private IBreadCrumbService CrumbService { get; set; }

        private List<CANTEEN_School> Schools = new List<CANTEEN_School>();
        private List<CANTEEN_Canteen> Canteens = new List<CANTEEN_Canteen>(); 
        private List<V_CANTEEN_SchoolType?> Types = new List<V_CANTEEN_SchoolType?>();
        private bool IsDataBusy { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await GetData();


            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_SCHOOLMANAGEMENT");
            SessionWrapper.PageSubTitle = TextProvider.Get("");
            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Canteen/Subscriptionlist", "MAINMENU_CANTEEN", null, null);
            CrumbService.AddBreadCrumb("/Canteen/Account", "MAINMENU_SCHOOLMANAGEMENT", null, null);

            Types = await CanteenProvider.GetSchoolTypeList();

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                var schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

                Schools = schools.OrderBy(p => p.Name).ToList();

                var canteens = await CanteenProvider.GetCanteens(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

                Canteens = canteens.OrderBy(p => p.Name).ToList();
            }

            return true;
        }
        private void AddSchool()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/School/Add/New");
        }
        private void EditSchool(CANTEEN_School Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/School/Add/" + Item.ID);
        }
        private async void RemoveSchool(CANTEEN_School Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE_SCHOOL"), TextProvider.Get("WARNING")))
                    return;

                IsDataBusy = true;
                StateHasChanged();

                await CanteenProvider.RemoveSchool(Item.ID);
                await GetData();

                IsDataBusy = false;
                StateHasChanged();
            }
        }
        private void AddCanteen()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/Canteen/Add/New");
        }
        private void EditCanteen(CANTEEN_Canteen Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/Canteen/Add/" + Item.ID);
        }

        private void StudentListCanteen(CANTEEN_Canteen Item)
        {
            BusyIndicatorService.IsBusy = true;
       
            NavManager.NavigateTo("/Backend/Canteen/StudentList/" + Item.ID);
            StateHasChanged();
        }
        private async void RemoveCanteen(CANTEEN_Canteen Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE_SCHOOL"), TextProvider.Get("WARNING")))
                    return;

                IsDataBusy = true;
                StateHasChanged();

                await CanteenProvider.RemoveCanteen(Item.ID);
                await GetData();

                IsDataBusy = false;
                StateHasChanged();
            }
        }
        private void EditSchoolPeriod(CANTEEN_School Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/School/Period/" + Item.ID);
        }

    }
}
