using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Telerik.Blazor;

namespace ICWebApp.Components.Canteen
{
    public partial class UploadMedicalFileComponent
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] public ISessionWrapper SessionWrapper { get; set; }
        [Inject] public IEnviromentService EnviromentService { get; set; }
        [Inject] public IFILEProvider FileProvider { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        private List<FILE_FileInfo> FileInfoList { get; set; }

        [Parameter] public CANTEEN_Subscriber Subscriber { get; set; }

        [Parameter] public bool Multiple { get; set; } = false;
        [Parameter] public EventCallback<Guid> OnRemove { get; set; }
        [Parameter] public EventCallback<Guid> OnUpload { get; set; }
        [Parameter] public string Accept { get; set; } = ".pdf, .docx, .xlsx, .xls, .csv, .jpg, .png";
        [Parameter] public string ID { get; set; }
        [Parameter] public bool SmallStyle { get; set; } = false;
        [Parameter] public bool RemoveAreYouSure { get; set; } = false;


        private List<IBrowserFile> loadedFiles = new();
        private bool isLoading;
        private string? ErrorMessage;
        private InputFile InptuFileElement { get; set; }
        private ElementReference dropZoneElement { get; set; }
        private string SmallStyleCSS
        {
            get
            {
                if (SmallStyle)
                {
                    return "small-style";
                }

                return "";
            } 
        }

        IJSObjectReference _module;
        IJSObjectReference _dropZoneInstance;

        protected override void OnInitialized()
        {
            isLoading = false;

            StateHasChanged();

            base.OnInitialized();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/UploadComponent/upload-component.js");
                _dropZoneInstance = await _module.InvokeAsync<IJSObjectReference>("initializeFileDropZone", dropZoneElement, InptuFileElement.Element);
            }
        }
        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            isLoading = true;
            loadedFiles.Clear();
            ErrorMessage = null;

            foreach (var file in e.GetMultipleFiles())
            {
                try
                {
                    if (string.IsNullOrEmpty(file.ContentType))
                    {
                        continue;
                    }

                    if (FileInfoList == null)
                    {
                        FileInfoList = new List<FILE_FileInfo>();
                    }

                    if (!Multiple)
                    {
                        FileInfoList.Clear();
                    }

                    loadedFiles.Add(file);

                    var trustedFileNameForFileStorage = Path.GetRandomFileName();

                    await using MemoryStream fs = new MemoryStream();
                    await file.OpenReadStream(100000000).CopyToAsync(fs);

                    var fi = new FILE_FileInfo();

                    fi.ID = Guid.NewGuid();
                    fi.CreationDate = DateTime.Now;
                    fi.FileExtension = Path.GetExtension(file.Name);
                    fi.FileName = Path.GetFileNameWithoutExtension(file.Name);
                    fi.Size = file.Size;

                    if (SessionWrapper.CurrentUser != null)
                    {
                        fi.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                    }

                    var fstorage = new FILE_FileStorage();

                    fstorage.ID = Guid.NewGuid();
                    fstorage.CreationDate = DateTime.Now;
                    fstorage.FILE_FileInfo_ID = fi.ID;
                    fstorage.FileImage = fs.ToArray();

                    fi.FILE_FileStorage = new List<FILE_FileStorage>();
                    fi.FILE_FileStorage.Add(fstorage);

                    if (FileInfoList == null)
                    {
                        FileInfoList = new List<FILE_FileInfo>();
                    }
                    FileInfoList.Add(fi);

                    Subscriber.FILE_FileInfo_SpecialMenu_ID = FileInfoList.FirstOrDefault().ID;
                    StateHasChanged();

                }
                catch (Exception ex)
                {
                    ErrorMessage = TextProvider.Get("FILE_UPLOAD_ERROR");
                }
            }

            StateHasChanged();

            await OnUpload.InvokeAsync();

            isLoading = false;
            StateHasChanged();
        }
        private async void RemoveFile(Guid FILE_FileInfo_ID)
        {
            if (RemoveAreYouSure)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("FILE_REMOVE_DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;
            }

            var itemToRemove = FileInfoList.FirstOrDefault(p => p.ID == FILE_FileInfo_ID);

            if (itemToRemove != null) 
            {
                FileInfoList.Remove(itemToRemove);
            }

            await OnRemove.InvokeAsync(FILE_FileInfo_ID);
            Subscriber.FILE_FileInfo_SpecialMenu_ID = null;

            StateHasChanged();
        }
        private async void DownloadFile(Guid File_FileInfo_ID)
        {
            var filetoDownload = FileInfoList.FirstOrDefault(p => p.ID == File_FileInfo_ID);

            if (filetoDownload != null)
            {
                FILE_FileStorage? blob = null;
                if (filetoDownload.FILE_FileStorage != null && filetoDownload.FILE_FileStorage.Count() > 0)
                {
                    blob = filetoDownload.FILE_FileStorage.FirstOrDefault();
                }
                else
                {
                    blob = await FileProvider.GetFileStorageAsync(filetoDownload.ID);
                }

                if (blob != null && blob.FileImage != null) 
                {
                    await EnviromentService.DownloadFile(blob.FileImage, filetoDownload.FileName + filetoDownload.FileExtension);
                }

                StateHasChanged();
            }
        }
        public async ValueTask DisposeAsync()
        {
            if (_dropZoneInstance != null)
            {
                try
                {
                    await _dropZoneInstance.InvokeVoidAsync("dispose");
                    await _dropZoneInstance.DisposeAsync();
                }
                catch { }
            }

            if (_module != null)
            {
                await _module.DisposeAsync();
            }
        }
        private async Task<bool> OpenFileDialog()
        {
            return await EnviromentService.ClickElement("fileInput-label-" + ID);
        }
        private string? GetBase64Image(FILE_FileInfo FILE_FileInfo) 
        {
            if (FILE_FileInfo != null) 
            {
                var FileStorage = FileProvider.GetFileStorage(FILE_FileInfo.ID);

                if(FILE_FileInfo.FILE_FileStorage != null && FILE_FileInfo.FILE_FileStorage.Count() > 0)
                {
                    return string.Format("data:image/" + FILE_FileInfo.FileExtension.Replace(".", "") + ";base64,{0}", Convert.ToBase64String(FILE_FileInfo.FILE_FileStorage.FirstOrDefault().FileImage));
                }
                else if (FILE_FileInfo.FileExtension != null && FileStorage != null)
                {
                    return string.Format("data:image/" + FILE_FileInfo.FileExtension.Replace(".", "") + ";base64,{0}", Convert.ToBase64String(FileStorage.FileImage));
                }
            }

            return null;
        }
    }
}
