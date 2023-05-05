using Freshdesk;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Pages.Legal.Developer
{
    public partial class Privacy
    {
        [Inject] IPRIVProvider PrivacyProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }

        private List<FILE_FileInfo> ExistingFiles = new List<FILE_FileInfo>();
        private List<PRIV_Backend_Privacy> Documents = new List<PRIV_Backend_Privacy>();

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_PRIVACY_DEVELOPER");

            await GetData();                 
            
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            Documents = await PrivacyProvider.GetPrivacyDocuments();

            ExistingFiles = new List<FILE_FileInfo>();

            foreach (var doc in Documents)
            {
                if (doc.FILE_FileInfo_ID != null)
                {
                    var file = await FileProvider.GetFileInfoAsync(doc.FILE_FileInfo_ID.Value);

                    if (file != null)
                    {
                        ExistingFiles.Add(file);
                    }
                }
            }
            

            return true;
        }
        private async Task<bool> FileUploaded(Guid ID)
        {
            foreach (var f in ExistingFiles)
            {
                await FileProvider.SetFileInfo(f);

                if (Documents.FirstOrDefault(p => p.FILE_FileInfo_ID == f.ID) == null)
                {
                    PRIV_Backend_Privacy doc = new PRIV_Backend_Privacy();

                    doc.ID = Guid.NewGuid();
                    doc.FILE_FileInfo_ID = f.ID;

                    await PrivacyProvider.SetPrivacyDocuments(doc);
                }
            }

            await GetData();
            StateHasChanged();
            

            return true;
        }
        private async Task<bool> FileRemoved(Guid ID)
        {
            await FileProvider.RemoveFileInfo(ID);

            var tempDoc = Documents.FirstOrDefault(p => p.FILE_FileInfo_ID == ID);

            if (tempDoc != null)
            {
                await PrivacyProvider.RemovePrivacyDocuments(tempDoc.ID);
            }

            return true;
        }
    }
}
