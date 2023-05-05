using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Canteen.Admin.SubPages
{
    public partial class CanteenAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] private IBreadCrumbService CrumbService { get; set; }

        [Parameter] public string ID { get; set; }

        private CANTEEN_Canteen? Data { get; set; }
        private List<CANTEEN_School> Schools { get; set; }
        private List<CANTEEN_School_Canteen> SchoolToCanteens { get; set; } = new List<CANTEEN_School_Canteen>();
        private List<CANTEEN_MealMenu> MenuTypes = new List<CANTEEN_MealMenu>();

        List<Guid?> SelectedSchooolIds = new List<Guid?>();
        private decimal? StartValue = 0;

        protected override async Task OnInitializedAsync()
        {
            if (ID == "New")
            {
                Data = new CANTEEN_Canteen();
                Data.ID = Guid.NewGuid();
                if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
                {
                    Data.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value;
                }
            }
            else
            {
                Data = await CanteenProvider.GetCanteen(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }
            }

            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null) 
            {
                Schools = await CanteenProvider.GetSchools(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);
            }

            SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_CANTEENMANAGEMENT");
            SessionWrapper.PageSubTitle = TextProvider.Get("");

            StartValue = Data.PricePerUse;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Canteen/Subscriptionlist", "MAINMENU_CANTEEN", null, null);
            CrumbService.AddBreadCrumb("/Admin/Canteen/CanteenManagement", "CANTEEN_CANTEENMANAGEMENT", null, null, true);

            var s2c = await CanteenProvider.GetSchoolsToCanteen(Data.ID);

            SelectedSchooolIds = s2c.Select(a => a.CANTEEN_School_ID).ToList();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Admin/Canteen/CanteenManagement");
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (Data != null)
            {
                var s2c = await CanteenProvider.GetSchoolsToCanteen(Data.ID);

                if (s2c != null && s2c.Count() > 0)
                {
                    foreach (var item in s2c.ToList())
                    {
                        await CanteenProvider.RemoveSchoolsToCanteen(item.ID);
                    }
                }

                await CanteenProvider.SetCanteen(Data);

                if (Data.PricePerUse != StartValue)
                {
                    var movements = await CanteenProvider.GetSubscriberMovementsByCanteen(Data.ID, true);

                    var movementsToChange = movements.Where(p => p.Date > DateTime.Now && p.CANTEEN_Subscriber_ID != null).ToList();

                    foreach (var move in movementsToChange)
                    {
                        if (Data.PricePerUse != null)
                        {
                            if (Data.PricePerUse > 0)
                            {
                                move.Fee = Data.PricePerUse.Value * -1;
                            }
                            else
                            {
                                move.Fee = Data.PricePerUse.Value;
                            }

                            await CanteenProvider.SetSubscriberMovement(move);
                        }
                    }
                }

                foreach (var item in SelectedSchooolIds)
                {
                    var newItem = new CANTEEN_School_Canteen();
                    newItem.CANTEEN_Canteen_ID = Data.ID;
                    newItem.CANTEEN_School_ID = item.Value;
                    await CanteenProvider.SetSchoolsToCanteen(newItem);
                }
            }

            NavManager.NavigateTo("/Admin/Canteen/CanteenManagement");
        }
    }
}
