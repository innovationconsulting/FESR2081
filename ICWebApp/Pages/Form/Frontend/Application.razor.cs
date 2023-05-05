using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Components.Dashboard;
using ICWebApp.Components.FormRenderer;
using ICWebApp.Components.FormTemplateEditor;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Charts.Chart.Internal;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class Application
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IAnchorService AnchorService { get; set; } 
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Inject] ITASKService TaskService { get; set; }
        [Inject] IJSRuntime JsRuntime { get; set; }
        [Inject] IFormApplicationService FormApplicationService { get; set; }

        [Parameter] public string DefinitionID { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string Credit { get; set; }
        private AUTH_Authority? Authority { get; set; }
        private FORM_Definition? Definition { get; set; }
        private FORM_Application? Data { get; set; }
        private List<FORM_Application_Upload>? UploadElements { get; set; }
        private List<FORM_Definition_Upload>? UploadFilesDefinitions { get; set; }
        private Container? FormContainer { get; set; }
        private bool IsInitializing = true;
        private bool IsValid = true;
        private PRIV_Privacy? Privacy { get; set; }
        private FORM_Definition_Template? Template { get; set; }
        private Editor? TemplateEditor { get; set; }
        private string? FormEditorValidationMessage { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (DefinitionID == null)
            {
                NavManager.NavigateTo("/");
                return;
            }

            AnchorService.ForceShow = true;
            AnchorService.SkipForceReset = true;
            StateHasChanged();

            await GetData();

            if (Definition == null)
            {
                NavManager.NavigateTo("/");
                return;
            }

            if (Definition.MultipleParallelApplications == false && SessionWrapper.CurrentUser != null && ID == "New")
            {
                Guid UserID = SessionWrapper.CurrentUser.ID;

                if(SessionWrapper.CurrentSubstituteUser != null)
                {
                    UserID = SessionWrapper.CurrentSubstituteUser.ID;
                }

                var exisitingApplications = await FormApplicationProvider.GetApplicationListByUser(UserID, Definition.ID);

                foreach (var app in exisitingApplications)
                {
                    var status = FormApplicationProvider.GetStatus(app.FORM_Application_Status_ID.Value);

                    if (status != null && status.FinishedStatus != true)
                    {
                        BusyIndicatorService.IsBusy = true;
                        NavManager.NavigateTo("/Form/Detail/" + DefinitionID);
                        StateHasChanged();
                        return;
                    }
                }
            }

            SessionWrapper.PageTitle = Definition.FORM_Name;
            SessionWrapper.PageSubTitle = Definition.ShortText;

            if (ID == "New")
            {
                CrumbService.ClearBreadCrumb();
                CrumbService.AddBreadCrumb("/Form", "FRONTEND_AUTHORITY_FORM_LIST", null);

                if (Authority != null)
                {
                    CrumbService.AddBreadCrumb("/Form/List/" + Authority.ID, null, null, Authority.Description);
                }

                CrumbService.AddBreadCrumb("/Form/Detail/" + Definition.ID, "FRONTEND_AUTHORITY_FORM", null, null);
                CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), null, null, Definition.FORM_Name);

                Data = new FORM_Application();

                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Definition.ID;
                Data.FORM_Application_Priority_ID = Guid.Parse("f1af3d5a-7a02-4faa-8845-34d2a5a66785"); //NO-PRIORITY
                Data.FORM_Application_Status_ID = Guid.Parse("151abd79-cf03-47a9-8719-d2d1eac7152c"); //INCOMPLETE
                Data.CreatedAt = DateTime.Now;

                if(Credit != null)
                {
                    Data.Credit = decimal.Parse(Credit);
                }

                if (SessionWrapper.CurrentUser != null)
                {
                    AUTH_Users_Anagrafic? dbUser = null;

                    if (SessionWrapper.CurrentSubstituteUser != null)
                    {
                        dbUser = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentSubstituteUser.ID);
                    }
                    else
                    {
                        dbUser = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentUser.ID);
                    }

                    if (dbUser != null)
                    {
                        Data.FirstName = dbUser.FirstName;
                        Data.LastName = dbUser.LastName;
                        Data.FiscalNumber = dbUser.FiscalNumber;
                        Data.Email = dbUser.Email;
                        Data.CountyOfBirth = dbUser.CountyOfBirth;
                        Data.PlaceOfBirth = dbUser.PlaceOfBirth;
                        Data.DateOfBirth = dbUser.DateOfBirth;
                        Data.Address = dbUser.Address;
                        Data.DomicileMunicipality = dbUser.DomicileMunicipality;
                        Data.DomicileNation = dbUser.DomicileNation;
                        Data.DomicilePostalCode = dbUser.DomicilePostalCode;
                        Data.DomicileProvince = dbUser.DomicileProvince;
                        Data.DomicileStreetAddress = dbUser.DomicileStreetAddress;
                        Data.Gender = dbUser.Gender;
                        Data.MobilePhone = dbUser.MobilePhone;
                        Data.RegisteredOffice = dbUser.RegisteredOffice;
                        Data.VATNumber = dbUser.VatNumber;

                        if (Data != null && Definition.HasIBAN == true && string.IsNullOrEmpty(Data.IBAN))
                        {
                            Data.IBAN = dbUser.IBAN;
                            Data.Bankname = dbUser.Bankname;
                            Data.KontoInhaber = dbUser.KontoInhaber;
                        }
                    }

                    if (SessionWrapper.CurrentSubstituteUser != null)
                    {
                        var dbRootUser = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentUser.ID);

                        if (dbRootUser != null)
                        {
                            Data.ROOT_AUTH_User_ID = dbRootUser.AUTH_Users_ID;
                            Data.ROOT_FirstName = dbRootUser.FirstName;
                            Data.ROOT_LastName = dbRootUser.LastName;
                            Data.ROOT_FiscalCode = dbRootUser.FiscalNumber;
                            Data.ROOT_CountyOfBirth = dbRootUser.CountyOfBirth;
                            Data.ROOT_PlaceOfBirth = dbRootUser.PlaceOfBirth;
                            Data.ROOT_DateOfBirth = dbRootUser.DateOfBirth;
                            Data.ROOT_Address = dbRootUser.Address;
                            Data.ROOT_DomicileMunicipality = dbRootUser.DomicileMunicipality;
                            Data.ROOT_DomicileNation = dbRootUser.DomicileNation;
                            Data.ROOT_DomicilePostalCode = dbRootUser.DomicilePostalCode;
                            Data.ROOT_DomicileProvince = dbRootUser.DomicileProvince;
                            Data.ROOT_DomicileStreetAddress = dbRootUser.DomicileStreetAddress;
                            Data.ROOT_Gender = dbRootUser.Gender;
                            Data.ROOT_MobilePhone = dbRootUser.MobilePhone;
                            Data.ROOT_Email = dbRootUser.Email;
                            Data.ROOT_Phone = dbRootUser.Phone;
                        }

                        if (dbUser != null && dbUser.GV_AUTH_Users_ID != null)
                        {
                            var dbGVUser = await AuthProvider.GetAnagraficByUserID(dbUser.GV_AUTH_Users_ID.Value);

                            if (dbGVUser != null)
                            {
                                Data.GV_AUTH_User_ID = dbGVUser.AUTH_Users_ID;
                                Data.GV_FirstName = dbGVUser.FirstName;
                                Data.GV_LastName = dbGVUser.LastName;
                                Data.GV_FiscalCode = dbGVUser.FiscalNumber;
                                Data.GV_CountyOfBirth = dbGVUser.CountyOfBirth;
                                Data.GV_PlaceOfBirth = dbGVUser.PlaceOfBirth;
                                Data.GV_DateOfBirth = dbGVUser.DateOfBirth;
                                Data.GV_Address = dbGVUser.Address;
                                Data.GV_DomicileMunicipality = dbGVUser.DomicileMunicipality;
                                Data.GV_DomicileNation = dbGVUser.DomicileNation;
                                Data.GV_DomicilePostalCode = dbGVUser.DomicilePostalCode;
                                Data.GV_DomicileProvince = dbGVUser.DomicileProvince;
                                Data.GV_DomicileStreetAddress = dbGVUser.DomicileStreetAddress;
                                Data.GV_Gender = dbGVUser.Gender;
                                Data.GV_MobilePhone = dbGVUser.MobilePhone;
                                Data.GV_Email = dbGVUser.Email;
                                Data.GV_Phone = dbGVUser.Phone;
                            }
                        }
                    }

                    if (SessionWrapper.CurrentSubstituteUser != null) 
                    { 
                        Data.AUTH_Users_ID = SessionWrapper.CurrentSubstituteUser.ID;
                    }
                    else
                    {
                        Data.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                    }

                    Data.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();
                }
            }
            else
            {
                CrumbService.ClearBreadCrumb();
                CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_MY_APPLICATIONS", null);
                CrumbService.AddBreadCrumb("/Form/Detail/" + Definition.ID, "FRONTEND_AUTHORITY_FORM", null, null);
                CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), null, null, Definition.FORM_Name);
            }

            if(Data != null && (Data.PayedAt != null || Data.SignedAt != null || Data.SubmitAt != null))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Form/Application/Preview/" + Data.ID);
                StateHasChanged();                
            }

            if(Definition != null && Definition.HasTemplate == true && Definition.FORM_Definition_Template_ID != null)
            {
                Template = await FormDefinitionProvider.GetDefinitionTemplate(Definition.FORM_Definition_Template_ID.Value);

                if(Template != null && Data != null && string.IsNullOrEmpty(Data.TemplateJsonData))
                {
                    Data.TemplateJsonData = Template.DefaultData;
                }

                IsInitializing = false;
            }

            await GetApplicationData();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            AnchorService.SkipForceReset = false;

            await base.OnParametersSetAsync();
        }
        private void OnContainerInitialized()
        {
            IsInitializing = false;
            StateHasChanged();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Definition = await FormDefinitionProvider.GetDefinition(Guid.Parse(DefinitionID));

                if (Definition != null && Definition.AUTH_Authority_ID != null)
                {
                    Authority = await AuthProvider.GetAuthority(Definition.AUTH_Authority_ID.Value);
                }

                UploadFilesDefinitions = await FormDefinitionProvider.GetDefinitionUploadList(Guid.Parse(DefinitionID));

                Privacy = await PrivProvider.GetPrivacy(SessionWrapper.AUTH_Municipality_ID.Value);

                if (!string.IsNullOrEmpty(ID) && ID != "New")
                {
                    Data = await FormApplicationProvider.GetApplication(Guid.Parse(ID));
                }
            }

            return true;
        }
        private async Task<bool> GetApplicationData()
        {
            if (UploadFilesDefinitions != null && Data != null)
            {
                UploadElements = new List<FORM_Application_Upload>();

                foreach (var def in UploadFilesDefinitions.OrderBy(p => p.SortOrder))
                {
                    var existingElement = await FormApplicationProvider.GetApplicationUploadByDefinition(def.ID, Data.ID);

                    if (existingElement != null)
                    {
                        existingElement.CACH_UploadFiles = new List<FILE_FileInfo>();

                        var items = await FormApplicationProvider.GetApplicationUploadFileList(existingElement.ID);

                        foreach (var i in items)
                        {
                            if (i.FILE_FileInfo_ID != null)
                            {
                                var item = await FileProvider.GetFileInfoAsync(i.FILE_FileInfo_ID.Value);

                                if (item != null)
                                {
                                    existingElement.CACH_UploadFiles.Add(item);
                                }
                            }
                        }

                        UploadElements.Add(existingElement);
                    }
                    else
                    {
                        UploadElements.Add(new FORM_Application_Upload()
                        {
                            ID = Guid.NewGuid(),
                            FORM_Application_ID = Data.ID,
                            FORM_Definition_Upload_ID = def.ID,
                            CACH_UploadFiles = new List<FILE_FileInfo>()
                        });
                    }
                }
            }

            return true;
        }
        private async void Commit()
        {
            if (FormContainer != null || TemplateEditor != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                bool result = true;

                if (FormContainer != null)
                {
                    result = await FormContainer.Validate();
                }

                if (UploadElements != null && UploadFilesDefinitions != null && UploadElements.Count() > 0)
                {
                    foreach (var item in UploadElements)
                    {
                        var def = UploadFilesDefinitions.FirstOrDefault(p => p.ID == item.FORM_Definition_Upload_ID);

                        if (def != null)
                        {
                            item.ERROR_CODE = null;

                            if (def.Required && (item.CACH_UploadFiles == null || item.CACH_UploadFiles.Count < 1))
                            {
                                item.ERROR_CODE = "FORM_ERROR_CODE_REQUIRED";
                                result = false;
                            }
                        }
                    }
                }

                Data.PrivacyErrorCSS = null;

                if (Definition != null && Data != null && Data.PrivacyReadAt == null)
                {
                    Data.PrivacyErrorCSS = "error";
                    result = false;
                }


                if (Definition != null && Definition.HasTemplate == true && TemplateEditor != null)
                {
                    var formValidationMessages = await TemplateEditor.Validate();

                    if(formValidationMessages != null && formValidationMessages.Count() > 0 )
                    {
                        FormEditorValidationMessage = null;

                        foreach (var message in formValidationMessages)
                        {
                            FormEditorValidationMessage += message + "<br>";
                        }
                        if (FormEditorValidationMessage != null)
                        {
                            FormEditorValidationMessage = FormEditorValidationMessage.Remove(FormEditorValidationMessage.Length - 4);
                        }

                        result = false;
                    }
                    else
                    {
                        FormEditorValidationMessage = null;
                    }
                }

                IsValid = result;

                if (!result)
                {
                    BusyIndicatorService.IsBusy = false;
                    StateHasChanged();

                    return;
                }

                if (Data != null)
                {
                    Data = await FormApplicationService.CheckApplication(Data);
                }

                if (Definition != null && Definition.HasTemplate == true && TemplateEditor != null)
                {
                    Data.TemplateJsonData = await TemplateEditor.SaveAsJson();

                    var fileArray = await TemplateEditor.SaveAsPDF();

                    if (fileArray != null)
                    {
                        var fi = new FILE_FileInfo();

                        fi.ID = Guid.NewGuid();
                        fi.FileName = Definition.FORM_Name;
                        fi.FileExtension = ".pdf";
                        fi.CreationDate = DateTime.Now;
                        fi.AUTH_Users_ID = Data.AUTH_Users_ID;
                        fi.Size = fileArray.Length;

                        var storage = new FILE_FileStorage();

                        storage.ID = Guid.NewGuid();
                        storage.FILE_FileInfo_ID = fi.ID;
                        storage.FileImage = fileArray;
                        storage.CreationDate = DateTime.Now;

                        fi = await FileProvider.SetFileInfo(fi);
                        storage = await FileProvider.SetFileStorage(storage);

                        if (fi != null)
                        {
                            Data.FILE_Fileinfo_ID = fi.ID;
                        }
                    }
                }

                await FormApplicationProvider.SetApplication(Data);

                await SaveFiles();

                if (FormContainer != null)
                {
                    await FormContainer.Save();
                }

                NavManager.NavigateTo("/Form/Application/Preview/" + Data.ID);

                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
            }
        }
        private async void Save()
        {
            if (Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (Definition != null && Definition.HasTemplate == true && TemplateEditor != null) 
                {
                    Data.TemplateJsonData = await TemplateEditor.SaveAsJson();
                }

                await FormApplicationProvider.SetApplication(Data);

                await SaveFiles();

                if (FormContainer != null)
                {
                    await FormContainer.Save();
                }

                NavManager.NavigateTo("/User/Services");
            }
        }
        private async Task<bool> SaveFiles()
        {
            if (UploadElements != null && UploadElements.Count() > 0 && Data != null)
            {
                foreach (var item in UploadElements)
                {
                    if(item != null)
                    {
                        if(item.FORM_Application_ID == null)
                        {
                            item.FORM_Application_ID = Data.ID;
                        }

                        await FormApplicationProvider.SetApplicationUpload(item);
                    }

                    if (item.CACH_UploadFiles != null && item.CACH_UploadFiles.Count() > 0)
                    {
                        foreach (var f in item.CACH_UploadFiles)
                        {
                            var existingElement = await FormApplicationProvider.GetApplicationUploadFileByFileID(f.ID);

                            if (existingElement != null)
                            {
                                await FileProvider.SetFileInfo(f);
                            }
                            else
                            {
                                var file = await FileProvider.SetFileInfo(f);

                                if (file != null)
                                {
                                    var newUploadFileElement = new FORM_Application_Upload_File();
                                    newUploadFileElement.ID = Guid.NewGuid();
                                    newUploadFileElement.FORM_Application_Upload_ID = item.ID;
                                    newUploadFileElement.FILE_FileInfo_ID = file.ID;

                                    await FormApplicationProvider.SetApplicationUploadFile(newUploadFileElement);
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
        private async Task<bool> OnRemove(Guid File_Info_ID)
        {
            var existingElement = await FormApplicationProvider.GetApplicationUploadFileByFileID(File_Info_ID);

            if(existingElement != null)
            {
                await FormApplicationProvider.RemoveApplicationUploadFile(existingElement.ID);
            }

            await FileProvider.RemoveFileInfo(File_Info_ID);

            StateHasChanged();

            return true;
        }
    }
}
