using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin.SubPages
{
    public partial class MunicipalFieldAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string DefinitionID { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string ActiveIndex { get; set; }
        [Parameter] public string WizardIndex { get; set; }

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
        private FORM_Definition_Municipal_Field? Data { get; set; }
        private List<LANG_Languages>? Languages { get; set; }
        private List<V_FORM_Definition_Municipal_Field_Type>? FieldTypes { get; set; }
        private Guid? CurrentLanguage { get; set; }
        private List<IEditorTool> Tools { get; set; } =
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
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font" };

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
                Data = new FORM_Definition_Municipal_Field();
                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Guid.Parse(DefinitionID);

                await FormDefinitionProvider.SetDefinitionMunField(Data);

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        if (Data.FORM_Definition_Municipal_Field_Extended == null)
                        {
                            Data.FORM_Definition_Municipal_Field_Extended = new List<FORM_Definition_Municipal_Field_Extended>();
                        }

                        if (Data.FORM_Definition_Municipal_Field_Extended.FirstOrDefault(p => p.LANG_Languages_ID == l.ID) == null)
                        {
                            var dataE = new FORM_Definition_Municipal_Field_Extended()
                            {
                                FORM_Definition_Municipal_Field_ID = Data.ID,
                                LANG_Languages_ID = l.ID
                            };

                            await FormDefinitionProvider.SetDefinitionMunFieldExtended(dataE);
                            Data.FORM_Definition_Municipal_Field_Extended.Add(dataE);
                        }
                    }
                }

                Data = await FormDefinitionProvider.GetDefinitionMunField(Data.ID);

                var count = await FormDefinitionProvider.GetDefinitionMunFieldList(Guid.Parse(DefinitionID));

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
                Data = await FormDefinitionProvider.GetDefinitionMunField(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }
            }

            if (Languages != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            if (CurrentLanguage != null)
            {
                FieldTypes = await FormDefinitionProvider.GetDefinitionMunFieldTypeList(CurrentLanguage.Value);
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void ReturnToPreviousPage()
        {
            if (ID == "New" && Data != null)
            {
                await FormDefinitionProvider.RemoveDefinitionProperty(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (Data != null)
            {
                foreach (var e in Data.FORM_Definition_Municipal_Field_Extended)
                {
                    await FormDefinitionProvider.SetDefinitionMunFieldExtended(e);
                }
            }

            await FormDefinitionProvider.SetDefinitionMunField(Data);
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
    }
}
