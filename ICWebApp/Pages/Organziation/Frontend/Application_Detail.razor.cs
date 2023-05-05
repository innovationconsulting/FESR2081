using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Organziation.Frontend
{
    public partial class Application_Detail
    {

        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public string ID { get; set; }

        private V_ORG_Requests? CurrentApplication;
        private List<ORG_Request_Status_Log> CurrentStatusLogList = new List<ORG_Request_Status_Log>();
        private List<ORG_Request_Attachment> CurrentApplicationUploads = new List<ORG_Request_Attachment>();
        private List<V_ORG_Request_Status> StatusList = new List<V_ORG_Request_Status>();
        private List<ORG_Request_Ressource> CurrentApplicationRessource = new List<ORG_Request_Ressource>();
        private AUTH_Municipality? Municipality { get; set; }
        private bool IsDataBusy { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Dashboard");
                StateHasChanged();
                return;
            }

            BusyIndicatorService.IsBusy = true;
            IsDataBusy = true;
            StateHasChanged();

            SessionWrapper.PageTitle = TextProvider.Get("ORG_REQUEST_DETAIL_TITLE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Organization/Dashboard", "ORG_REQUEST_DASHBOARD_TITLE", null, null, true);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), "ORG_REQUEST_DETAIL_TITLE", null, null, true);

            CurrentApplication = await OrgProvider.GetVRequest(Guid.Parse(ID));

            if (CurrentApplication != null)
            {
                CurrentStatusLogList = await GetApplicationStatusLog();
                CurrentApplicationRessource = await GetCurrentApplicationRessource();
            }

            StatusList = await OrgProvider.GetStatusList();

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<ORG_Request_Status_Log>> GetApplicationStatusLog()
        {
            return await OrgProvider.GetRequestStatusLogListByRequest(CurrentApplication.ID);
        }
        private async Task<List<ORG_Request_Ressource>> GetCurrentApplicationRessource()
        {
            return await OrgProvider.GetRequestRessourceList(CurrentApplication.ID);
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
