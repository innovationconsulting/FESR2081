using HtmlAgilityPack;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.News.Backend
{
    public partial class ArticleEdit
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] INEWSProvider NewsProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string? FamilyID { get; set; }

        private List<NEWS_Article> Articles = new List<NEWS_Article>();
        private List<FILE_FileInfo> ArticleImage = new List<FILE_FileInfo>();
        private Guid? CurrentLanguage;
        private List<LANG_Languages> Languages = new List<LANG_Languages>();
        private bool Italian
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == LanguageSettings.Italian)
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = LanguageSettings.Italian;
                    StateHasChanged();
                }
            }
        }
        private bool German
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == LanguageSettings.German)
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = LanguageSettings.German;
                    StateHasChanged();
                }
            }
        }
        private List<IEditorTool> Tools { get; set; } =
        new List<IEditorTool>()
        {
            new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
            new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
            new UnorderedList(),
            new EditorButtonGroup(new CreateLink(), new Unlink()),
            new InsertTable(),
            new DeleteTable(),
            new EditorButtonGroup(new AddRowBefore(), new AddRowAfter(), new DeleteRow(), new MergeCells(), new SplitCell())
        };
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font" };
        private DateTime PublishingDate = DateTime.Now;
        private bool Active = true;
        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrEmpty(FamilyID))
            {
                ReturnToPreviousPage();
                return;
            }

            var languages = await LangProvider.GetAll();

            if (languages != null && languages.Count() > 0)
            {
                Languages = languages;
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            if (FamilyID == "New")
            {
                SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_NEWS_ADD");
                
                Guid NewFamilyID = Guid.NewGuid();

                foreach(var lang in Languages)
                {
                    var article = new NEWS_Article();

                    article.ID = Guid.NewGuid();
                    article.FamilyID = NewFamilyID;

                    if (SessionWrapper.AUTH_Municipality_ID != null)
                    {
                        article.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                    }

                    article.LANG_Languages_ID = lang.ID;
                    article.Enabled = Active;
                    article.InputType = "CUSTOM";

                    Articles.Add(article);
                }
            }
            else
            {
                SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_NEWS_EDIT");

                var articles = await NewsProvider.GetArticleList(Guid.Parse(FamilyID));

                if(articles != null && articles.Count() > 0)
                {
                    Articles = articles;

                    if (Articles.FirstOrDefault().PublishingDate != null)
                    {
                        PublishingDate = Articles.FirstOrDefault().PublishingDate.Value;
                    }
                    if(Articles.FirstOrDefault().Enabled != null)
                    {
                        Active = Articles.FirstOrDefault().Enabled.Value;
                    }

                    foreach(var lang in Languages)
                    {
                        if(Articles.Where(p => p.LANG_Languages_ID == lang.ID).FirstOrDefault() == null)
                        {
                            var article = new NEWS_Article();

                            article.ID = Guid.NewGuid();
                            article.FamilyID = Articles.FirstOrDefault().FamilyID;

                            if (SessionWrapper.AUTH_Municipality_ID != null)
                            {
                                article.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                            }

                            article.LANG_Languages_ID = lang.ID;
                            article.PublishingDate = DateTime.Now;
                            article.Enabled = Active;
                            article.InputType = "CUSTOM";

                            Articles.Add(article);
                        }
                    }

                    var image = await NewsProvider.GetArticleImage(Guid.Parse(FamilyID));

                    if (image != null && image.FILE_Fileinfo_ID != null)
                    {
                        var fi = await FileProvider.GetFileInfoAsync(image.FILE_Fileinfo_ID.Value);

                        if (fi != null)
                        {
                            ArticleImage = new List<FILE_FileInfo>() { fi };
                        }
                    }

                    foreach (var art in Articles)
                    {
                        var ressList = await NewsProvider.GetArticleRessource(art.ID);

                        var fileInfoList = new List<FILE_FileInfo>();

                        if (ressList != null && ressList.Count() > 0)
                        {
                            foreach (var res in ressList)
                            {
                                if (res.FILE_FileInfo_ID != null)
                                {
                                    var fi = await FileProvider.GetFileInfoAsync(res.FILE_FileInfo_ID.Value);

                                    if (fi != null)
                                    {
                                        fileInfoList.Add(fi);
                                    }
                                }
                            }

                            art.ArticleRessource = fileInfoList;
                        }
                    }
                }
                else
                {
                    ReturnToPreviousPage();
                    return;
                }
            }

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb(NavManager.Uri, "MAINMENU_BACKEND_NEWS", null, null, true);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var art in Articles)
            {
                if (!string.IsNullOrEmpty(art.Title))
                {
                    if (art.Content != null)
                    {
                        HtmlDocument htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(art.Content);
                        string strippedContent = htmlDoc.DocumentNode.InnerText;

                        if (strippedContent != null && strippedContent.Length > 100)
                        {
                            art.ShortContent = strippedContent.Substring(0, 100) + "...";
                        }
                        else if (strippedContent != null)
                        {
                            art.ShortContent = strippedContent;
                        }
                    }
                     
                    art.PublishingDate = PublishingDate;
                    art.Enabled = Active;

                    var existingRessource = await NewsProvider.GetArticleRessource(art.ID);

                    foreach(var ress in art.ArticleRessource)
                    {
                        if(existingRessource != null && existingRessource.Select(p => p.FILE_FileInfo_ID).Contains(ress.ID))
                        {
                            continue;
                        }

                        NEWS_Article_Ressource newRess = new NEWS_Article_Ressource();

                        newRess.ID = Guid.NewGuid();
                        newRess.NEWS_Article_ID = art.ID;
                        newRess.FILE_FileInfo_ID = ress.ID;

                        await FileProvider.SetFileInfo(ress);
                        await NewsProvider.SetArticleRessource(newRess);
                    }

                    if (existingRessource != null) 
                    {
                        var ressToDelete = existingRessource.Where(p => !art.ArticleRessource.Select(x => x.ID).ToList().Contains(p.FILE_FileInfo_ID.Value)).ToList();

                        foreach(var ress in ressToDelete)
                        {
                            await NewsProvider.RemoveArticleRessource(ress);
                        }
                    }
                    

                    await NewsProvider.SetArticle(art);
                }
            }

            if (Articles != null && Articles.FirstOrDefault() != null && Articles.FirstOrDefault().FamilyID != null) 
            {
                var existingImage = await NewsProvider.GetArticleImage(Articles.FirstOrDefault().FamilyID.Value);

                if (ArticleImage != null && ArticleImage.Count() > 0)
                {
                    if(existingImage == null)
                    {
                        existingImage = new NEWS_Article_Image();

                        existingImage.ID = Guid.NewGuid();
                        existingImage.NEWS_Family_ID = Articles.FirstOrDefault().FamilyID.Value;
                    }

                    existingImage.FILE_Fileinfo_ID = ArticleImage.FirstOrDefault().ID;

                    await FileProvider.SetFileInfo(ArticleImage.FirstOrDefault());
                    await NewsProvider.SetArticleImage(existingImage);
                }
            }

            ReturnToPreviousPage();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/News");
            StateHasChanged();
        }
    }
}
