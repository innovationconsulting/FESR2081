using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Canteen.Admin.SubPages
{
    public partial class RessourcesAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string ActiveIndex { get; set; }

        private CANTEEN_Ressources? Data { get; set; }
        private List<LANG_Languages>? Languages { get; set; }
        private Guid? CurrentLanguage { get; set; }
        private List<FILE_FileInfo> FileList { get; set; } = new List<FILE_FileInfo>();
        private List<FILE_FileInfo> FileListIt { get; set; } = new List<FILE_FileInfo>();

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
            Languages = await LangProvider.GetAll();

            if (ID == "New")
            {
                Data = new CANTEEN_Ressources();
                Data.ID = Guid.NewGuid();
                Data.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;

                await CanteenProvider.SetRessource(Data);

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        var dataE = new CANTEEN_Ressources_Extended()
                        {
                            CANTEEN_Ressources_ID = Data.ID,
                            LANG_Language_ID = l.ID
                        };

                        await CanteenProvider.SetRessourceExtended(dataE);
                        Data.CANTEEN_Ressources_Extended.Add(dataE);

                    }
                }

                var count = await CanteenProvider.GetRessourceList(SessionWrapper.AUTH_Municipality_ID.Value);

                if (count != null && count.Count > 0) 
                {
                    Data.SortOrder = count.Count + 1;
                }
                else
                {
                    Data.SortOrder = 1;
                }
            }
            else
            {
                Data = await CanteenProvider.GetRessource(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }

                if (Data != null && Data.FILE_FileInfo_ID != null)
                {
                    var file = await FileProvider.GetFileInfoAsync(Data.FILE_FileInfo_ID.Value);

                    if(file != null)
                    {
                        FileList = new List<FILE_FileInfo>() { file };
                    }
                }
                else if(Data != null)
                {
                    foreach (var extended in Data.CANTEEN_Ressources_Extended)
                    {
                        if (extended != null && extended.FILE_FileInfo_ID != null)
                        {
                            var file = await FileProvider.GetFileInfoAsync(extended.FILE_FileInfo_ID.Value);

                            if (file != null && extended.LANG_Language_ID == LanguageSettings.German)
                            {
                                FileList = new List<FILE_FileInfo>() { file };
                            }
                            else if(file != null)
                            {
                                FileListIt = new List<FILE_FileInfo>() { file };
                            }
                        }
                    }
                }
            }

            if (Languages != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void ReturnToPreviousPage()
        {
            if (ID == "New" && Data != null)
            {
                await CanteenProvider.RemoveRessource(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Admin/Canteen/DetailPage/" + ActiveIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var e in Data.CANTEEN_Ressources_Extended)
            {
                if (e.LANG_Language_ID == LanguageSettings.German)
                {
                    var file = await FileProvider.SetFileInfo(FileList.FirstOrDefault());
                    e.FILE_FileInfo_ID = file?.ID;
                }
                else
                {
                    var file = await FileProvider.SetFileInfo(FileListIt.FirstOrDefault());
                    e.FILE_FileInfo_ID = file?.ID;
                }
                await CanteenProvider.SetRessourceExtended(e);
            }

            /* OLD CODE
            if(FileList != null && FileList.Count() > 0)
            {
                var file = await FileProvider.SetFileInfo(FileList.FirstOrDefault());
                
                if (file != null)
                {
                    Data.FILE_FileInfo_ID = file.ID;
                }
            }*/

            await CanteenProvider.SetRessource(Data);
            NavManager.NavigateTo("/Admin/Canteen/DetailPage/" + ActiveIndex);
        }
        private void LanguageChanged()
        {
            StateHasChanged();
        }
        private async void OnRemove(Guid File_Info_ID)
        {
            await FileProvider.RemoveFileInfo(File_Info_ID);

            StateHasChanged();
        }
    }
}
