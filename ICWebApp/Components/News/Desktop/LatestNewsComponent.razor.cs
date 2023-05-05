using DocumentFormat.OpenXml.Office2010.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;

namespace ICWebApp.Components.News.Desktop
{
    public partial class LatestNewsComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] INEWSProvider NEWSProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public Guid CurrentArticleId { get; set; }

        private List<NEWS_Article>? ArticleList = new List<NEWS_Article>();
        private bool IsDataBusy = true;
        private ElementReference? _containerRef;
        private bool InputInitialized = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadPages();
            IsDataBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_containerRef != null && InputInitialized == false)
            {
                try
                {
                    var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/News/LatestNews.js");

                    await module.InvokeVoidAsync("AddScrollListener", "news-preview-container");

                    InputInitialized = true;
                }
                catch { }
            }

            if (EnviromentService.ShowMunicipalPanoramic == false)
                EnviromentService.ShowMunicipalPanoramic = true;

            await base.OnAfterRenderAsync(firstRender);
        }
        private void GoToNews(string NewsID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/News/" + NewsID);
            StateHasChanged();
        }
        private async Task<bool> LoadPages()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                ArticleList = await NEWSProvider.GetArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LANGProvider.GetCurrentLanguageID(), 30);
            }

            return true;
        }       
    }
}
