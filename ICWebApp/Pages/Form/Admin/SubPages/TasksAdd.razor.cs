using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin.SubPages
{
    public partial class TasksAdd
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

        private FORM_Definition_Tasks Data { get; set; }
        private List<LANG_Languages>? Languages { get; set; }
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
        private List<AUTH_Users> UserList = new List<AUTH_Users>();
        private FORM_Definition_Tasks_Responsible? CurrentTarget = null;
        private List<FORM_Definition_Tasks_Responsible> TargetList { get; set; }
        private bool ShowTargetWindow { get; set; } = false;

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
                Data = new FORM_Definition_Tasks();
                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Guid.Parse(DefinitionID);

                await FormDefinitionProvider.SetDefinitionTask(Data);

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {

                        if (Data.FORM_Definition_Tasks_Extended == null)
                        {
                            Data.FORM_Definition_Tasks_Extended = new List<FORM_Definition_Tasks_Extended>();
                        }

                        if (Data.FORM_Definition_Tasks_Extended.FirstOrDefault(p => p.LANG_Languages_ID == l.ID) == null)
                        {
                            var dataE = new FORM_Definition_Tasks_Extended()
                            {
                                FORM_Definition_Tasks_ID = Data.ID,
                                LANG_Languages_ID = l.ID
                            };

                            await FormDefinitionProvider.SetDefinitionTaskExtended(dataE);
                            Data.FORM_Definition_Tasks_Extended.Add(dataE);
                        }
                    }
                }

                Data = await FormDefinitionProvider.GetDefinitionTask(Data.ID);
            }
            else
            {
                Data = await FormDefinitionProvider.GetDefinitionTask(Guid.Parse(ID));

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
                await FormDefinitionProvider.RemoveDefinitionTask(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var e in Data.FORM_Definition_Tasks_Extended)
            {
                await FormDefinitionProvider.SetDefinitionTaskExtended(e);
            }

            await FormDefinitionProvider.SetDefinitionTask(Data);
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private void LanguageChanged()
        {
            StateHasChanged();
        }
        private async void AddTarget()
        {
            CurrentTarget = new FORM_Definition_Tasks_Responsible();
            CurrentTarget.FORM_Definition_Tasks_ID = Data.ID;

            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                UserList = await AuthProvider.GetUserList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, AuthRoles.Employee);    //EMPLOYEE
            }

            ShowTargetWindow = true;
            StateHasChanged();
        }
        private async void UpdateTarget(GridCommandEventArgs args)
        {
            var target = (FORM_Definition_Tasks_Responsible)args.Item;

            if (target != null)
            {
                CurrentTarget = target;

                if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
                {
                    UserList = await AuthProvider.GetUserList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, AuthRoles.Employee);    //EMPLOYEE
                }

                ShowTargetWindow = true;
                StateHasChanged();
            }
        }
        private async void DeleteTarget(GridCommandEventArgs args)
        {
            var target = (FORM_Definition_Tasks_Responsible)args.Item;

            if (target != null)
            {
                await FormDefinitionProvider.RemoveDefinitionTaskResponsible(target.ID);
                TargetList = await FormDefinitionProvider.GetDefinitionTaskResponsibleList(Data.ID);
                StateHasChanged();
            }
        }
        private async void SaveTarget()
        {
            if (CurrentTarget != null)
            {
                CurrentTarget.AUTH_Users = null;
                await FormDefinitionProvider.SetDefinitionTaskResponsible(CurrentTarget);
                TargetList = await FormDefinitionProvider.GetDefinitionTaskResponsibleList(Data.ID);
                ShowTargetWindow = false;
                CurrentTarget = null;
                StateHasChanged();
            }
        }
        private async void CloseTarget()
        {
            ShowTargetWindow = false;
            StateHasChanged();
        }
    }
}
