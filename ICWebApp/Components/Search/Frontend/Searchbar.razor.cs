using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Domain.Models.Searchbar;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Stripe;

namespace ICWebApp.Components.Search.Frontend
{
    public partial class Searchbar
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] INEWSProvider NEWSProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        private SearchInput Search = new SearchInput();
        private List<AUTH_MunicipalityApps>? AktiveApps = new List<AUTH_MunicipalityApps>();
        private List<SearchbarItem>? DefinitionList;
        private List<SearchbarItem>? AuthorityList;
        private List<SearchbarItem>? ArticleList;

        protected override async Task OnInitializedAsync()
        {
            await GetData();

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                AktiveApps = await AuthProvider.GetMunicipalityApps();

                DefinitionList = new List<SearchbarItem>();
                AuthorityList = new List<SearchbarItem>();
                ArticleList = new List<SearchbarItem>();

                var data = await FormDefinitionProvider.GetDefinitionListOnline(SessionWrapper.AUTH_Municipality_ID.Value);

                if (AktiveApps != null && AktiveApps.Select(p => p.APP_Application_ID).ToList().Contains(Applications.Forms))
                {
                    foreach (var item in data.Where(p => p.FORM_Definition_Category_ID == FORMCategories.Applications).ToList())
                    {
                        DefinitionList.Add(new SearchbarItem()
                        {
                            Url = "/Form/Detail/" + item.ID,
                            SubTitleUrl = "/Form/List/" + item.AUTH_Authority_ID,
                            Title = item.FORM_Name,
                            ShortText = item.ShortText,
                            SubTitle = item.AmtName
                        });
                    }

                    var auth = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, true);

                    foreach(var item in auth.Where(p => data.Select(p => p.AUTH_Authority_ID).Distinct().Contains(p.ID)).ToList())
                    {
                        AuthorityList.Add(new SearchbarItem()
                        {
                            Url = "/Form/List/" + item.ID,
                            Title = item.Description,
                            ShortText = item.ShortText
                        });
                    }
                }

                if (AktiveApps != null && AktiveApps.Select(p => p.APP_Application_ID).ToList().Contains(Applications.Mantainences))
                {
                    foreach (var item in data.Where(p => p.FORM_Definition_Category_ID == FORMCategories.Maintenance).ToList())
                    {
                        DefinitionList.Add(new SearchbarItem()
                        {
                            Url = "/Mantainance/Detail/" + item.ID,
                            Title = item.FORM_Name,
                            ShortText = item.ShortText
                        });
                    }
                }

                if (AktiveApps != null && AktiveApps.Select(p => p.APP_Application_ID).ToList().Contains(Applications.Canteen))
                {
                    DefinitionList.Add(new SearchbarItem()
                    {
                        ShortText = TextProvider.Get("MAINMENU_CANTEEN_SERVICE_DESCRIPTION"),
                        Title = TextProvider.Get("MAINMENU_CANTEEN"),
                        Url = "/Canteen"
                    });
                }

                if (AktiveApps != null && AktiveApps.Select(p => p.APP_Application_ID).ToList().Contains(Applications.Rooms))
                {
                    DefinitionList.Add(new SearchbarItem()
                    {
                        ShortText = TextProvider.Get("MAINMENU_ROOMS_SERVICE_DESCRIPTION"),
                        Title = TextProvider.Get("MAINMENU_ROOMS"),
                        Url = "/Rooms"
                    });
                }

                if (AktiveApps != null && AktiveApps.Select(p => p.APP_Application_ID).ToList().Contains(Applications.News))
                {

                    var news = await NEWSProvider.GetArticleList(SessionWrapper.AUTH_Municipality_ID.Value, LANGProvider.GetCurrentLanguageID());

                    if (news != null && news.Count() > 0)
                    {
                        foreach (var item in news)
                        {
                            var art = new SearchbarItem()
                            {
                                Url = "/News/" + item.ID,
                                Title = item.Title,
                                ShortText = item.ShortContent
                            };

                            if (item.PublishingDate != null)
                            {
                                art.SubTitle = item.PublishingDate.Value.ToString("dd MMM yyyy");
                            }

                            ArticleList.Add(art);
                        }
                    }
                }
            }

            return true;
        }
        private async void NavigateTo(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (!NavManager.Uri.EndsWith(url))
                {
                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo(url);


                    StateHasChanged();
                }
            }

            await JSRuntime.InvokeVoidAsync("searchbar_ToggleVisibility", "search-modal");
        }
    }
}
