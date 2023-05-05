using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Telerik.Blazor.Components;

namespace ICWebApp.Components.File
{
    public partial class PDFViewer
    {
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IJSRuntime JsRuntime { get; set; }
        [Parameter] public Guid? FILE_FileInfoID { get; set; }
        [Parameter] public bool DisplayPreviewAnySize { get; set; } = false;

        private FILE_FileStorage? FileStorage;
        private FILE_FileInfo? FileInfo;
        private int _screenWidth = 750;
        private bool WindowVisible { get; set; } = false;
        private bool RerenderViewer = true;

        private List<string> AllowedImageExtensions = new List<string>()
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".svg",
            ".webp",
            ".gif",
        };
        protected override async Task OnParametersSetAsync()
        {
            if(FILE_FileInfoID != null)
            {
                FileInfo = await FileProvider.GetFileInfoAsync(FILE_FileInfoID.Value);
                FileStorage = await FileProvider.GetFileStorageAsync(FILE_FileInfoID.Value);


                RerenderViewer = false;
                StateHasChanged();

                Thread.Sleep(1);

                RerenderViewer = true;
                StateHasChanged();
            }

            await base.OnParametersSetAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _screenWidth = await JsRuntime.InvokeAsync<int>("getRootWidth");
                StateHasChanged();
            }
        }
        private void ShowFullScreen()
        {
            WindowVisible = true;
            StateHasChanged();
        }
        private void HideFullScreen()
        {
            WindowVisible = false;
            StateHasChanged();
        }
        private async void DownloadFile(Guid File_FileInfo_ID, bool force=false)
        {
            var filetoDownload = FileInfo;

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
                    await EnviromentService.DownloadFile(blob.FileImage, filetoDownload.FileName + filetoDownload.FileExtension, force);
                }

                StateHasChanged();
            }
        }
        private decimal CalcZoom()
        {
            const int x0 = 350;
            const double y0 = .50;
            const int x1 = 1000;
            const double y1 = 1.50;
            var x = _screenWidth;
            var y = y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
            y = double.Clamp(y, 0.5, 1.5);
            return (decimal)y;
        }
    }
}
