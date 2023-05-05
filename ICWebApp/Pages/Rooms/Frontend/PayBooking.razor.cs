using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Components.Payments;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Rooms;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Rooms.Frontend
{
    public partial class PayBooking
    {
        [Inject] ID3Helper D3Helper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [Parameter] public string BookingGroupID { get; set; }

        private bool ShowPayProvider { get; set; } = false;
        private List<Guid?> PAY_Transaction_IDs = new List<Guid?>();
        private ROOM_BookingGroup? BookingGroup;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;

            SessionWrapper.PageTitle = TextProvider.Get("ROOM_PAYMENT");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_ROOMBOOKING", null, null);
            CrumbService.AddBreadCrumb(NavManager.Uri, "ROOM_PAYMENT", null, null, true);

            StateHasChanged();
             
            BookingGroup = await RoomProvider.GetBookingGroup(Guid.Parse(BookingGroupID)); 

            if(BookingGroup == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();

                return;
            }

            var payTransactions = await RoomProvider.GetBookingTransactionList(BookingGroup.ID);

            if(payTransactions == null || payTransactions.Count() == 0)
            {
                PAY_Transaction trans = new PAY_Transaction();

                trans.ID = Guid.NewGuid();
                trans.AUTH_Users_ID = BookingGroup.AUTH_User_ID;
                trans.AUTH_Municipality_ID = BookingGroup.AUTH_MunicipalityID;
                trans.CreationDate = DateTime.Now;
                trans.Description = TextProvider.Get("FRONTEND_BOOKING_PAYMENT_TITLE").Replace("{0}", BookingGroup.FirstName + " " + BookingGroup.LastName);
                trans.PAY_Type_ID = Guid.Parse("b16d8119-e050-46c8-bb81-a1d08891f298"); //STANDARD

                decimal TotalAmount = 0;

                await PayProvider.SetTransaction(trans);

                var bookings = await RoomProvider.GetBookingsByGroupID(BookingGroup.ID);

                foreach (var booking in bookings.Where(p => p.ROOM_Room_ID != null).OrderBy(p => p.ROOM_Room_ID).ThenBy(p => p.StartDate))
                {
                    var room = await RoomProvider.GetVRoom(booking.ROOM_Room_ID.Value);

                    if (room != null && room.HasDirectPay == true && booking.StartDate != null && booking.EndDate != null && booking.ROOM_Room_ID != null) 
                    {
                        PAY_Transaction_Position transPos = new PAY_Transaction_Position();

                        transPos.ID = Guid.NewGuid();
                        transPos.PAY_Transaction_ID = trans.ID;

                        if (booking.StartDate.Value.DayOfYear == booking.EndDate.Value.DayOfYear && booking.StartDate.Value.Year == booking.EndDate.Value.Year)
                        {
                            transPos.Description = room.Name + "   " + booking.StartDate.Value.ToString("dd.MM.yyyy HH:mm") + " - " + booking.EndDate.Value.ToString("HH:mm");
                        }
                        else
                        {
                            transPos.Description = room.Name + "   " + booking.StartDate.Value.ToString("dd.MM.yyyy HH:mm") + " - " + booking.EndDate.Value.ToString("dd.MM.yyyy HH:mm");
                        }

                        var price = await BookingService.GetRoomCost(booking.ROOM_Room_ID.Value, SessionWrapper.CurrentUser.AUTH_Company_Type_ID, booking.StartDate.Value, booking.EndDate.Value);

                        if (price > 0) 
                        {
                            transPos.Amount = price;
                        }

                        var ident = await PayProvider.GetPagoPaRoomsIdentifier();

                        if (ident != null)
                        {
                            transPos.PagoPA_Identification = ident.PagoPA_Identifier;
                            transPos.TipologiaServizio = ident.TipologiaServizio;
                        }

                        if (transPos.Amount != null)
                        {
                            TotalAmount += transPos.Amount.Value;
                        }

                        if (transPos.Amount != null && transPos.Amount > 0)
                        {
                            await PayProvider.SetTransactionPosition(transPos);
                        }
                    }
                }

                var options = await RoomProvider.GetBookingOptions(BookingGroup.ID);

                foreach (var option in options.Where(p => p.ROOM_Room_ID != null).OrderBy(p => p.CreationDate).ToList())
                {
                    if (option.ROOM_Room_ID != null && option.ROOM_RoomOption_ID != null)
                    {
                        var room = await RoomProvider.GetVRoom(option.ROOM_Room_ID.Value);
                        var dbOption = await RoomProvider.GetVRoomOption(option.ROOM_RoomOption_ID.Value);

                        if (room != null && room.HasDirectPay == true && dbOption != null && !string.IsNullOrEmpty(option.Price))
                        {
                            var price = decimal.Parse(option.Price);

                            PAY_Transaction_Position transPos = new PAY_Transaction_Position();

                            transPos.ID = Guid.NewGuid();
                            transPos.PAY_Transaction_ID = trans.ID;
                            transPos.Description = room.Name + "   " + dbOption.Name;

                            if (price > 0)
                            {
                                transPos.Amount = price;
                            }

                            var ident = await PayProvider.GetPagoPaRoomsIdentifier();
                            if (ident != null)
                            {
                                transPos.PagoPA_Identification = ident.PagoPA_Identifier;
                                transPos.TipologiaServizio = ident.TipologiaServizio;
                            }
                            

                            if (transPos.Amount != null)
                            {
                                TotalAmount += transPos.Amount.Value;
                            }

                            if (transPos.Amount != null && transPos.Amount > 0)
                            {
                                await PayProvider.SetTransactionPosition(transPos);
                            }
                        }
                    }
                }

                trans.TotalAmount = TotalAmount;

                await PayProvider.SetTransaction(trans);

                ROOM_BookingTransactions NewTransaction = new ROOM_BookingTransactions();

                NewTransaction.ID = Guid.NewGuid();
                NewTransaction.PAY_Transaction_ID = trans.ID;
                NewTransaction.ROOM_BookingGroupID = BookingGroup.ID;

                await RoomProvider.SetBookingTransaction(NewTransaction);

                payTransactions = new List<ROOM_BookingTransactions>() { NewTransaction };
            }

            bool Payed = false;

            if (payTransactions != null && payTransactions.Count() > 0)
            {
                foreach (var appTrans in payTransactions)
                {
                    if (appTrans.PAY_Transaction_ID != null)
                    {
                        var payTrans = await PayProvider.GetTransaction(appTrans.PAY_Transaction_ID.Value);

                        if (payTrans != null && payTrans.PaymentDate != null)
                        {
                            Payed = true;
                        }
                        else
                        {
                            Payed = false;
                            break;
                        }
                    }
                }
            }

            if(BookingGroup.PayedAt != null)
            {
                Payed = true;
            }

            if (Payed)
            {
                await CompletePayment();

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
                return;
            }

            if (payTransactions == null || payTransactions.Count() == 0)
            {
                await CompletePayment();
            }
            else
            {
                PAY_Transaction_IDs = payTransactions.Select(p => p.PAY_Transaction_ID).ToList();
                ShowPayProvider = true;
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> CompletePayment()
        {
            if (BookingGroup != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                BookingGroup.PayedAt = DateTime.Now;

                bool ToSign = false;

                var bookings = await RoomProvider.GetBookingsByGroupID(BookingGroup.ID);

                foreach (var roomID in bookings.Select(p => p.ROOM_Room_ID).Distinct())
                {
                    if (roomID != null)
                    {
                        var room = await RoomProvider.GetRoom(roomID.Value);

                        if (room != null && room.HasSigning == true)
                        {
                            ToSign = true;
                        }
                    }
                }

                if (ToSign == true)
                {
                    BookingGroup.ROOM_BookingStatus_ID = ROOMStatus.ToSign;

                    await RoomProvider.CreateBookingStatusLog(BookingGroup, ROOMStatus.ToSign);
                    await RoomProvider.SetBookingGroup(BookingGroup);

                    NavManager.NavigateTo("/Room/Sign/" + BookingGroupID);
                    StateHasChanged();
                }
                else
                {
                    await RoomProvider.SetBookingComitted(BookingGroup, NavManager.BaseUri);

                    //D3! rooms ok
                    Task.Run(async () => await D3Helper.ProtocolRoomBooking(BookingGroup)).ConfigureAwait(false);
                    /************************/

                    NavManager.NavigateTo("/Room/Comitted/" + BookingGroup.ID);
                    StateHasChanged();
                }              
            }

            return true;
        }
    }
}