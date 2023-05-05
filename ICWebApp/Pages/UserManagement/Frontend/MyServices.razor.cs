using DocumentFormat.OpenXml.InkML;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Rooms;
using ICWebApp.Domain.Models.User;
using ICWebApp.Pages.Organziation.Backend;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Telerik.Blazor;

namespace ICWebApp.Pages.UserManagement.Frontend
{
    public partial class MyServices
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IBookingService BookingService { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] IMessageService Messageservice { get; set; }
        [Inject] IMSGProvider MSGProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<ServiceDataItem> ApplicationList = new List<ServiceDataItem>();
        private List<ServiceDataItem> ManteinanceList = new List<ServiceDataItem>();
        private List<ServiceDataItem> BookingList = new List<ServiceDataItem>();
        private List<ServiceDataItem> OrganisationList = new List<ServiceDataItem>();
        private List<ServiceDataItem> MessageList = new List<ServiceDataItem>();
        private List<MSG_Message> UnreadMessages = new List<MSG_Message>();
        private bool ShowBankDataWindow = false;
        private ROOM_BookingGroup? CurrentBooking;
        private string CurrentTab = "services";

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper == null || SessionWrapper.CurrentUser == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_MY_SERVICES", null, null, true);

            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_MY_SERVICES");


            await GetApplicationData();
            await GetManteinanceData();
            await GetBookingData();
            await GetOrganisationData();
            await GetMessages();

            SessionWrapper.ShowTitleSepparation = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            AddAnchors();

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task GetApplicationData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                ApplicationList.Clear();

                var data = await FormApplicationProvider.GetApplications(SessionWrapper.AUTH_Municipality_ID.Value, GetCurrentUserID());

                foreach (var d in data) 
                {
                    ApplicationList.Add(new ServiceDataItem()
                    {
                        CreationDate = d.SubmitAt ?? d.CreatedAt,
                        File_FileInfo_ID = d.FILE_Fileinfo_ID,
                        LastChangeDate = d.StatusChangeDate,
                        ProtocollNumber = d.ProgressivNumber,
                        StatusIcon = d.StatusIcon,
                        StatusText = d.Status,
                        Title = d.FormName,
                        DetailAction = (() => GoToApplication(d.ID))
                    }); 
                }
            }

            return;
        }
        private async Task GetManteinanceData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                ManteinanceList.Clear();

                var data = await FormApplicationProvider.GetMantainances(SessionWrapper.AUTH_Municipality_ID.Value, GetCurrentUserID());

                foreach (var d in data)
                {
                    ManteinanceList.Add(new ServiceDataItem()
                    {
                        CreationDate = d.SubmitAt ?? d.CreatedAt,
                        File_FileInfo_ID = d.FILE_Fileinfo_ID,
                        LastChangeDate = d.StatusChangeDate,
                        ProtocollNumber = d.ProgressivNumber,
                        StatusIcon = d.StatusIcon,
                        StatusText = d.Status,
                        Title = d.Mantainance_Title,
                        Description = d.FormName,
                        DetailAction = (() => GoToApplication(d.ID))
                    });
                }
            }

            return;
        }
        private async Task GetBookingData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                BookingList.Clear();

                await RoomProvider.VerifyBookingsState(SessionWrapper.CurrentUser.ID, NavManager.BaseUri);

                var Bookings = await RoomProvider.GetBookingGroupByUser(SessionWrapper.CurrentUser.ID);
                var BackendBookings = await RoomProvider.GetBookingGroupBackendByUser(SessionWrapper.CurrentUser.ID);

                if (SessionWrapper.CurrentSubstituteUser != null)
                {
                    Bookings = await RoomProvider.GetBookingGroupByUser(SessionWrapper.CurrentSubstituteUser.ID);
                    BackendBookings = await RoomProvider.GetBookingGroupBackendByUser(SessionWrapper.CurrentSubstituteUser.ID);
                }

                foreach (var booking in Bookings)
                {
                    var newItem = new ServiceDataItem()
                    {
                        CreationDate = booking.SubmitAt ?? booking.CreationDate,
                        Title = booking.Title,
                        File_FileInfo_ID = booking.FILE_FileInfo_ID,
                        ProtocollNumber = booking.ProgressivNumberCombined,
                        StatusIcon = booking.IconCSS,
                        StatusText = booking.Status,
                        Days = booking.Days,
                        Rooms = booking.Rooms
                    };

                    if (booking.ROOM_BookingStatus_ID == ROOMStatus.ToPay)
                    {
                        newItem.DetailAction = (() =>  Booking_GoToPayment(booking.ID));
                    }
                    else if (booking.ROOM_BookingStatus_ID == ROOMStatus.ToSign)
                    {
                        newItem.DetailAction = (() => Booking_GoToSigning(booking.ID));
                    }
                    else if (booking.ROOM_BookingStatus_ID == ROOMStatus.Cancelled || booking.ROOM_BookingStatus_ID == ROOMStatus.CancelledByCitizen)
                    {
                        newItem.StatusCSS = "booking-canceled";
                    }
                    else
                    {
                        newItem.DetailAction = (() => Booking_ShowDetails(booking.ID));
                    }

                    if((booking.ROOM_BookingStatus_ID == ROOMStatus.Accepted || 
                        booking.ROOM_BookingStatus_ID == ROOMStatus.AcceptedWithChanges || 
                        booking.ROOM_BookingStatus_ID == ROOMStatus.Comitted) &&
                        booking.StartDate > DateTime.Now)
                    {
                        newItem.CancelAction = (() => CancelBooking(booking));
                    }


                    BookingList.Add(newItem);
                }

                foreach (var booking in BackendBookings)
                {
                    var newItem = new ServiceDataItem()
                    {
                        CreationDate = booking.CreatedAt,
                        Title = booking.Title,
                        StatusIcon = booking.IconCSS,
                        StatusText = booking.Status,
                        Days = booking.Days,
                        Rooms = booking.Rooms,
                        Description = TextProvider.Get("FRONTEND_BOOKING_BACKEND_TEXT")
                    };

                    BookingList.Add(newItem);
                }
            }

            return;
        }
        private async Task GetOrganisationData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                OrganisationList.Clear();

                var Items = await OrgProvider.GetRequestList(SessionWrapper.CurrentUser.ID);

                if (Items != null)
                {

                    foreach (var item in Items)
                    {
                        var newItem = new ServiceDataItem()
                        {
                            CreationDate = item.SubmitAt ?? item.CreationDate,
                            Title = item.Firstname + " " + item.Lastname,
                            Description = item.CompanyType,
                            File_FileInfo_ID = item.FILE_Fileinfo_ID,
                            ProtocollNumber = item.ProgressivNumber,
                            StatusIcon = item.StatusIcon,
                            StatusText = item.Status,
                            DetailAction = (() => Organization_GoToApplication(item.ID)),
                            DetailTextCode =  await Organization_ActionButtonText(item.ID)
                        };

                        OrganisationList.Add(newItem);
                    }
                }
            }

            return;
        }
        private async Task GetMessages()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                MessageList.Clear();
                UnreadMessages.Clear();
                var Messages = await Messageservice.GetMessages(SessionWrapper.CurrentUser.ID, null);
            
                if (Messages != null)
                {
                    foreach (var item in Messages)
                    {
                        var messageIsRead = item.FirstReadDate != null;
                        var newItem = new ServiceDataItem()
                        {
                            CreationDate = item.CreationDate,
                            Title = item.Subject,
                            Description = item.Messagetext,
                            DetailAction = (() => GoToMessage(item)),
                            MessageIsRead = messageIsRead,
                        };
                        if (!messageIsRead)
                        {
                            newItem.ReadMessage = () => { 
                                Messageservice.SetMessageRead(item);
                                UnreadMessages.Remove(item);
                                StateHasChanged();
                            };
                            UnreadMessages.Add(item);
                        }

                        MessageList.Add(newItem);
                    }
                }
            }

            return;
        }

        private async Task MarkMessagesRead()
        {
            if (UnreadMessages.Count > 0)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                await Messageservice.SetMessagesRead(UnreadMessages);
                await GetMessages();
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            } 
            
        }
        private Guid GetCurrentUserID()
        {
            Guid CurrentUserID = SessionWrapper.CurrentUser.ID;

            if (SessionWrapper.CurrentSubstituteUser != null)
            {
                CurrentUserID = SessionWrapper.CurrentSubstituteUser.ID;
            }

            return CurrentUserID;
        }
        private async Task GoToApplication(Guid FORM_Application_ID)
        {
            var application = await FormApplicationProvider.GetApplication(FORM_Application_ID);

            if (application != null && application.FORM_Definition_ID != null)
            {
                var definition = await FormDefinitionProvider.GetDefinition(application.FORM_Definition_ID.Value);

                if (definition != null && !definition.Enabled)
                {
                    return;
                }
            }

            if (application != null)
            {
                if (application.FORM_Application_Status_ID == FORMStatus.Incomplete)
                {
                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo("/Form/Application/" + application.FORM_Definition_ID + "/" + application.ID);
                    StateHasChanged();
                }
                else if (application.SubmitAt == null && application.FORM_Application_Status_ID == FORMStatus.InSigning)
                {
                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo("/Form/Application/Preview/" + application.ID);
                    StateHasChanged();                                            
                }
                else if (application.SubmitAt == null && (application.FORM_Application_Status_ID == FORMStatus.ToSign || application.FORM_Application_Status_ID == FORMStatus.ToPay
                         || application.FORM_Application_Status_ID == FORMStatus.PayProcessing))
                {
                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo("/Form/Application/Preview/" + application.ID);
                    StateHasChanged();
                }
                else if (application != null)
                {
                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo("/Form/Application/UserDetails/" + application.ID);
                    StateHasChanged();
                }
            }

        }
        private void Booking_GoToPayment(Guid Booking_ID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Room/Payment/" + Booking_ID);
            StateHasChanged();
        }
        private void Booking_GoToSigning(Guid Booking_ID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Room/Sign/" + Booking_ID);
            StateHasChanged();
        }
        private void Booking_ShowDetails(Guid Booking_ID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Room/User/" + Booking_ID);
            StateHasChanged();
        }
        private async void CancelBooking(V_ROOM_Booking_Group Item)
        {
            if ((Item.ROOM_BookingStatus_ID == ROOMStatus.Accepted || Item.ROOM_BookingStatus_ID == ROOMStatus.AcceptedWithChanges || Item.ROOM_BookingStatus_ID == ROOMStatus.Comitted) && Item.StartDate > DateTime.Now)
            {
                var dbBooking = await RoomProvider.GetBookingGroup(Item.ID);

                if (dbBooking == null)
                {
                    return;
                }

                var paymentTransactions = await RoomProvider.GetBookingTransactionList(dbBooking.ID);

                if (paymentTransactions != null && paymentTransactions.Count() > 0)
                {
                    CurrentBooking = dbBooking;
                    ShowBankDataWindow = true;
                    StateHasChanged();

                    return;
                }


                if (!await Dialogs.ConfirmAsync(TextProvider.Get("ROOM_BOOKING_CANCEL_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.SetBookingCanceled(dbBooking.ID, NavManager.BaseUri);

                if (SessionWrapper.AUTH_Municipality_ID != null)
                {
                    var dbBookingGroup = await RoomProvider.GetBookingGroup(dbBooking.ID);

                    if (dbBookingGroup != null && dbBookingGroup.ROOM_Booking_Type_ID != ROOMBookingType.Blocked)
                    {
                        BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, dbBookingGroup.ID, dbBookingGroup.Title, true);
                    }
                }

                await GetBookingData();
                StateHasChanged();
            }
        }
        private async void SaveCancelBooking()
        {
            if (CurrentBooking != null)
            {
                ShowBankDataWindow = false;
                StateHasChanged();

                if (!await Dialogs.ConfirmAsync(TextProvider.Get("ROOM_BOOKING_CANCEL_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.SetBookingCanceled(CurrentBooking.ID, NavManager.BaseUri);

                if (SessionWrapper.AUTH_Municipality_ID != null)
                {
                    var dbBookingGroup = await RoomProvider.GetBookingGroup(CurrentBooking.ID);

                    if (dbBookingGroup != null && dbBookingGroup.ROOM_Booking_Type_ID != ROOMBookingType.Blocked)
                    {
                        BookingService.SendMessagesToContacts(SessionWrapper.AUTH_Municipality_ID.Value, dbBookingGroup.ID, dbBookingGroup.Title, true);
                    }
                }

                await GetBookingData();
                StateHasChanged();
            }
        }
        private void HideCancelBooking()
        {
            CurrentBooking = null;
            ShowBankDataWindow = false;
            StateHasChanged();
        }
        private async void Organization_GoToApplication(Guid ORG_Request_ID)
        {
            var application = await OrgProvider.GetRequest(ORG_Request_ID);

            if (application != null && application.ORG_Request_Status_ID == Guid.Parse("75b8b15c-9fda-4748-a7a2-1f2d409a521e"))  //TO SIGN
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Application/Sign/" + application.ID);
                StateHasChanged();
                return;
            }
            else if (application != null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Detail/" + application.ID);
                StateHasChanged();
            }            
        }
        private async Task<string?> Organization_ActionButtonText(Guid ORG_Request_ID)
        {
            var application = await OrgProvider.GetRequest(ORG_Request_ID);

            if (application != null && application.ORG_Request_Status_ID == Guid.Parse("75b8b15c-9fda-4748-a7a2-1f2d409a521e"))  //TO SIGN
            {
                return "BUTTON_SIGN";
            }

            return null;
        }
        private void ClearAnchors(string TabName)
        {
            if(TabName != CurrentTab)
            {
                CurrentTab = TabName;

                AnchorService.ClearAnchors();
                AddAnchors();

                StateHasChanged();
            }
        }
        private void AddAnchors()
        {
            if (CurrentTab == "services")
            {
                AnchorService.AddAnchor(TextProvider.Get("FRONTEND_SERVICES_APPLICATIONS"), TextProvider.Get("FRONTEND_SERVICES_APPLICATIONS"), 1);
                AnchorService.AddAnchor(TextProvider.Get("FRONTEND_SERVICES_MANTEINANCE"), TextProvider.Get("FRONTEND_SERVICES_MANTEINANCE"), 2);
                AnchorService.AddAnchor(TextProvider.Get("FRONTEND_SERVICES_BOOKINGS"), TextProvider.Get("FRONTEND_SERVICES_BOOKINGS"), 3);
                AnchorService.AddAnchor(TextProvider.Get("FRONTEND_SERVICES_ORGANISATIONS"), TextProvider.Get("FRONTEND_SERVICES_ORGANISATIONS"), 4);
            }
            else if (CurrentTab == "messages")
            {
                AnchorService.AddAnchor(TextProvider.Get("FRONTEND_SERVICES_MESSAGES"), TextProvider.Get("FRONTEND_SERVICES_MESSAGES"), 1);
            }
        }
        private void GoToMessage(MSG_Message? Message)
        {
            if (Message != null)
            {
                Message.FirstReadDate = DateTime.Now;

                MSGProvider.SetMessage(Message);

                if (Message.Link != null)
                {
                    if (NavManager.BaseUri.Contains("localhost"))
                    {
                        Message.Link = Message.Link.Replace("https://test.comunix.bz.it/", "https://localhost:7149/");
                    }

                    if (NavManager.Uri != Message.Link)
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo(Message.Link);
                    }

                    StateHasChanged();
                }
                else
                {
                    if (!NavManager.Uri.Contains("/Backend/MessageCommunications"))
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo("/Backend/MessageCommunications");
                    }
                    StateHasChanged();
                }
            }
        }
    }
}
