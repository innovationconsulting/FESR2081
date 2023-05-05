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


namespace ICWebApp.Components.Canteen
{
    public partial class UserDokuments
    {
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public string AUTH_Users_ID { get; set; }

        private CANTEEN_User? User;
        private List<FILE_FileInfo> ExistingFiles = new List<FILE_FileInfo>();
        private List<CANTEEN_User_Documents> UserDocuments = new List<CANTEEN_User_Documents>();

        protected override async Task OnInitializedAsync()
        {
            if (AUTH_Users_ID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                User = CanteenProvider.GetCanteenUserByID(Guid.Parse(AUTH_Users_ID), SessionWrapper.AUTH_Municipality_ID.Value);

                await GetData();
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            if(!string.IsNullOrEmpty(AUTH_Users_ID))
            {
                UserDocuments = await CanteenProvider.GetUserDocumentList(Guid.Parse(AUTH_Users_ID));

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
            if (User != null) 
            {
                foreach (var f in ExistingFiles)
                {
                    await FileProvider.SetFileInfo(f);

                    if (UserDocuments.FirstOrDefault(p => p.FILE_FileInfo_ID == f.ID) == null)
                    {
                        CANTEEN_User_Documents doc = new CANTEEN_User_Documents();

                        doc.ID = Guid.NewGuid();
                        doc.CANTEEN_User_ID = User.ID;
                        doc.FILE_FileInfo_ID = f.ID;

                        await CanteenProvider.SetUserDocument(doc);
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
                await CanteenProvider.RemoveUserDocument(tempDoc.ID);
            }

            return true;
        }
    }
}
