using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin.SubPages
{
    public partial class DeadlineAdd
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

        private FORM_Definition_Deadlines Data { get; set; }
        private List<LANG_Languages>? Languages { get; set; }
        private List<FORM_Definition_Deadlines_TimeType> TimeTypes { get; set; }
        private List<FORM_Definition_Deadlines_Target> TargetList { get; set; }
        private Guid? CurrentLanguage { get; set; }
        private bool ShowTargetWindow { get; set; } = false;
        private List<AUTH_Users> UserList = new List<AUTH_Users>();
        private FORM_Definition_Deadlines_Target? CurrentTarget = null;

        protected override async Task OnInitializedAsync()
        {
            if(DefinitionID == null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                NavManager.NavigateTo("/Form/Definition");
            }

            Languages = await LangProvider.GetAll();
            TimeTypes = await FormDefinitionProvider.GetDefinitionDeadlinesTimeTypeList();

            if (ID == "New")
            {
                Data = new FORM_Definition_Deadlines();
                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Guid.Parse(DefinitionID);

                await FormDefinitionProvider.SetDefinitionDeadlines(Data);
            }
            else
            {
                Data = await FormDefinitionProvider.GetDefinitionDeadlines(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }

                TargetList = await FormDefinitionProvider.GetDefinitionDeadlinesTargetList(Data.ID);
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
                await FormDefinitionProvider.RemoveDefinitionDeadlines(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await FormDefinitionProvider.SetDefinitionDeadlines(Data);
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private async void AddTarget()
        {
            CurrentTarget = new FORM_Definition_Deadlines_Target();
            CurrentTarget.ID = Guid.NewGuid();
            CurrentTarget.FORM_Definition_Deadline_ID = Data.ID;

            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                UserList = await AuthProvider.GetUserList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, AuthRoles.Employee);    //EMPLOYEE
            }

            ShowTargetWindow = true;
            StateHasChanged();
        }
        private async void UpdateTarget(GridCommandEventArgs args)
        {
            var target = (FORM_Definition_Deadlines_Target)args.Item;

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
            var target = (FORM_Definition_Deadlines_Target)args.Item;

            if (target != null)
            {
                await FormDefinitionProvider.RemoveDefinitionDeadlinesTarget(target.ID);
                TargetList = await FormDefinitionProvider.GetDefinitionDeadlinesTargetList(Data.ID);
                StateHasChanged();
            }
        }
        private async void SaveTarget()
        {
            if (CurrentTarget != null)
            {
                CurrentTarget.AUTH_Users = null;
                await FormDefinitionProvider.SetDefinitionDeadlinesTarget(CurrentTarget);
                TargetList = await FormDefinitionProvider.GetDefinitionDeadlinesTargetList(Data.ID);
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
