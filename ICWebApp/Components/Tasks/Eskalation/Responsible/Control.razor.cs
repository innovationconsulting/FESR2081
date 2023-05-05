using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Eskalation.Responsible
{    public partial class Control
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TASKProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }   

        [Parameter] public Guid TaskEskalationID { get; set; }
        [Parameter] public List<TASK_Task_Eskalation_Responsible> ResponsibleList { get; set; }
        [Parameter] public EventCallback<TASK_Task_Eskalation_Responsible> ItemAdded { get; set; }
        [Parameter] public EventCallback<TASK_Task_Eskalation_Responsible> ItemRemoved { get; set; }

        private List<AUTH_Users> AllResponsibles = new List<AUTH_Users>();
        private List<AUTH_Users> ResponsiblesToAdd = new List<AUTH_Users>();
        private bool ResponsibleDropdownVisibility = false;
        private SearchInput? Search = new SearchInput();

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                AllResponsibles = await AuthProvider.GetUserList(SessionWrapper.AUTH_Municipality_ID.Value, AuthRoles.Employee);    //EMPLOYEE
                ResponsiblesToAdd = AllResponsibles.Where(p => !ResponsibleList.Select(p => p.AUTH_Users_ID).Contains(p.ID)).ToList();
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async void AddResponsible(AUTH_Users Item)
        {
            var item = new TASK_Task_Eskalation_Responsible()
            {
                ID = Guid.NewGuid(),
                AUTH_Users_ID = Item.ID,
                TASK_Task_Eskalation_ID = TaskEskalationID,
                SortDesc = Item.Lastname + " " + Item.Firstname
            };

            ResponsibleList.Add(item);
            ResponsiblesToAdd = AllResponsibles.Where(p => !ResponsibleList.Select(p => p.AUTH_Users_ID).Contains(p.ID)).ToList();

            HideResponsibleDropdown();

            await ItemAdded.InvokeAsync(item);
            StateHasChanged();
        }
        private async void RemoveResponsible(AUTH_Users Item)
        {
            var tagToRemove = ResponsibleList.FirstOrDefault(p => p.AUTH_Users_ID == Item.ID);

            if(tagToRemove != null)
            {
                ResponsibleList.Remove(tagToRemove);
            }

            ResponsiblesToAdd = AllResponsibles.Where(p => !ResponsibleList.Select(p => p.AUTH_Users_ID).Contains(p.ID)).ToList();

            await ItemRemoved.InvokeAsync(tagToRemove);
            StateHasChanged();
        }
        private void ToggleResponsibleDropdown()
        {
            ResponsibleDropdownVisibility = !ResponsibleDropdownVisibility;
            StateHasChanged();
        }
        private void HideResponsibleDropdown()
        {
            ResponsibleDropdownVisibility = false;
            StateHasChanged();
        }
    }
}
