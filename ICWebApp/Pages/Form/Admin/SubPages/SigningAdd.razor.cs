using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin.SubPages
{
    public partial class SigningAdd
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
        [Parameter] public string WizardIndex { get; set; }

        private FORM_Definition_Signings Data { get; set; }
        private List<LANG_Languages>? Languages { get; set; }
        private Guid? CurrentLanguage { get; set; }

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
                Data = new FORM_Definition_Signings();
                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Guid.Parse(DefinitionID);

                await FormDefinitionProvider.SetDefinitionSigning(Data);

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        if (Data.FORM_Definition_Signings_Extended == null)
                        {
                            Data.FORM_Definition_Signings_Extended = new List<FORM_Definition_Signings_Extended>();
                        }

                        if (Data.FORM_Definition_Signings_Extended.FirstOrDefault(p => p.LANG_Languages_ID == l.ID) == null)
                        {
                            var dataE = new FORM_Definition_Signings_Extended()
                            {
                                FORM_Definition_Signings_ID = Data.ID,
                                LANG_Languages_ID = l.ID
                            };

                            await FormDefinitionProvider.SetDefinitionSigningExtended(dataE);
                            Data.FORM_Definition_Signings_Extended.Add(dataE);
                        }

                    }
                }

                Data = await FormDefinitionProvider.GetDefinitionSigning(Data.ID);

                var count = await FormDefinitionProvider.GetDefinitionSigningList(Guid.Parse(DefinitionID));

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
                Data = await FormDefinitionProvider.GetDefinitionSigning(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
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
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var e in Data.FORM_Definition_Signings_Extended)
            {
                await FormDefinitionProvider.SetDefinitionSigningExtended(e);
            }

            await FormDefinitionProvider.SetDefinitionSigning(Data);
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex);
        }
        private void LanguageChanged()
        {
            StateHasChanged();
        }
    }
}
