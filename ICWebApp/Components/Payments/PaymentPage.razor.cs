using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Payments
{
    public partial class PaymentPage
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] ICONFProvider ConfProvider{ get; set; }
        [Inject] IPAYService PayService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Parameter] public List<Guid?> Transactions { get; set; }
        [Parameter] public EventCallback<List<Guid>> OnPaymentComplete { get; set; }
        [Parameter] public EventCallback OnBackToPrevious { get; set; }
        [Parameter] public EventCallback<bool> OnPaymentClicked { get; set; }
        [Parameter] public bool ShowBackButton { get; set; } = false;
        [Parameter] public string ReturnUrl { get; set; }
        [Parameter] public string SourceCode { get; set; }
        private List<PAY_Transaction>? PayTransactions { get; set; }
        private bool IsPagoPA = false;
        private AUTH_Users_Anagrafic? Anagrafic = null;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageSubTitle = TextProvider.Get("PAY_DETAIL_SUMMARY_TITLE");

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                Anagrafic = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentUser.ID);
            }

            PayTransactions = await GetTransactions();

            IsPagoPA = true;            

            if (PayTransactions != null)
            {
                string PagoPaNummero = DateTime.Now.ToString("yyyyMMddhhmmssFFFFFF");
                Guid FamilyID = Guid.NewGuid();

                if(PayTransactions.Where(p => p.Family_ID != null && p.Family_ID != Guid.Empty).FirstOrDefault() != null &&
                   PayTransactions.Where(p => p.Family_ID != null && p.Family_ID != Guid.Empty).FirstOrDefault().Family_ID != null)
                {
                    FamilyID = PayTransactions.Where(p => p.Family_ID != null && p.Family_ID != Guid.Empty).FirstOrDefault().Family_ID.Value;
                }

                foreach (var trans in PayTransactions)
                {
                    if (trans.Family_ID == null || trans.Family_ID == Guid.Empty)
                    {
                        trans.Family_ID = FamilyID;
                    }

                    trans.PagoPANummero = PagoPaNummero;

                    if (string.IsNullOrEmpty(SourceCode))
                    {
                        trans.ComunixSource = SourceCode;
                    }
                    else
                    {
                        trans.ComunixSource = "DEFAULT";
                    }


                    await PayProvider.SetTransaction(trans);
                }                
            }

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<PAY_Transaction>?> GetTransactions()
        {
            if(Transactions != null && Transactions.Count > 0)
            {
                return await PayProvider.GetTransactionList(Transactions);
            }

            return null;
        }
        private async Task<bool> OnPayClicked()
        {                       
            if (PayTransactions != null && PayTransactions.Count() > 0 && SessionWrapper.CurrentUser != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await OnPaymentClicked.InvokeAsync();

                string locReturnUrl = NavManager.BaseUri + "PagoPA/Success?Family_ID=" + PayTransactions.FirstOrDefault().Family_ID.Value.ToString() + "&ReturnUrl=" + Uri.EscapeDataString(ReturnUrl);
                string CancelUrl = NavManager.Uri;

                var Positions = new List<PAY_Transaction_Position>();

                foreach (var trans in PayTransactions)
                {
                    Positions.AddRange(trans.PAY_Transaction_Position);
                }

                if (PayTransactions.FirstOrDefault() != null && PayTransactions.FirstOrDefault().PagoPANummero != null)
                {
                    if(PayTransactions.FirstOrDefault().TotalAmount == 0)
                    {
                        OnPaymentComplete.InvokeAsync();
                        return true;
                    }

                    var Configuration = await ConfProvider.GetPagoPAConfiguration(null);

                    if (Configuration != null)
                    {
                        var redirectUrl = await PayService.PAGOPA_Create(Configuration,PayTransactions.FirstOrDefault().Family_ID.Value.ToString(), Positions, PayTransactions.FirstOrDefault().PagoPANummero, locReturnUrl, CancelUrl);

                        if (redirectUrl != null)
                        {
                            NavManager.NavigateTo(redirectUrl, true);
                            StateHasChanged();

                            return true;
                        }
                        else
                        {
                            var log = new PAY_PagoPA_Log();

                            log.ID = Guid.NewGuid();
                            log.CreationDate = DateTime.Now;
                            log.XMLData = "Error On Call";
                            log.CallType = "Redirect URL";
                            log.Incoming = false;

                            await PayProvider.SetPagoPALog(log);
                        }
                    }

                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            return true;
        }
        private void BackToPrevious()
        {
            OnBackToPrevious.InvokeAsync();
        }
    }
}
