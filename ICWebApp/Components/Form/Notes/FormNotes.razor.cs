using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Form.Notes
{
    public partial class FormNotes
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Parameter] public Guid FORM_Application_ID { get; set; }

        private System.Timers.Timer? _timer;
        private bool IsDataBusy { get; set; } = true;
        private List<V_FORM_Application_Note>? FormApplicationNoteMessages { get; set; }
        private List<V_FORM_Application_Note>? LastApplicationNoteMessages { get; set; }
        private List<V_FORM_Application_Note_Dokument>? FormApplicationNoteDokuments { get; set; }
        private FORM_Definition? Definition { get; set; }
        private FORM_Application? Application { get; set; }
        private FORM_Application_Note? NoteToSend { get; set; }
        private List<FILE_FileInfo>? FileInfoUploadList { get; set; }
        private bool ShowFileUploadContainer { get; set; } = false;
        private bool Checking { get; set; } = false;

        protected override async Task OnParametersSetAsync()
        {
            await GetData();
            await LoadMessages();

            NoteToSend = new FORM_Application_Note();
            FileInfoUploadList = new List<FILE_FileInfo>();

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

            if(FormApplicationNoteMessages != null)
            {

                if (LastApplicationNoteMessages == null || LastApplicationNoteMessages.Count() != FormApplicationNoteMessages.Count())
                {
                    LastApplicationNoteMessages = new List<V_FORM_Application_Note>(FormApplicationNoteMessages);
                    try
                    {
                        await EnviromentService.ScrollToElement("Note-bottom-element");
                    }
                    catch { }
                }
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

                FormApplicationNoteMessages = await FormApplicationProvider.GetViewApplicationNoteList(FORM_Application_ID);
                FormApplicationNoteDokuments = await FormApplicationProvider.GetViewApplicationNoteDokumentList(FORM_Application_ID);
               
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

            if (NoteToSend != null && SessionWrapper.CurrentUser != null)
            {
                if (!string.IsNullOrEmpty(NoteToSend.Message) || FileInfoUploadList != null && FileInfoUploadList.Count() > 0)
                {
                    NoteToSend.ID = Guid.NewGuid();
                    NoteToSend.SendDate = DateTime.Now;
                    NoteToSend.FORM_Application_ID = FORM_Application_ID;
                    NoteToSend.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

                    await FormApplicationProvider.SetApplicationNote(NoteToSend);

                    if (FileInfoUploadList != null && FileInfoUploadList.Count() > 0)
                    {
                        foreach (var fileInfo in FileInfoUploadList)
                        {
                            await FileProvider.SetFileInfo(fileInfo);

                            var doc = new FORM_Application_Note_Dokument();

                            doc.ID = Guid.NewGuid();
                            doc.FILE_FileInfo_ID = fileInfo.ID;
                            doc.FORM_Application_ID = FORM_Application_ID;
                            doc.FORM_Application_Note_ID = NoteToSend.ID;

                            await FormApplicationProvider.SetApplicationDokumentNote(doc);
                        }
                    }

                    await LoadMessages();
                }
            }

            NoteToSend = new FORM_Application_Note();

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
    }
}
