using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.DataStore.PagoPA.Domain;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Newtonsoft.Json;
using System.Text;
using Telerik.Reporting;
using Telerik.ReportViewer.Blazor;

namespace ICWebApp.Pages.Landing.Frontend
{
    public partial class Landing
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFORM_ReportRendererHelper ReportHelper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] INEWSProvider NEWSProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }

        private List<AUTH_MunicipalityApps>? AktiveApps = new List<AUTH_MunicipalityApps>();
        private NEWS_Article? Article;
        private AUTH_Municipality? Municipality { get; set; }

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = null;
            SessionWrapper.PageSubTitle = null;

            CrumbService.ClearBreadCrumb();
            CrumbService.ShowBreadCrumb = false;

            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var articles = await NEWSProvider.GetArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LANGProvider.GetCurrentLanguageID(), 30);

                if (articles != null)
                {
                    if (!string.IsNullOrEmpty(ID))
                    {
                        Article = articles.Where(p => p.ID == Guid.Parse(ID)).FirstOrDefault();

                        if (Article == null)
                        {
                            Article = articles.OrderByDescending(p => p.PublishingDate).FirstOrDefault();
                        }
                    }
                    else
                    {
                        Article = articles.OrderByDescending(p => p.PublishingDate).FirstOrDefault();
                    }
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var articles = await NEWSProvider.GetArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LANGProvider.GetCurrentLanguageID(), 30);

                if (articles != null)
                {
                    if (!string.IsNullOrEmpty(ID))
                    {
                        Article = articles.Where(p => p.ID == Guid.Parse(ID)).FirstOrDefault();

                        if(Article == null)
                        {
                            Article = articles.OrderByDescending(p => p.PublishingDate).FirstOrDefault();
                        }
                    }
                    else
                    {
                        Article = articles.OrderByDescending(p => p.PublishingDate).FirstOrDefault();
                    }
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                AktiveApps = await AuthProvider.GetMunicipalityApps();
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }      
    }
}
