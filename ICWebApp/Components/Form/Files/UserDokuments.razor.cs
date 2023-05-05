using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.Form.Files
{
    public partial class UserDokuments
    {
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IFORMApplicationProvider FORMApplicationProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public FORM_Application? Application { get; set; }

        private List<FILE_FileInfo> ExistingFiles = new List<FILE_FileInfo>();
        private List<FORM_Application_Archive> UserDocuments = new List<FORM_Application_Archive>();

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                await GetData();
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            if(Application != null)
            {
                UserDocuments = await FORMApplicationProvider.GetArchive(Application.ID);

                ExistingFiles = new List<FILE_FileInfo>();

                foreach (var doc in UserDocuments) 
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
            }

            return true;
        }
        private async Task<bool> FileUploaded(Guid ID)
        {
            if (Application != null) 
            {
                foreach (var f in ExistingFiles)
                {
                    await FileProvider.SetFileInfo(f);

                    if (UserDocuments.FirstOrDefault(p => p.FILE_FileInfo_ID == f.ID) == null)
                    {
                        FORM_Application_Archive doc = new FORM_Application_Archive();

                        doc.ID = Guid.NewGuid();
                        doc.AUTH_Users_ID = Application.AUTH_Users_ID;
                        doc.FORM_Application_ID = Application.ID;
                        doc.CreationDate = DateTime.Now;
                        doc.FILE_FileInfo_ID = f.ID;

                        await FORMApplicationProvider.SetArchive(doc);
                    }
                }

                await GetData();
                StateHasChanged();
            }

            return true;
        }
        private async Task<bool> FileRemoved(Guid ID)
        {
            await FileProvider.RemoveFileInfo(ID);

            var tempDoc = UserDocuments.FirstOrDefault(p => p.FILE_FileInfo_ID == ID);

            if(tempDoc != null)
            {
                await FORMApplicationProvider.RemoveArchive(tempDoc.ID);
            }

            return true;
        }
    }
}
