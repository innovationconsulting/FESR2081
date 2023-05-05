using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin.SubPages
{
    public partial class RessourcesAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string DefinitionID { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string ActiveIndex { get; set; }
        [Parameter] public string WizardIndex { get; set; }

        private FORM_Definition_Ressources Data { get; set; }
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
            if(DefinitionID == null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                NavManager.NavigateTo("/Form/Definition");
            }

            Languages = await LangProvider.GetAll();

            if (ID == "New")
            {
                Data = new FORM_Definition_Ressources();
                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Guid.Parse(DefinitionID);

                await FormDefinitionProvider.SetDefinitionRessource(Data);

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        if (Data.FORM_Definition_Ressources_Extended == null)
                        {
                            Data.FORM_Definition_Ressources_Extended = new List<FORM_Definition_Ressources_Extended>();
                        }

                        if (Data.FORM_Definition_Ressources_Extended.FirstOrDefault(p => p.LANG_Language_ID == l.ID) == null)
                        {
                            var dataE = new FORM_Definition_Ressources_Extended()
                            {
                                FORM_Definition_Ressources_ID = Data.ID,
                                LANG_Language_ID = l.ID
                            };

                            await FormDefinitionProvider.SetDefinitionRessourceExtended(dataE);
                            Data.FORM_Definition_Ressources_Extended.Add(dataE);
                        }
                    }
                }

                Data = await FormDefinitionProvider.GetDefinitionRessource(Data.ID);

                var count = await FormDefinitionProvider.GetDefinitionRessourceList(Guid.Parse(DefinitionID));

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
                Data = await FormDefinitionProvider.GetDefinitionRessource(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }

                if (Data.FILE_FileInfo_ID != null)
                {
                    var file = await FileProvider.GetFileInfoAsync(Data.FILE_FileInfo_ID.Value);

                    if(file != null)
                    {
                        FileList = new List<FILE_FileInfo>() { file };
                    }
                }
                else
                {
                    foreach (var extended in Data.FORM_Definition_Ressources_Extended)
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
                await FormDefinitionProvider.RemoveDefinitionRessource(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var e in Data.FORM_Definition_Ressources_Extended)
            {
                await FormDefinitionProvider.SetDefinitionRessourceExtended(e);
            }
            foreach (var e in Data.FORM_Definition_Ressources_Extended)
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
                await FormDefinitionProvider.SetDefinitionRessourceExtended(e);
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

            await FormDefinitionProvider.SetDefinitionRessource(Data);
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
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
