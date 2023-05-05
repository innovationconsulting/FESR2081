using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace ICWebApp.Pages.News.Frontend
{
    public partial class NewsDetail
    {
        [Inject] ICONFProvider ConfProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] INEWSProvider NEWSProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set;}
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }

        [Parameter] public string? ArticleID { get; set; }

        private List<AUTH_MunicipalityApps>? AktiveApps = new List<AUTH_MunicipalityApps>();
        private ElementReference? _containerRef;
        private bool InputInitialized = false;
        private List<NEWS_Article>? ArticleList = new List<NEWS_Article>();
        private NEWS_Article? Article;
        private List<NEWS_Article_Ressource>? ArticleRessourceList;
        private AUTH_Municipality? Municipality;

        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrEmpty(ArticleID))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/");
                StateHasChanged();
            }

            await LoadDetailPage(Guid.Parse(ArticleID)); 

            if(Article == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            SessionWrapper.PageTitle = Article.Title;

            if (AktiveApps == null || AktiveApps.Count() == 0)
            {
                AktiveApps = await AUTHProvider.GetMunicipalityApps();
            }

            CrumbService.ClearBreadCrumb();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (EnviromentService.ShowMunicipalPanoramic == false)
                EnviromentService.ShowMunicipalPanoramic = true;

            if (_containerRef != null && InputInitialized == false)
            {
                var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/News/LatestNews.js");

                await module.InvokeVoidAsync("AddScrollListener", "news-preview-container");

                InputInitialized = true;
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private async Task<bool> LoadDetailPage(Guid ArticleID)
        {
            Article = await NEWSProvider.GetArticle(ArticleID);
            
            if (Article != null)
            {
                ArticleRessourceList = await NEWSProvider.GetArticleRessource(Article.ID);
            }

            NEWS_Log log = new NEWS_Log();
            log.ID = Guid.NewGuid();
            log.NEWS_Article_ID = ArticleID;

            if (SessionWrapper.CurrentUser != null)
            {
                log.AUTH_User_ID = SessionWrapper.CurrentUser.ID;
            }

            log.CreationDate = DateTime.Now;
            log.LogType = "Reading";

            await NEWSProvider.SetLog(log);

            if ((ArticleList == null || ArticleList.Count() == 0) && SessionWrapper.AUTH_Municipality_ID != null)
            {
                ArticleList = await NEWSProvider.GetArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LANGProvider.GetCurrentLanguageID());
            }

            StateHasChanged();
            return true;
        }
        private async void GoToNews(string NewsID)
        {
            if (Article != null && Article.ID != Guid.Parse(NewsID))
            {
                await LoadDetailPage(Guid.Parse(NewsID));
            }
        }
        private void GoToRooms()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Rooms");
            StateHasChanged();
        }
        private void GoToMensa()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Canteen");
            StateHasChanged();
        }
        private void GoToApplications()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Form");
            StateHasChanged();
        }
        private void GoToNotifications()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Mantainance");

            StateHasChanged();
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
    }
}
