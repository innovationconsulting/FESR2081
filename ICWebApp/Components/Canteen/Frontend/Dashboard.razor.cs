using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.ActionBar;
using ICWebApp.Domain.Models.User;
using ICWebApp.Pages.Organziation.Backend;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor;
using Telerik.SvgIcons;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class Dashboard
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IMailerService MailerService { get; set; }
        [Inject] ISMSService SMSService { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] IActionBarService ActionBarService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private decimal CurrentBalance = 0;
        private List<V_CANTEEN_Subscriber_Movements> LatestMovements = new List<V_CANTEEN_Subscriber_Movements>();
        private List<V_CANTEEN_Subscriber> Subscribers = new List<V_CANTEEN_Subscriber>();
        private V_CANTEEN_Schoolyear_Current? CurrentSchoolyear;
        private List<AUTH_MunicipalityApps>? AktiveApps = new List<AUTH_MunicipalityApps>();
        private List<ActionBarItem> ActionItems = new List<ActionBarItem>();
        private List<ServiceDataItem> SubscribersList = new List<ServiceDataItem>();
        private int ShowAmount = 5;
        private decimal OpenBalance = 0;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;

            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_CANTEEN_SERVICE");
            SessionWrapper.PageSubTitle = TextProvider.Get("MAINMENU_CANTEEN_SERVICE_DESCRIPTION");

            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Service", "MAINMENU_CANTEEN", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen/Service", "MAINMENU_CANTEEN", null, null);
            }

            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

            StateHasChanged();

            AktiveApps = await AuthProvider.GetMunicipalityApps();

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                await LoadData();
            }

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                SessionWrapper.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();

            }

            CreateActionBar();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void CreateActionBar()
        {
            ActionItems.Clear();

            ActionItems.Add(new ActionBarItem()
            {
                SortOrder = 0,
                Action = InfoPage,
                Icon = "css/bootstrap-italia/svg/sprites.svg#it-info-circle",
                Title = TextProvider.Get("CANTEEN_DASHBOARD_INFOPAGE")
            });

            if (AktiveApps != null && AktiveApps.Select(p => p.APP_Application_ID).ToList().Contains(Applications.Forms) && MyCivisService.Enabled != true)
            {
                ActionItems.Add(new ActionBarItem()
                {
                    SortOrder = 6,
                    Action = RequestRest,
                    Icon = "css/bootstrap-italia/svg/sprites.svg#it-restore",
                    Title = TextProvider.Get("CANTEEN_DASHBOARD_REQUEST_REST_BALANCE")
                });
            }

            ActionItems.Add(new ActionBarItem()
            {
                SortOrder = 7,
                Action = NavigateToTaxReportPage,
                Icon = "css/bootstrap-italia/svg/sprites.svg#it-inbox",
                Title = TextProvider.Get("CANTEEN_TAX_REPORTS")
            });
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
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                EnviromentService.ScrollToTop();
                StateHasChanged();
            }

            base.OnAfterRender(firstRender);
        }
        private async Task<bool> LoadData()
        {
            CurrentSchoolyear = (await CanteenProvider.GetSchoolsyearsCurrent(SessionWrapper.AUTH_Municipality_ID.Value, true)).FirstOrDefault();
            CurrentBalance = CanteenProvider.GetUserBalance(SessionWrapper.CurrentUser.ID);
            OpenBalance = CanteenProvider.GetOpenPayent(SessionWrapper.CurrentUser.ID) * -1;

            await LoadMovementData();

            Subscribers = await CanteenProvider.GetVSubscribersByUserID(SessionWrapper.CurrentUser.ID);
            Subscribers = Subscribers.Where(a => a.Archived == null).ToList();

            SubscribersList.Clear();

            foreach (var d in Subscribers)
            {
                var item = new ServiceDataItem()
                {
                    CreationDate = d.CreationDate,
                    File_FileInfo_ID = d.FILE_FileInfo_ID,
                    ProtocollNumber = d.ProgressivNumber,
                    StatusIcon = d.StatusIcon,
                    StatusText = d.StatusText,
                };

                if (d.InChange == true)
                {
                    item.Title = TextProvider.Get("CANTEEN_SUBSCRIPTION_EDIT_TITLE") + " - " + d.FirstName + " " + d.LastName;
                }
                else
                {
                    item.Title = d.FirstName + " " + d.LastName;
                }

                if (d.CANTEEN_Subscriber_Status_ID == CanteenStatus.Incomplete) //INCOMPLETE
                {
                    if (CurrentSchoolyear != null && CurrentSchoolyear.RegisterBeginDate <= DateTime.Now && CurrentSchoolyear.RegisterEndDate.Value.AddDays(1) >= DateTime.Now)
                    {
                        item.DetailAction = (() => EditSubscription(d));
                        item.DetailTextCode = "CANTEEN_DASHBOARD_COMPLETE";

                        if(d.InChange == true)
                        {
                            item.CancelAction = (() => CancelChangeRequest(d));
                            item.CancelTextCode = "CANTEEN_CANCEL_CHANGE_SUBSCRIPTION";
                        }
                    }
                }
                else if (d.CANTEEN_Subscriber_Status_ID == CanteenStatus.ToSign)
                {
                    if (CurrentSchoolyear != null && CurrentSchoolyear.RegisterBeginDate <= DateTime.Now && CurrentSchoolyear.RegisterEndDate.Value.AddDays(1) >= DateTime.Now)
                    {
                        item.DetailAction = (() => SignSubscription(d));
                        item.DetailTextCode = "CANTEEN_DASHBOARD_SIGN";
                    }
                }
                else if (d.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted || d.CANTEEN_Subscriber_Status_ID == CanteenStatus.Comitted) //ACCEPTED
                {
                    item.DetailAction = (() => GoToSubscriber(d.ID));
                    
                    if(d.FILE_FileInfo_ID != null && d.CANTEEN_Subscriber_Status_ID == CanteenStatus.Accepted && d.SuccessorSubscriptionID == null)
                    {
                        item.CancelAction = (() => NewVersionSubscription(d));
                        item.CancelTextCode = "CANTEEN_CHANGE_SUBSCRIPTION";
                    }
                }
                

                SubscribersList.Add(item);
            }

            return true;
        }
        private async Task LoadMovementData()
        {
            LatestMovements = await CanteenProvider.GetPastVSubscriberMovementsByUser(SessionWrapper.CurrentUser.ID, ShowAmount);
        }
        private void GoToSubscriber(Guid SubscriberID)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Detail/Subscribe/" + SubscriberID);
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Detail/Subscribe/" + SubscriberID);
            }
        }
        private async void RechargeBalance()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/RechargeAmount");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/RechargeAmount");
            }
        }
        private async void RegisterAbsence()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Absence");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Absence");
            }
        }
        private async void RequestRest()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Application/f8d6bbcb-ce04-495f-ac4e-702a7abed900/New/" + CurrentBalance);
        }
        private async void InfoPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivisInfo");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/info");
            }
        }    
        private void CreateSubscription()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Subscribe");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Subscribe");
            }
        } 
        private async void SignSubscription(V_CANTEEN_Subscriber? sub)
        {
            if (sub != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (MyCivisService.Enabled == true)
                {
                    NavManager.NavigateTo("/Canteen/MyCivis/Sign" + sub.SubscriptionFamilyID);
                }
                else
                {
                    NavManager.NavigateTo("/Canteen/Sign/" + sub.SubscriptionFamilyID);
                }
            }
        }
        private async void NewVersionSubscription(V_CANTEEN_Subscriber? sub)
        {

            if (!await Dialogs.ConfirmAsync(TextProvider.Get("CANTEEN_CREATE_NEW_VERSION_OF_SUBSCRIPTION_ARE_YOU_SURE"),
                    TextProvider.Get("INFORMATION")))
            {
                Console.WriteLine("Action cancelled");
            }
            else
            {
                if (sub != null && sub.ID != null)
                {
                    var newSub = await CanteenProvider.CloneAndArchiveSubscriber(sub.ID);

                    if (newSub != null)
                    {
                        BusyIndicatorService.IsBusy = true;

                        if (MyCivisService.Enabled == true)
                        {
                            NavManager.NavigateTo("/Canteen/MyCivis/Subscribe/" + newSub.SubscriptionFamilyID + "/1");
                        }
                        else
                        {
                            NavManager.NavigateTo("/Canteen/Subscribe/" + newSub.SubscriptionFamilyID + "/1");
                        }

                        StateHasChanged();
                    }
                }
            }

            StateHasChanged();
        }
        private async void CancelChangeRequest(V_CANTEEN_Subscriber? sub)
        {
            if (!await Dialogs.ConfirmAsync(TextProvider.Get("CANTEEN_CANCEL_CHANGE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
            {
                return;
            }

            if (sub == null || sub.ID == null)
            {
                return;
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            var locSubscriber = await CanteenProvider.GetSubscriberWithoutInclude(sub.ID);

            if (locSubscriber != null)
            {
                if (locSubscriber.PreviousSubscriptionID != null)
                {
                    var previousSubscriber = await CanteenProvider.GetSubscriberWithoutInclude(locSubscriber.PreviousSubscriptionID.Value);

                    if (previousSubscriber != null)
                    {
                        previousSubscriber.SuccessorSubscriptionID = null;

                        await CanteenProvider.SetSubscriber(previousSubscriber);
                    }
                }

                await CanteenProvider.RemoveSubscriber(locSubscriber.ID, true);
            }

            await LoadData();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
        private void EditSubscription(V_CANTEEN_Subscriber? sub)
        {
            if (sub != null)
            {
                BusyIndicatorService.IsBusy = true;

                if (sub.PreviousSubscriptionID != null)
                {
                    if (MyCivisService.Enabled == true)
                    {
                        NavManager.NavigateTo("/Canteen/MyCivis/Subscribe/" + sub.SubscriptionFamilyID + "/1");
                    }
                    else
                    {
                        NavManager.NavigateTo("/Canteen/Subscribe/" + sub.SubscriptionFamilyID + "/1");
                    }
                }
                else
                {
                    if (MyCivisService.Enabled == true)
                    {
                        NavManager.NavigateTo("/Canteen/MyCivis/Subscribe/" + sub.SubscriptionFamilyID);
                    }
                    else
                    {
                        NavManager.NavigateTo("/Canteen/Subscribe/" + sub.SubscriptionFamilyID);
                    }
                }

                StateHasChanged();
            }
        }
        private void NavigateToTaxReportPage()
        {
            BusyIndicatorService.IsBusy = true;

            if (MyCivisService.Enabled)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/TaxReports");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/TaxReports");
            }

            StateHasChanged();
        }
        private async void IncreaseShowAmount()
        {
            ShowAmount += 5;
            await LoadMovementData();
            StateHasChanged();
        }
    }
}
