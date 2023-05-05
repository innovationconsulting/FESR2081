using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.Components.Signing;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Components.Templates
{
    public partial class TextTemplateInputComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public Guid ApplicationID { get; set; }
        [Parameter] public string ExternalContext { get; set; }
        [Parameter] public string ExternalID { get; set; }

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
        private List<V_TEXT_Template> TextList = new List<V_TEXT_Template>();
        private bool IsDataBusy = true;
        private bool EditWindowVisiblity = false;
        private TEXT_Template? Template;
        private List<TEXT_Template_Extended>? TemplateExtended;
        private List<TEXT_Template_Keyword> KeywordList = new List<TEXT_Template_Keyword>();
        private bool IsNew = false;
        private TelerikEditor? TemplateEditor;

        protected override async Task OnInitializedAsync()
        {
            Languages = await LangProvider.GetAll();

            if (Languages != null && Languages.FirstOrDefault() != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;

                KeywordList = await TextProvider.GetTemplateKeywords(ExternalContext);
            }

            await GetTextList();

            IsDataBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetTextList()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                TextList = await TextProvider.GetTemplates(SessionWrapper.AUTH_Municipality_ID.Value, ExternalContext, ExternalID, null);
            }

            return true;
        }
        private async void AddTemplate(bool IsDocument)
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Template = new TEXT_Template();
                Template.ID = Guid.NewGuid();
                Template.IsDocument = IsDocument;

                var lastItem = TextList.Max(p => p.SortOrder);

                if (lastItem != null)
                {
                    Template.SortOrder = lastItem.Value + 1;
                }
                else
                {
                    Template.SortOrder = 1;
                }

                Template.ExternalContext = ExternalContext;
                Template.ExternalID = ExternalID;
                Template.APP_Applications_ID = ApplicationID;
                Template.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;

                if (Languages != null)
                {
                    TemplateExtended = new List<TEXT_Template_Extended>();

                    foreach (var l in Languages)
                    {
                        var defaultTemp = await TextProvider.GetDefaultTemplate(l.ID);

                        var dataE = new TEXT_Template_Extended()
                        {
                            ID = Guid.NewGuid(),
                            TEXT_Template_ID = Template.ID,
                            LANG_Languages_ID = l.ID
                        };

                        if(defaultTemp != null)
                        {
                            dataE.Content = defaultTemp.Content;
                        }

                        TemplateExtended.Add(dataE);
                    }
                }

                IsNew = true;
                EditWindowVisiblity = true;
                StateHasChanged();
            }
        }
        private async void DeleteTemplate(Guid ID)
        {
            if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                return;

            IsDataBusy = true;
            StateHasChanged();

            await TextProvider.RemoveTemplate(ID);

            await GetTextList();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async void EditTemplate(Guid ID)
        {
            Template = await TextProvider.GetTemplate(ID);
            TemplateExtended = await TextProvider.GetTemplateExtended(ID);

            IsNew = false;
            EditWindowVisiblity = true;
            StateHasChanged();
        }
        private void CloseTemplate()
        {
            Template = null;
            TemplateExtended = null;

            IsNew = false;
            EditWindowVisiblity = false;
            StateHasChanged();
        }
        private async void SaveTemplate()
        {
            if (Template != null && TemplateExtended != null)
            {
                IsDataBusy = true;
                StateHasChanged();

                Template.CreatedAt = DateTime.Now;

                await TextProvider.SetTemplate(Template);

                foreach(var extended in TemplateExtended)
                {
                    await TextProvider.SetTemplateExtended(extended);
                }
            }

            Template = null;
            TemplateExtended = null;

            await GetTextList();

            IsNew = false;
            EditWindowVisiblity = false;
            IsDataBusy = false;
            StateHasChanged();
        }
        private async void SetKeyWordValue(string Keyword)
        {
            if (TemplateEditor != null) 
            {
                await TemplateEditor.ExecuteAsync(new HtmlCommandArgs("insertHtml", "<b>" + Keyword + "</b>", true));
                StateHasChanged();
            }
        }
    }
}
