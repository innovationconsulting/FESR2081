using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Admin
{
    public partial class SchoolyearManagement
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Inject] private IBreadCrumbService CrumbService { get; set; }


        private List<V_CANTEEN_Schoolyear> SchoolyearList = new List<V_CANTEEN_Schoolyear>();
        private bool IsDataBusy { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await GetData();


            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_CANTEEN_SCHOOLYEARS");
            SessionWrapper.PageSubTitle = TextProvider.Get("");
            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Canteen/Account", "MAINMENU_BACKEND_CANTEEN_SCHOOLYEARS", null, null);

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                SchoolyearList = await CanteenProvider.GetSchoolsyearAll(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            }

            return true;
        }
        private void AddSchoolyear()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/Schoolyear/Add/New");
        }
        private void EditSchoolyear(V_CANTEEN_Schoolyear Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Canteen/Admin/Schoolyear/Add/" + Item.id);
        }
        private async void RemoveSchoolyear(V_CANTEEN_Schoolyear Item)
        {
            if (Item != null && Item.id != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE_SCHOOLYEAR"), TextProvider.Get("WARNING")))
                    return;

                IsDataBusy = true;
                StateHasChanged();

                await CanteenProvider.RemoveSchoolyear(Item.id);
                await GetData();

                IsDataBusy = false;
                StateHasChanged();
            }
        }
    }
}
