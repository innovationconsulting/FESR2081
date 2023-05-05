using DocumentFormat.OpenXml;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor;

namespace ICWebApp.Pages.News.Backend
{
    public partial class Administration
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] INEWSProvider NewsProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private List<V_NEWS_Article>? ArticleList = new List<V_NEWS_Article>();

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_NEWS");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb(NavManager.Uri, "MAINMENU_BACKEND_NEWS", null, null, true);

            await GetData();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }   

        private async Task<bool> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var articles = await NewsProvider.GetViewArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LangProvider.GetCurrentLanguageID(), "CUSTOM");

                if (LangProvider.GetCurrentLanguageID() == LanguageSettings.German)
                {
                    var articlesDifferentLang = await NewsProvider.GetViewArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LanguageSettings.Italian, "CUSTOM");

                    if (articlesDifferentLang != null && articles != null)
                    {
                        var articlesToAdd = articlesDifferentLang.Where(p => !articles.Select(x => x.FamilyID).Contains(p.FamilyID));

                        articles.AddRange(articlesToAdd);
                    }
                }
                else
                {
                    var articlesDifferentLang = await NewsProvider.GetViewArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LanguageSettings.German, "CUSTOM");

                    if (articlesDifferentLang != null && articles != null)
                    {
                        var articlesToAdd = articlesDifferentLang.Where(p => !articles.Select(x => x.FamilyID).Contains(p.FamilyID));

                        articles.AddRange(articlesToAdd);
                    }
                }

                ArticleList = articles;
            }

            return true;
        }
        private void EditArticle(Guid FamilyID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/News/Edit/" + FamilyID);
            StateHasChanged();
        }
        private void AddArticle()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/News/Edit/New");
            StateHasChanged();
        }
        private async void RemoveArticle(Guid FamilyID)
        {
            var articlesToDelete = await NewsProvider.GetArticleList(FamilyID);

            if (articlesToDelete != null)
            {
                foreach (var art in articlesToDelete)
                {
                    art.DeletedAt = DateTime.Now;

                    await NewsProvider.SetArticle(art);
                }
            }

            await GetData();
            StateHasChanged();
        }
    }
}
