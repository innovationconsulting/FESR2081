using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class RechargeAmount
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Parameter] public string Completed { get; set; }

        private decimal CurrentBalance = 0;
        private decimal NewBalance = 0;
        private decimal MaxBalance = 0;
        private decimal MinBalance = 0;
        private List<CANTEEN_Subscriber_Movements> LatestMovements = new List<CANTEEN_Subscriber_Movements>();
        private List<CANTEEN_Subscriber> Subscribers = new List<CANTEEN_Subscriber>();
        private bool IsDataBusy { get; set; } = false;
        private bool ShowPayProvider { get; set; } = false;
        private Guid MovementID { get; set; } = Guid.NewGuid();
        private List<Guid?> PAY_Transaction_IDs = new List<Guid?>();
        private CANTEEN_Subscriber_Movements? UncompletedTransaction;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;

            SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_DASHBOARD_BALANCE");

            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/RechargeAmount", "CANTEEN_DASHBOARD_BALANCE", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/RechargeAmount", "CANTEEN_DASHBOARD_BALANCE", null, null);
            }

            StateHasChanged();

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                await LoadData();
            }

            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

            BusyIndicatorService.IsBusy = false;

            if (Completed == null)
            {
                //if (SessionWrapper != null && SessionWrapper.CurrentUser != null) 
                //{ 
                //    var lastTransactions = await CanteenProvider.GetSubscriberMovementsByUser(SessionWrapper.CurrentUser.ID);
                //    lastTransactions = lastTransactions.Where(a => a.PaymentTransactionID != null && (a.PaymentTransaction != null && a.PaymentTransaction.PaymentDate == null && a.PaymentTransaction.PagoPANotificationDate == null)).ToList();

                //    if (lastTransactions.Count > 0)
                //    {
                //        ShowPayProvider = true;
                //        PAY_Transaction_IDs.Add(lastTransactions.FirstOrDefault().PaymentTransactionID ?? Guid.NewGuid());
                //        MovementID = lastTransactions.FirstOrDefault().ID;
                //        UncompletedTransaction = lastTransactions.FirstOrDefault();
                //    }
                //}
            }
            else
            {
                await PaymentCompleted();
            }

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void SessionWrapper_OnCurrentUserChanged()
        {
            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                BusyIndicatorService.IsBusy = true;

                if (MyCivisService.Enabled == true)
                {
                    NavManager.NavigateTo("/Canteen/MyCivis");
                }
                else
                {
                    NavManager.NavigateTo("/Canteen");
                }

                StateHasChanged();
            }
        }
        private async Task<bool> LoadData()
        {
            IsDataBusy = true;
            StateHasChanged();

            CurrentBalance = CanteenProvider.GetUserBalance(SessionWrapper.CurrentUser.ID);
            NewBalance = CanteenProvider.GetOpenPayent(SessionWrapper.CurrentUser.ID) * (-1);

            if (NewBalance < 0)
            {
                NewBalance = 0;
            }

            MaxBalance = NewBalance;

            MinBalance = 50;

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var config = await CanteenProvider.GetConfiguration(SessionWrapper.AUTH_Municipality_ID.Value);

                if(config != null && config.RechargeMinAmount != null)
                {
                    MinBalance = config.RechargeMinAmount.Value;
                }
            }

            if (MaxBalance < MinBalance)
            {
                MinBalance = MaxBalance;
            }

            Subscribers = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);

            IsDataBusy = false;
            StateHasChanged();

            return true;
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Service");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Service");
            }
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (NewBalance > 0 && (PAY_Transaction_IDs == null || PAY_Transaction_IDs.Count() == 0))
            {
                var newPayment = new CANTEEN_Subscriber_Movements();
                newPayment.ID = Guid.NewGuid();
                newPayment.Date = DateTime.Now;
                newPayment.CANTEEN_Subscriber_Movement_Type_ID = Guid.Parse("cb7303e5-aed5-4d5d-8b27-5b31cfacca14");
                newPayment.Fee = NewBalance;
                newPayment.AUTH_User_ID = SessionWrapper.CurrentUser.ID;
                newPayment.Description = TextProvider.GetOrCreate("CANTEEN_DASHBOARD_RECARGEBALANCE");

                await CanteenProvider.SetSubscriberMovement(newPayment);

                MovementID = newPayment.ID;
                UncompletedTransaction = newPayment;

                PAY_Transaction trans = new PAY_Transaction();
                trans.ID = Guid.NewGuid();
                trans.AUTH_Users_ID = newPayment.AUTH_User_ID;
                trans.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID;
                trans.CreationDate = DateTime.Now;
                trans.Description = TextProvider.GetOrCreate("CANTEEN_CANTEEN") + " " + TextProvider.GetOrCreate("CANTEEN_DASHBOARD_RECARGEBALANCE");
                trans.PAY_Type_ID = Guid.Parse("b16d8119-e050-46c8-bb81-a1d08891f298"); //STANDARD

                decimal TotalAmount = 0;

                await PayProvider.SetTransaction(trans);

                newPayment.PaymentTransactionID = trans.ID;
                await CanteenProvider.SetSubscriberMovement(newPayment);
                PAY_Transaction_IDs.Add(trans.ID);

                PAY_Transaction_Position transPos = new PAY_Transaction_Position();
                transPos.ID = Guid.NewGuid();
                transPos.PAY_Transaction_ID = trans.ID;
                transPos.Description = TextProvider.GetOrCreate("CANTEEN_DASHBOARD_RECARGEBALANCE") + " - " + SessionWrapper.CurrentUser.Username;

                var pagoPaIdentifier = await PayProvider.GetPagoPaMensaIdentifier();

                if(pagoPaIdentifier != null)
                {
                    transPos.PagoPA_Identification = pagoPaIdentifier.PagoPA_Identifier;
                    transPos.TipologiaServizio = pagoPaIdentifier.TipologiaServizio;
                }

                transPos.Amount = NewBalance;

                if (transPos.Amount != null)
                {
                    TotalAmount += transPos.Amount.Value;
                }

                trans.TotalAmount = TotalAmount;
                await PayProvider.SetTransaction(trans);
                await PayProvider.SetTransactionPosition(transPos);

                ShowPayProvider = true;
                BusyIndicatorService.IsBusy = false;
                IsDataBusy = false;
                StateHasChanged();
            }
            else
            {
                if (PAY_Transaction_IDs.FirstOrDefault() != null)
                {
                    var trans = await PayProvider.GetTransaction(PAY_Transaction_IDs.FirstOrDefault().Value);

                    if (trans != null && UncompletedTransaction != null)
                    {
                        var transPos = await PayProvider.GetTransactionPositionList(trans.ID);

                        if (transPos != null && transPos.Count() > 0)
                        {
                            var posToAdd = transPos.FirstOrDefault(p => p.IsFee == false);

                            if (posToAdd != null)
                            {
                                var pagoPaIdentifier = await PayProvider.GetPagoPaMensaIdentifier();

                                if (pagoPaIdentifier != null)
                                {
                                    posToAdd.PagoPA_Identification = pagoPaIdentifier.PagoPA_Identifier;
                                }


                                posToAdd.Amount = NewBalance;

                                if (posToAdd.Amount != null)
                                {
                                    trans.TotalAmount = transPos.Sum(p => p.Amount);
                                }

                                UncompletedTransaction.Fee = NewBalance;

                                await PayProvider.SetTransactionPosition(posToAdd);
                                await PayProvider.SetTransaction(trans);
                                await CanteenProvider.SetSubscriberMovement(UncompletedTransaction);
                            }
                        }
                    }
                }

                AnchorService.ClearAnchors();
                ShowPayProvider = true;
                BusyIndicatorService.IsBusy = false;
                IsDataBusy = false;
                StateHasChanged();
            }

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();
        }
        private void BackToPreviousPayment()
        {
            ShowPayProvider = false;
            StateHasChanged();
            return;
        }
        private async Task<bool> PaymentCompleted()
        {
            if(Completed != null)
            {
                MovementID = Guid.Parse(Completed);
            }

            var movement = await CanteenProvider.GetSubscriberMovement(MovementID);

            if (movement != null && movement.PaymentTransactionID != null)
            {
                var paymentTransaction = await PayProvider.GetTransaction(movement.PaymentTransactionID.Value);

                if (paymentTransaction != null && paymentTransaction.PaymentDate != null)
                {
                    if (paymentTransaction.PaymentDate == null)
                    {
                        paymentTransaction.PaymentDate = DateTime.Now;
                        await PayProvider.SetTransaction(paymentTransaction);
                    }

                    if (MyCivisService.Enabled == true)
                    {
                        NavManager.NavigateTo("/Canteen/MyCivis/Service");
                    }
                    else
                    {
                        NavManager.NavigateTo("/Canteen/Service");
                    }

                    StateHasChanged();
                }
            }
            return true;
        }
    }
}