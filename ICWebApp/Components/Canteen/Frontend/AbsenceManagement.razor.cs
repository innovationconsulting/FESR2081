using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class AbsenceManagement
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Parameter] public string SubscriberID { get; set; }

        private List<CANTEEN_Subscriber_Movements> LatestMovements = new List<CANTEEN_Subscriber_Movements>();
        private List<CANTEEN_Subscriber> Subscribers = new List<CANTEEN_Subscriber>();
        private Guid? CurrentTab;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("CANTEEN_DASHBOARD_ABSENCEMANAGEMENT");

            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Absence", "CANTEEN_DASHBOARD_ABSENCEMANAGEMENT", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen/Service", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/Absence", "CANTEEN_DASHBOARD_ABSENCEMANAGEMENT", null, null);
            }

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                var subs = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);
                var currentSchoolyear = await CanteenProvider.GetSchoolsyearsCurrent(SessionWrapper.AUTH_Municipality_ID.Value);

                if (currentSchoolyear != null && currentSchoolyear.Count() > 0)
                {
                    Subscribers = subs.Where(p => p.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted && p.Archived == null && p.SchoolyearID == currentSchoolyear.FirstOrDefault().id).ToList();
                }

                await LoadData();

                if (Subscribers.FirstOrDefault() != null)
                {
                    CurrentTab = Subscribers.FirstOrDefault().ID;
                    AddAnchors(CurrentTab.Value);
                }
            }

            if(Subscribers.Count() > 1)
            {
                SessionWrapper.ShowTitleSepparation = false;
            }
            else
            {
                SessionWrapper.ShowTitleSepparation = true;
            }

            BusyIndicatorService.IsBusy = false;
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
                    CrumbService.AddBreadCrumb("/Canteen/MyCivis", "MAINMENU_CANTEEN", null, null);
                }
                else
                {
                    CrumbService.AddBreadCrumb("/Canteen", "MAINMENU_CANTEEN", null, null);
                }

                StateHasChanged();
            }
        }
        private async Task<bool> LoadData()
        {
            LatestMovements.Clear();

            foreach (var sub in Subscribers)
            {
                await CanteenProvider.CreateSubscriber_Movements(sub.ID);

                LatestMovements.AddRange(await GetMovementsBySubcriberID(sub.ID));
            }

            return true;
        }
        private async Task<List<CANTEEN_Subscriber_Movements>> GetMovementsBySubcriberID(Guid ID)
        {
            var data = await CanteenProvider.GetSubscriberMovementsBySubscriber(ID);
            data = data.Where(a => a.Date >= DateTime.Today && a.Fee < 0).OrderBy(o => o.Date).ToList();

            if (DateTime.Now.Hour >= 10)
            {
                data = data.Where(a => a.Date > DateTime.Today).ToList();
            }

            return data;
        }
        private async Task<bool> DailyCancellation(CANTEEN_Subscriber_Movements? sourceitem)
        {
            if (sourceitem == null)
                return false;

            var item = await CanteenProvider.GetSubscriberMovement(sourceitem.ID);

            if (sourceitem.CANTEEN_Subscriber_ID != null)
            {
                var sub = await CanteenProvider.GetSubscriber(sourceitem.CANTEEN_Subscriber_ID.Value);

                if (item != null && sub != null)
                {
                    if (item != null && item.CancelDate != null)
                    {
                        item.CancelDate = null;

                        await CanteenProvider.SetSubscriberMovement(item);

                        if (item.AUTH_User_ID != null)
                        {
                            var userLang = await AUTHProvider.GetSettings(item.AUTH_User_ID.Value);

                            if (item.AUTH_User_ID != null && SessionWrapper.AUTH_Municipality_ID != null)
                            {
                                var msgParameter = new List<MSG_Message_Parameters>()
                                {
                                    new MSG_Message_Parameters()
                                    {
                                        Code = "{Date}",
                                        Message = item.Date.Value.ToString("dd.MM.yyyy")
                                    },
                                    new MSG_Message_Parameters()
                                    {
                                        Code = "{Name}",
                                        Message = sub.FullName
                                    }
                                };

                                var msg = await MessageService.GetMessage(item.AUTH_User_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_CANTEEN_DAILY_REOPENED_TEXT", "NOTIF_CANTEEN_DAILY_REOPENED_SHORTTEXT", "NOTIF_CANTEEN_DAILY_REOPENED_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true, msgParameter);

                                if (msg != null)
                                {
                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (item != null && item.Date == DateTime.Today && DateTime.Now.Hour >= 10)
                        {
                            return false;
                        }

                        item.CancelDate = DateTime.Now;
                        await CanteenProvider.SetSubscriberMovement(item);

                        if (item.AUTH_User_ID != null)
                        {
                            var userLang = await AUTHProvider.GetSettings(item.AUTH_User_ID.Value);

                            if (item.AUTH_User_ID != null && SessionWrapper.AUTH_Municipality_ID != null)
                            {
                                var msgParameter = new List<MSG_Message_Parameters>()
                            {
                                new MSG_Message_Parameters()
                                {
                                    Code = "{Date}",
                                    Message = item.Date.Value.ToString("dd.MM.yyyy")
                                },
                                new MSG_Message_Parameters()
                                {
                                    Code = "{Name}",
                                    Message = sub.FullName
                                }
                            };

                                var msg = await MessageService.GetMessage(item.AUTH_User_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_CANTEEN_DAILY_CANCEL_TEXT", "NOTIF_CANTEEN_DAILY_CANCEL_SHORTTEXT", "NOTIF_CANTEEN_DAILY_CANCEL_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true, msgParameter);

                                if (msg != null)
                                {
                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                                }
                            }
                        }
                    }

                    await LoadData();
                }
            }

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
        private void ClearAnchors(Guid TabID)
        {
            if (TabID != CurrentTab)
            {
                CurrentTab = TabID;

                AnchorService.ClearAnchors();
                AddAnchors(TabID);

                StateHasChanged();
            }
        }
        private void AddAnchors(Guid TabID)
        {
            if(LatestMovements != null && LatestMovements.Count > 0)
            {
                foreach(var month in LatestMovements.Where(p => p.CANTEEN_Subscriber_ID == TabID && p.Date != null).Select(p => p.Date.Value.Month).Distinct().OrderBy(p => p).ToList())
                {
                    var monthName = GetMonthName(month);

                    AnchorService.AddAnchor(monthName, monthName, month);
                }
            }
        }
        private string GetMonthName(int month)
        {
            return new DateTime(2010, month, 1).ToString("MMMM", CultureInfo.CurrentCulture);
        }
    }
}