using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.DataStore.PagoPA.Domain;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.User.Frontend
{
    public partial class ServiceItem
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public ServiceDataItem Item { get; set; }
        [Parameter] public Guid ParentID { get; set; }

        public Guid ItemID = Guid.NewGuid();
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
        private void ReadMessage()
        {
            if (Item.MessageIsRead != null && !Item.MessageIsRead.Value)
            {
                if (Item.ReadMessage != null)
                {
                    Item.MessageIsRead = true;
                    Item.ReadMessage();
                    StateHasChanged();
                }
            }
            
        }
    }
}
