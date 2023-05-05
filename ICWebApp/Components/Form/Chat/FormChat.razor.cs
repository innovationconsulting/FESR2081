using HtmlAgilityPack;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace ICWebApp.Components.Form.Chat
{
    public partial class FormChat
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public Guid FORM_Application_ID { get; set; }
        [Parameter] public bool ForceChatButton { get; set; } = false;

        private System.Timers.Timer? _timer;
        private bool IsDataBusy { get; set; } = true;
        private List<V_FORM_Application_Chat>? FormApplicationChatMessages { get; set; }
        private List<V_FORM_Application_Chat>? LastApplicationChatMessages { get; set; }
        private List<V_FORM_Application_Chat_Dokument>? FormApplicationChatDokuments { get; set; }
        private FORM_Definition? Definition { get; set; }
        private FORM_Application? Application { get; set; }
        private FORM_Application_Chat? ChatToSend { get; set; }
        private List<FILE_FileInfo>? FileInfoUploadList { get; set; }
        private bool ShowFileUploadContainer { get; set; } = false;
        private bool Checking { get; set; } = false;
        private ElementReference? _inputRef;
        private bool InputInitialized = false;
        private HyperlinkItem? HyperlinkItem = null;

        protected override async Task OnParametersSetAsync()
        {
            await GetData();
            await LoadMessages();

            ChatToSend = new FORM_Application_Chat();
            FileInfoUploadList = new List<FILE_FileInfo>();

            InputInitialized = false;

            IsDataBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _timer = new System.Timers.Timer(1000);
                _timer.Elapsed += CheckMessages;
                _timer.Enabled = true;
                _timer.AutoReset = true;
            }

            if(FormApplicationChatMessages != null)
            {

                if (LastApplicationChatMessages == null || LastApplicationChatMessages.Count() != FormApplicationChatMessages.Count())
                {
                    LastApplicationChatMessages = new List<V_FORM_Application_Chat>(FormApplicationChatMessages);
                    try
                    {
                        await EnviromentService.ScrollToElement("chat-bottom-element");
                    }
                    catch { }
                }
            }

            if (_inputRef != null && !string.IsNullOrEmpty(_inputRef.Value.Id) && InputInitialized == false) 
            {
                var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/Services/pasteInteropHelper.js");

                var selfReference = DotNetObjectReference.Create(this);

                await module.InvokeVoidAsync("addOnPasteEventListener", _inputRef, selfReference);

                InputInitialized = true;
            }


            await base.OnAfterRenderAsync(firstRender);
        }
        private async void CheckMessages(object? sender, System.Timers.ElapsedEventArgs e)
        {
            await LoadMessages();
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
        public async Task<bool> GetData()
        {
            Application = await FormApplicationProvider.GetApplication(FORM_Application_ID);

            if (Application != null && Application.FORM_Definition_ID != null) 
            {
                Definition = await FormDefinitionProvider.GetDefinition(Application.FORM_Definition_ID.Value);
            }
            return true;
        }
        public async Task<bool> LoadMessages()
        {
            if (!Checking)
            {
                Checking = true;

                FormApplicationChatMessages = await FormApplicationProvider.GetViewApplicationChatList(FORM_Application_ID);
                FormApplicationChatDokuments = await FormApplicationProvider.GetViewApplicationChatDokumentListByApplication(FORM_Application_ID);

                if (SessionWrapper.CurrentUser != null)
                {
                    var MessageToSetAsRead = FormApplicationChatMessages.Where(p => p.AUTH_Users_ID != SessionWrapper.CurrentUser.ID && p.ReadDate == null).ToList();

                    foreach (var m in MessageToSetAsRead)
                    {
                        if (m != null && m.ID != null)
                        {
                            var dbM = await FormApplicationProvider.GetApplicationChat(m.ID);

                            if (dbM != null)
                            {
                                dbM.ReadDate = DateTime.Now;

                                await FormApplicationProvider.SetApplicationChat(dbM);
                            }
                        }
                    }
                }
                Checking = false;
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
        private async Task<bool> SendMessage()
        {
            IsDataBusy = true;
            StateHasChanged();

            if (ChatToSend != null && SessionWrapper.CurrentUser != null)
            {
                if (!string.IsNullOrEmpty(ChatToSend.Message) || FileInfoUploadList != null && FileInfoUploadList.Count() > 0)
                {
                    ChatToSend.ID = Guid.NewGuid();
                    ChatToSend.SendDate = DateTime.Now;
                    ChatToSend.FORM_Application_ID = FORM_Application_ID;
                    ChatToSend.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

                    if(HyperlinkItem != null)
                    {
                        ChatToSend.HyperlinkPastedUrl = HyperlinkItem.PastedUrl;
                        ChatToSend.HyperlinkFaviconUrl = HyperlinkItem.FaviconUrl;
                        ChatToSend.HyperlinkLinkName = HyperlinkItem.HyperlinkName;

                        HyperlinkItem = null;
                    }

                    await FormApplicationProvider.SetApplicationChat(ChatToSend);

                    if (FileInfoUploadList != null && FileInfoUploadList.Count() > 0)
                    {
                        foreach (var fileInfo in FileInfoUploadList)
                        {
                            await FileProvider.SetFileInfo(fileInfo);

                            var doc = new FORM_Application_Chat_Dokument();

                            doc.ID = Guid.NewGuid();
                            doc.FILE_FileInfo_ID = fileInfo.ID;
                            doc.FORM_Application_ID = FORM_Application_ID;
                            doc.FORM_Application_Chat_ID = ChatToSend.ID;

                            await FormApplicationProvider.SetApplicationDokumentChat(doc);
                        }
                    }

                    await LoadMessages();
                }
            }

            ChatToSend = new FORM_Application_Chat();

            HideDocuments();

            IsDataBusy = false;
            StateHasChanged();

            return true;
        }
        private void ToggleDocuments()
        {
            if (!ShowFileUploadContainer)
            {
                ShowFileUploadContainer = true;
            }
            else
            {
                HideDocuments();
            }

            StateHasChanged();
        }
        private void HideDocuments()
        {
            ShowFileUploadContainer = false;
            FileInfoUploadList = new List<FILE_FileInfo>();
            StateHasChanged();
        }
        private async void RemoveFile(Guid FILE_Info_ID)
        {
            IsDataBusy = true;
            StateHasChanged();

            await FileProvider.RemoveFileInfo(FILE_Info_ID);

            IsDataBusy = false;
            StateHasChanged();
        }
        [JSInvokable]
        public async void HandlePaste(string text)
        {
            Regex UrlMatch = new Regex(@"(?i)(http(s)?:\/\/)?(\w{2,25}\.)+\w{3}([a-z0-9\-?=$-_.+!*()]+)(?i)", RegexOptions.Singleline);

            var match = UrlMatch.Match(text);

            if (match.Success) 
            {
                text = match.Value;

                HyperlinkItem = new HyperlinkItem();

                try
                {
                    HyperlinkItem.FaviconUrl = GetHtml(text, 0);
                }
                catch { }

                HyperlinkItem.PastedUrl = text;

                try
                {
                    HyperlinkItem.HyperlinkName = GetHtml(text, 1);
                }
                catch
                {
                    HyperlinkItem.HyperlinkName = text;
                }

                StateHasChanged();
            }
        }
        private string? GetHtml(string Url, int Type)
        {
            string htmlCode;

            using (WebClient client = new WebClient())
            {
                htmlCode = client.DownloadString(Url);
            }

            if (Type == 0) 
            {
                var uri = new Uri(Url);

                return uri.Scheme + "://" + uri.Host + "/favicon.ico";
            }
            else if (Type == 1) 
            {
                Match match = Regex.Match(htmlCode, @"<title>([^<]+)", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }
        private void RemoveHyperlink()
        {
            HyperlinkItem = null;

            StateHasChanged();
        }
    }
}
