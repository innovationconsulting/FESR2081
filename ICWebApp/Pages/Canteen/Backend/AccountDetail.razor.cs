using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Backend
{
    public partial class AccountDetail
    {
        [Inject] private IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] private ISessionWrapper SessionWrapper { get; set; }
        [Inject] private ICANTEENProvider CanteenProvider { get; set; }
        [Inject] private ITEXTProvider TextProvider { get; set; }
        [Inject] private IPAYProvider PayProvider { get; set; }
        [Inject] private NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string UserID { get; set; }

        private V_CANTEEN_User? CurrentUser;
        private List<V_CANTEEN_Subscriber_Movements> Movements = new List<V_CANTEEN_Subscriber_Movements>();
        private bool ShowMovementWindow = false;
        private decimal MovementToAddInitialValue = 0;
        private CANTEEN_Subscriber_Movements? MovementToAdd;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Canteen/Account", "MAINMENU_BACKEND_ACCOUNT_BALANCE_OVERVIEW", null, null);
            CrumbService.AddBreadCrumb(NavManager.BaseUri, "CANTEEN_ACCOUNT_DETAIL", null, null, true);

            CurrentUser = await CanteenProvider.GetVUser(Guid.Parse(UserID));

            if (CurrentUser != null && CurrentUser.AUTH_User_ID != null)
            {
                SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_ACCOUNT_DETAIL") + " - " + CurrentUser.FullName;
                UpdateMovements();
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/Canteen/Account");
        }
        private void AddMovement()
        {
            if (CurrentUser != null && CurrentUser.AUTH_User_ID != null)
            {
                MovementToAdd = new CANTEEN_Subscriber_Movements();

                MovementToAdd.ID = Guid.NewGuid();
                    MovementToAdd.AUTH_User_ID = CurrentUser.AUTH_User_ID;
                MovementToAdd.CANTEEN_Subscriber_Movement_Type_ID = Guid.Parse("cb7303e5-aed5-4d5d-8b27-5b31cfacca14"); //RECHARGE

                MovementToAdd.Date = DateTime.Now;
                MovementToAdd.PaymentTransactionID = Guid.Empty;

                MovementToAdd.IsManual = false;
                MovementToAdd.IsMunicipalInput = true;
                MovementToAddInitialValue = MovementToAdd.Fee ?? 0;
                ShowMovementWindow = true;
                StateHasChanged();
            }
        }
        private async void EditMovement(V_CANTEEN_Subscriber_Movements Item)
        {
            MovementToAdd = await CanteenProvider.GetSubscriberMovementById(Item.ID);
            MovementToAddInitialValue = MovementToAdd?.Fee ?? 0;
            ShowMovementWindow = true;
            StateHasChanged();
        }
        private async void SaveMovement()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MovementToAdd != null)
            {
                var trans = new PAY_Transaction();

                trans.ID = Guid.NewGuid();
                trans.ComunixSource = "MENSA BACKEND";
                trans.PaymentDate = DateTime.Now;
                trans.TotalAmount = MovementToAdd.Fee;
                trans.PAY_Type_ID = Guid.Parse("B16D8119-E050-46C8-BB81-A1D08891F298");
                trans.Family_ID = Guid.NewGuid();
                if (SessionWrapper.AUTH_Municipality_ID != null)
                {
                    trans.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                }
                trans.Description = MovementToAdd.Description;

                if (CurrentUser != null && CurrentUser.AUTH_User_ID != null)
                {
                    trans.AUTH_Users_ID = CurrentUser.AUTH_User_ID.Value;
                }

                await PayProvider.SetTransaction(trans);

                MovementToAdd.PaymentTransactionID = trans.ID;

                await CanteenProvider.SetSubscriberMovement(MovementToAdd);
            }

            if (CurrentUser != null && CurrentUser.AUTH_User_ID != null)
            {
                UpdateMovements();
                CurrentUser = await CanteenProvider.GetVUser(Guid.Parse(UserID));
            }

            BusyIndicatorService.IsBusy = false;
            ShowMovementWindow = false;
            StateHasChanged();
        }
        private async void HideMovementWindow()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (CurrentUser != null && CurrentUser.AUTH_User_ID != null)
            {
                UpdateMovements();
            }

            MovementToAddInitialValue = 0;
            BusyIndicatorService.IsBusy = false;
            ShowMovementWindow = false;
            StateHasChanged();
        }
        private async void DeleteMovement(V_CANTEEN_Subscriber_Movements Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await CanteenProvider.RemoveSubscriberMovement(Item.ID);

                if (CurrentUser != null && CurrentUser.AUTH_User_ID != null)
                {
                    UpdateMovements();
                    CurrentUser = await CanteenProvider.GetVUser(Guid.Parse(UserID));
                }

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }

        private async void UpdateMovements()
        {
            if(CurrentUser != null && CurrentUser.AUTH_User_ID != null) {
                Movements = await CanteenProvider.GetPastVSubscriberMovementsByUser(CurrentUser.AUTH_User_ID.Value);
                StateHasChanged();
            }
            
        }
    }
}