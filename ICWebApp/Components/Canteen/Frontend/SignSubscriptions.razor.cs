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
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Domain.Models;
using ICWebApp.Application.Settings;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class SignSubscriptions
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] private IFILEProvider FileProvider { get; set; }
        [Inject] private ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ILANGProvider LanguageProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }

        [Inject] ID3Helper D3Helper { get; set; }
        [Parameter] public string SubscriptionFamilyID { get; set; }

        private Guid? File_FileInfoID;
        private IDictionary<string, object> ReportParameters { get; set; }
        private Guid _subscriptionFamilyID = Guid.NewGuid();

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            ReportParameters = new Dictionary<string, object>();
            _subscriptionFamilyID = Guid.Parse(SubscriptionFamilyID);

            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

            Telerik.Reporting.ReportParameter reportParameterID = new Telerik.Reporting.ReportParameter();
            ReportParameters.Add("SubscriptionFamilyID", SubscriptionFamilyID);
            reportParameterID.Name = "SubscriptionFamilyID";
            reportParameterID.Text = SubscriptionFamilyID.ToString();
            reportParameterID.Value = SubscriptionFamilyID.ToString();
            reportParameterID.Type = Telerik.Reporting.ReportParameterType.String;
            reportParameterID.AllowBlank = false;
            reportParameterID.AllowNull = false;

            //CANTEEN_Subscriber_SubscriptionFamilyID
            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Sign", "CANTEEN_SIGN_TITLE", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen", "MAINMENU_CANTEEN", null, null);
                CrumbService.AddBreadCrumb("/Canteen/Sign", "CANTEEN_SIGN_TITLE", null, null);
            }

            var reportPackager = new ReportPackager();
            var reportSource = new InstanceReportSource();

            var CurrentLanguage = LanguageProvider.GetLanguageByCode(CultureInfo.CurrentCulture.Name);

            string reportFileName = "MensaApplication" + CurrentLanguage.Code + ".trdp";

            var BasePath = @"D:\Comunix\Reports\" + reportFileName;

            if (NavManager.BaseUri.Contains("localhost"))
            {
                BasePath = @"C:\VisualStudioProjects\Comunix\ICWebApp.Reporting\wwwroot\Reports\" + reportFileName;
            }

            using (var sourceStream = System.IO.File.OpenRead(BasePath))
            {
                var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
                report.ReportParameters.Clear();
                report.ReportParameters.Add(reportParameterID);

                reportSource.ReportDocument = report;
                
                var reportProcessor = new ReportProcessor();
                var deviceInfo = new System.Collections.Hashtable();

                deviceInfo.Add("ComplianceLevel", "PDF/A-2b");

                RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

                var ms = new MemoryStream(result.DocumentBytes);
                ms.Position = 0;

                FILE_FileInfo fi = new FILE_FileInfo();
                fi.ID = Guid.NewGuid();
                fi.FileName = "MensaApplication.pdf";
                fi.CreationDate = DateTime.Now;
                fi.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                fi.FileExtension = ".pdf";
                fi.Size = ms.Length;
                var fstorage = new FILE_FileStorage();

                fstorage.ID = Guid.NewGuid();
                fstorage.CreationDate = DateTime.Now;
                fstorage.FILE_FileInfo_ID = fi.ID;

                fstorage.FileImage = ms.ToArray();

                fi.FILE_FileStorage = new List<FILE_FileStorage>();
                fi.FILE_FileStorage.Add(fstorage);

                await FileProvider.SetFileInfo(fi);

                File_FileInfoID = fi.ID;

                var listSubsriptions = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);
                listSubsriptions = listSubsriptions.Where(a => a.SubscriptionFamilyID == _subscriptionFamilyID).ToList();
                foreach (var item in listSubsriptions)
                {
                    item.FILE_FileInfo_ID = fi.ID;
                    await CanteenProvider.SetSubscriber(item);
                }


                if (File_FileInfoID == null)
                {
                    BusyIndicatorService.IsBusy = false;
                }
                StateHasChanged();
            }

            if (File_FileInfoID == null)
            {
                BusyIndicatorService.IsBusy = false;
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
        private async void DocumentSigned()
        {
            //HIER STATUS SETZEN
            var listSubsriptions = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);

            listSubsriptions = listSubsriptions.Where(a => a.SubscriptionFamilyID == _subscriptionFamilyID).ToList();

            var lastNumber = CanteenProvider.GetLatestProgressivNumber(SessionWrapper.AUTH_Municipality_ID.Value, DateTime.Now.Year);

            foreach (var item in listSubsriptions)
            {
                item.FILE_FileInfo_ID = File_FileInfoID;
                item.CANTEEN_Subscriber_Status = null;
                item.CANTEEN_Subscriber_Status_ID = CanteenStatus.Comitted;
                item.SignedDate = DateTime.Now;

                if (item.ProgressivNumber == null || item.ProgressivNumber == 0)
                {
                    item.ProgressivYear = DateTime.Now.Year;

                    item.ProgressivNumber = lastNumber + 1;
                }

                await CanteenProvider.SetSubscriber(item);
            }

            if (SessionWrapper.CurrentUser != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                var msg = await MessageService.GetMessage(SessionWrapper.CurrentUser.ID, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_CANTEEN_COMITTED_TEXT", "NOTIF_CANTEEN_COMITTED_SHORTTEXT", "NOTIF_CANTEEN_COMITTED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true);

                if (msg != null)
                {
                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Canteen/Service");
                }

                //SEND TO AUTHORITY
                var authorityMsg = await MessageService.GetMessage(SessionWrapper.CurrentUser.ID, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_MUN_CANTEEN_COMITTED_TEXT", "NOTIF_MUN_CANTEEN_COMITTED_SHORTTEXT", "NOTIF_MUN_CANTEEN_COMITTED_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true);

                var authorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null, null);

                var subsititutionAuthority = authorityList.FirstOrDefault(p => p.IsMensa == true);

                if (authorityMsg != null && subsititutionAuthority != null)
                {
                    await MessageService.SendMessageToAuthority(subsititutionAuthority.ID, authorityMsg, NavManager.BaseUri + "/Backend/Canteen/Subscriptionlist");
                }
            }

            if (File_FileInfoID != null)
            {
                Task.Run(async () => await D3Helper.ProtocolNewCanteenSubscription(File_FileInfoID.Value, listSubsriptions)).ConfigureAwait(false);
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
        private void BackToPrevious()
        {
            if (MyCivisService.Enabled == true)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Subscribe/" + SubscriptionFamilyID);
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Subscribe/" + SubscriptionFamilyID);
            }

            StateHasChanged();
        }
    }
}
