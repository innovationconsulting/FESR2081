using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Backend
{
    public partial class AccountBalanceList
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string CanteenID { get; set; }

        private List<V_CANTEEN_User> UserList = new List<V_CANTEEN_User>();
        private List<V_CANTEEN_User> SelectedUserList = new List<V_CANTEEN_User>();
        private CANTEEN_Configuration? Configuration;
        private Guid _canteenID;
        private bool IsDataBusy { get; set; } = true;
        private CANTEEN_SearchFilter searchFilter = new();
        private bool ExportAllPages { get; set; } = true;
        private bool LowBalanceSelected { get; set; } = false;
        private bool ServiceStopSelected { get; set; } = false;

        private bool DisabledSelected { get; set; } = false;

        private bool showDisabled { get; set; }
        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.ID == null ||
                SessionWrapper.CurrentUser.AUTH_Municipality_ID == null) NavManager.NavigateTo("/Canteen");

            searchFilter = new CANTEEN_SearchFilter();


            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_ACCOUNT_BALANCE_OVERVIEW");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Canteen/Account", "MAINMENU_BACKEND_ACCOUNT_BALANCE_OVERVIEW", null,
                null, true);

            await GetData();

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }

        private async Task GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                UserList = await CanteenProvider.GetVUserList(SessionWrapper.AUTH_Municipality_ID.Value);
                Configuration = await CanteenProvider.GetConfiguration(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            SelectedUserList = UserList.Where(e => e.ServiceDisabledDate == null).ToList();
            StateHasChanged();

        }
        private void ShowDetails(V_CANTEEN_User Item)
        {
            BusyIndicatorService.IsBusy = true;

            NavManager.NavigateTo("/Canteen/AccountDetail/" + Item.AUTH_User_ID.ToString());
            StateHasChanged();
        }

        private void Reload()
        {
            if(LowBalanceSelected)
                SetBalanceLowFilter();
            else if(ServiceStopSelected)
                SetBalanceServiceStopFilter();
            else
                ClearTagFilter();
            StateHasChanged();
        }

        private void ClearTagFilter()
        {
            LowBalanceSelected = false;
            ServiceStopSelected = false;
            DisabledSelected = false;
            SelectedUserList = UserList.Where(e => (e.ServiceDisabledDate == null || showDisabled)).ToList();

            StateHasChanged();
        }

        private void SetBalanceLowFilter()
        {
            LowBalanceSelected = true;
            ServiceStopSelected = false;
            DisabledSelected = false;

            if (Configuration != null && Configuration.BalanceLowWarning != null)
            {
                SelectedUserList = UserList.Where(p => p.Balance <= Configuration.BalanceLowWarning && (p.ServiceDisabledDate == null || showDisabled)).ToList();
            }

            StateHasChanged();
        }

        private void SetBalanceServiceStopFilter()
        {
            ServiceStopSelected = true;
            LowBalanceSelected = false;
            DisabledSelected = false;

            if (Configuration != null && Configuration.BalanceLowServiceStop != null)
            {
                SelectedUserList = UserList.Where(p => p.Balance <= Configuration.BalanceLowServiceStop && (p.ServiceDisabledDate == null || showDisabled)).ToList();
            }

            StateHasChanged();
        }

        private void SetDisabledFilter()
        {
            ServiceStopSelected = false;
            LowBalanceSelected = false;
            DisabledSelected = true;
            SelectedUserList = UserList.Where(p => p.ServiceDisabledDate != null).ToList();
            StateHasChanged();
        }

        private async void SendReminder(V_CANTEEN_User Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("REMIND_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await SendPaymentReminder(Item);

                await GetData();

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }

        private async void DisableUser(V_CANTEEN_User Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DISABLE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (Item.AUTH_User_ID != null && Item.MunicipalityID != null)
                {
                    Item.ServiceDisabledDate = DateTime.Now;
                    var dbItem = CanteenProvider.GetCanteenUserByID(Item.AUTH_User_ID.Value, Item.MunicipalityID.Value);

                    if (dbItem != null)
                    {
                        dbItem.ServiceDisabledDate = Item.ServiceDisabledDate;
                        await CanteenProvider.SetCanteenUser(dbItem);

                        var subs = await CanteenProvider.GetSubscribersByUserID(Item.AUTH_User_ID.Value);

                        foreach (var subscriber in subs)
                        {
                            if (subscriber.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted ||
                                subscriber.CANTEEN_Subscriber_Status_ID == CanteenStatus.Waitlist)
                            {
                                subscriber.CANTEEN_Subscriber_Status = null;
                                subscriber.CANTEEN_Subscriber_Status_ID = CanteenStatus.Disabled;

                                var subMovements =
                                    await CanteenProvider.GetSubscriberMovementsBySubscriber(subscriber.ID);

                                foreach (var movement in subMovements.Where(p => p.Date > DateTime.Now))
                                {
                                    await CanteenProvider.RemoveSubscriberMovement(movement.ID);
                                }

                                await CanteenProvider.SetSubscriber(subscriber);
                            }
                        }
                    }
                }

                await GetData();

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }

        private async Task SendPaymentReminder(V_CANTEEN_User Item)
        {
            if (Item.AUTH_User_ID != null)
            {
                var user = await AuthProvider.GetUser(Item.AUTH_User_ID.Value);

                if (Item.AUTH_User_ID != null && user != null && user.AUTH_Municipality_ID != null)
                {
                    MSG_Message? msg;
                    if (Configuration != null && Configuration.ServiceStopEnabled &&
                        Configuration.BalanceLowServiceStop != null &&
                        Item.Balance < Configuration.BalanceLowServiceStop)
                    {
                        var msgType = Guid.Parse("42AD8B13-3C85-4BEC-8A9C-FD3F14159352"); //Direct all
                        msg = await MessageService.GetMessage(Item.AUTH_User_ID.Value, Item.MunicipalityID.Value,
                            "NOTIF_CANTEEN_STOP_SERVICE_TEXT", "NOTIF_CANTEEN_STOP_SERVICE_SHORTTEXT",
                            "NOTIF_CANTEEN_STOP_SERVICE_TITLE", msgType, true);
                    }
                    else
                    {
                        var msgType = Guid.Parse("DCD04015-C1BD-4AD5-99E6-AEEF7F35BFA4"); //Direct mail push list
                        msg = await MessageService.GetMessage(Item.AUTH_User_ID.Value, Item.MunicipalityID.Value,
                            "NOTIF_CANTEEN_LOW_BALANCE_TEXT", "NOTIF_CANTEEN_LOW_BALANCE_SHORTTEXT",
                            "NOTIF_CANTEEN_LOW_BALANCE_TITLE", msgType, true);
                    }


                    if (msg != null)
                    {
                        await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                    }
                }
            }

            if (Item.AUTH_User_ID != null && Item.MunicipalityID != null)
            {
                Item.LastNotificationDate = DateTime.Now;
                var dbItem = CanteenProvider.GetCanteenUserByID(Item.AUTH_User_ID.Value, Item.MunicipalityID.Value);

                if (dbItem != null)
                {
                    dbItem.LastNotificationDate = Item.LastNotificationDate;
                    await CanteenProvider.SetCanteenUser(dbItem);
                }
            }
        }

        private async void SendMassiveReminder()
        {
            if (Configuration != null)
            {
                var users = SelectedUserList.Where(p =>
                    p.Balance < Configuration.BalanceLowWarning || p.Balance < Configuration.BalanceLowServiceStop);

                if (users.Count() > 0)
                {
                    var text = TextProvider.Get("CANTEEN_PAYMENT_REMEMBER_ARE_YOU_SURE");

                    text = text.Replace("{PersonCount}", users.Count().ToString());

                    if (!await Dialogs.ConfirmAsync(text, TextProvider.Get("WARNING")))
                        return;

                    BusyIndicatorService.IsBusy = true;
                    StateHasChanged();

                    foreach (var Item in users)
                    {
                        await SendPaymentReminder(Item);
                    }

                    await GetData();

                    BusyIndicatorService.IsBusy = false;
                    StateHasChanged();
                }
            }
        }
    }
}