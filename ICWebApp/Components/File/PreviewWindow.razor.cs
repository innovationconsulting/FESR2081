using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;


namespace ICWebApp.Components.File
{
    public partial class PreviewWindow
    {
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ITEXTProvider TextProvider{ get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        private int _screenWidth = 750;
        protected override async Task OnInitializedAsync()
        {
            EnviromentService.OnShowDownloadPreviewWindow += EnviromentService_OnShowDownloadPreviewWindow;
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _screenWidth = await JSRuntime.InvokeAsync<int>("getRootWidth");
                StateHasChanged();
            }
        }
        private void EnviromentService_OnShowDownloadPreviewWindow()
        {
            StateHasChanged();
        }
        private void HidePreview()
        {
            EnviromentService.ShowDownloadPreviewWindow = false;
            StateHasChanged();
        }
    }
}
