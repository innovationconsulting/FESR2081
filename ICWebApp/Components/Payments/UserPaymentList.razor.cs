using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Components.Payments
{
    public partial class UserPaymentList
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        [Parameter] public List<Guid?>? FilterTransactionList { get; set; } = null;

        private List<PAY_Transaction>? Transactions { get; set; }
        private List<PAY_Type>? Types { get; set; }
        private bool IsBusy = true;


        protected override async Task OnInitializedAsync()
        {
            Types = await PayProvider.GetTypeList();

            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            IsBusy = true;
            StateHasChanged();

            await GetTransactions();

            IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        public async Task<bool> GetTransactions()
        {
            if (FilterTransactionList != null && FilterTransactionList.Count() > 0)
            {
                Transactions = await PayProvider.GetTransactionList(FilterTransactionList);
            }

            return true;
        }
        private void GoToPaymentPage(PAY_Transaction Trans)
        {
            if(Trans != null && Trans.PaymentDate == null)
            {
                NavManager.NavigateTo("/Payment/" + Trans.ID.ToString() + "/" + Uri.EscapeDataString("/" + NavManager.Uri.Replace(NavManager.BaseUri, "")));
                StateHasChanged();
                return;
            }
        }
    }
}
