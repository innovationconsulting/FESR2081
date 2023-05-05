using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;
using Telerik.Reporting.Processing;
using Telerik.Reporting;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Sessionless;
using ICWebApp.Domain.DBModels;
using ICWebApp.Application.Interface.Sessionless;
using System.Security.Cryptography;
using System.Globalization;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Wordprocessing;
using ICWebApp.Application.Interface.Helper;

namespace ICWebApp.Pages.Rooms.Frontend
{
    public partial class SignBooking
    {
        [Inject] ID3Helper D3Helper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }

        [Parameter] public string BookingGroupID { get; set; }

        private Guid? File_FileInfoID;
        private ROOM_BookingGroup? BookingGroup;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            SessionWrapper.PageTitle = TextProvider.Get("FRONTEND_BOOKING_SINGING");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_ROOMBOOKING", null, null);
            CrumbService.AddBreadCrumb(NavManager.Uri, "FRONTEND_BOOKING_SINGING", null, null, true);

            BookingGroup = await RoomProvider.GetBookingGroup(Guid.Parse(BookingGroupID));

            if(BookingGroup == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();
                return;
            }

            if (BookingGroup != null)
            {
                bool localhost = false;

                if (NavManager.BaseUri.Contains("localhost"))
                {
                    localhost = true;
                }

                var file = await RoomProvider.GetDocument(BookingGroup.ID, LangProvider.GetCurrentLanguageID(), localhost);

                if (BookingGroup != null && file != null)
                {
                    BookingGroup.FILE_FileInfo_ID = file.ID;
                    File_FileInfoID = file.ID;

                    await RoomProvider.SetBookingGroup(BookingGroup);
                }
            }
            else
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();
                return;
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void DocumentSigned()
        {
            if (BookingGroup != null)
            {
                await RoomProvider.SetBookingComitted(BookingGroup, NavManager.BaseUri);

                //D3! rooms ok
                Task.Run(async () => await D3Helper.ProtocolRoomBooking(BookingGroup)).ConfigureAwait(false);
                /************************/

                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Room/Comitted/" + BookingGroup.ID);
                StateHasChanged();
            }
        }
    }
}
