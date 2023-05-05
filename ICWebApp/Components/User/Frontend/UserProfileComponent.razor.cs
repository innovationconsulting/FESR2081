using Blazored.LocalStorage;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.User.Frontend
{
    public partial class UserProfileComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] ILocalStorageService LocalStorageService { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        private List<ORG_Selection_Item> DropDownData = new List<ORG_Selection_Item>();
        private List<V_AUTH_Users_Organizations> Organizations = new List<V_AUTH_Users_Organizations>();

        private bool _userHasUnreadMessages = false;
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.OnCurrentUserChanged += SessionWrapper_OnCurrentUserChanged;
            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentSubUserChanged; ;
            SessionWrapper.MunicipalityApps = await AuthProvider.GetMunicipalityApps();

            if (SessionWrapper.CurrentUser != null)
            {
                Organizations = await AuthProvider.GetUsersOrganizations(SessionWrapper.CurrentUser.ID);                

                Organizations = Organizations.Where(p => p.ConfirmedAt != null).ToList();

                DropDownData.Clear();

                DropDownData.Add(new ORG_Selection_Item()
                {
                    ID = SessionWrapper.CurrentUser.ID,
                    Name = SessionWrapper.CurrentUser.Firstname + " " + SessionWrapper.CurrentUser.Lastname
                });

                foreach (var orgs in Organizations)
                {
                    DropDownData.Add(new ORG_Selection_Item() { ID = orgs.ORG_AUTH_Users_ID.Value, Name = orgs.ORG_Fullname });
                }

                _userHasUnreadMessages = MessageService.UserHasUnreadMessages(SessionWrapper.CurrentUser.ID);
                MessageService.RefreshRequested += MessageService_RefreshRequested;            
            }
            StateHasChanged();

            await base.OnInitializedAsync();
        }

        private async void MessageService_RefreshRequested()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    _userHasUnreadMessages = MessageService.UserHasUnreadMessages(SessionWrapper.CurrentUser.ID);
                    StateHasChanged();
                });
            }
            catch { }
        }

        private void SessionWrapper_OnCurrentSubUserChanged()
        {
            StateHasChanged();
        }

        private async void SessionWrapper_OnCurrentUserChanged()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                Organizations = await AuthProvider.GetUsersOrganizations(SessionWrapper.CurrentUser.ID);

                Organizations = Organizations.Where(p => p.ConfirmedAt != null).ToList();

                DropDownData.Clear();

                DropDownData.Add(new ORG_Selection_Item()
                {
                    ID = SessionWrapper.CurrentUser.ID,
                    Name = SessionWrapper.CurrentUser.Firstname + " " + SessionWrapper.CurrentUser.Lastname
                });

                foreach (var orgs in Organizations)
                {
                    DropDownData.Add(new ORG_Selection_Item() { ID = orgs.ORG_AUTH_Users_ID.Value, Name = orgs.ORG_Fullname });
                }
            }
            StateHasChanged();
        }
        private void HandleOrganizationRequest()
        {
            if (!NavManager.Uri.EndsWith("/Organization/Dashboard"))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Dashboard");
                StateHasChanged();
            }
        }
        private void HandleNewOrganizationRequest()
        {
            if (!NavManager.Uri.EndsWith("/Organization/Application"))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Application");
                StateHasChanged();
            }
        }
        private void UserSettingsPage()
        {
            BusyIndicatorService.IsBusy = true;

            if (SessionWrapper.CurrentSubstituteUser != null)
            {
                if (!NavManager.Uri.EndsWith("/Organization/Management/" + SessionWrapper.CurrentSubstituteUser.ID))
                {
                    NavManager.NavigateTo("/Organization/Management/" + SessionWrapper.CurrentSubstituteUser.ID);
                }
                else
                {
                    BusyIndicatorService.IsBusy = false;
                }
            }
            else
            {
                if (!NavManager.Uri.EndsWith("/User/Profile"))
                {
                    NavManager.NavigateTo("/User/Profile");
                }
                else
                {
                    BusyIndicatorService.IsBusy = false;
                }
            }

            StateHasChanged();
        }
        private async void SelectOrg(Guid ID)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
                                
            var SelecteddbUser = await AuthProvider.GetUser(ID);

            if (SelecteddbUser != null)
            {
                var organization = Organizations.FirstOrDefault(p => p.ORG_AUTH_Users_ID == ID);

                if (SelecteddbUser.IsOrganization && organization != null && organization.AUTH_Users_ID != null)
                {
                    if (!AuthProvider.HasUserRole(SelecteddbUser.ID, AuthRoles.Citizen))
                    {
                        await AuthProvider.SetRole(SelecteddbUser.ID, AuthRoles.Citizen);
                    }

                    SessionWrapper.CurrentSubstituteUser = SelecteddbUser;
                    await LocalStorageService.SetItemAsStringAsync("Comunix.Login.SubstituteUserID", SelecteddbUser.ID.ToString());
                }
                else
                {
                    SessionWrapper.CurrentSubstituteUser = null;
                    await LocalStorageService.RemoveItemAsync("Comunix.Login.SubstituteUserID");
                }

                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/");
                StateHasChanged();
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            
        }
        private void GoToLogin()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Login", true);
            StateHasChanged();
        }
        private async void HandleLogout()
        {
            await AccountService.Logout();
            NavManager.NavigateTo("/");
        }
        private void ShowMyServices()
        {
            if (!NavManager.Uri.Contains("/User/Services"))
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();
            }
        }
    }
}
