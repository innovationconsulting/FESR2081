using DocumentFormat.OpenXml.Wordprocessing;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class Detail
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] private ILANGProvider LangProvider { get; set; }
        [Parameter] public string ID { get; set; }
        private AUTH_Authority? Authority { get; set; }
        private FORM_Definition? Data { get; set; }
        private List<FORM_Definition_Ressources>? DataRessources;
        private List<FORM_Definition_Ressources_Extended> DataResourceExtendeds =
            new List<FORM_Definition_Ressources_Extended>();
        private List<FORM_Definition_Property>? DataProperties;
        private List<FORM_Definition_Event>? DataEvents;
        private bool ApplicationAlreadyRunning { get; set; } = false;
        protected override async Task OnParametersSetAsync()
        {
            if (ID == null)
            {
                NavManager.NavigateTo("/");
                return;
            }

            AnchorService.ForceShow = true;
            AnchorService.SkipForceReset = true;
            StateHasChanged();

            await GetData();

            if (Data == null)
            {
                NavManager.NavigateTo("/");
                return;
            }

            SessionWrapper.PageTitle = Data.FORM_Name;
            SessionWrapper.PageSubTitle = Data.ShortText;


            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Form", "FRONTEND_AUTHORITY_FORM_LIST", null);

            if (Authority != null)
            {
                CrumbService.AddBreadCrumb("/Form/List/" + Authority.ID, null, null, Authority.Description);
            }

            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), null, null, Data.FORM_Name);

            ApplicationAlreadyRunning = false;

            if (Data.MultipleParallelApplications == false && SessionWrapper.CurrentUser != null)
            {
                Guid UserID = SessionWrapper.CurrentUser.ID;

                if (SessionWrapper.CurrentSubstituteUser != null)
                {
                    UserID = SessionWrapper.CurrentSubstituteUser.ID;
                }

                var exisitingApplications = await FormApplicationProvider.GetApplicationListByUser(UserID, Data.ID);

                foreach (var app in exisitingApplications)
                {
                    var status = FormApplicationProvider.GetStatus(app.FORM_Application_Status_ID.Value);

                    if (status != null && status.FinishedStatus != true)
                    {
                        ApplicationAlreadyRunning = true;
                    }
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            AnchorService.SkipForceReset = false;

            if (ApplicationAlreadyRunning)
            {
                SessionWrapper.PageButtonAction = null;
                SessionWrapper.PageButtonActionTitle = null;
            }
            else if (Data.ApplicationDeadline == null || Data.ApplicationDeadline >= DateTime.Now)
            {
                SessionWrapper.PageButtonAction = SubmitForm;
                SessionWrapper.PageButtonActionTitle = TextProvider.Get("FORM_DETAIL_SUBMIT_BUTTON");
            }
            else
            {
                SessionWrapper.PageButtonAction = null;
                SessionWrapper.PageButtonActionTitle = null;
            }

            await base.OnParametersSetAsync();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Data = await FormDefinitionProvider.GetDefinition(Guid.Parse(ID));

                if (Data != null && Data.AUTH_Authority_ID != null)
                {
                    Authority = await AuthProvider.GetAuthority(Data.AUTH_Authority_ID.Value);
                }

                if (Data != null) 
                {
                    DataRessources = await FormDefinitionProvider.GetDefinitionRessourceList(Data.ID);
                    foreach (var res in DataRessources)
                    {
                        var extended = (await FormDefinitionProvider.GetDefinitionRessourceExtendedList(res.ID, LangProvider.GetCurrentLanguageID())).FirstOrDefault();
                        if (extended != null)
                            DataResourceExtendeds.Add(extended);
                    }
                }

                if (Data != null)
                {
                    DataProperties = await FormDefinitionProvider.GetDefinitionPropertyList(Data.ID);
                }

                if (Data != null)
                {
                    DataEvents = await FormDefinitionProvider.GetDefinitionEventsList(Data.ID);
                }
            }

            return true;
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
        private void SubmitForm()
        {
            if (Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Form/Application/" + Data.ID + "/New");
                StateHasChanged();
            }
        }
    }
}
