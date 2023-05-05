using DocumentFormat.OpenXml.Drawing.Charts;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using ICWebApp.Components.Rooms;
using ICWebApp.Pages.Form.Frontend;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;
using ICWebApp.Domain.Models;
using ICWebApp.Application.Interface;
using System.Security.Cryptography;
using System.Text.Json;
using RestSharp.Extensions;
using Newtonsoft.Json;
using ICWebApp.Application.Settings;
using System.Drawing.Text;

namespace ICWebApp.Pages.Rooms.Admin.Subpages
{
    public partial class RoomEdit
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IRoomGalerieHelper RoomGalerieHelper { get; set; }
        [Inject] IGeoService GeoService { get; set; }

        [CascadingParameter] public DialogFactory? Dialogs { get; set; }
        [Parameter] public string? RoomID { get; set; }
        [Parameter] public string? ParentID { get; set; }
        [Parameter] public string? AktiveIndex { get; set; }
        [Parameter] public string? PreviousAktiveIndex { get; set; }

        private ROOM_Rooms? Data;
        private List<LANG_Languages>? Languages;
        private Guid? CurrentLanguage;
        public string[] Subdomains = new string[] { "a", "b", "c" };
        public string UrlTemplate = "https://#= subdomain #.tile.openstreetmap.org/#= zoom #/#= x #/#= y #.png";
        private bool IsDataBusy = true;
        private int CurrentTab = 0;
        private List<IEditorTool> Tools =
        new List<IEditorTool>()
        {
            new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
            new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
            new UnorderedList(),
            new EditorButtonGroup(new CreateLink(), new Unlink()),
            new InsertTable(),
            new DeleteTable(),
            new EditorButtonGroup(new AddRowBefore(), new AddRowAfter(), new DeleteRow(), new MergeCells(), new SplitCell())
        };
        public List<string> RemoveAttributes = new List<string>() { "data-id" };
        public List<string> StripTags = new List<string>() { "font" };
        private List<MapLocationItem>? MapLocation;
        private List<ROOM_RoomGalerie>? RoomGalerieList;
        private AUTH_Municipality? Municipality;
        private double StartLan;
        private double StartLat;
        private ROOM_RoomGalerie? RoomGalerieItem;
        private List<FILE_FileInfo> FileList = new List<FILE_FileInfo>();
        private bool ShowGalerieWindow = false;
        private List<V_ROOM_Rooms>? AdditionalRoomList;
        private List<V_ROOM_RoomOptions>? RoomOptionList;
        private ROOM_RoomOptions? RoomOptionItem;       
        private bool ShowRoomOptionWindow  = false;
        private List<V_AUTH_Company_Type> CompanyTypeList = new List<V_AUTH_Company_Type>();
        private List<ROOM_RoomPricing> RoomPricing = new List<ROOM_RoomPricing>();    
        private bool ShowRoomInventoryPositions = false;
        private Guid? RoomOptionID = null;
        private List<V_ROOM_RoomPricing_Type> PricingType = new List<V_ROOM_RoomPricing_Type>();
        private TelerikMap? Map;
        private List<V_ROOM_RoomOptionsCategory> OptionCategoryList = new List<V_ROOM_RoomOptionsCategory>();
        private List<ROOM_Rooms_Contact> RoomContacts = new List<ROOM_Rooms_Contact>();
        private bool ShowRoomOptionImport = false;
        private List<ROOM_RoomOptions> ImportableOptions = new List<ROOM_RoomOptions>();
        private IEnumerable<ROOM_RoomOptions> SelectedImportableOptions = new List<ROOM_RoomOptions>();
        private string? InventoryMinValueError = null;
        private bool ShowRoomInventoryPositionsList = false;
        private List<V_ROOM_RoomOptions>? RoomInventoryList;
        private bool ShowRoomInventoryPositionsQuickAdd = false;
        private ROOM_RoomOptions_Positions? NewInventoryPosition = null;

        private bool Italian
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = Guid.Parse("e450421a-baff-493e-a390-71b49be6485f");
                    StateHasChanged();
                }
            }
        }
        private bool German
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");
                    StateHasChanged();
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            OptionCategoryList = await RoomProvider.GetRoomOptionsCategories();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var settings = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

                if (settings != null && !string.IsNullOrEmpty(settings.DefaultRoomOptions))
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<List<ROOM_RoomOptions>>(settings.DefaultRoomOptions);

                        if (result != null)
                        {
                            ImportableOptions = result;
                        }
                    }
                    catch { }
                }
            }

            await base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            IsDataBusy = true;

            CurrentTab = 0;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Rooms", "MAINMENU_ROOMBOOKING", null, null);

            if (SessionWrapper.AUTH_Municipality_ID == null)
            {
                ReturnToPreviousPage();
                return;
            }

            Languages = await LangProvider.GetAll();

            if (Languages != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            if (RoomID == "New")
            {
                SessionWrapper.PageTitle = TextProvider.Get("ROOMBOOKING_ROOM_CREATE");

                Data = new ROOM_Rooms();

                Data.ID = Guid.NewGuid();
                Data.MunicipalityID = SessionWrapper.AUTH_Municipality_ID.Value;
                Data.PricingType = Guid.Parse("cc78621e-c084-469f-ba73-594eca519f94");
                Data.BufferTime = 0;

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        var dataE = new ROOM_Rooms_Extended()
                        {
                            ID = Guid.NewGuid(),
                            ROOM_Rooms_ID = Data.ID,
                            LANG_Languages_ID = l.ID
                        };

                        if (Data.ROOM_Rooms_Extended == null)
                        {
                            Data.ROOM_Rooms_Extended = new List<ROOM_Rooms_Extended>();
                        }

                        Data.ROOM_Rooms_Extended.Add(dataE);
                    }
                }

                if(ParentID != null)
                {
                    Data.RoomGroupFamilyID = Guid.Parse(ParentID);
                }
            }
            else
            {
                Data = await GetRoom();

                SessionWrapper.PageTitle = TextProvider.Get("ROOMBOOKING_ROOM_EDIT");

                if(Data != null && Data.ROOM_Rooms_Extended != null && Data.ROOM_Rooms_Extended.Count() > 0 && CurrentLanguage != null)
                {
                    SessionWrapper.PageTitle += " - " + Data.ROOM_Rooms_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Name;
                }
            }

            await GetData();
            await GetGalerieData();

            if (string.IsNullOrEmpty(ParentID))
            {
                await GetAdditionalRoomData();
                CurrentTab = 0;
            }
            else
            {
                Data.HasRooms = false;
                CurrentTab = 0;
            }

            if (Municipality != null && Data != null && (Data.Lng == null || Data.Lat == null))
            {
                Data.Lng = Municipality.Lng;
                Data.Lat = Municipality.Lat;
            }

            if (Data != null && Data.Lat != null && Data.Lng != null)
            {
                StartLat = Data.Lat.Value;
                StartLan = Data.Lng.Value;

                MapLocation = new List<MapLocationItem>()
                {
                    new MapLocationItem()
                    {
                        Lat = StartLat,
                        Lan = StartLan
                    }
                };
            }

            await GetRoomOptionData();
            await GetRoomPricing();

            if (Data != null)
            {
                RoomContacts = await RoomProvider.GetRoomsContacts(Data.ID);
            }

            PricingType = await RoomProvider.GetRoomPricingType();

            if (!string.IsNullOrEmpty(AktiveIndex))
            {
                StateHasChanged();
                CurrentTab = int.Parse(AktiveIndex);
            }

            if(PreviousAktiveIndex == null)
            {
                PreviousAktiveIndex = "0";
            }

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async Task<ROOM_Rooms?> GetRoom()
        {
            return await RoomProvider.GetRoom(Guid.Parse(RoomID));
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return true;
        }
        private async Task<bool> GetGalerieData()
        {
            if (Data != null)
            {
                RoomGalerieList = await RoomProvider.GetRoomGalerie(null, Data.ID);
            }

            return true;
        }
        private async Task<bool> GetAdditionalRoomData()
        {
            AdditionalRoomList = await RoomProvider.GetAllRoomsByBuildingID(Data.ID);

            return true;  
        }
        private async Task<bool> GetRoomOptionData()
        {
            RoomOptionList = await RoomProvider.GetRoomOptionsList(Data.ID);

            return true;
        }
        private async Task<bool> GetRoomPricing()
        {
            if (Data != null)
            {
                CompanyTypeList = await AuthProvider.GetVCompanyType();

                if (CompanyTypeList != null)
                {
                    RoomPricing = await RoomProvider.GetRoomPricing(Data.ID);

                    if(RoomPricing == null)
                    {
                        RoomPricing = new List<ROOM_RoomPricing>();
                    }

                    CompanyTypeList = CompanyTypeList.Where(p => p.ShowOnRooms == true).ToList();

                    var pricingToAdd = CompanyTypeList.Where(p => !RoomPricing.Select(x => x.AUTH_Company_Type_ID).Contains(p.ID)).ToList();

                    foreach(var newPrice in pricingToAdd)
                    {
                        var price = new ROOM_RoomPricing();

                        price.CreationDate = DateTime.Now;
                        price.ROOM_Rooms_ID = Data.ID;
                        price.AUTH_Company_Type_ID = newPrice.ID;
                        price.ID = Guid.NewGuid();

                        if(newPrice.IsPrivate == true)
                        {
                            price.Default = true;
                        }

                        RoomPricing.Add(price);
                    }
                }
            }

            return true;
        }
        private async void MapClicked(MapClickEventArgs args)
        {
            var location = args.Location;

            if (Data != null && location != null)
            {
                Data.Lng = Convert.ToDouble(location.Longitude);
                Data.Lat = Convert.ToDouble(location.Latitude);

                MapLocation = new List<MapLocationItem>()
                {
                    new MapLocationItem()
                    {
                        Lat = Data.Lat.Value,
                        Lan = Data.Lng.Value
                    }
                };

                StartLat = Data.Lat.Value;
                StartLan = Data.Lng.Value;

                var result = await GeoService.GetAddress(new Geocoding.Location(Data.Lat.Value, Data.Lng.Value));

                if(result != null)
                {
                    Data.ROOM_Rooms_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Address = result.FormattedAddress;
                }

                StateHasChanged();
            }
        }
        private async void SetGeoLocation()
        {
            string addressToGeocode = "";

            if(Data.ROOM_Rooms_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Address != null)
            {
                addressToGeocode = Data.ROOM_Rooms_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Address;
            }

            if(Data.ROOM_Rooms_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Location != null)
            {
                addressToGeocode += ", " + Data.ROOM_Rooms_Extended.FirstOrDefault(p => p.LANG_Languages_ID == CurrentLanguage).Location;
            }

            addressToGeocode += ", Italien";

            if (!string.IsNullOrEmpty(addressToGeocode))
            {
               var result = await GeoService.GetCoordinates(addressToGeocode);

                if(result != null && Data != null)
                {
                    Data.Lng = Convert.ToDouble(result.Coordinates.Longitude);
                    Data.Lat = Convert.ToDouble(result.Coordinates.Latitude);

                    StartLan = Data.Lng.Value;
                    StartLat = Data.Lat.Value;

                    MapLocation = new List<MapLocationItem>()
                    {
                        new MapLocationItem()
                        {
                            Lat = Data.Lat.Value,
                            Lan = Data.Lng.Value
                        }
                    };

                    StateHasChanged();
                }
            }
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;

            if (ParentID != null)
            {
                NavManager.NavigateTo("/RoomEditAdmin/" + ParentID + "/" + PreviousAktiveIndex);
                StateHasChanged();
            }
            else
            {
                NavManager.NavigateTo("/RoomManagementAdmin");
                StateHasChanged();
            }
        }
        private async Task<bool> SaveRoom()
        {
            if (Data != null)
            {
                await RoomProvider.SetRoom(Data);

                if(RoomContacts != null)
                {
                    var existingContacts = await RoomProvider.GetRoomsContacts(Data.ID);

                    foreach(var cont in existingContacts)
                    {
                        await RoomProvider.RemoveRoomsContacts(cont.ID);
                    }

                    foreach(var cont in RoomContacts)
                    {
                        await RoomProvider.SetRoomsContacts(cont);
                    }
                }

                foreach (var ext in Data.ROOM_Rooms_Extended)
                {
                    await RoomProvider.SetRoomExtended(ext);
                }

                foreach(var price in RoomPricing)
                {
                    await RoomProvider.UpdateRoomPricing(price);
                }
            }

            return true;
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveRoom();

            if (ParentID != null)
            {
                NavManager.NavigateTo("/RoomEditAdmin/" + ParentID + "/" + PreviousAktiveIndex);
                StateHasChanged();
            }
            else
            {
                NavManager.NavigateTo("/RoomManagementAdmin");
                StateHasChanged();
            }
        }
        private async void DeleteImage(ROOM_RoomGalerie Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.RemoveGalerie(Item);
                await GetGalerieData();
                StateHasChanged();
            }
        }
        private async void AddGalerieImage()
        {
            await SaveRoom();

            RoomGalerieItem = new ROOM_RoomGalerie();
            RoomGalerieItem.ID = Guid.NewGuid();
            FileList = new List<FILE_FileInfo>();
            ShowGalerieWindow = true;
            StateHasChanged();
        }
        private async void SaveGalerie()
        {
            if (Data != null && RoomGalerieItem != null)
            {
                int lastPos = 0;
                bool FirstImageSet = false;

                if (RoomGalerieList != null && RoomGalerieList.OrderBy(o => o.Pos).LastOrDefault() != null && RoomGalerieList.OrderBy(o => o.Pos).LastOrDefault().Pos != null)
                {
                    lastPos = RoomGalerieList.OrderBy(o => o.Pos).LastOrDefault().Pos.Value;
                }

                lastPos++;

                foreach (var item in FileList)
                {
                    var roomGalerieItem = new ROOM_RoomGalerie();
                    roomGalerieItem.ID = Guid.NewGuid();
                    roomGalerieItem.Pos = lastPos;

                    var existingItemFromDBList = await RoomProvider.GetRoomGalerie(RoomGalerieItem.ID,null);
                    var existingItemFromDB = existingItemFromDBList.FirstOrDefault();
                    if (existingItemFromDB != null)
                    {
                        roomGalerieItem = existingItemFromDB;
                    }
                    else
                    {
                        roomGalerieItem.ID = Guid.NewGuid();
                    }

                    if (RoomGalerieList != null && RoomGalerieList.FirstOrDefault(p => p.FirstImage == true) == null && !FirstImageSet) 
                    {
                        roomGalerieItem.FirstImage = true;
                        FirstImageSet = true;
                    }

                    roomGalerieItem.Copyright = RoomGalerieItem.Copyright;
                    roomGalerieItem.FileName = item.FileName;
                    roomGalerieItem.FileType = item.FileExtension;
                    roomGalerieItem.FILE_FileInfo = item.ID;
                    roomGalerieItem.ROOM_Room_ID = Data.ID;
                    
                    roomGalerieItem.ServerPath = "/RoomImages/" + roomGalerieItem.FileName + roomGalerieItem.FileType;

                    if (existingItemFromDB == null)
                    {
                        await FileProvider.SetFileInfo(item);
                    }
                    
                    await RoomProvider.UpdateRoomGalerie(roomGalerieItem);

                    lastPos++;
                }

                await GetGalerieData();
            }


            ShowGalerieWindow = false;
            StateHasChanged();
        }
        private void CloseGalerieWindow()
        {
            ShowGalerieWindow = false;
            StateHasChanged();
        }
        private async void MoveUpImage(ROOM_RoomGalerie opt)
        {
            if (RoomGalerieList != null && RoomGalerieList.Count() > 0)
            {
                var newPos = RoomGalerieList.FirstOrDefault(p => p.Pos == opt.Pos - 1);

                if (newPos != null)
                {
                    opt.Pos = opt.Pos - 1;
                    newPos.Pos = newPos.Pos + 1;
                    await RoomProvider.UpdateRoomGalerie(opt);
                    await RoomProvider.UpdateRoomGalerie(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownImage(ROOM_RoomGalerie opt)
        {
            if (RoomGalerieList != null && RoomGalerieList.Count() > 0)
            {
                var newPos = RoomGalerieList.FirstOrDefault(p => p.Pos == opt.Pos + 1);

                if (newPos != null)
                {
                    opt.Pos = opt.Pos + 1;
                    newPos.Pos = newPos.Pos - 1;

                    await RoomProvider.UpdateRoomGalerie(opt);
                    await RoomProvider.UpdateRoomGalerie(newPos);
                }
            }

            StateHasChanged();
        }
        private async void SetFirstImage(ROOM_RoomGalerie opt)
        {
            if(Data != null && RoomGalerieList != null)
            {
                foreach(var img in RoomGalerieList)
                {
                    img.FirstImage = false;
                    await RoomProvider.UpdateRoomGalerie(img);
                }

                opt.FirstImage = true;

                await RoomProvider.SetRoom(Data);
                await RoomProvider.UpdateRoomGalerie(opt);
                await GetGalerieData();

                StateHasChanged();
            }
        }
        private async void EditGalerieImage(ROOM_RoomGalerie opt)
        {
            await SaveRoom();

            RoomGalerieItem = opt;

            FileList = new List<FILE_FileInfo>();

            var fileinfo =  FileProvider.GetFileInfo(opt.FILE_FileInfo??Guid.Empty);
            FileList.Add(fileinfo);
            ShowGalerieWindow = true;
            StateHasChanged();
        }
        private void OnWizardChanged()
        {
            StateHasChanged();
        }
        private async void AddAdditionalRoom()
        {
            if (Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await SaveRoom();

                NavManager.NavigateTo("/RoomEditAdmin/New/" + 0 + "/" + CurrentTab + "/" + Data.ID);
                StateHasChanged();
            }
        }
        private async void EditAdditionalRoom(V_ROOM_Rooms Item)
        {
            if (Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                await RoomProvider.SetRoom(Data);

                foreach (var ext in Data.ROOM_Rooms_Extended)
                {
                    await RoomProvider.SetRoomExtended(ext);
                }

                NavManager.NavigateTo("/RoomEditAdmin/" + Item.ID + "/" + 0 + "/" + CurrentTab + "/" + Data.ID);
                StateHasChanged();
            }
        }
        private async void DeleteAdditionalRoom(V_ROOM_Rooms Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.RemoveRoom(Item.ID);
                await GetAdditionalRoomData();
                StateHasChanged();
            }
        }
        private async void MoveUpOption(V_ROOM_RoomOptions opt)
        {
            if (RoomOptionList != null && RoomOptionList.Count() > 0 && opt.ID != null)
            {
                var dbItem = await RoomProvider.GetRoomOption(opt.ID);
                var newPos = RoomOptionList.FirstOrDefault(p => p.Pos == opt.Pos - 1);

                if (newPos != null && newPos.ID != null && dbItem != null)
                {
                    var newDBPos = await RoomProvider.GetRoomOption(newPos.ID);

                    if (newDBPos != null)
                    {
                        dbItem.Pos = dbItem.Pos - 1;
                        newDBPos.Pos = newDBPos.Pos + 1;

                        await RoomProvider.SetRoomOption(dbItem);
                        await RoomProvider.SetRoomOption(newDBPos);
                    }
                }

                await GetRoomOptionData();
            }

            StateHasChanged();
        }
        private async void MoveDownOption(V_ROOM_RoomOptions opt)
        {
            if (RoomOptionList != null && RoomOptionList.Count() > 0 && opt.ID != null)
            {
                var dbItem = await RoomProvider.GetRoomOption(opt.ID);
                var newPos = RoomOptionList.FirstOrDefault(p => p.Pos == opt.Pos + 1);

                if (newPos != null && newPos.ID != null && dbItem != null)
                {
                    var newDBPos = await RoomProvider.GetRoomOption(newPos.ID);

                    if (newDBPos != null)
                    {
                        dbItem.Pos = dbItem.Pos + 1;
                        newDBPos.Pos = newDBPos.Pos - 1;

                        await RoomProvider.SetRoomOption(dbItem);
                        await RoomProvider.SetRoomOption(newDBPos);
                    }
                }

                await GetRoomOptionData();
            }

            StateHasChanged();
        }
        private async void AddOption()
        {
            if (Data != null)
            {
                await SaveRoom();

                RoomOptionItem = new ROOM_RoomOptions();

                RoomOptionItem.ID = Guid.NewGuid();
                RoomOptionItem.ROOM_Room_ID = Data.ID;
                RoomOptionItem.CreationDate = DateTime.Now;
                RoomOptionItem.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID;

                if (RoomOptionList != null)
                {
                    RoomOptionItem.Pos = RoomOptionList.Count() + 1;
                }
                else
                {
                    RoomOptionItem.Pos = 0;
                }

                RoomOptionItem.Contacts = new List<ROOM_RoomOptions_Contact>();

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        var dataE = new ROOM_RoomOptions_Extended()
                        {
                            ID = Guid.NewGuid(),
                            ROOM_RoomOptions_ID = RoomOptionItem.ID,
                            LANG_Language_ID = l.ID
                        };

                        if (RoomOptionItem.ROOM_RoomOptions_Extended == null)
                        {
                            RoomOptionItem.ROOM_RoomOptions_Extended = new List<ROOM_RoomOptions_Extended>();
                        }

                        RoomOptionItem.ROOM_RoomOptions_Extended.Add(dataE);
                    }
                }

                ShowRoomOptionWindow = true;
                StateHasChanged();
            }
        }
        private async void EditOption(V_ROOM_RoomOptions Item)
        {
            if (Data != null && Item != null && Item.ID != null)
            {
                //SAVE ITEM
                await RoomProvider.SetRoom(Data);

                foreach (var ext in Data.ROOM_Rooms_Extended)
                {
                    await RoomProvider.SetRoomExtended(ext);
                }

                RoomOptionItem = await RoomProvider.GetRoomOption(Item.ID);

                if (RoomOptionItem != null)
                {
                    RoomOptionItem.Contacts = await RoomProvider.GetRoomOptionContacts(Item.ID);
                }

                ShowRoomOptionWindow = true;
                StateHasChanged();
            }
        }
        private void CloseOption()
        {
            RoomOptionItem = null;

            ShowRoomOptionWindow = false;
            StateHasChanged();
        }
        private async void DeleteOption(V_ROOM_RoomOptions Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await RoomProvider.RemoveRoomOption(Item.ID);
                await GetRoomOptionData();

                StateHasChanged();
            }
        }
        private async void SaveOption()
        {
            if(RoomOptionItem != null)
            {
                if(RoomOptionItem.Quantity != null && RoomOptionItem.Quantity.Value < 0)
                {
                    InventoryMinValueError = TextProvider.Get("ROOM_INVENTORY_MIN_VALUE");
                    StateHasChanged();
                    return;
                }
                else
                {
                    InventoryMinValueError = null;
                }

                await RoomProvider.SetRoomOption(RoomOptionItem);

                if(RoomOptionItem.ROOM_RoomOptions_Category_ID == ROOMOptionCategory.Inventory && RoomOptionItem.Quantity != null)
                {
                    var existingPositions = await RoomProvider.GetRoomOptionsPositionList(RoomOptionItem.ID);

                    if(existingPositions == null || existingPositions.Count() == 0)
                    {
                        var NewItem = new ROOM_RoomOptions_Positions();

                        NewItem.ID = Guid.NewGuid();
                        NewItem.Quantity = RoomOptionItem.Quantity;
                        NewItem.ROOM_Rooms_Options_ID = RoomOptionItem.ID;
                        NewItem.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                        NewItem.CreationDate = DateTime.Now;

                        await RoomProvider.SetRoomOptionsPosition(NewItem);
                    }
                }

                foreach(var ext in RoomOptionItem.ROOM_RoomOptions_Extended)
                {
                    await RoomProvider.SetRoomOptionsExtended(ext);
                }

                if(RoomOptionItem.Contacts != null)
                {
                    var existingContacts = await RoomProvider.GetRoomOptionContacts(RoomOptionItem.ID);

                    foreach (var cont in existingContacts) 
                    {
                        await RoomProvider.RemoveRoomOptionContacts(cont.ID);
                    }

                    foreach (var cont in RoomOptionItem.Contacts)
                    {
                        await RoomProvider.SetRoomOptionContacts(cont);
                    }
                }
            }

            await GetRoomOptionData();

            RoomOptionItem = null;
            ShowRoomOptionWindow = false;
            StateHasChanged();
        }
        private async void CloseOptionWindow()
        {
            await GetRoomOptionData();

            RoomOptionItem = null;
            ShowRoomOptionWindow = false;
            StateHasChanged();
        }
        private void OnPriceDefaultChanged(ROOM_RoomPricing Item)
        {
            if (Item != null)
            {
                foreach (var price in RoomPricing)
                {
                    if(price.ID != Item.ID)
                    {
                        price.Default = false;
                    }
                }
            }

            StateHasChanged();
        }
        private void RoomPricingTypeChanged()
        {
            StateHasChanged();
        }
        private void ShowInventory(V_ROOM_RoomOptions Item)
        {
            if (Item != null)
            {
                RoomOptionID = Item.ID;

                ShowRoomInventoryPositions = true;
                StateHasChanged();
            }
        }
        private async void CloseInventoryPositionsWindow()
        {
            RoomOptionID = null;
            ShowRoomInventoryPositions = false;

            await GetRoomOptionData();

            StateHasChanged();
        }
        private void ImportOptions()
        {
            ShowRoomOptionImport = true;
            StateHasChanged();
        }
        private void HideImporter()
        {
            if (SelectedImportableOptions != null)
                SelectedImportableOptions.ToList().Clear();

            ShowRoomOptionImport = false;
            StateHasChanged();
        }
        private async void ImportSelectedOptions()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null && Data != null)
            {
                foreach (var item in SelectedImportableOptions)
                {
                    item.ID = Guid.NewGuid();
                    item.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                    item.CreationDate = DateTime.Now;
                    item.ROOM_Room_ID = Data.ID;

                    var extendedItems = item.ROOM_RoomOptions_Extended;

                    item.ROOM_RoomOptions_Extended = null;

                    await RoomProvider.SetRoomOption(item);

                    foreach (var ext in extendedItems)
                    {
                        ext.ID = Guid.NewGuid();
                        ext.ROOM_RoomOptions_ID = item.ID;

                        await RoomProvider.SetRoomOptionsExtended(ext);
                    }

                    item.ROOM_RoomOptions_Extended = extendedItems;
                }
            }

            await GetRoomOptionData();

            if (SelectedImportableOptions != null)
                SelectedImportableOptions = new List<ROOM_RoomOptions>();

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var settings = await RoomProvider.GetSettings(SessionWrapper.AUTH_Municipality_ID.Value);

                if (settings != null && !string.IsNullOrEmpty(settings.DefaultRoomOptions))
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<List<ROOM_RoomOptions>>(settings.DefaultRoomOptions);

                        if (result != null)
                        {
                            ImportableOptions = result;
                        }
                    }
                    catch { }
                }
            }

            ShowRoomOptionImport = false;
            StateHasChanged();
        }
        private async void RoomContactsChanged()
        {
            if (Data != null)
            {
                if (RoomContacts != null)
                {
                    var existingContacts = await RoomProvider.GetRoomsContacts(Data.ID);

                    foreach (var cont in existingContacts.Where(p => !RoomContacts.Select(x => x.ID).Contains(p.ID)).ToList())
                    {
                        await RoomProvider.RemoveRoomsContacts(cont.ID);
                    }

                    foreach (var cont in RoomContacts)
                    {
                        await RoomProvider.SetRoomsContacts(cont);
                    }
                }

                RoomContacts = await RoomProvider.GetRoomsContacts(Data.ID);
                StateHasChanged();
            }
        }
        private async void OnShowRoomInventoryPositionsList()
        {
            if (Data != null)
            {
                var inventory = await RoomProvider.GetRoomOptionsList(Data.ID);

                RoomInventoryList = inventory.Where(p => p.ROOM_RoomOptions_Category_ID == ROOMOptionCategory.Inventory).ToList();

                ShowRoomInventoryPositionsList = true;
                StateHasChanged();
            }
        }
        private async void HideRoomInventoryPositionList()
        {
            await GetRoomOptionData();

            ShowRoomInventoryPositionsList = false;
            StateHasChanged();
        }
        private void SetNewInventoryValue(V_ROOM_RoomOptions OptionItem)
        {
            NewInventoryPosition = new ROOM_RoomOptions_Positions();

            NewInventoryPosition.ID = Guid.NewGuid();
            NewInventoryPosition.Quantity = OptionItem.Quantity;
            NewInventoryPosition.ROOM_Rooms_Options_ID = OptionItem.ID;
            NewInventoryPosition.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

            ShowRoomInventoryPositionsQuickAdd = true;
            StateHasChanged();
        }
        private void HideInventoryValue()
        {
            NewInventoryPosition = null;

            ShowRoomInventoryPositionsQuickAdd = false;
            StateHasChanged();
        }
        private async void SaveInventoryValue()
        {
            if(NewInventoryPosition != null)
            {
                if (NewInventoryPosition.Quantity != null && NewInventoryPosition.Quantity.Value < 0)
                {
                    InventoryMinValueError = TextProvider.Get("ROOM_INVENTORY_MIN_VALUE");
                    StateHasChanged();
                    return;
                }
                else
                {
                    InventoryMinValueError = null;
                }

                NewInventoryPosition.CreationDate = DateTime.Now;

                await RoomProvider.SetRoomOptionsPosition(NewInventoryPosition);

                if (Data != null)
                {
                    var inventory = await RoomProvider.GetRoomOptionsList(Data.ID);

                    RoomInventoryList = inventory.Where(p => p.ROOM_RoomOptions_Category_ID == ROOMOptionCategory.Inventory).ToList();
                }
            }

            HideInventoryValue();
        }
    }
}
