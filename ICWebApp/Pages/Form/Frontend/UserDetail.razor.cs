using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class UserDetail
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IPAYProvider PayProvider { get; set; }
        [Parameter] public string ID { get; set; }

        private FORM_Application? CurrentApplication;
        private FORM_Definition? CurrentDefinition;
        private List<FORM_Application_Status_Log> CurrentStatusLogList = new List<FORM_Application_Status_Log>();
        private List<FORM_Definition_Upload> CurrentDefinitionUploads = new List<FORM_Definition_Upload>();
        private List<FORM_Application_Upload> CurrentApplicationUploads = new List<FORM_Application_Upload>();
        private List<FORM_Application_Upload_File> CurrentApplicationFileUploads = new List<FORM_Application_Upload_File>();
        private List<V_FORM_Application_Ressource> CurrentApplicationRessource = new List<V_FORM_Application_Ressource>();
        private List<Guid?> CurrenTransactionList = new List<Guid?>();
        private List<FORM_Application_Status> StatusList = new List<FORM_Application_Status>();
        private List<FORM_Application_Priority> PriorityList = new List<FORM_Application_Priority>();
        private bool IsDataBusy { get; set; } = true;
        private AUTH_Municipality? Municipality { get; set; }
        private string[] Subdomains { get; set; } = new string[] { "a", "b", "c" };
        private string UrlTemplate { get; set; } = "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png";
        private string Attribution { get; set; } = "&copy; <a href='https://osm.org/copyright'>OpenStreetMap contributors</a>";
        private List<FILE_FileInfo> DynamicFiles = new List<FILE_FileInfo>();

        protected override async Task OnParametersSetAsync()
        {
            if (ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Backend/Form/Administration");
                StateHasChanged();
                return;
            }

            BusyIndicatorService.IsBusy = true;
            IsDataBusy = true;
            StateHasChanged();

            CurrentApplication = await GetApplication();
            StatusList = GetStatusList();
            PriorityList = await FormApplicationProvider.GetPriorities();

            if (CurrentApplication != null)
            {
                CurrentDefinition = await GetDefinition();
                CurrentStatusLogList = await GetApplicationStatusLog();
                CurrenTransactionList = await GetApplicationTransactionList();
                CurrentApplicationUploads = await GetCurrentApplicationUpload();
                CurrentApplicationFileUploads = await GetCurrentApplicationUploadFiles();
                CurrentApplicationRessource = await GetCurrentApplicationRessource();
                DynamicFiles = await GetCurrentApplicationDynamicFiles();
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            CrumbService.ClearBreadCrumb();

            if (CurrentDefinition != null)
            {
                CurrentDefinitionUploads = await GetCurrentDefinitionUpload();


                if (CurrentDefinition.FORM_Definition_Category_ID == FORMCategories.Maintenance)
                {
                    CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_MY_MANTAINANCES", null);
                    SessionWrapper.PageTitle = CurrentApplication.Mantainance_Title;
                }
                else
                {
                    CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_MY_APPLICATIONS", null);
                    SessionWrapper.PageTitle = CurrentDefinition.FORM_Name;
                }

                CrumbService.AddBreadCrumb(NavManager.Uri, CurrentDefinition.FORM_Name, null, null, true);
            }

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async Task<FORM_Application?> GetApplication()
        {
            return await FormApplicationProvider.GetApplication(Guid.Parse(ID));
        }
        private async Task<FORM_Definition?> GetDefinition()
        {
            return await FormDefinitionProvider.GetDefinition(CurrentApplication.FORM_Definition_ID.Value);
        }
        private async Task<List<FORM_Application_Status_Log>> GetApplicationStatusLog()
        {
            return await FormApplicationProvider.GetApplicationStatusLogList(CurrentApplication.ID);
        }
        private async Task<List<Guid?>> GetApplicationTransactionList()
        {
            var data = await FormApplicationProvider.GetApplicationTransactionList(CurrentApplication.ID);

            return data.Select(p => p.PAY_Transaction_ID).ToList();
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
        private async Task<List<V_FORM_Application_Ressource>> GetCurrentApplicationRessource()
        {
            return await FormApplicationProvider.GetVApplicationRessourceList(CurrentApplication.ID);
        }
        private async Task<List<FORM_Definition_Upload>> GetCurrentDefinitionUpload()
        {
            return await FormDefinitionProvider.GetDefinitionUploadList(CurrentDefinition.ID);
        }
        private async Task<List<FILE_FileInfo>> GetCurrentApplicationDynamicFiles()
        {
            return await FormApplicationProvider.GetApplicationUploadFiles(CurrentApplication.ID, CurrentDefinition.ID);
        }        
        private List<FORM_Application_Status> GetStatusList()
        {
            var data = FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

            return data.ToList();
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
        private void BackToPrevious()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/User/Services");
            StateHasChanged();            
        }
    }
}
