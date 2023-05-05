using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Components.FormRenderer;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Backend
{
    public partial class Application
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORM_ReportRendererHelper FormRendererHelper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IFILEProvider FILEProvider { get; set; }
        [Inject] ITEXTProvider TEXTProvider { get; set; }
        [Inject] ITASKService TaskService { get; set; }

        [Parameter] public string ID { get; set; }

        private AUTH_Authority? Authority { get; set; }
        private FORM_Definition? Definition { get; set; }
        private FORM_Application? Data { get; set; }
        private List<FORM_Application_Upload>? UploadElements { get; set; }
        private List<FORM_Definition_Upload>? UploadFilesDefinitions { get; set; }
        private Container? FormContainer { get; set; }
        private bool IsInitializing = true;
        private bool IsValid = true;
        private int CurrentTab = 0;
        private Guid? DefinitionID = null;
        private Guid? SelectedUserID = null;
        private Guid? AuthorityID = null;
        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private List<FORM_Definition> DefinitionList = new List<FORM_Definition>();
        private IDictionary<string, object> ReportParameters { get; set; }
        private string ReportName { get; set; }
        private List<SignerItem> Signings { get; set; }
        private FILE_FileInfo? File_FileInfo { get; set; }
        private Guid? File_FileInfoID;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("BACKEND_FORM_APPLICATION_PAGE_TITLE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Form/Administration", "MAINMENU_BACKEND_FORM_ADMINISTRATION", null);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), "BACKEND_FORM_APPLICATION_PAGE_TITLE", null, null, true);

            if (ID == "New")
            {
                await GetFirstStepData();
                SessionWrapper.PageSubTitle = TextProvider.Get("BACKEND_FORM_APPLICATION_PAGE_SUB_TITLE");
            }
            else
            {
                Data = await FormApplicationProvider.GetApplication(Guid.Parse(ID));

                if (Data != null && Data.FORM_Definition_ID != null)
                {
                    CurrentTab = 1;
                    DefinitionID = Data.FORM_Definition_ID;
                }

                await GetDefinitionData();
                await GetApplicationData();

                if (Definition != null && !string.IsNullOrEmpty(Definition.FORM_Name))
                {
                    SessionWrapper.PageSubTitle = Definition.FORM_Name;
                }

                if(Data != null && Definition != null && Data.IsManualInput == true && Data.FORM_Application_Status_ID == FORMStatus.ToSign) //TO-Sign
                {
                    var definedSignings = await FormDefinitionProvider.GetDefinitionSigningList(Data.FORM_Definition_ID.Value);

                    if (definedSignings != null && definedSignings.Count > 0)
                    {
                        Signings = new List<SignerItem>();

                        foreach (var s in definedSignings.OrderBy(p => p.SortOrder))
                        {
                            Signings.Add(new SignerItem() { Description = s.Description });
                        }
                    }

                    if (Data != null && Data.FILE_Fileinfo_ID != null)
                    {
                        File_FileInfo = await FILEProvider.GetFileInfoAsync(Data.FILE_Fileinfo_ID.Value);
                    }

                    ReportName = await FormRendererHelper.ExecuteReport(Definition.ID, Data.ID);
                    SetReportParameters();

                    CurrentTab = 2;
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetFirstStepData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                AuthorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value);
                var data = await FormDefinitionProvider.GetDefinitionList(SessionWrapper.AUTH_Municipality_ID.Value);

                DefinitionList = data.Where(p => p.Enabled == true).ToList();
            }

            return true;
        }
        private void OnContainerInitialized()
        {
            IsInitializing = false;
            StateHasChanged();
        }
        private async Task<bool> GetDefinitionData()
        {
            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null && DefinitionID != null)
            {
                Definition = await FormDefinitionProvider.GetDefinition(DefinitionID.Value);

                if (Definition != null && Definition.AUTH_Authority_ID != null)
                {
                    Authority = await AuthProvider.GetAuthority(Definition.AUTH_Authority_ID.Value);
                }

                UploadFilesDefinitions = await FormDefinitionProvider.GetDefinitionUploadList(DefinitionID.Value);
            }

            return true;
        }
        private async Task<bool> GetApplicationData()
        {
            if (UploadFilesDefinitions != null && Data != null)
            {
                UploadElements = new List<FORM_Application_Upload>();

                foreach (var def in UploadFilesDefinitions.OrderBy(p => p.SortOrder))
                {
                    var existingElement = await FormApplicationProvider.GetApplicationUploadByDefinition(def.ID, Data.ID);

                    if (existingElement != null)
                    {
                        existingElement.CACH_UploadFiles = new List<FILE_FileInfo>();

                        var items = await FormApplicationProvider.GetApplicationUploadFileList(existingElement.ID);

                        foreach (var i in items)
                        {
                            if (i.FILE_FileInfo_ID != null)
                            {
                                var item = await FileProvider.GetFileInfoAsync(i.FILE_FileInfo_ID.Value);

                                if (item != null)
                                {
                                    existingElement.CACH_UploadFiles.Add(item);
                                }
                            }
                        }

                        UploadElements.Add(existingElement);
                    }
                    else
                    {
                        UploadElements.Add(new FORM_Application_Upload()
                        {
                            ID = Guid.NewGuid(),
                            FORM_Application_ID = Data.ID,
                            FORM_Definition_Upload_ID = def.ID,
                            CACH_UploadFiles = new List<FILE_FileInfo>()
                        });
                    }
                }
            }

            return true;
        }
        private async void Commit()
        {
            if (FormContainer != null && Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                var result = await FormContainer.Validate();

                if (UploadElements != null && UploadFilesDefinitions != null && UploadElements.Count() > 0)
                {
                    foreach (var item in UploadElements)
                    {
                        var def = UploadFilesDefinitions.FirstOrDefault(p => p.ID == item.FORM_Definition_Upload_ID);

                        if (def != null)
                        {
                            item.ERROR_CODE = null;

                            if (def.Required && (item.CACH_UploadFiles == null || item.CACH_UploadFiles.Count < 1))
                            {
                                item.ERROR_CODE = "FORM_ERROR_CODE_REQUIRED";
                                result = false;
                            }
                        }
                    }
                }

                IsValid = result;

                if (!result)
                {
                    BusyIndicatorService.IsBusy = false;
                    StateHasChanged();

                    return;
                }

                Data.IsManualInput = true;
                Data.FORM_Application_Status_ID = FORMStatus.ToSign; //TO SIGN

                await FormApplicationProvider.SetApplication(Data);

                await SaveFiles();

                await FormContainer.Save();

                if (Data.FORM_Definition_ID != null)
                {
                    var tasksToCreate = await FormDefinitionProvider.GetDefinitionTaskList(Data.FORM_Definition_ID.Value);
                    
                    if(tasksToCreate != null)
                    {
                        foreach(var task in tasksToCreate)
                        {
                            var responsible = await FormDefinitionProvider.GetDefinitionTaskResponsibleList(task.ID);

                            var authUsers = new List<AUTH_Users>();

                            if(responsible != null)
                            {
                                foreach(var resp in responsible)
                                {
                                    var user = await AuthProvider.GetUser(resp.AUTH_Users_ID.Value);

                                    if (user != null)
                                    {
                                        authUsers.Add(user);
                                    }
                                }
                            }

                            DateTime? deadline = null;

                            if(task.DeadlineDays != null)
                            {
                                deadline = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

                                deadline.Value.AddDays(task.DeadlineDays.Value);
                            }

                            var newTask = await TaskService.CreateTask(1, Data.ID.ToString(), SessionWrapper.CurrentUser.ID, false, task.ID.ToString(), task.Description, NavManager.BaseUri + "/Backend/Form/Detail/" + Data.ID, deadline, authUsers);

                            var eskalations = await FormDefinitionProvider.GetDefinitionDeadlinesList(Data.FORM_Definition_ID.Value);

                            if (newTask != null && eskalations != null && eskalations.Count() > 0)
                            {
                                foreach (var esk in eskalations)
                                {
                                    if (esk.FORM_Definition_Deadlines_TimeType_ID != null && esk.AdditionalDays != null)
                                    {
                                        var defTargets = await FormDefinitionProvider.GetDefinitionDeadlinesTargetList(esk.ID);

                                        var resultDate = await FormDefinitionProvider.GetDefinitionEskalationDate(esk.FORM_Definition_Deadlines_TimeType_ID.Value, Data.SubmitAt, Data.LegalDeadline, Data.EstimatedDeadline, esk.AdditionalDays.Value);

                                        if (resultDate != null && defTargets != null && defTargets.Count() > 0)
                                        {
                                            await TaskService.CreateEskalation(newTask.ID, resultDate.Value, defTargets.Where(p => p.AUTH_Users_ID != null).Select(p => p.AUTH_Users_ID.Value).ToList());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var definedSignings = await FormDefinitionProvider.GetDefinitionSigningList(Data.FORM_Definition_ID.Value);

                if (definedSignings != null && definedSignings.Count > 0)
                {
                    Signings = new List<SignerItem>();

                    foreach (var s in definedSignings.OrderBy(p => p.SortOrder))
                    {
                        Signings.Add(new SignerItem() { Description = s.Description });
                    }
                }

                if (Data != null && Data.FILE_Fileinfo_ID != null)
                {
                    File_FileInfo = await FILEProvider.GetFileInfoAsync(Data.FILE_Fileinfo_ID.Value);
                }

                ReportName = await FormRendererHelper.ExecuteReport(Definition.ID, Data.ID);
                SetReportParameters();

                CurrentTab = 2;

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private async Task<bool> SaveFiles()
        {
            if (UploadElements != null && UploadElements.Count() > 0 && Data != null)
            {
                foreach (var item in UploadElements)
                {
                    if (item != null)
                    {
                        if (item.FORM_Application_ID == null)
                        {
                            item.FORM_Application_ID = Data.ID;
                        }

                        await FormApplicationProvider.SetApplicationUpload(item);
                    }

                    if (item.CACH_UploadFiles != null && item.CACH_UploadFiles.Count() > 0)
                    {
                        foreach (var f in item.CACH_UploadFiles)
                        {
                            var existingElement = await FormApplicationProvider.GetApplicationUploadFileByFileID(f.ID);

                            if (existingElement != null)
                            {
                                await FileProvider.SetFileInfo(f);
                            }
                            else
                            {
                                var file = await FileProvider.SetFileInfo(f);

                                if (file != null)
                                {
                                    var newUploadFileElement = new FORM_Application_Upload_File();
                                    newUploadFileElement.ID = Guid.NewGuid();
                                    newUploadFileElement.FORM_Application_Upload_ID = item.ID;
                                    newUploadFileElement.FILE_FileInfo_ID = file.ID;

                                    await FormApplicationProvider.SetApplicationUploadFile(newUploadFileElement);
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
        private async Task<bool> OnRemove(Guid File_Info_ID)
        {
            var existingElement = await FormApplicationProvider.GetApplicationUploadFileByFileID(File_Info_ID);

            if (existingElement != null)
            {
                await FormApplicationProvider.RemoveApplicationUploadFile(existingElement.ID);
            }

            await FileProvider.RemoveFileInfo(File_Info_ID);

            StateHasChanged();

            return true;
        }
        private void ReturnToFirstStep()
        {
            CurrentTab = 0;
            StateHasChanged();
        }
        private async void OnFirstStepChanged()
        {
            if (SelectedUserID != null && DefinitionID != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await GetDefinitionData();

                Data = new FORM_Application();

                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = DefinitionID;
                Data.FORM_Application_Status_ID = Guid.Parse("151abd79-cf03-47a9-8719-d2d1eac7152c"); //INCOMPLETE
                Data.CreatedAt = DateTime.Now;

                if (SelectedUserID != null)
                {
                    AUTH_Users_Anagrafic? dbUser = null;

                    if(SessionWrapper.CurrentSubstituteUser != null)
                    {
                        dbUser = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentSubstituteUser.ID);
                    }
                    else
                    {
                        dbUser = await AuthProvider.GetAnagraficByUserID(SelectedUserID.Value);
                    }

                    if(dbUser != null)
                    {
                        Data.FirstName = dbUser.FirstName;
                        Data.LastName = dbUser.LastName;
                        Data.FiscalNumber = dbUser.FiscalNumber;
                        Data.Email = dbUser.Email;
                        Data.CountyOfBirth = dbUser.CountyOfBirth;
                        Data.PlaceOfBirth = dbUser.PlaceOfBirth;
                        Data.DateOfBirth = dbUser.DateOfBirth;
                        Data.Address = dbUser.Address;
                        Data.DomicileMunicipality = dbUser.DomicileMunicipality;
                        Data.DomicileNation = dbUser.DomicileNation;
                        Data.DomicilePostalCode = dbUser.DomicilePostalCode;
                        Data.DomicileProvince = dbUser.DomicileProvince;
                        Data.DomicileStreetAddress = dbUser.DomicileStreetAddress;
                        Data.Gender = dbUser.Gender;
                        Data.MobilePhone = dbUser.MobilePhone;
                        Data.RegisteredOffice = dbUser.RegisteredOffice;
                        Data.VATNumber = dbUser.VatNumber;
                    }

                    if (SessionWrapper.CurrentSubstituteUser != null)
                    {
                        var dbRootUser = await AuthProvider.GetAnagraficByUserID(SelectedUserID.Value);

                        if (dbRootUser != null)
                        {
                            Data.ROOT_AUTH_User_ID = dbRootUser.AUTH_Users_ID;
                            Data.ROOT_FirstName = dbRootUser.FirstName;
                            Data.ROOT_LastName = dbRootUser.LastName;
                            Data.ROOT_FiscalCode = dbRootUser.FiscalNumber;
                            Data.ROOT_CountyOfBirth = dbRootUser.CountyOfBirth;
                            Data.ROOT_PlaceOfBirth = dbRootUser.PlaceOfBirth;
                            Data.ROOT_DateOfBirth = dbRootUser.DateOfBirth;
                            Data.ROOT_Address = dbRootUser.Address;
                            Data.ROOT_DomicileMunicipality = dbRootUser.DomicileMunicipality;
                            Data.ROOT_DomicileNation = dbRootUser.DomicileNation;
                            Data.ROOT_DomicilePostalCode = dbRootUser.DomicilePostalCode;
                            Data.ROOT_DomicileProvince = dbRootUser.DomicileProvince;
                            Data.ROOT_DomicileStreetAddress = dbRootUser.DomicileStreetAddress;
                            Data.ROOT_Gender = dbRootUser.Gender;
                            Data.ROOT_MobilePhone = dbRootUser.MobilePhone;
                            Data.ROOT_Email = dbRootUser.Email;
                            Data.ROOT_Phone = dbRootUser.Phone;
                        }

                        if (dbUser != null && dbUser.GV_AUTH_Users_ID != null)
                        {
                            var dbGVUser = await AuthProvider.GetAnagraficByUserID(dbUser.GV_AUTH_Users_ID.Value);

                            if (dbGVUser != null)
                            {
                                Data.GV_AUTH_User_ID = dbGVUser.AUTH_Users_ID;
                                Data.GV_FirstName = dbGVUser.FirstName;
                                Data.GV_LastName = dbGVUser.LastName;
                                Data.GV_FiscalCode = dbGVUser.FiscalNumber;
                                Data.GV_CountyOfBirth = dbGVUser.CountyOfBirth;
                                Data.GV_PlaceOfBirth = dbGVUser.PlaceOfBirth;
                                Data.GV_DateOfBirth = dbGVUser.DateOfBirth;
                                Data.GV_Address = dbGVUser.Address;
                                Data.GV_DomicileMunicipality = dbGVUser.DomicileMunicipality;
                                Data.GV_DomicileNation = dbGVUser.DomicileNation;
                                Data.GV_DomicilePostalCode = dbGVUser.DomicilePostalCode;
                                Data.GV_DomicileProvince = dbGVUser.DomicileProvince;
                                Data.GV_DomicileStreetAddress = dbGVUser.DomicileStreetAddress;
                                Data.GV_Gender = dbGVUser.Gender;
                                Data.GV_MobilePhone = dbGVUser.MobilePhone;
                                Data.GV_Email = dbGVUser.Email;
                                Data.GV_Phone = dbGVUser.Phone;
                            }
                        }
                    }

                    Data.AUTH_Users_ID = SelectedUserID;

                    Data.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();
                }

                await GetApplicationData();

                CurrentTab = 1;
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private void AuthorityChanged()
        {
            StateHasChanged();
        }
        private async void DefinitionChanged()
        {
            if (DefinitionID != null)
            {
                await GetDefinitionData();

                if (Definition != null && !string.IsNullOrEmpty(Definition.FORM_Name))
                {
                    SessionWrapper.PageSubTitle = Definition.FORM_Name;
                    StateHasChanged();
                } 
            }
        }
        private void UserSelected(Guid? AUTH_Users_ID)
        {
            SelectedUserID = AUTH_Users_ID;
            StateHasChanged();
        }
        private void SetReportParameters()
        {
            if (Data != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                ReportParameters = new Dictionary<string, object>();

                ReportParameters.Add("LanguageID", LANGProvider.GetCurrentLanguageID().ToString());
                ReportParameters.Add("MunicipalityID", SessionWrapper.AUTH_Municipality_ID.Value.ToString());
                ReportParameters.Add("ApplicationID", Data.ID.ToString());
            }
        }
        private async void SendToSign()
        {
            if (Data != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                var fi = await FormApplicationProvider.GetOrCreateFileInfo(Data.ID, ReportName);

                if (fi != null)
                {
                    File_FileInfoID = fi.ID;
                    StateHasChanged();
                }
                else
                {
                    BusyIndicatorService.IsBusy = false;
                    StateHasChanged();
                }
            }
        }
    }
}
