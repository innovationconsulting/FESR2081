using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.Textvorlagen;
using Microsoft.AspNetCore.Components;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Stripe;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Backend
{
    public partial class Detail
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFormAdministrationHelper FormAdministrationHelper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IFORM_ReportPrintHelper FormReportPrintHelper { get; set; }
        [Inject] IFORM_ReportRendererHelper FormReportRendererHelper { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IPAYProvider PAYProvider { get; set; }
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string? ActiveIndex { get; set; }

        private List<V_FORM_Application> Data = new List<V_FORM_Application>();
        private List<Guid> AllowedAuthorities = new List<Guid>();
        private Administration_Filter_Item Filter = new Administration_Filter_Item();
        private FORM_Application? CurrentApplication;
        private FORM_Definition? CurrentDefinition;
        private List<FORM_Application_Upload> CurrentApplicationUploads = new List<FORM_Application_Upload>();
        private List<FORM_Application_Upload_File> CurrentApplicationFileUploads = new List<FORM_Application_Upload_File>();
        private List<FORM_Definition_Upload> CurrentDefinitionUploads = new List<FORM_Definition_Upload>();
        private List<FORM_Application_Field_Data> CurrentApplicationFieldData = new List<FORM_Application_Field_Data>();
        private List<FORM_Application_Status> StatusList = new List<FORM_Application_Status>();
        private List<FORM_Application_Status_Log> CurrentStatusLogList = new List<FORM_Application_Status_Log>();
        private List<FILE_FileInfo> CurrentUserUpload = new List<FILE_FileInfo>();
        private List<FORM_Application_Ressource> CurrentApplicationRessource = new List<FORM_Application_Ressource>();
        private List<Guid?> CurrenTransactionList = new List<Guid?>();
        private TextItem TextItem = new TextItem();
        private TextItem TextDocumentItem = new TextItem();
        private List<AUTH_Users> UserList = new List<AUTH_Users>();
        private List<FORM_Application_Priority> PriorityList = new List<FORM_Application_Priority>();
        private List<V_FORM_Definition_Municipal_Field> DefinitionMunicipalFields = new List<V_FORM_Definition_Municipal_Field>();
        private List<FORM_Application_Municipal_Field_Data> ApplicationMunicipalFields = new List<FORM_Application_Municipal_Field_Data>();
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
        private bool FilterWindowVisible { get; set; } = false;
        private string? CurrentWizardTitle { get; set; }
        private Guid? StartStatus { get; set; }
        private bool ShowResponsibleWindow { get; set; } = false;
        private AUTH_Municipality? Municipality { get; set; }
        public string[] Subdomains { get; set; } = new string[] { "a", "b", "c" };
        public string UrlTemplate { get; set; } = "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png";
        public string Attribution { get; set; } = "&copy; <a href='https://osm.org/copyright'>OpenStreetMap contributors</a>";
        private bool ChangeStatusWindowVisible { get; set; } = false;
        private bool ShowRessourceUploadFlag { get; set; } = false;
        private bool ShowRequestPaymentFlag { get; set; } = false;
        private FORM_Application_Ressource? CurrentAppRessource;
        private List<PAY_Transaction_Position> TransPositions = new List<PAY_Transaction_Position>();
        private int? SelectedPagoPaIdentifierId = null;
        private bool PayBollo { get; set; } = false;
        private List<TASK_Task_Responsible?> TaskResponsibleList { get; set; }
        private bool ResponsibleSelectionVisibile = false;
        private bool ChangeManteinanceVisibile = false;
        private bool ShowDocumentTemplateWindow = false;
        private List<FILE_FileInfo> DynamicFiles = new List<FILE_FileInfo>();
        private bool HasCommitteeRights = false;

        private List<PAY_PagoPa_Identifier> PagoPaIdentifiers = new List<PAY_PagoPa_Identifier>();
        
        protected override async Task OnInitializedAsync()
        {
            AllowedAuthorities = await GetAuthorities();

            HasCommitteeRights = AuthProvider.HasUserRole(AuthRoles.Committee) ||
                                 AuthProvider.HasUserRole(AuthRoles.Developer);
            
            if (FormAdministrationHelper.Filter != null)
            {
                Filter = FormAdministrationHelper.Filter;
            }

            if(ActiveIndex != null)
            {
                ApplicationTabIndex = int.Parse(ActiveIndex);
            }

            CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_OVERVIEW");

            Data = await GetData(Filter);
            PriorityList = await FormApplicationProvider.GetPriorities();
            PagoPaIdentifiers = await PAYProvider.GetAllPagoPaApplicaitonsIdentifiers(SessionWrapper.AUTH_Municipality_ID);
            Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

            IsDataBusy = false;
            IsWizardBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            //ApplicationTabIndex = 0;
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_FORM_ADMINISTRATION");
            SessionWrapper.PageSubTitle = TextProvider.Get("MAINMENU_BACKEND_FORM_ADMINISTRATION_SUBTITLE");

            if(ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Backend/Form/Administration");
                StateHasChanged();
                return;
            }

            IsDataBusy = true;
            IsWizardBusy = true;
            StateHasChanged();

            CurrentApplication = await GetCurrentApplication();

            if(CurrentApplication != null)
            {
                CurrentDefinition = await GetCurrentDefinition();
                CurrentApplicationUploads = await GetCurrentApplicationUpload();
                CurrentApplicationFileUploads = await GetCurrentApplicationUploadFiles();
                CurrentApplicationFieldData = await GetCurrentApplicationFieldData();
                CurrentStatusLogList = await GetCurrentApplicationStatusLog();
                CurrenTransactionList = await GetCurrentApplicationTransactionList();
                CurrentApplicationRessource = await GetCurrentApplicationRessource();
                DynamicFiles = await GetCurrentApplicationDynamicFiles();
                StatusList = GetStatusList();

                if (CurrentDefinition != null)
                {
                    DefinitionMunicipalFields = await GetCurrentDefinitionMunFields();
                    ApplicationMunicipalFields = await GetCurrentApplicationMunFieldData();
                }

                StartStatus = CurrentApplication.FORM_Application_Status_ID;
            }


            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Form/Administration", "MAINMENU_BACKEND_FORM_ADMINISTRATION", null, null);

            if (CurrentDefinition != null)
            {
                CrumbService.AddBreadCrumb(NavManager.Uri, CurrentDefinition.FORM_Name, null, null, true);

                CurrentDefinitionUploads = await GetCurrentDefinitionUpload();
            }
            
            if (CurrentDefinition != null && CurrentDefinition.HasTasks == true)
            {
                if (CurrentDefinition != null)
                {
                    var contextName = "";
                    if (CurrentApplication != null && CurrentDefinition != null)
                    {
                        if (CurrentDefinition.FORM_Name != null && CurrentDefinition.FORM_Name.Length > 45)
                        {
                            contextName = CurrentDefinition.FORM_Name + "..." + " - " + CurrentApplication.FirstName + " " + CurrentApplication.LastName;
                        }
                        else
                        {
                            contextName = CurrentDefinition.FORM_Name + " - " + CurrentApplication.FirstName + " " + CurrentApplication.LastName;
                        }
                    }
                    else
                    {
                        if (CurrentApplication != null)
                        {
                            contextName = CurrentApplication.FirstName + " " + CurrentApplication.LastName;
                        }
                    }

                    if (CurrentApplication.Mantainance_Title != null)
                    {
                        contextName = CurrentApplication.Mantainance_Title + " - " + CurrentApplication.FirstName + " " + CurrentApplication.LastName;
                    }
                    var notifyCreator = CurrentApplication.IsMunicipal ||
                                        (CurrentApplication.IsMunicipalCommittee ?? false);
                    await TaskService.SetContext(CurrentApplication.Mantainance_Title == null ? 1 : 2, ID, contextName, notifyCreator, CurrentApplication.LegalDeadline ?? CurrentApplication.EstimatedDeadline);    //APPLICATION BACKEND CONTEXT
                }
                else
                {
                    var contextName = CurrentApplication.Mantainance_Title + " - " + CurrentApplication.FirstName + " " + CurrentApplication.LastName;
                    var notifyCreator = CurrentApplication.IsMunicipal ||
                                        (CurrentApplication.IsMunicipalCommittee ?? false);
                    await TaskService.SetContext(CurrentApplication.Mantainance_Title == null ? 1 : 2, ID, contextName, notifyCreator, CurrentApplication.LegalDeadline ?? CurrentApplication.EstimatedDeadline);    //MAINTENANCE BACKEND CONTEXT
                }
            }
           

            if (CurrentApplication != null && CurrentApplication.TASK_Task_ID == null)
            {
                CurrentApplication.TASK_Task_ID = Guid.NewGuid();
                TaskResponsibleList = new List<TASK_Task_Responsible?>();
            }
            else
            {
                TaskResponsibleList = (await TaskProvider.GetTaskResponsibleList(CurrentApplication.TASK_Task_ID.Value)).ToList();
            }

            CheckApplicationTabIndex();
            
            IsDataBusy = false;
            IsWizardBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }

        private void CheckApplicationTabIndex()
        {
            if (ApplicationTabIndex == 4 && CurrentDefinition?.FORM_Definition_Category_ID == FORMCategories.Maintenance)
                ApplicationTabIndex = 0;

            if (ApplicationTabIndex == 7 && !(CurrentDefinition?.HasChat ?? false))
                ApplicationTabIndex = 0;

            if (ApplicationTabIndex == 8 && CurrentDefinition.FORM_Definition_Category_ID == FORMCategories.Maintenance)
                ApplicationTabIndex = 0;
        }
        private async Task<FORM_Application?> GetCurrentApplication()
        {
            return await FormApplicationProvider.GetApplication(Guid.Parse(ID));
        }
        private async Task<FORM_Definition?> GetCurrentDefinition()
        {
            return await FormDefinitionProvider.GetDefinition(CurrentApplication.FORM_Definition_ID.Value);
        }
        private async Task<List<FORM_Application_Upload>> GetCurrentApplicationUpload()
        {
            return await FormApplicationProvider.GetApplicationUploadList(Guid.Parse(ID));
        }
        private async Task<List<FORM_Application_Upload_File>> GetCurrentApplicationUploadFiles()
        {
            var filesList = new List<FORM_Application_Upload_File>();

            foreach (var up in CurrentApplicationUploads)
            {
                filesList.AddRange(await FormApplicationProvider.GetApplicationUploadFileList(up.ID));
            }

            return filesList;
        }
        private async Task<List<FORM_Application_Field_Data>> GetCurrentApplicationFieldData()
        {
            return await FormApplicationProvider.GetApplicationFieldDataMunicipalList(CurrentApplication.ID);
        }
        private async Task<List<FORM_Definition_Upload>> GetCurrentDefinitionUpload()
        {
            return await FormDefinitionProvider.GetDefinitionUploadList(CurrentDefinition.ID);
        }
        private async Task<List<FORM_Application_Status_Log>> GetCurrentApplicationStatusLog()
        {
            var locList = await FormApplicationProvider.GetApplicationStatusLogList(CurrentApplication.ID);

            if (CurrentApplication.CreatedAt != null)
            {
                locList.Add(new FORM_Application_Status_Log()
                {
                    AUTH_Users_ID = CurrentApplication.AUTH_Users_ID,
                    User = CurrentApplication.FirstName + " " + CurrentApplication.LastName,
                    ChangeDate = CurrentApplication.CreatedAt,
                    StatusIcon = "fa-regular fa-file-plus",
                    Status = TextProvider.Get("FORM_USER_DETAIL_CREATED_STATUS")
                });
            }

            if (CurrentApplication.SubmitAt != null)
            {
                var CommittedStatus = StatusList.FirstOrDefault(p => p.ID == FORMStatus.Comitted);

                if (CommittedStatus != null)
                {
                    locList.Add(new FORM_Application_Status_Log()
                    {
                        AUTH_Users_ID = CurrentApplication.AUTH_Users_ID,
                        User = CurrentApplication.FirstName + " " + CurrentApplication.LastName,
                        ChangeDate = CurrentApplication.SubmitAt,
                        StatusIcon = CommittedStatus.Icon,
                        Status = CommittedStatus.Name
                    });
                }
            }

            return locList;
        }
        private async Task<List<FORM_Application_Responsible>> GetCurrentApplicationResponsible()
        {
            return await FormApplicationProvider.GetApplicationResponsibleListByApplication(CurrentApplication.ID);
        }
        private async Task<List<FORM_Application_Ressource>> GetCurrentApplicationRessource()
        {
            var ressources = await FormApplicationProvider.GetApplicationRessourceList(CurrentApplication.ID);

            if (ressources != null)
            {
                return ressources.ToList();
            }

            return new List<FORM_Application_Ressource>();
        }
        private async Task<List<V_FORM_Definition_Municipal_Field>> GetCurrentDefinitionMunFields()
        {
            var data = await FormDefinitionProvider.GetDefinitionMunFieldList(CurrentDefinition.ID, LangProvider.GetCurrentLanguageID());

            if (data != null)
            {
                return data.ToList();
            }

            return new List<V_FORM_Definition_Municipal_Field>();
        }
        private async Task<List<FORM_Application_Municipal_Field_Data>> GetCurrentApplicationMunFieldData()
        {
            var data = await FormApplicationProvider.GetFormApplicationMunicipalFieldList(CurrentApplication.ID);

            if (data != null)
            {
                if(DefinitionMunicipalFields != null && DefinitionMunicipalFields.Count() > 0)
                {
                    var itemsToAdd = DefinitionMunicipalFields.Where(p => !data.Select(p => p.FORM_Definition_Municipal_Field_ID).Contains(p.ID));

                    foreach(var item in itemsToAdd)
                    {
                        var newItem = new FORM_Application_Municipal_Field_Data();

                        newItem.ID = Guid.NewGuid();
                        newItem.FORM_Definition_Municipal_Field_ID = item.ID;
                        newItem.FORM_Application_ID = CurrentApplication.ID;
                        newItem.FORM_Definition_Municipal_Field_Type_ID = item.FORM_Definition_Municipal_Field_Type_ID;
                        newItem.SortOrder = item.SortOrder;

                        var extendedItemsDE = await FormDefinitionProvider.GetDefinitionMunFieldExtendedList(item.ID, Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"));

                        if(extendedItemsDE != null && extendedItemsDE.Count() > 0)
                        {
                            newItem.DescriptionDE = extendedItemsDE.FirstOrDefault().Description;

                        }

                        var extendedItemsIT = await FormDefinitionProvider.GetDefinitionMunFieldExtendedList(item.ID, Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"));

                        if (extendedItemsIT != null && extendedItemsIT.Count() > 0)
                        {
                            newItem.DescriptionIT = extendedItemsIT.FirstOrDefault().Description;

                        }

                        data.Add(newItem);
                    }
                }

                return data.ToList();
            }

            return new List<FORM_Application_Municipal_Field_Data>();
        }
        private async Task<List<FILE_FileInfo>> GetCurrentApplicationDynamicFiles()
        {
            return await FormApplicationProvider.GetApplicationUploadFiles(CurrentApplication.ID, CurrentDefinition.ID);
        }
        private async Task<List<Guid?>> GetCurrentApplicationTransactionList()
        {
            var data = await FormApplicationProvider.GetApplicationTransactionList(CurrentApplication.ID);

            return data.Select(p => p.PAY_Transaction_ID).ToList();
        }
        private List<FORM_Application_Status> GetStatusList()
        {
            var data = FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            data = data.Where(e => e.UserSelectable || e.ID == CurrentApplication.FORM_Application_Status_ID)
                .ToList();
            return data;
        }
        private async Task<List<Guid>> GetAuthorities()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                var userAuthorities = await AuthProvider.GetUserAuthorities(SessionWrapper.CurrentUser.ID);

                return userAuthorities.Where(p => p.AUTH_Authority_ID != null).Select(p => p.AUTH_Authority_ID.Value).ToList();
            }

            return new List<Guid>();
        }
        private async Task<List<V_FORM_Application>> GetData(Administration_Filter_Item? Filter)
        {
            if (SessionWrapper.AUTH_Municipality_ID != null && SessionWrapper.CurrentUser != null)
            {
                var applications = await FormApplicationProvider.GetApplications(SessionWrapper.AUTH_Municipality_ID.Value, SessionWrapper.CurrentUser.ID, AllowedAuthorities, Filter);

                FormAdministrationHelper.Filter = Filter;

                return applications;
            }

            return new List<V_FORM_Application>();
        }
        private async void FilterSearch(Administration_Filter_Item Filter)
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
        private void SelectApplication(Guid? ID)
        {
            if (ID != null && (CurrentApplication == null || ID != CurrentApplication.ID))
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                NavManager.NavigateTo("/Backend/Form/Detail/" + ID.ToString());
            }
        }
        private void BackToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/Form/Administration");
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
        private void OnStepChanged()
        {
            IsWizardBusy = true;
            StateHasChanged();
        }
        private void OnTabIndexChanged(int Index)
        {
            if(CurrentDefinition != null && CurrentDefinition.FORM_Definition_Category_ID != FORMCategories.Maintenance)
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
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_NOTES");                    
                }
                else if (Index == 3)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_DETAILS");
                }
                else if (Index == 4)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_PREVIEW");
                }
                else if (Index == 5)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_DOCUMENTS");
                }
                else if (Index == 6)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_STATUS_LOG");
                }
                else if (Index == 7)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_CHAT");
                }
                else if (Index == 8)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_PAYMENTS");
                }
                else if (Index == 9)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_INTERNAL_ARCHIVE");
                }
            }
            else
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
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_NOTES");
                }
                else if (Index == 3)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_DETAILS");
                }
                else if (Index == 4)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_DOCUMENTS");
                }
                else if (Index == 5)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_STATUS_LOG");
                }
                else if (Index == 6)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_TAB_CHAT");
                }
                else if (Index == 7)
                {
                    CurrentWizardTitle = TextProvider.Get("BACKEND_FORM_DETAIL_APPLICATION_INTERNAL_ARCHIVE");
                }
            }

            IsWizardBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ChangeStatus()
        {
            if (CurrentApplication != null)
            {
                var status = StatusList.FirstOrDefault(p => p.ID == CurrentApplication.FORM_Application_Status_ID);

                if (status != null)
                {
                    if (status.GeneratesPDF == true && ShowDocumentTemplateWindow != true)
                    {
                        ShowDocumentTemplateWindow = true;
                        StateHasChanged();
                    }
                    else
                    {
                        BusyIndicatorService.IsBusy = true;
                        StateHasChanged();

                        if (!await Dialogs.ConfirmAsync(TextProvider.Get("APPLICATION_STATUS_CHANGE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                        {
                            BusyIndicatorService.IsBusy = false;
                            await InvokeAsync(() => StateHasChanged());
                            StateHasChanged();
                            return false;
                        }

                        var StatusLog = new FORM_Application_Status_Log();

                        StatusLog.ID = Guid.NewGuid();

                        StatusLog.FORM_Application_ID = CurrentApplication.ID;
                        StatusLog.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                        StatusLog.FORM_Application_Status_ID = CurrentApplication.FORM_Application_Status_ID;
                        StatusLog.ChangeDate = DateTime.Now;

                        await FormApplicationProvider.SetApplication(CurrentApplication);
                        await FormApplicationProvider.SetApplicationStatusLog(StatusLog);

                        var Languages = await LangProvider.GetAll();

                        if (Languages != null)
                        {
                            foreach (var l in Languages)
                            {
                                var dataE = new FORM_Application_Status_Log_Extended()
                                {
                                    FORM_Application_Status_Log_ID = StatusLog.ID,
                                    LANG_Languages_ID = l.ID
                                };

                                if (l.ID == Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"))  //DE
                                {
                                    dataE.Title = TextProvider.Get(status.TEXT_SystemTexts_Code, LanguageSettings.German);
                                    dataE.Reason = TextDocumentItem.German;
                                }
                                else if (l.ID == Guid.Parse("e450421a-baff-493e-a390-71b49be6485f")) //IT
                                {
                                    dataE.Title = TextProvider.Get(status.TEXT_SystemTexts_Code, LanguageSettings.Italian);
                                    dataE.Reason = TextDocumentItem.Italian;
                                }
                                else
                                {
                                    dataE.Title = TextProvider.Get(status.TEXT_SystemTexts_Code, LanguageSettings.German);
                                    dataE.Reason = TextDocumentItem.German;
                                }

                                await FormApplicationProvider.SetApplicationStatusLogExtended(dataE);
                            }
                        }

                        CurrentStatusLogList = await GetCurrentApplicationStatusLog();

                        FILE_FileInfo? FileDE = null;
                        FILE_FileInfo? FileIT = null;

                        if (CurrentDefinition != null && CurrentApplication.FORM_Application_Status_ID != null)
                        {
                            var currentStatus = StatusList.FirstOrDefault(p => p.ID == CurrentApplication.FORM_Application_Status_ID.Value);

                            if (currentStatus != null && currentStatus.GeneratesPDF == true && CurrentApplication.FILE_Fileinfo_ID != null && SessionWrapper.AUTH_Municipality_ID != null && Languages != null)
                            {
                                if (!string.IsNullOrEmpty(TextDocumentItem.German))
                                {
                                    FileDE = await CreateResponseFile(CurrentApplication, CurrentDefinition, Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"), TextProvider.Get("NOTIF_MANTAINANCE_STATUS_CHANGED_TITLE", LanguageSettings.German));
                                }

                                if (!string.IsNullOrEmpty(TextDocumentItem.Italian))
                                {
                                    FileIT = await CreateResponseFile(CurrentApplication, CurrentDefinition, Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"), TextProvider.Get("NOTIF_MANTAINANCE_STATUS_CHANGED_TITLE", LanguageSettings.Italian));
                                }

                                CurrentApplicationRessource = await GetCurrentApplicationRessource();
                            }

                            if (currentStatus != null && currentStatus.FinishedStatus == true)
                            {
                                CurrentApplication.Archived = DateTime.Now;

                                await FormApplicationProvider.SetApplication(CurrentApplication);
                            }
                        }

                        if (CurrentApplication.AUTH_Users_ID != null && CurrentDefinition != null)
                        {
                            var userLang = await AuthProvider.GetSettings(CurrentApplication.AUTH_Users_ID.Value);

                            if (CurrentApplication.AUTH_Users_ID != null && CurrentApplication.AUTH_Municipality_ID != null)
                            {
                                MSG_Message? msg = null;

                                string text = "";
                                Guid userLangID = LanguageSettings.German;

                                if (userLang != null && userLang.LANG_Languages_ID == LanguageSettings.Italian)
                                {
                                    if (!string.IsNullOrEmpty(TextItem.Italian))
                                    {
                                        text = await TextProvider.ReplaceGeneralKeyWords(CurrentApplication.AUTH_Users_ID.Value, TextItem.Italian);
                                    }

                                    text = await FormApplicationProvider.ReplaceKeywords(CurrentApplication.ID, LanguageSettings.Italian, text, StartStatus);

                                    userLangID = LanguageSettings.Italian;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(TextItem.German))
                                    {
                                        text = await TextProvider.ReplaceGeneralKeyWords(CurrentApplication.AUTH_Users_ID.Value, TextItem.German);
                                    }

                                    text = await FormApplicationProvider.ReplaceKeywords(CurrentApplication.ID, LanguageSettings.German, text, StartStatus);
                                }


                                if (CurrentDefinition.FORM_Definition_Category_ID == FORMCategories.Maintenance)
                                {
                                    msg = await MessageService.GetMessage(CurrentApplication.AUTH_Users_ID.Value, CurrentApplication.AUTH_Municipality_ID.Value, text,
                                                                          TextProvider.Get("NOTIF_MANTAINANCE_STATUS_CHANGED_SHORTTEXT", userLangID),
                                                                          TextProvider.Get("NOTIF_MANTAINANCE_STATUS_CHANGED_TITLE", userLangID),
                                                                          Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), false, null);
                                }
                                else
                                {
                                    msg = await MessageService.GetMessage(CurrentApplication.AUTH_Users_ID.Value, CurrentApplication.AUTH_Municipality_ID.Value, text,
                                                                          TextProvider.Get("NOTIF_APPLICAITON_STATUS_CHANGED_SHORTTEXT", userLangID),
                                                                          TextProvider.Get("NOTIF_APPLICAITON_STATUS_CHANGED_TITLE", userLangID),
                                                                          Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), false, null);
                                }

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

                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Form/Application/UserDetails/" + CurrentApplication.ID, AttachmentsList);
                                }
                            }
                        }

                        StartStatus = StatusLog.FORM_Application_Status_ID;
                        StateHasChanged();

                        await InvokeAsync(() => StateHasChanged());

                        ChangeStatusWindowVisible = false;
                        StateHasChanged();

                        BusyIndicatorService.IsBusy = false;
                        StateHasChanged();
                    }
                }
            }

            return true;
        }
        private async Task<FILE_FileInfo?> CreateResponseFile(FORM_Application App, FORM_Definition Def, Guid LANG_Language_ID, string FileName)
        {
            if(App == null || Def == null)
            {
                return null;
            }

            var currentApp = FileProvider.GetFileStorage(App.FILE_Fileinfo_ID.Value);

            var resultStream = new MemoryStream();

            if (currentApp != null && currentApp.FileImage != null)
            {
                bool HasDynamicSection = true;

                var FieldData = await FormApplicationProvider.GetApplicationFieldDataList(App.ID);

                if (FieldData == null || FieldData.Count() == 0)
                {
                    HasDynamicSection = false;
                }

                var ReportName = await FormReportRendererHelper.ExecuteReport(Def.ID, App.ID);

                var ApplicationPDFStream = FormReportPrintHelper.GetPDF(ReportName, LANG_Language_ID.ToString(), SessionWrapper.AUTH_Municipality_ID.Value.ToString(), App.ID.ToString(), HasDynamicSection);
                var ResponsePDFStream = FormReportPrintHelper.GetResponsePDF(LANG_Language_ID.ToString(), SessionWrapper.AUTH_Municipality_ID.Value.ToString(), App.ID.ToString());

                //GERNERATE PDF AND SAVE
                using (PdfDocument one = PdfReader.Open(ResponsePDFStream, PdfDocumentOpenMode.Import))
                using (PdfDocument two = PdfReader.Open(ApplicationPDFStream, PdfDocumentOpenMode.Import))
                using (PdfDocument outPdf = new PdfDocument())
                {
                    FormReportPrintHelper.CopyPages(one, outPdf);
                    FormReportPrintHelper.CopyPages(two, outPdf);

                    outPdf.Save(resultStream);
                }
            }
            else
            {
                resultStream = FormReportPrintHelper.GetResponsePDF(LANG_Language_ID.ToString(), SessionWrapper.AUTH_Municipality_ID.Value.ToString(), App.ID.ToString());
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

            var ress = new FORM_Application_Ressource();
            ress.ID = Guid.NewGuid();
            ress.FORM_Application_ID = App.ID;
            ress.CreationDate = DateTime.Now;
            ress.FILE_FileInfo_ID = f.ID;
            ress.UserUpload = false;

            await FormApplicationProvider.SetApplicationRessource(ress);

            return f;
        }
        private async Task<bool> TransactionCreated(Guid PAY_Transcation_ID)
        {
            var isNewTransaction =
                (await FormApplicationProvider.GetApplicationTransactionList(CurrentApplication.ID)).All(e =>
                    e.PAY_Transaction_ID != PAY_Transcation_ID);
            if (!isNewTransaction)
                return false;
            
            var newItem = new FORM_Application_Transactions()
            {
                ID = Guid.NewGuid(),
                FORM_Application_ID = CurrentApplication.ID,
                PAY_Transaction_ID = PAY_Transcation_ID
            };
            
            await FormApplicationProvider.SetApplicationTransaction(newItem);
            CurrenTransactionList =  await GetCurrentApplicationTransactionList();
            
            if (CurrentApplication != null)
            {
                var msg = await GetNewTransactionCreatedMessage(PAY_Transcation_ID);
                if (msg != null)
                {
                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Form/Application/UserDetails/" + CurrentApplication.ID);

                    return true;
                }
            }

            StateHasChanged();
            return true;
        }

        private async Task<MSG_Message?> GetNewTransactionCreatedMessage(Guid transactionId)
        {
            var transaction = await PAYProvider.GetTransaction(transactionId);
            if (transaction == null) return null;
            var msg = await MessageService.GetMessage(CurrentApplication.AUTH_Users_ID.Value, CurrentApplication.AUTH_Municipality_ID.Value,
                "NEW_PAYMENT_TEXT", "NEW_PAYMENT_SHORTTEXT", "NEW_PAYMENT_TITLE",
                Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true);
            var formCreatorUserId = CurrentApplication.AUTH_Users_ID;
            if (msg != null && formCreatorUserId != null && CurrentDefinition != null)
            {
                var lang = (await AuthProvider.GetSettings(formCreatorUserId.Value))?.LANG_Languages_ID ?? LangProvider.GetCurrentLanguageID();
                var definitionExtended = (await FormDefinitionProvider.GetDefinitionExtendedList(CurrentDefinition.ID, lang)).FirstOrDefault();
                var name = definitionExtended?.Name ?? "";
                msg.Subject = msg.Subject.Replace("{FormName}", name);
                msg.Messagetext = msg.Messagetext.Replace("{FormName}", name);
                var transactionType = await PAYProvider.GetType(transaction.PAY_Type_ID ?? Guid.Empty);
                var transactionTypeName = transactionType == null
                    ? ""
                    : TextProvider.Get(transactionType.TEXT_SystemTexts_Code, lang);
                
                msg.Messagetext = msg.Messagetext.Replace("{PayType}", transactionTypeName);
                msg.Messagetext = msg.Messagetext.Replace("{TotalAmount}", transaction.TotalAmount?.ToString("C"));

                var positionsString = "<table class='task-table'>{PositionContent}</table>";
                var positions = await PAYProvider.GetTransactionPositionList(transactionId);
                var rows = "";
                foreach (var pos in positions)
                {
                    var row = "<tr><td style='padding-right: 40px'>{desc}</td><td valign='top' align='right'>{amount}</td></tr>";
                    row = row.Replace("{desc}", pos.Description);
                    row = row.Replace("{amount}", pos.Amount?.ToString("C"));
                    rows += row;
                }
                positionsString = positionsString.Replace("{PositionContent}", rows);
                msg.Messagetext = msg.Messagetext.Replace("{PositionsTable}", positionsString);
                msg.Messagetext = msg.Messagetext.Replace("{Link}",
                    NavManager.BaseUri + "Form/Application/UserDetails/" + CurrentApplication.ID);
            }

            return msg;
        }
        private async void ArchivedChanged()
        {
            if(CurrentApplication != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if(CurrentApplication.FORM_Application_Status_ID != StartStatus)
                {
                    CurrentApplication.FORM_Application_Status_ID = StartStatus;
                }

                await FormApplicationProvider.SetApplication(CurrentApplication);

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private async Task<bool> FileRemoved(Guid FILE_Fileinfo_ID)
        {
            if (CurrentApplication != null)
            {
                var ressources = await FormApplicationProvider.GetApplicationRessourceList(CurrentApplication.ID);

                if (ressources != null)
                {
                    var locFile = ressources.FirstOrDefault(p => p.FILE_FileInfo_ID == FILE_Fileinfo_ID);

                    if (locFile != null)
                    {
                        await FileProvider.RemoveFileInfo(FILE_Fileinfo_ID);
                    }
                }

                StateHasChanged();
            }
            return true;
        }
        private async Task<bool> RemoveUserRessource(Guid FILE_Fileinfo_ID)
        {
            if (CurrentApplication != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("APPLICATION_REMOVE_FILE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                {
                    BusyIndicatorService.IsBusy = false;
                    await InvokeAsync(() => StateHasChanged());
                    StateHasChanged();
                    return false;
                }


                var ressources = await FormApplicationProvider.GetApplicationRessourceList(CurrentApplication.ID);

                if (ressources != null)
                {
                    var locFile = ressources.FirstOrDefault(p => p.FILE_FileInfo_ID == FILE_Fileinfo_ID);


                    if (locFile != null)
                    {
                        if (locFile.PAY_Transaction_ID != null)
                        {
                            var payment = await PAYProvider.GetTransaction(locFile.PAY_Transaction_ID.Value);

                            if(payment != null)
                            {
                                await PAYProvider.RemoveTransaction(payment.ID);
                            }
                        }

                        await FormApplicationProvider.RemoveApplicationRessource(locFile.ID);
                        await FileProvider.RemoveFileInfo(FILE_Fileinfo_ID);
                    }
                }

                CurrentApplicationRessource = await GetCurrentApplicationRessource();

                StateHasChanged();
            }
            return true;
        }
        private async Task<bool> SaveRessources()
        {
            if (CurrentApplication != null)
            {
                var ressources = await FormApplicationProvider.GetApplicationRessourceList(CurrentApplication.ID);

                foreach(var f in CurrentUserUpload)
                {
                    await FileProvider.SetFileInfo(f);

                    FORM_Application_Ressource? existing = null;

                    if(ressources != null)
                    {
                        existing = ressources.FirstOrDefault(p => p.FILE_FileInfo_ID == f.ID);
                    }

                    if (existing == null)
                    {
                        existing = new FORM_Application_Ressource();
                        existing.ID = Guid.NewGuid();
                        existing.FORM_Application_ID = CurrentApplication.ID;
                        existing.CreationDate = DateTime.Now;
                        existing.FILE_FileInfo_ID = f.ID;
                        existing.UserUpload = true;
                    }

                    await FormApplicationProvider.SetApplicationRessource(existing);
                }

                ShowRessourceUploadFlag = false;

                CurrentApplicationRessource = await GetCurrentApplicationRessource();

                StateHasChanged();
            }

            return true;
        }
        private void ShowRessourceUpload()
        {
            CurrentUserUpload = new List<FILE_FileInfo>();

            ShowRessourceUploadFlag = true;
            StateHasChanged();
        }
        private void CancelRessourceUpload()
        {
            ShowRessourceUploadFlag = false;
            StateHasChanged();
        }
        private void ShowChangeStatusWindow()
        {
            if (CurrentApplication != null)
            {
                StartStatus = CurrentApplication.FORM_Application_Status_ID;
                TextItem = new TextItem();
                TextDocumentItem = new TextItem();

                ChangeStatusWindowVisible = true;
                StateHasChanged();
            }
        }
        private void CloseChangeStatusWindow()
        {
            ChangeStatusWindowVisible = false;
            ShowDocumentTemplateWindow = false;
            if (CurrentApplication != null)
            {
                CurrentApplication.FORM_Application_Status_ID = StartStatus;
            }
            StateHasChanged();
        }
        private void ShowRequestPayment(FORM_Application_Ressource Ressource)
        {
            CurrentAppRessource = Ressource;
            PayBollo = false;
            TransPositions = new List<PAY_Transaction_Position>();
            
            if (PagoPaIdentifiers.Any(e => e.ID == CurrentDefinition?.DefaultPagoPaIdentifierId))
                SelectedPagoPaIdentifierId = CurrentDefinition?.DefaultPagoPaIdentifierId;
            else
                SelectedPagoPaIdentifierId = null;

            ShowRequestPaymentFlag = true;
            StateHasChanged();
        }
        private void HideRequestPayment()
        {
            CurrentAppRessource = null;

            ShowRequestPaymentFlag = false;
            StateHasChanged();
        }
        private async Task<bool> RequestPayment()
        {
            if (CurrentAppRessource != null && (PayBollo == true || TransPositions!= null && TransPositions.Count() > 0))
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (CurrentApplication != null && CurrentDefinition != null && CurrentAppRessource != null && CurrentAppRessource.FILE_FileInfo_ID != null)
                {
                    if (!await Dialogs.ConfirmAsync(TextProvider.Get("REQUEST_BOLLO_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    {
                        BusyIndicatorService.IsBusy = false;
                        await InvokeAsync(() => StateHasChanged());
                        StateHasChanged();
                        return false;
                    }

                    var fileStorage = await FileProvider.GetFileStorageAsync(CurrentAppRessource.FILE_FileInfo_ID.Value);

                    if (fileStorage != null)
                    {
                        var trans = new PAY_Transaction();

                        trans.ID = Guid.NewGuid();
                        trans.AUTH_Municipality_ID = CurrentApplication.AUTH_Municipality_ID;
                        trans.AUTH_Users_ID = CurrentApplication.AUTH_Users_ID.Value;
                        trans.CreationDate = DateTime.Now;
                        trans.Description = CurrentDefinition.FORM_Name + " - " + TextProvider.Get("PAYMENT_REQUEST_DESCRIPTION");
                        trans.PAY_Type_ID = Guid.Parse("b16d8119-e050-46c8-bb81-a1d08891f298"); //STANDARD
                        trans.ComunixSource = "FORMS_FILES";

                        await PAYProvider.SetTransaction(trans);

                        decimal? sum = 0;

                        if (PayBollo == true)
                        {
                            var transPos = new PAY_Transaction_Position();

                            transPos.ID = Guid.NewGuid();
                            transPos.PAY_Transaction_ID = trans.ID;
                            transPos.BolloNumber = DateTime.Now.Second + "" + DateTime.Now.Day + "" + DateTime.Now.Year + "" + DateTime.Now.Month + "" + DateTime.Now.Millisecond + "" + DateTime.Now.Minute + "" + DateTime.Now.Hour;
                            transPos.BolloCreationDate = DateTime.Now;
                            transPos.Description = TextProvider.Get("FORM_PAYMENT_BOLLO_DESCRIPTION") + " - " + transPos.BolloNumber + " - 16,00 €";
                            transPos.IsBollo = true;

                            var pagoPaIdentifier =
                                PagoPaIdentifiers.FirstOrDefault(e => e.ID == SelectedPagoPaIdentifierId);
                            transPos.PagoPA_Identification = pagoPaIdentifier?.PagoPA_Identifier;
                            transPos.TipologiaServizio = pagoPaIdentifier?.TipologiaServizio;
                            transPos.BolloCreationDate = DateTime.Now;

                            transPos.BolloFILE_FileInfo_ID = fileStorage.FILE_FileInfo_ID;

                            transPos.BolloHashType = 0;
                            transPos.Amount = 16;

                            sum += transPos.Amount;

                            await PAYProvider.SetTransactionPosition(transPos);
                        }

                        if(TransPositions != null && TransPositions.Count() > 0)
                        {
                            foreach(var pos in TransPositions)
                            {
                                var pagoPaIdentifier =
                                    PagoPaIdentifiers.FirstOrDefault(e => e.ID == SelectedPagoPaIdentifierId);
                                pos.ID = Guid.NewGuid();
                                pos.PAY_Transaction_ID = trans.ID;
                                pos.PagoPA_Identification = pagoPaIdentifier?.PagoPA_Identifier;
                                pos.TipologiaServizio = pagoPaIdentifier?.TipologiaServizio;
                                sum += pos.Amount;

                                await PAYProvider.SetTransactionPosition(pos);
                            }
                        }

                        trans.TotalAmount = sum;

                        await PAYProvider.SetTransaction(trans);

                        FORM_Application_Transactions appTrans = new FORM_Application_Transactions();

                        appTrans.ID = Guid.NewGuid();
                        appTrans.PAY_Transaction_ID = trans.ID;
                        appTrans.FORM_Application_ID = CurrentApplication.ID;

                        await FormApplicationProvider.SetApplicationTransaction(appTrans);

                        CurrenTransactionList = await GetCurrentApplicationTransactionList();

                        CurrentAppRessource.BolloRequested = true;
                        CurrentAppRessource.PAY_Transaction_ID = trans.ID;

                        await FormApplicationProvider.SetApplicationRessource(CurrentAppRessource);

                        if (CurrentApplication.AUTH_Users_ID != null && CurrentDefinition != null)
                        {
                            var userLang = await AuthProvider.GetSettings(CurrentApplication.AUTH_Users_ID.Value);

                            if (CurrentApplication.AUTH_Users_ID != null && CurrentApplication.AUTH_Municipality_ID != null)
                            {
                                var msg = await GetNewTransactionCreatedMessage(trans.ID);

                                if (msg != null)
                                {
                                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Form/Application/UserDetails/" + CurrentApplication.ID);
                                }
                            }
                        }
                    }
                }

                HideRequestPayment();
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }

            return false;
        }
        private void AddPosition() 
        {
            if (TransPositions != null)
            {
                TransPositions.Add(new PAY_Transaction_Position()
                {
                    ID = Guid.NewGuid(),
                    Amount = 0
                });

                StateHasChanged();
            }
        }
        private void RemovePosition(Guid ID)
        {
            if (TransPositions != null)
            {
                var itemToRemove = TransPositions.FirstOrDefault(p => p.ID == ID);

                if(itemToRemove != null)
                {
                    TransPositions.Remove(itemToRemove);
                }

                StateHasChanged();
            }
        }
        private void ShowResponsibleSelection()
        {
            ResponsibleSelectionVisibile = !ResponsibleSelectionVisibile;
            StateHasChanged();
        }
        private async Task<bool> ResponsibleQuickAdd(TASK_Task_Responsible Responsible)
        {
            if (CurrentApplication != null && CurrentApplication.TASK_Task_ID != null && CurrentDefinition != null && CurrentDefinition.AUTH_Authority_ID != null)
            {
                var existingTask = await TaskProvider.GetTask(CurrentApplication.TASK_Task_ID.Value);

                if (existingTask == null)
                {
                    DateTime? deadline = null;

                    if(CurrentApplication != null && CurrentApplication.LegalDeadline != null)
                    {
                        deadline = CurrentApplication.LegalDeadline;
                    }
                    else if (CurrentApplication != null && CurrentApplication.EstimatedDeadline != null)
                    {
                        deadline = CurrentApplication.EstimatedDeadline;
                    }
                    existingTask = await TaskService.CreateTask(TaskService.TASK_Context_ID, TaskService.ContextElementID, SessionWrapper.CurrentUser.ID, false, CurrentDefinition.AUTH_Authority_ID.ToString(), TaskService.ContextName, NavManager.Uri, deadline, null, CurrentApplication.TASK_Task_ID);
                    await FormApplicationProvider.SetApplication(CurrentApplication);
                }

                await TaskService.SetResponsible(Responsible);
            }

            ResponsibleSelectionVisibile = false;
            StateHasChanged();

            return true;
        }
        private async Task<bool> ResponsibleQuickRemove(TASK_Task_Responsible Responsible)
        {
            await TaskProvider.RemoveTaskResponsible(Responsible.ID);
            ResponsibleSelectionVisibile = false;
            StateHasChanged();
            return true;
        }
        private async Task<bool> ResponsibleRemove(TASK_Task_Responsible Responsible)
        {
            TaskResponsibleList.Remove(Responsible);

            await TaskProvider.RemoveTaskResponsible(Responsible.ID);
            ResponsibleSelectionVisibile = false;
            StateHasChanged();
            return true;
        }
        private void ResponsibleOverlayClicked()
        {
            ResponsibleSelectionVisibile = false;
            StateHasChanged();
        }
        private void EditManteinance()
        {
            ChangeManteinanceVisibile = true;
            StateHasChanged();
        }
        private void CloseChangeManteinance()
        {
            ChangeManteinanceVisibile = false;
            StateHasChanged();
        }
        private async void ChangeManteinance()
        {
            BusyIndicatorService.IsBusy = true;
            ChangeManteinanceVisibile = false;
            StateHasChanged();

            await Task.Delay(1);

            if (CurrentApplication != null && CurrentApplication.TASK_Task_ID != null && CurrentDefinition != null && CurrentDefinition.AUTH_Authority_ID != null)
            {
                var existingTask = await TaskProvider.GetTask(CurrentApplication.TASK_Task_ID.Value);

                if (existingTask == null)
                {
                    DateTime? deadline = null;

                    if (CurrentApplication != null && CurrentApplication.MunicipalDeadline != null)
                    {
                        deadline = CurrentApplication.MunicipalDeadline;
                    }

                    existingTask = await TaskService.CreateTask(TaskService.TASK_Context_ID, TaskService.ContextElementID, SessionWrapper.CurrentUser.ID, false, CurrentDefinition.AUTH_Authority_ID.ToString(), TaskService.ContextName, NavManager.Uri, deadline, null, CurrentApplication.TASK_Task_ID);
                }
                else
                {
                    existingTask.Deadline = CurrentApplication.MunicipalDeadline;

                    await TaskProvider.SetTask(existingTask);
                }
            }

            if (CurrentApplication != null)
            {
                await FormApplicationProvider.SetApplication(CurrentApplication);
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
    }
}