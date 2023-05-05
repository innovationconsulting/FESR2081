using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Components.Payments
{
    public partial class PaymentList
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IPAYService PayService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }

        [Parameter] public bool UserSelectionEnabled { get; set; } = false;
        [Parameter] public Guid? UserSelectionUserID { get; set; } = null;
        [Parameter] public List<Guid?>? FilterTransactionList { get; set; } = null;
        [Parameter] public Guid? FilterAUTH_Users_ID { get; set; } = null;
        [Parameter] public Guid? FilterAUTH_Municipality_ID { get; set; } = null;
        [Parameter] public EventCallback<Guid> OnTransactionCreated { get; set; }
        [Parameter] public bool ShowBollo { get; set; } = false;
        [Parameter] public bool AllowBolloRefundAll { get; set; } = false;
        [Parameter] public bool IsApplicationPaymentList { get; set; } = false;
        [Parameter] public int? DefaultPagoPaIdentifier { get; set; } = null;

        private List<PAY_Transaction>? Transactions { get; set; }
        private List<PAY_Type>? Types { get; set; }
        private PAY_Transaction? CurrentTransaction = null;
        private List<PAY_Transaction_Position>? CurrentTransactionPositions;
        private List<PAY_PagoPa_Identifier> PagoPaIdentifiers = new List<PAY_PagoPa_Identifier>();
        private int? SelectedPagoPaIdentifier = null;
        private bool WindowVisible { get; set; } = false;
        private bool _newTransaction = false;
        private List<IEditorTool> Tools { get; set; } =
        new List<IEditorTool>()
        {
            new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
            new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
            new UnorderedList(),
            new EditorButtonGroup(new CreateLink(), new Unlink())
        };
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font" };
        private bool loading = false;
        private bool IsBusy = true;
        private List<Guid?>? NewTransactions { get; set; } = new List<Guid?>();
        private List<PAY_Transaction_Position>? TransactionBolloList;
        private string? PaymentInvalidError;


        protected override async Task OnInitializedAsync()
        {
            Types = await PayProvider.GetTypeList();
            if (IsApplicationPaymentList)
            {
                PagoPaIdentifiers = await PayProvider.GetAllPagoPaApplicaitonsIdentifiers(SessionWrapper.AUTH_Municipality_ID);
            }
            if (DefaultPagoPaIdentifier != null && PagoPaIdentifiers.Any(e => e.ID == DefaultPagoPaIdentifier))
            {
                SelectedPagoPaIdentifier = DefaultPagoPaIdentifier;
            }
            else
            {
                SelectedPagoPaIdentifier = null;
            }
            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            IsBusy = true;
            StateHasChanged();

            if (DefaultPagoPaIdentifier != null && PagoPaIdentifiers.Any(e => e.ID == DefaultPagoPaIdentifier))
            {
                SelectedPagoPaIdentifier = DefaultPagoPaIdentifier;
            }
            else
            {
                SelectedPagoPaIdentifier = null;
            }
            
            await GetTransactions();
            await GetTransactionBollos();

            IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        public async Task<bool> GetTransactions()
        {
            if (!loading)
            {
                loading = true;

                if (FilterTransactionList != null && FilterTransactionList.Count() > 0)
                {
                    Transactions = await PayProvider.GetTransactionList(FilterTransactionList);
                }
                else if (FilterAUTH_Users_ID != null)
                {
                    Transactions = await PayProvider.GetTransactionList(FilterAUTH_Users_ID.Value);
                }
                else if (FilterAUTH_Municipality_ID != null)
                {
                    Transactions = await PayProvider.GetTransactionListByMunicipality(FilterAUTH_Municipality_ID.Value);
                }
                else
                {
                    Transactions = new List<PAY_Transaction>();
                }

                if (Transactions != null && NewTransactions != null)
                {
                    var newItemList = await PayProvider.GetTransactionList(NewTransactions);

                    if (newItemList != null)
                    {
                        var ItemsToAdd = newItemList.Where(x => !Transactions.Select(p => p.ID).Contains(x.ID)).ToList();

                        if (ItemsToAdd != null)
                        {
                            Transactions = Transactions.Union(ItemsToAdd).ToList();
                        }
                    }
                }

                loading = false;
            }

            return true;
        }
        public async Task<bool> GetTransactionBollos()
        {
            if (Transactions != null) 
            {
                TransactionBolloList = new List<PAY_Transaction_Position>();

                foreach(var trans in Transactions)
                {
                    TransactionBolloList.AddRange(trans.PAY_Transaction_Position.Where(p => p.IsBollo).ToList());
                }
            }

            return true;
        }
        public void AddTransaction()
        {
            _newTransaction = true;
            CurrentTransaction = new PAY_Transaction();
            
            CurrentTransaction.ID = Guid.NewGuid();
            CurrentTransaction.CreationDate = DateTime.Now;

            if (UserSelectionUserID != null)
            {
                CurrentTransaction.AUTH_Users_ID = UserSelectionUserID;
            }

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                CurrentTransaction.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
            }

            CurrentTransactionPositions = new List<PAY_Transaction_Position>();

            CurrentTransaction.PAY_Type_ID = Guid.Parse("b16d8119-e050-46c8-bb81-a1d08891f298");

            WindowVisible = true;
            StateHasChanged();
        }
        public async void EditTransaction(PAY_Transaction Item)
        {
            _newTransaction = false;
            CurrentTransaction = Item;
            CurrentTransactionPositions = await PayProvider.GetTransactionPositionList(Item.ID);

            WindowVisible = true;
            StateHasChanged();
        }
        public async void DeleteTransaction(PAY_Transaction Item)
        {
            if (Item != null)
            {
                await PayProvider.RemoveTransaction(Item.ID);
                await GetTransactions();
                StateHasChanged();
            }
        }
        private async Task<bool> SaveTransaction()
        {
            if (CurrentTransaction != null && CurrentTransactionPositions != null)
            {
                CurrentTransaction.TotalAmount = CurrentTransactionPositions.Sum(p => p.Amount);

                if(CurrentTransaction.TotalAmount == null || CurrentTransaction.TotalAmount == 0)
                {
                    PaymentInvalidError = TextProvider.Get("PAYMENT_TRANSACTION_VALIDATION_ERROR");
                    StateHasChanged();

                    return false;
                }

                PaymentInvalidError = null;

                IsBusy = true;
                StateHasChanged();
                WindowClose();

                var existingPositions = await PayProvider.GetTransactionPositionList(CurrentTransaction.ID);

                if (existingPositions != null)
                {
                    foreach (var p in existingPositions)
                    {
                        await PayProvider.RemoveTransactionPosition(p.ID);
                    }
                }

                await PayProvider.SetTransaction(CurrentTransaction);

                var ident = PagoPaIdentifiers.FirstOrDefault(e => e.ID == SelectedPagoPaIdentifier);
                foreach (var p in CurrentTransactionPositions)
                {
                    if (ident != null)
                    {
                        p.TipologiaServizio = ident.TipologiaServizio;
                        p.PagoPA_Identification = ident.PagoPA_Identifier;
                    }
                    await PayProvider.SetTransactionPosition(p);
                }

                if (NewTransactions != null)
                {
                    NewTransactions.Add(CurrentTransaction.ID);
                }

                await GetTransactions();

                await OnTransactionCreated.InvokeAsync(CurrentTransaction.ID);

                CurrentTransaction = null;
                StateHasChanged();
            }
            else
            {
                CurrentTransaction = null;
                WindowClose();
            }

            return true;
        }
        private void WindowClose()
        {
            _newTransaction = false;
            WindowVisible = false;
            StateHasChanged();
        }
        private void UserSelected(Guid? AUTH_Users_ID)
        {
            if (CurrentTransaction != null)
            {
                CurrentTransaction.AUTH_Users_ID = AUTH_Users_ID;
            }
        }
        private async void MoveUpPosition(PAY_Transaction_Position item)
        {
            if (CurrentTransactionPositions != null && CurrentTransactionPositions.Count() > 0)
            {
                var newPos = CurrentTransactionPositions.FirstOrDefault(p => p.SortOrder == item.SortOrder - 1);

                if (newPos != null)
                {
                    item.SortOrder = item.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                }
            }

            StateHasChanged();
        }
        private async void MoveDownPosition(PAY_Transaction_Position item)
        {
            if (CurrentTransactionPositions != null && CurrentTransactionPositions.Count() > 0)
            {
                var newPos = CurrentTransactionPositions.FirstOrDefault(p => p.SortOrder == item.SortOrder + 1);

                if (newPos != null)
                {
                    item.SortOrder = item.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                }
            }

            StateHasChanged();
        }
        public async Task DeletePositionHandler(GridCommandEventArgs args)
        {
            PAY_Transaction_Position item = (PAY_Transaction_Position)args.Item;

            if (CurrentTransactionPositions != null)
            {
                CurrentTransactionPositions.Remove(item);
            }

            StateHasChanged();
        }
        public void CreatePositionHandler(GridCommandEventArgs args)
        {
            PAY_Transaction_Position item = (PAY_Transaction_Position)args.Item;

            if (CurrentTransactionPositions != null && CurrentTransaction != null)
            {
                item.ID = Guid.NewGuid();
                item.PAY_Transaction_ID = CurrentTransaction.ID;
                item.SortOrder = CurrentTransactionPositions.Count() + 1;

                CurrentTransactionPositions.Add(item);
            }

            StateHasChanged();
        }
        public void UpdatePositionHandler(GridCommandEventArgs args)
        {
            PAY_Transaction_Position item = (PAY_Transaction_Position)args.Item;

            if (CurrentTransactionPositions != null)
            {
                var listITem = CurrentTransactionPositions.FirstOrDefault(p => p.ID == item.ID);

                if (listITem != null)
                {
                    listITem.Amount = item.Amount;
                    listITem.Description = item.Description;
                }
            }

            StateHasChanged();
        }
        public async void RefundBollo(PAY_Transaction_Position item)
        {
            IsBusy = true;
            StateHasChanged();

            if(Transactions != null && item != null && item.Amount != null)
            {
                var trans = Transactions.FirstOrDefault(p => p.ID == item.PAY_Transaction_ID);

                if (trans != null && trans.STRIPE_Session_ID != null && trans.AUTH_Users_ID != null)
                {
                    var userAnagrafic = await AuthProvider.GetAnagraficByUserID(trans.AUTH_Users_ID.Value);

                    if (userAnagrafic != null)
                    {
                        long amountFormatted = long.Parse(item.Amount.Value.ToString().Replace(",", "").Replace(".", ""));

                        var result = await PayService.CreateRefund(trans.STRIPE_Session_ID, userAnagrafic, amountFormatted);

                        if(result != null)
                        {
                            item.RefundDate = DateTime.Now;

                            await PayProvider.SetTransactionPosition(item);
                            await GetTransactionBollos();
                        }
                    }
                }
            }

            IsBusy = false;
            StateHasChanged();
        }
    }
}
