using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Components.FormRenderer;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Stripe;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;
using Telerik.Blazor.Components.Map;

namespace ICWebApp.Pages.Form.Frontend.Mantainance
{
    public partial class Mantainance
    {
        [Inject] ID3Helper D3Helper { get; set; }
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
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IGeoService GeoService { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] ITASKService TaskService { get; set; }

        [Parameter] public string DefinitionID { get; set; }
        private AUTH_Authority? Authority { get; set; }
        private FORM_Definition? Definition { get; set; }
        private FORM_Application? Data { get; set; }
        private AUTH_Municipality? Municipality { get; set; }
        private List<FORM_Application_Upload>? UploadElements { get; set; }
        private List<FORM_Definition_Upload>? UploadFilesDefinitions { get; set; }
        private List<FORM_Definition_Ressources>? DataRessources;
        private List<FORM_Definition_Property>? DataProperties;
        private List<FORM_Application_Priority>? PriorityList;
        private bool IsValid = true;
        /*private List<IEditorTool> Tools { get; set; } =
       new List<IEditorTool>()
       {
            new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
            new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
            new UnorderedList(),
            new EditorButtonGroup(new CreateLink(), new Unlink())
       };*/
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font" };
        public string[] Subdomains { get; set; } = new string[] { "a", "b", "c" };
        public string UrlTemplate { get; set; } = "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png";
        public string Attribution { get; set; } = "&copy; <a href='https://osm.org/copyright'>OpenStreetMap contributors</a>";
        private string _address = "";
        private double StartLan;
        private double StartLat;
        private bool GetLocRunning = false;

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

            SessionWrapper.PageTitle = Definition.FORM_Name;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Mantainance", "FRONTEND_MANTAINANCE_LIST", null);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), null, null, Definition.FORM_Name);

            Data = new FORM_Application();

            Data.ID = Guid.NewGuid();
            Data.FORM_Definition_ID = Definition.ID;
            Data.FORM_Application_Status_ID = Guid.Parse("151abd79-cf03-47a9-8719-d2d1eac7152c"); //INCOMPLETE
            Data.CreatedAt = DateTime.Now;

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

                    if (!string.IsNullOrEmpty(dbUser.MobilePhone))
                    {
                        Data.MobilePhone = dbUser.MobilePhone;
                    }
                    else
                    {
                        Data.MobilePhone = dbUser.Phone;
                    }

                    Data.RegisteredOffice = dbUser.RegisteredOffice;
                    Data.VATNumber = dbUser.VatNumber;
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

            Data.Mantainance_LanLat_Title = TextProvider.Get("FORM_MANTAINANCE_PIN_TITLE");
            if (Municipality != null)
            {
                Data.Mantainance_Lan = Municipality.Lng.ToString();
                Data.Mantainance_Lat = Municipality.Lat.ToString();
            }

            Data.FORM_Application_Priority_ID = Guid.Parse("f1af3d5a-7a02-4faa-8845-34d2a5a66785");

            if(Municipality != null && Data != null)
            {
                StartLat = Municipality.LatLan[0];
                StartLan = Municipality.LatLan[1];

                if (Data.Mantainance_LanLat != null)
                {
                    if (Data.Mantainance_LanLat[0] != null && Data.Mantainance_LanLat[1] != null)
                    {
                        StartLat = Data.Mantainance_LanLat[0].Value;
                        StartLan = Data.Mantainance_LanLat[1].Value;
                    }
                }
            }

            await SetAdress();
            await GetApplicationData();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            AnchorService.SkipForceReset = false;

            await base.OnParametersSetAsync();
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

                if (Definition != null)
                {
                    DataRessources = await FormDefinitionProvider.GetDefinitionRessourceList(Definition.ID);
                }

                if (Definition != null)
                {
                    DataProperties = await FormDefinitionProvider.GetDefinitionPropertyList(Definition.ID);
                }

                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                PriorityList = await FormApplicationProvider.GetPriorities();
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
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            bool result = true;

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

            if(Data != null && string.IsNullOrEmpty(Data.Mantainance_Title))
            {
                Data.Mantainance_Title_ErrorCode = "FORM_MANTAINANCE_TITLE_REQUIRED";
                result = false;
            }
            else
            {
                Data.Mantainance_Title_ErrorCode = null;
            }

            IsValid = result;

            if (!result)
            {
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();

                return;
            }

            Data.SubmitAt = DateTime.Now;
            if (Definition?.EstimateProcessingTime != null)
            {
                Data.EstimatedDeadline = DateTime.Now.AddDays(Definition.EstimateProcessingTime.Value);
            }
            if (Definition?.LegalDeadline != null)
            {
                Data.LegalDeadline = DateTime.Now.AddDays(Definition.LegalDeadline.Value);
            }
            
            Data.FORM_Application_Status_ID = FORMStatus.Comitted; //COMITTED            

            if (Data.FORM_Definition_ID != null)
            {
                var lastNumber = FormApplicationProvider.GetLatestProgressivNumber(Data.FORM_Definition_ID.Value, Data.AUTH_Municipality_ID.Value, DateTime.Now.Year);

                if (Data.ProgressivNumber == null || Data.ProgressivNumber == 0)
                {
                    Data.ProgressivYear = DateTime.Now.Year;

                    Data.ProgressivNumber = lastNumber + 1;
                }
            }

            await FormApplicationProvider.SetApplication(Data);

            await SaveFiles();

            if (Definition != null)
            {
                var tasksToCreate = await FormDefinitionProvider.GetDefinitionTaskList(Data.FORM_Definition_ID.Value);

                if (tasksToCreate != null)
                {
                    foreach (var task in tasksToCreate)
                    {
                        var responsible = await FormDefinitionProvider.GetDefinitionTaskResponsibleList(task.ID);

                        var authUsers = new List<AUTH_Users>();

                        if (responsible != null)
                        {
                            foreach (var resp in responsible)
                            {
                                var user = await AuthProvider.GetUser(resp.AUTH_Users_ID.Value);

                                if (user != null)
                                {
                                    authUsers.Add(user);
                                }
                            }
                        }

                        DateTime? deadline = null;

                        if (task.DeadlineDays != null)
                        {
                            deadline = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

                            deadline.Value.AddDays(task.DeadlineDays.Value);
                        }

                        var notifyCreator = Data.IsMunicipal || (Data.IsMunicipalCommittee ?? false);
                        var newTask = await TaskService.CreateTask(2, Data.ID.ToString(), SessionWrapper.CurrentUser.ID, notifyCreator, task.ID.ToString(), task.Description, NavManager.BaseUri + "/Backend/Form/Detail/" + Data.ID, deadline, authUsers);

                        var eskalations = await FormDefinitionProvider.GetDefinitionDeadlinesList(Data.FORM_Definition_ID.Value);

                        if (newTask != null && eskalations != null && eskalations.Count() > 0)
                        {
                            foreach (var esk in eskalations)
                            {
                                if (esk.FORM_Definition_Deadlines_TimeType_ID != null && esk.AdditionalDays != null)
                                {
                                    var defTargets = await FormDefinitionProvider.GetDefinitionDeadlinesTargetList(esk.ID);

                                    var resultDate = await FormDefinitionProvider.GetDefinitionEskalationDate(esk.FORM_Definition_Deadlines_TimeType_ID.Value, Data.SubmitAt, Data.LegalDeadline, Data.EstimatedDeadline, esk.AdditionalDays.Value);

                                    if (resultDate != null && defTargets != null && defTargets.Count() > 0)
                                    {
                                        await TaskService.CreateEskalation(newTask.ID, resultDate.Value, defTargets.Where(p => p.AUTH_Users_ID != null).Select(p => p.AUTH_Users_ID.Value).ToList());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var Parameters = new List<MSG_Message_Parameters>()
            {
                new MSG_Message_Parameters()
                {
                    Code = "{FormName}",
                    Message = Definition.FORM_Name ?? ""
                },
                new MSG_Message_Parameters()
                {
                    Code = "{Meldung}",
                    Message = Data.Mantainance_Title ?? ""
                },
                new MSG_Message_Parameters()
                {
                    Code ="{ProtocolNumber}",
                    Message = Definition.FormCode+"-"+Data.ProgressivYear+"-"+Data.ProgressivNumber
                }
            };

            if (Data.AUTH_Users_ID != null && Data.AUTH_Municipality_ID != null)
            {
                var msg = await MessageService.GetMessage(Data.AUTH_Users_ID.Value, Data.AUTH_Municipality_ID.Value, "MANTAINANCE_COMITTED_MAIL_TEXT", "MANTAINANCE_COMITTED_MAIL_SHORTTEXT", "MANTAINANCE_COMITTED_MAIL_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true, Parameters);

                if (msg != null)
                {

                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Form/Application/UserDetails/" + Data.ID);
                }

                //SEND TO AUTHORITY
                var authorityMsg = await MessageService.GetMessage(Data.AUTH_Users_ID.Value, Data.AUTH_Municipality_ID.Value, "MUN_MANTAINANCE_COMITTED_MAIL_TEXT", "MUN_MANTAINANCE_COMITTED_MAIL_SHORTTEXT", "MUN_MANTAINANCE_COMITTED_MAIL_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true, Parameters);

                if (authorityMsg != null && Definition.AUTH_Authority_ID != null)
                {
                    await MessageService.SendMessageToAuthority(Definition.AUTH_Authority_ID.Value, authorityMsg, NavManager.BaseUri + "/Backend/Form/Detail/" + Data.ID);
                }
            }
            NavManager.NavigateTo("/Mantainance/Comitted/" + Data.ID);
            //D3! (maintenance?) ok
            Task.Run(async () => await D3Helper.ProtocolNewFormApplication(Data)).ConfigureAwait(false);
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

        }

        private async Task<MSG_Message?> GetMaintenanceMessage()
        {
            return null;
        }

        private async Task<MSG_Message?> GetMunMaintenanceMessage()
        {
            return null;
        }
        private async Task<bool> SaveFiles()
        {
            if (UploadElements != null && UploadElements.Count() > 0 && Data != null)
            {
                foreach (var item in UploadElements)
                {
                    if (item != null)
                    {
                        if (item.FORM_Application_ID == null)
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

            if (existingElement != null)
            {
                await FormApplicationProvider.RemoveApplicationUploadFile(existingElement.ID);
            }

            await FileProvider.RemoveFileInfo(File_Info_ID);

            StateHasChanged();

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
        private async void MapClicked(MapClickEventArgs args)
        {
            var location = args.Location;

            if (Data != null && location != null)
            {
                Data.Mantainance_Lan = location.Longitude.ToString();
                Data.Mantainance_Lat = location.Latitude.ToString();

                await SetAdress();

                StateHasChanged();
            }
        }
        private async void SetGeoLocation()
        {
            string addressToGeocode = "";

            if (_address != null)
            {
                addressToGeocode = _address;
            }

            addressToGeocode += ", Italien";

            if (!string.IsNullOrEmpty(addressToGeocode))
            {
                var result = await GeoService.GetCoordinates(addressToGeocode);

                if (result != null && Data != null)
                {
                    Data.Mantainance_Lan = result.Coordinates.Longitude.ToString();
                    Data.Mantainance_Lat = result.Coordinates.Latitude.ToString();

                    StateHasChanged();
                }
            }
        }
        private async void SetCurrentLocation(GeoItem? _coordinates)
        {
            if (Data != null && _coordinates != null)
            {
                Data.Mantainance_Lan = _coordinates.Lan.ToString();
                Data.Mantainance_Lat = _coordinates.Lat.ToString();

                await SetAdress();

                StateHasChanged();
            }
        }
        private void GeoService_OnResponse()
        {
            var location = GeoService.GetGeoData();

            if (Data != null && location != null)
            {
                Data.Mantainance_Lan = location.Lan.ToString();
                Data.Mantainance_Lat = location.Lat.ToString();

                StartLan = location.Lan;
                StartLat = location.Lat;

                StateHasChanged();
            }

            GetLocRunning = false;
            StateHasChanged();
        }
        private void SetPriority(Guid Priority_ID)
        {
            if (Data != null)
            {
                Data.FORM_Application_Priority_ID = Priority_ID;
                StateHasChanged();
            }
        }
        private async Task<string> SetAdress()
        {
            if (Data != null && !string.IsNullOrEmpty(Data.Mantainance_Lat) && !string.IsNullOrEmpty(Data.Mantainance_Lan))
            {
                try
                {
                    var result = await GeoService.GetAddress(new Geocoding.Location(double.Parse(Data.Mantainance_Lat), double.Parse(Data.Mantainance_Lan)));

                    if (result != null)
                    {
                        _address = result.FormattedAddress;
                    }

                    StateHasChanged();
                }
                catch
                {
                }
            }
            return _address;
        }
    }
}
