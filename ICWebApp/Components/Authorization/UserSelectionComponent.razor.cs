using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Authorization
{
    public partial class UserSelectionComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }

        [Parameter] public Guid? AUTH_Users_ID { get; set; }
        [Parameter] public EventCallback<Guid?> OnSelection { get; set; }
        [Parameter] public bool ShowTitle { get; set; } = true;
        [Parameter] public bool IsUser { get; set; } = true;

        private List<AUTH_Users> UserList = new List<AUTH_Users>();
        private bool ShowNewWindow { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await GetData();

            StateHasChanged();

            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            if (SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                if (IsUser == true) 
                { 
                    UserList = await AuthProvider.GetUserList(SessionWrapper.AUTH_Municipality_ID.Value, null, false);
                }
                else
                {
                    UserList = await AuthProvider.GetUserList(SessionWrapper.AUTH_Municipality_ID.Value, null, true);
                }
            }

            return true;
        }
        private void ShowNewUserPopUp()
        {
            ShowNewWindow = true;
            StateHasChanged();
        }
        private async void OnCallBack(Guid? UserID)
        {
            if (UserID != null)
            {
                await GetData();
                AUTH_Users_ID = UserID;
                OnUserSelected();
            }

            ShowNewWindow = false;
            StateHasChanged();
        }
        private void OnUserSelected()
        {
            OnSelection.InvokeAsync(AUTH_Users_ID);
        }
    }
}
