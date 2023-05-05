using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Filter.Responsible
{    public partial class Control
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TASKProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }   

        [Parameter] public List<Guid> AUTH_Users_List { get; set; }
        [Parameter] public EventCallback<Guid> ItemAdded { get; set; }
        [Parameter] public EventCallback<Guid> ItemRemoved { get; set; }

        private List<AUTH_Users> AllResponsibles = new List<AUTH_Users>();
        private List<AUTH_Users> ResponsiblesToAdd = new List<AUTH_Users>();
        private bool ResponsibleDropdownVisibility = false;
        private SearchInput? Search = new SearchInput();

        protected override async Task OnInitializedAsync()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                AllResponsibles = await AuthProvider.GetUserList(SessionWrapper.AUTH_Municipality_ID.Value, AuthRoles.Employee);    //EMPLOYEE
                ResponsiblesToAdd = AllResponsibles.Where(p => !AUTH_Users_List.Contains(p.ID)).ToList();
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async void AddResponsible(AUTH_Users Item)
        {
            AUTH_Users_List.Add(Item.ID);
            ResponsiblesToAdd = AllResponsibles.Where(p => !AUTH_Users_List.Contains(p.ID)).ToList();

            HideResponsibleDropdown();

            await ItemAdded.InvokeAsync(Item.ID);
            StateHasChanged();
        }
        private async void RemoveResponsible(AUTH_Users Item)
        {
            var tagToRemove = AUTH_Users_List.FirstOrDefault(p => p == Item.ID);

            if(tagToRemove != null)
            {
                AUTH_Users_List.Remove(tagToRemove);
            }

            ResponsiblesToAdd = AllResponsibles.Where(p => !AUTH_Users_List.Contains(p.ID)).ToList();

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
