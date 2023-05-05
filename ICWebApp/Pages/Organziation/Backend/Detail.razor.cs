using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.Textvorlagen;
using Microsoft.AspNetCore.Components;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Telerik.Blazor;

namespace ICWebApp.Pages.Organziation.Backend
{
    public partial class Detail
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] IRequestAdministrationHelper RequestAdministrationHelper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IFORM_ReportPrintHelper FormReportPrintHelper { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] ITASKService TaskService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string ID { get; set; }


        private Administration_Filter_Request Filter = new Administration_Filter_Request();
        private List<V_ORG_Requests> Data = new List<V_ORG_Requests>();
        private V_ORG_Requests? CurrentApplicationView;
        private ORG_Request? CurrentApplication;
        private TextItem? TextItem = new TextItem();
        private List<ORG_Request_Status_Log> CurrentStatusLogList = new List<ORG_Request_Status_Log>();
        private List<ORG_Request_Ressource> CurrentApplicationRessource = new List<ORG_Request_Ressource>();
        private List<V_ORG_Request_Status> StatusList = new List<V_ORG_Request_Status>();
        private List<ORG_Request_Attachment> CurrentApplicationAttachments = new List<ORG_Request_Attachment>();
        private AUTH_Municipality CurrentMunicipality;
        private List<V_AUTH_BolloFree_Reason>? BolloFreeReasons;

        private int _applicationTabIndex = 0;
        private int ApplicationTabIndex
        {
            get
            {
                return _applicationTabIndex;
            }
            set
            {
                _applicationTabIndex = value;
                OnTabIndexChanged(_applicationTabIndex);
            }
        }
        private bool IsDataBusy { get; set; } = true;
        private bool IsWizardBusy { get; set; } = true;
        private Guid? StartStatus { get; set; }
        private bool FilterWindowVisible { get; set; } = false;
        private string? CurrentWizardTitle { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (RequestAdministrationHelper.Filter != null)
            {
                Filter = RequestAdministrationHelper.Filter;
            }

            CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_OVERVIEW");
            
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                CurrentMunicipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            Data = await GetData(Filter);
            StatusList = await GetStatusList();
            BolloFreeReasons = await AuthProvider.GetVBolloFreeReasons();

            IsDataBusy = false;
            IsWizardBusy = false;
            BusyIndicatorService.IsBusy = false;

            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("ORG_BACKEND_APPLICATION_TITLE");
            CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_OVERVIEW");

            if (ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Backend/Application/List");
                StateHasChanged();
                return;
            }

            IsDataBusy = true;
            IsWizardBusy = true;
            StateHasChanged();

            CurrentApplication = await GetCurrentApplication();
            CurrentApplicationView = await GetCurrentApplicationView();

            if (CurrentApplication != null)
            {
                CurrentStatusLogList = await GetCurrentApplicationStatusLog();
                CurrentApplicationRessource = await GetCurrentApplicationRessource();
                CurrentApplicationAttachments = await GetCurrentApplicationAttachments();

                StartStatus = CurrentApplication.ORG_Request_Status_ID;
            }

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Organization/Backend/Application/List", "ORG_BACKEND_APPLICATION_TITLE", null, null);

            if (CurrentApplication != null)
            {
                CrumbService.AddBreadCrumb(NavManager.Uri, CurrentApplication.Firstname + " " + CurrentApplication.Lastname, null, null, true);
            }

            var contextName = "";
            if (CurrentApplication != null)
            {
                contextName = CurrentApplication.Firstname + " " + CurrentApplication.Lastname;
            }
            await TaskService.SetContext(4, ID, contextName, false, null);    //ROOM CONTEXT

            IsDataBusy = false;
            IsWizardBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async Task<ORG_Request?> GetCurrentApplication()
        {
            return await OrgProvider.GetRequest(Guid.Parse(ID));
        }
        private async Task<V_ORG_Requests?> GetCurrentApplicationView()
        {
            return await OrgProvider.GetVRequest(Guid.Parse(ID));
        }
        private async Task<List<ORG_Request_Status_Log>> GetCurrentApplicationStatusLog()
        {
            var locList = await OrgProvider.GetRequestStatusLogListByRequest(CurrentApplication.ID);

            if (CurrentApplication.CreationDate != null)
            {
                locList.Add(new ORG_Request_Status_Log()
                {
                    AUTH_Users_ID = CurrentApplication.AUTH_Users_ID,
                    User = CurrentApplication.User_Firstname + " " + CurrentApplication.User_Lastname,
                    ChangeDate = CurrentApplication.CreationDate,
                    StatusIcon = "fa-regular fa-file-plus",
                    Status = TextProvider.Get("FORM_USER_DETAIL_CREATED_STATUS")
                });
            }

            if (CurrentApplication.SubmitAt != null)
            {
                var CommittedStatus = StatusList.FirstOrDefault(p => p.ID == FORMStatus.Comitted);

                if (CommittedStatus != null)
                {
                    locList.Add(new ORG_Request_Status_Log()
                    {
                        AUTH_Users_ID = CurrentApplication.AUTH_Users_ID,
                        User = CurrentApplication.User_Firstname + " " + CurrentApplication.User_Lastname,
                        ChangeDate = CurrentApplication.SubmitAt,
                        StatusIcon = CommittedStatus.Icon,
                        Status = CommittedStatus.Description
                    });
                }
            }

            return locList;
        }
        private async Task<List<ORG_Request_Ressource>> GetCurrentApplicationRessource()
        {
            var ressources = await OrgProvider.GetRequestRessourceList(CurrentApplication.ID);

            if (ressources != null)
            {
                return ressources.Where(p => p.UserUpload == false).ToList();
            }

            return new List<ORG_Request_Ressource>();
        }
        private async Task<List<ORG_Request_Attachment>> GetCurrentApplicationAttachments()
        {
            var attachments = await OrgProvider.GetRequestAttachments(CurrentApplication.ID);

            if (attachments != null)
            {
                return attachments.ToList();
            }

            return new List<ORG_Request_Attachment>();
        }
        private async Task<List<V_ORG_Requests>> GetData(Administration_Filter_Request? Filter)
        {
            if (SessionWrapper.AUTH_Municipality_ID != null && SessionWrapper.CurrentUser != null)
            {
                var applications = await OrgProvider.GetRequestList(Filter);

                RequestAdministrationHelper.Filter = Filter;

                return applications;
            }

            return new List<V_ORG_Requests>();
        }
        private async Task<List<V_ORG_Request_Status>> GetStatusList()
        {
            var data = await OrgProvider.GetStatusList();

            return data;
        }
        private async void FilterSearch(Administration_Filter_Request Filter)
        {
            FilterWindowVisible = false;
            IsDataBusy = true;
            IsWizardBusy = true;
            StateHasChanged();

            this.Filter = Filter;

            Data = await GetData(this.Filter);

            IsDataBusy = false;
            IsWizardBusy = false;
            StateHasChanged();
        }
        private void FilterClose()
        {
            FilterWindowVisible = false;
            StateHasChanged();
        }
        private void ShowFilter()
        {
            FilterWindowVisible = true;
            StateHasChanged();
        }
        private void BackToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Organization/Backend/Application/List");
        }
        private void SelectApplication(Guid? ID)
        {
            if (ID != null && (CurrentApplication == null || ID != CurrentApplication.ID))
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged(); 
                NavManager.NavigateTo("/Organization/Backend/Application/Detail/" + ID.ToString());
            }
        }
        private void OnTabIndexChanged(int Index)
        {
            if (Index == 0)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_OVERVIEW");
            }
            else if (Index == 1)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_ANAGRAFIC");
            }
            else if (Index == 2)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_DETAILS");
            }
            else if (Index == 3)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_PREVIEW");
            }
            else if (Index == 4)
            {
                CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_STATUS_LOG");
            }            

            IsWizardBusy = false;
            StateHasChanged();
        }
        private void OnStepChanged()
        {
            IsWizardBusy = true;
            StateHasChanged();
        }
        private async void ArchivedChanged()
        {
            if (CurrentApplication != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (CurrentApplication.ORG_Request_Status_ID != StartStatus)
                {
                    CurrentApplication.ORG_Request_Status_ID = StartStatus;
                }

                await OrgProvider.SetRequest(CurrentApplication);

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private async Task<bool> ChangeStatus()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (CurrentApplication != null && CurrentApplicationView != null && TextItem != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("APPLICATION_STATUS_CHANGE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                {
                    BusyIndicatorService.IsBusy = false;
                    await InvokeAsync(() => StateHasChanged());
                    StateHasChanged();
                    return false;
                }

                var StatusLog = new ORG_Request_Status_Log();

                StatusLog.ID = Guid.NewGuid();

                StatusLog.ORG_Request_ID = CurrentApplication.ID;
                StatusLog.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                StatusLog.ORG_Request_Status_ID = CurrentApplication.ORG_Request_Status_ID;
                StatusLog.ChangeDate = DateTime.Now;

                await OrgProvider.SetRequest(CurrentApplication);
                await OrgProvider.SetRequestStatusLog(StatusLog);

                var Languages = await LangProvider.GetAll();

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        var dataE = new ORG_Request_Status_Log_Extended()
                        {
                            ORG_Request_Status_Log_ID = StatusLog.ID,
                            LANG_Languages_ID = l.ID
                        };

                        if (l.ID == LanguageSettings.German)  //DE
                        {
                            dataE.Reason = await OrgProvider.ReplaceKeywords(CurrentApplication.ID, LanguageSettings.German, TextItem.German ?? "", StartStatus);
                        }
                        else if (l.ID == LanguageSettings.Italian) //IT
                        {
                            dataE.Reason = dataE.Reason = await OrgProvider.ReplaceKeywords(CurrentApplication.ID, LanguageSettings.Italian, TextItem.Italian ?? "", StartStatus);
                        }
                        else
                        {
                            dataE.Reason = TextItem.German;
                        }
                        
                        await OrgProvider.SetRequestStatusLogExtended(dataE);
                    }
                }

                if (StatusLog.ORG_Request_Status_ID == Guid.Parse("334b499b-e6ff-42af-a083-2438622bbd15"))    //GENEHMIGT/ANGENOMMEN
                {
                    await CreateNewOrganization();
                }

                CurrentStatusLogList = await GetCurrentApplicationStatusLog();


                FILE_FileInfo? FileDE = null;
                FILE_FileInfo? FileIT = null;

                if (CurrentApplication.ORG_Request_Status_ID != null && TextItem != null)
                {
                    var currentStatus = StatusList.FirstOrDefault(p => p.ID == CurrentApplication.ORG_Request_Status_ID.Value);

                    if (currentStatus != null && currentStatus.GeneratesPDF == true && CurrentApplication.FILE_Fileinfo_ID != null && SessionWrapper.AUTH_Municipality_ID != null && Languages != null)
                    {
                        if (!string.IsNullOrEmpty(TextItem.German))
                        {
                            FileDE = await CreateResponseFile(CurrentApplication, LanguageSettings.German, TextProvider.Get(currentStatus.TEXT_SystemText_Code, LanguageSettings.German));
                        }

                        if (!string.IsNullOrEmpty(TextItem.Italian))
                        {
                            FileIT = await CreateResponseFile(CurrentApplication, LanguageSettings.Italian, TextProvider.Get(currentStatus.TEXT_SystemText_Code, LanguageSettings.Italian));
                        }

                        CurrentApplicationRessource = await GetCurrentApplicationRessource();
                    }

                    if (currentStatus != null && currentStatus.IsFinalStatus == true)
                    {
                        CurrentApplication.Archived = DateTime.Now;

                        await OrgProvider.SetRequest(CurrentApplication);
                    }
                }

                if (CurrentApplication.AUTH_Users_ID != null)
                {
                    string text = "";
                    Guid userLangID = LanguageSettings.German;

                    var userLang = await AuthProvider.GetSettings(CurrentApplication.AUTH_Users_ID.Value);

                    if (userLang != null && userLang.LANG_Languages_ID == LanguageSettings.Italian)
                    {
                        if (!string.IsNullOrEmpty(TextItem.Italian))
                        {
                            text = await TextProvider.ReplaceGeneralKeyWords(CurrentApplication.AUTH_Users_ID.Value, TextItem.Italian);
                        }

                        text = await OrgProvider.ReplaceKeywords(CurrentApplication.ID, LanguageSettings.Italian, text, StartStatus);

                        userLangID = LanguageSettings.Italian;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(TextItem.German))
                        {
                            text = await TextProvider.ReplaceGeneralKeyWords(CurrentApplication.AUTH_Users_ID.Value, TextItem.German);
                        }

                        text = await OrgProvider.ReplaceKeywords(CurrentApplication.ID, LanguageSettings.German, text, StartStatus);
                    }

                    if (CurrentApplication.AUTH_Users_ID != null && CurrentApplication.AUTH_Municipality_ID != null)
                    {
                        var msg = await MessageService.GetMessage(CurrentApplication.AUTH_Users_ID.Value, CurrentApplication.AUTH_Municipality_ID.Value, text,
                                                                  TextProvider.Get("NOTIF_SUBSTITUTION_STATUS_CHANGED_SHORTTEXT", userLangID),
                                                                  TextProvider.Get("NOTIF_SUBSTITUTION_STATUS_CHANGED_TITLE", userLangID),
                                                                  Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), false, null);

                        if (msg != null)
                        {
                            var AttachmentsList = new List<FILE_FileInfo>();

                            if (FileDE != null)
                            {
                                AttachmentsList.Add(FileDE);
                            }

                            if (FileIT != null)
                            {
                                AttachmentsList.Add(FileIT);
                            }

                            await MessageService.SendMessage(msg, NavManager.BaseUri + "/Organization/Detail/" + CurrentApplication.ID, AttachmentsList);
                        }
                    }
                }

                StartStatus = StatusLog.ORG_Request_Status_ID;
                TextItem = new TextItem();
                StateHasChanged();
                await InvokeAsync(() => StateHasChanged());
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            return true;
        }
        private async Task<FILE_FileInfo?> CreateResponseFile(ORG_Request App, Guid LANG_Language_ID, string FileName)
        {
            if (App == null)
            {
                return null;
            }

            MemoryStream resultStream = new MemoryStream();

            var ApplicationPDFStream = OrgProvider.CreatePDF(CurrentApplication);
            var ResponsePDFStream = OrgProvider.GetResponsePDF(LANG_Language_ID.ToString(), SessionWrapper.AUTH_Municipality_ID.Value.ToString(), App.ID.ToString());

            //GERNERATE PDF AND SAVE
            using (PdfDocument one = PdfReader.Open(ResponsePDFStream, PdfDocumentOpenMode.Import))
            using (PdfDocument two = PdfReader.Open(ApplicationPDFStream, PdfDocumentOpenMode.Import))
            using (PdfDocument outPdf = new PdfDocument())
            {
                FormReportPrintHelper.CopyPages(one, outPdf);
                FormReportPrintHelper.CopyPages(two, outPdf);

                outPdf.Save(resultStream);
            }

            FILE_FileInfo f = new FILE_FileInfo();

            f.ID = Guid.NewGuid();
            f.AUTH_Users_ID = App.AUTH_Users_ID;
            f.CreationDate = DateTime.Now;
            f.FileName = FileName;
            f.FileExtension = ".pdf";
            f.Size = resultStream.Length;

            FILE_FileStorage storage = new FILE_FileStorage();

            storage.ID = Guid.NewGuid();
            storage.FILE_FileInfo_ID = f.ID;
            storage.FileImage = resultStream.ToArray();
            storage.CreationDate = DateTime.Now;

            f.FILE_FileStorage = new List<FILE_FileStorage>() { storage };

            await FileProvider.SetFileInfo(f);

            var ress = new ORG_Request_Ressource();
            ress.ID = Guid.NewGuid();
            ress.ORG_Request_ID = App.ID;
            ress.CreationDate = DateTime.Now;
            ress.FILE_FileInfo_ID = f.ID;
            ress.UserUpload = false;

            await OrgProvider.SetRequestRessource(ress);

            return f;
        }
        private async void DownloadRessource(Guid FILE_Fileinfo_ID, string? Name)
        {
            var fileToDownload = await FileProvider.GetFileInfoAsync(FILE_Fileinfo_ID);

            if (fileToDownload != null)
            {
                FILE_FileStorage? blob = null;
                if (fileToDownload.FILE_FileStorage != null && fileToDownload.FILE_FileStorage.Count() > 0)
                {
                    blob = fileToDownload.FILE_FileStorage.FirstOrDefault();
                }
                else
                {
                    blob = await FileProvider.GetFileStorageAsync(fileToDownload.ID);
                }

                if (blob != null && blob.FileImage != null)
                {
                    if (string.IsNullOrEmpty(Name))
                    {
                        await EnviromentService.DownloadFile(blob.FileImage, fileToDownload.FileName + fileToDownload.FileExtension);
                    }
                    else
                    {
                        await EnviromentService.DownloadFile(blob.FileImage, Name + fileToDownload.FileExtension);
                    }
                }
            }

            StateHasChanged();
        }
        private async Task<bool> CreateNewOrganization()
        {
            //CHECK IF GV EXISTS, IF NOT CREATE REGISTRATION
            if (CurrentApplication.GV_AUTH_Users_ID == null && CurrentApplication.GV_FiscalNumber != null && CurrentApplicationView.HasGV == 1)
            {
                var GV = await AuthProvider.GetUser(CurrentApplication.GV_FiscalNumber, null, SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, true);

                if (GV != null)
                {
                    CurrentApplication.GV_AUTH_Users_ID = GV.ID;

                    await OrgProvider.SetRequest(CurrentApplication);
                }
                else
                {
                    AUTH_Users newUser = new AUTH_Users();

                    newUser.ID = Guid.NewGuid();
                    newUser.Firstname = CurrentApplication.GV_Firstname;
                    newUser.Lastname = CurrentApplication.GV_Lastname;
                    newUser.Username = CurrentApplication.GV_FiscalNumber;
                    newUser.Email = CurrentApplication.GV_Email;
                    newUser.PhoneNumber = CurrentApplication.GV_Phone;
                    newUser.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value;
                    newUser.RegistrationMode = "Citizen Backend";
                    newUser.IsOrganization = false;

                    var anagrafic = new AUTH_Users_Anagrafic();

                    anagrafic.ID = Guid.NewGuid();
                    anagrafic.AUTH_Users_ID = newUser.ID;
                    anagrafic.FirstName = CurrentApplication.GV_Firstname;
                    anagrafic.LastName = CurrentApplication.GV_Lastname;
                    anagrafic.FiscalNumber = CurrentApplication.GV_FiscalNumber;
                    anagrafic.Email = CurrentApplication.GV_Email;
                    anagrafic.CountyOfBirth = CurrentApplication.GV_CountyOfBirth;
                    anagrafic.PlaceOfBirth = CurrentApplication.GV_PlaceOfBirth;
                    anagrafic.DateOfBirth = CurrentApplication.GV_DateOfBirth;
                    anagrafic.Address = CurrentApplication.GV_DomicileStreetAddress + ", " + CurrentApplication.GV_DomicilePostalCode + " " + CurrentApplication.GV_DomicileMunicipality;
                    anagrafic.DomicileMunicipality = CurrentApplication.GV_DomicileMunicipality;
                    anagrafic.DomicileNation = CurrentApplication.GV_DomicileNation;
                    anagrafic.DomicilePostalCode = CurrentApplication.GV_DomicilePostalCode;
                    anagrafic.DomicileProvince = CurrentApplication.GV_DomicileProvince;
                    anagrafic.DomicileStreetAddress = CurrentApplication.GV_DomicileStreetAddress;
                    anagrafic.Gender = CurrentApplication.GV_Gender;
                    anagrafic.MobilePhone = CurrentApplication.GV_Phone;
                    anagrafic.RegisteredOffice = "Comunix Substitution";
                    anagrafic.SelectedMunicipality = CurrentApplication.GV_SelectedMunicipality;

                    await AuthProvider.RegisterUser(newUser, anagrafic);

                    await AuthProvider.SetAnagrafic(anagrafic);

                    CurrentApplication.GV_AUTH_Users_ID = newUser.ID;

                    await OrgProvider.SetRequest(CurrentApplication);
                }
            }

            //CREATE COMPANY

            AUTH_Users_Anagrafic? existingOrg = null;

            if (CurrentApplication.IsNewOrgRequest == false)
            {
                if (SessionWrapper.AUTH_Municipality_ID != null)  //OPEN UP FOR ADDITIONAL ORGS WITH SAME FISCAL CODE
                {
                    if (!string.IsNullOrEmpty(CurrentApplication.FiscalNumber))
                    {
                        existingOrg = await AuthProvider.GetAnagraficByFiscalCode(SessionWrapper.AUTH_Municipality_ID.Value, CurrentApplication.FiscalNumber);
                    }
                    else if (!string.IsNullOrEmpty(CurrentApplication.VatNumber))
                    {
                        existingOrg = await AuthProvider.GetAnagraficByFiscalCode(SessionWrapper.AUTH_Municipality_ID.Value, CurrentApplication.VatNumber);
                    }
                }
            }
            AUTH_Users? newOrg = null;

            if (existingOrg == null || existingOrg.AUTH_Users_ID == null)
            {
                newOrg = new AUTH_Users();

                newOrg.ID = Guid.NewGuid();
                newOrg.Firstname = CurrentApplication.Firstname;
                newOrg.Lastname = CurrentApplication.Lastname;
                newOrg.Username = CurrentApplication.Firstname;
                newOrg.Email = CurrentApplication.Email;
                newOrg.PhoneNumber = CurrentApplication.Phone;
                newOrg.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value;
                newOrg.RegistrationMode = "Organization";
                newOrg.IsOrganization = true;
                newOrg.AUTH_Company_Type_ID = CurrentApplication.AUTH_Company_Type_ID;
                newOrg.AUTH_Company_LegalForm_ID = CurrentApplication.AUTH_Company_LegalForm_ID;
          
                var anagraficOrg = new AUTH_Users_Anagrafic();

                anagraficOrg.ID = Guid.NewGuid();
                anagraficOrg.AUTH_Users_ID = newOrg.ID;
                anagraficOrg.FirstName = CurrentApplication.Firstname;
                anagraficOrg.LastName = CurrentApplication.Lastname;
                anagraficOrg.FiscalNumber = CurrentApplication.FiscalNumber;
                anagraficOrg.VatNumber = CurrentApplication.VatNumber;
                anagraficOrg.Email = CurrentApplication.Email;
                anagraficOrg.Address = CurrentApplication.DomicileStreetAddress + ", " + CurrentApplication.DomicilePostalCode + " " + CurrentApplication.DomicileMunicipality;
                anagraficOrg.DomicileMunicipality = CurrentApplication.DomicileMunicipality;
                anagraficOrg.DomicileNation = CurrentApplication.DomicileNation;
                anagraficOrg.DomicilePostalCode = CurrentApplication.DomicilePostalCode;
                anagraficOrg.DomicileProvince = CurrentApplication.DomicileProvince;
                anagraficOrg.DomicileStreetAddress = CurrentApplication.DomicileStreetAddress;
                anagraficOrg.MobilePhone = CurrentApplication.MobilePhone;
                anagraficOrg.Phone = CurrentApplication.Phone;
                anagraficOrg.PECEmail = CurrentApplication.PECEmail;
                anagraficOrg.CodiceDestinatario = CurrentApplication.CodiceDestinatario;
                anagraficOrg.AUTH_Company_LegalForm_ID = CurrentApplication.AUTH_Company_LegalForm_ID;
                anagraficOrg.HandelskammerEintragung = CurrentApplication.HandelskammerEintragung;
                anagraficOrg.BolloFree = CurrentApplication.BolloFree;
                anagraficOrg.AUTH_BolloFree_Reason_ID = CurrentApplication.AUTH_BolloFree_Reason_ID;
                anagraficOrg.IBAN = CurrentApplication.IBAN;
                anagraficOrg.KontoInhaber = CurrentApplication.KontoInhaber;
                anagraficOrg.Bankname = CurrentApplication.Bankname;
                anagraficOrg.EintragungDatum = CurrentApplication.EintragungDatum;
                anagraficOrg.EintragungNr = CurrentApplication.EintragungNr;
                anagraficOrg.SelectedMunicipality = CurrentApplication.SelectedMunicipality;

                if (CurrentApplicationView.HasGV == 1)
                {
                    anagraficOrg.GV_AUTH_Users_ID = CurrentApplication.GV_AUTH_Users_ID;
                }

                anagraficOrg.RegisteredOffice = "Comunix Organization";

                await AuthProvider.RegisterUser(newOrg, anagraficOrg);

                await AuthProvider.SetAnagrafic(anagraficOrg);
            }
            else
            {
                newOrg = await AuthProvider.GetUser(existingOrg.AUTH_Users_ID.Value);

                if (newOrg != null)
                {
                    newOrg.IsOrganization = true;
                    newOrg.AUTH_Company_Type_ID = CurrentApplication.AUTH_Company_Type_ID;
                    newOrg.AUTH_Company_LegalForm_ID = CurrentApplication.AUTH_Company_LegalForm_ID;
                    newOrg.RegistrationMode = "Organization";

                    await AuthProvider.UpdateUser(newOrg);  
                }
            }

            if (newOrg != null)
            {
                if (CurrentApplication.GV_AUTH_Users_ID != null && CurrentApplication.GV_AUTH_Users_ID != CurrentApplication.AUTH_Users_ID)
                {
                    var existingGVUser = await AuthProvider.GetUserOrganization(newOrg.ID, CurrentApplication.GV_AUTH_Users_ID.Value);

                    if (existingGVUser == null)
                    {
                        AUTH_ORG_Users newOrgUserGV = new AUTH_ORG_Users();

                        newOrgUserGV.ID = Guid.NewGuid();
                        newOrgUserGV.ORG_AUTH_Users_ID = newOrg.ID;
                        newOrgUserGV.AUTH_Users_ID = CurrentApplication.GV_AUTH_Users_ID;
                        newOrgUserGV.AUTH_ORG_Role_ID = Guid.Parse("da2a52c6-7c67-4f8e-9afd-5af026ab445e"); //GV Alle Rechte 
                        newOrgUserGV.ConfirmedAt = DateTime.Now;

                        await AuthProvider.SetUserOrganization(newOrgUserGV);
                    }
                }

                var existingOrgUser = await AuthProvider.GetUserOrganization(newOrg.ID, CurrentApplication.AUTH_Users_ID.Value);

                if (existingOrgUser == null)
                {
                    AUTH_ORG_Users newOrgUser = new AUTH_ORG_Users();

                    newOrgUser.ID = Guid.NewGuid();
                    newOrgUser.ORG_AUTH_Users_ID = newOrg.ID;
                    newOrgUser.AUTH_Users_ID = CurrentApplication.AUTH_Users_ID;
                    newOrgUser.ConfirmedAt = DateTime.Now;
                    newOrgUser.AUTH_ORG_Role_ID = Guid.Parse("76724c77-9b1e-4f8f-9444-057f7894783f"); //Administrator Alle Rechte 

                    await AuthProvider.SetUserOrganization(newOrgUser);
                }
            }

            return true;
        }
    }
}
