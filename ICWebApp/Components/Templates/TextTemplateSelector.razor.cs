using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Textvorlagen;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Components.Templates
{
    public partial class TextTemplateSelector
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        [Parameter] public string ExternalContext { get; set; }
        [Parameter] public string ExternalID { get; set; }
        [Parameter] public TextItem TextItem { get; set; }
        [Parameter] public bool IsDocument { get; set; } = false;

        private List<V_TEXT_Template> Data = new List<V_TEXT_Template>();
        private V_TEXT_Template? SelectedTemplate;
        private string? PreviousExternalID;
        private List<LANG_Languages>? Languages { get; set; }
        private Guid? CurrentLanguage { get; set; }
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
        private bool IsDataBusy = true;
        protected override async Task OnInitializedAsync()
        {
            Languages = await LangProvider.GetAll();

            if (Languages != null && Languages.FirstOrDefault() != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            if(PreviousExternalID != ExternalID)
            {
                PreviousExternalID = ExternalID;
                Data = new List<V_TEXT_Template>();
                SelectedTemplate = null;

                if (SessionWrapper.AUTH_Municipality_ID != null && !string.IsNullOrEmpty(ExternalID))
                {
                    var templates = await TextProvider.GetTemplates(SessionWrapper.AUTH_Municipality_ID.Value, ExternalContext, ExternalID, IsDocument);

                    if(templates != null && templates.Count() > 0)
                    {
                        Data = templates;
                        SelectedTemplate = templates.FirstOrDefault();
                        SelectionChanged();
                    }
                    else
                    {
                        IsDataBusy = true;
                        StateHasChanged();

                        List<TemplateItem> TemplateList = new List<TemplateItem>();
                        var defaultTextDE = await TextProvider.GetDefaultTemplate(LanguageSettings.German, IsDocument);
                        var defaultTextIT = await TextProvider.GetDefaultTemplate(LanguageSettings.Italian, IsDocument);

                        if (defaultTextDE != null) 
                        {
                            TemplateItem itemDE = new TemplateItem();

                            itemDE.LANG_Language_ID = LanguageSettings.German;
                            itemDE.Content = defaultTextDE.Content;

                            TemplateList.Add(itemDE);
                        }

                        if (defaultTextIT != null)
                        {
                            TemplateItem itemIT = new TemplateItem();

                            itemIT.LANG_Language_ID = LanguageSettings.Italian;
                            itemIT.Content = defaultTextIT.Content;

                            TemplateList.Add(itemIT);
                        }

                        TemplateSelectionChanged(TemplateList);
                    }

                }

                IsDataBusy = false;
                StateHasChanged();
            }
            await base.OnParametersSetAsync();
        }
        private async void SelectionChanged()
        {
            if (SelectedTemplate != null)
            {
                IsDataBusy = true;
                StateHasChanged();

                List<TemplateItem> TemplateList = new List<TemplateItem>();

                var dataExt = await TextProvider.GetTemplateExtended(SelectedTemplate.ID);

                if (dataExt != null && dataExt.FirstOrDefault(p => p.LANG_Languages_ID == LanguageSettings.German) != null)
                {
                    TemplateItem itemDE = new TemplateItem();

                    itemDE.LANG_Language_ID = LanguageSettings.German;
                    itemDE.Content = dataExt.FirstOrDefault(p => p.LANG_Languages_ID == LanguageSettings.German).Content;

                    TemplateList.Add(itemDE);
                }
                else
                {
                    var defaultTextDE = await TextProvider.GetDefaultTemplate(LanguageSettings.German, IsDocument);

                    if (defaultTextDE != null)
                    {
                        TemplateItem itemDE = new TemplateItem();

                        itemDE.LANG_Language_ID = LanguageSettings.German;
                        itemDE.Content = defaultTextDE.Content;

                        TemplateList.Add(itemDE);
                    }
                }

                if (dataExt != null && dataExt.FirstOrDefault(p => p.LANG_Languages_ID == LanguageSettings.Italian) != null)
                {
                    TemplateItem itemIT = new TemplateItem();

                    itemIT.LANG_Language_ID = LanguageSettings.Italian;
                    itemIT.Content = dataExt.FirstOrDefault(p => p.LANG_Languages_ID == LanguageSettings.Italian).Content;

                    TemplateList.Add(itemIT);
                }
                else
                {
                    var defaultTextIT = await TextProvider.GetDefaultTemplate(LanguageSettings.Italian, IsDocument);

                    if (defaultTextIT != null)
                    {
                        TemplateItem itemIT = new TemplateItem();

                        itemIT.LANG_Language_ID = LanguageSettings.Italian;
                        itemIT.Content = defaultTextIT.Content;

                        TemplateList.Add(itemIT);
                    }
                }

                TemplateSelectionChanged(TemplateList);
                IsDataBusy = false;
                StateHasChanged();
            }
        }
        private bool TemplateSelectionChanged(List<TemplateItem> Templates)
        {
            if (Templates != null && Templates.Count() > 0)
            {
                var templateDE = Templates.FirstOrDefault(p => p.LANG_Language_ID == LanguageSettings.German);
                var templateIT = Templates.FirstOrDefault(p => p.LANG_Language_ID == LanguageSettings.Italian);

                if (templateDE != null)
                {
                    TextItem.German = templateDE.Content;
                }
                else
                {
                    TextItem.German = null;
                }

                if (templateIT != null)
                {
                    TextItem.Italian = templateIT.Content;
                }
                else
                {
                    TextItem.Italian = null;
                }
            }

            return true;
        }
    }
}