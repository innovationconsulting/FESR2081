using Blazored.LocalStorage;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Authorization.External
{
    public partial class LoginExternalError
    {
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAccountService AccountService {get;set;}
        [Inject] IAUTHProvider AuthProvider {get;set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] Blazored.SessionStorage.ISessionStorageService SessionStorage { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ILocalStorageService LocalStorage { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public string ErrorCode { get; set; }

        private string RedirectURL { get; set; }

        protected override void OnInitialized()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                RedirectURL = await SessionStorage.GetItemAsync<string>("RedirectURL");

                var MunicipalID = await LocalStorage.GetItemAsync<string>("Comunix.Municipality");

                if (MunicipalID != null)
                {
                    SessionWrapper.AUTH_Municipality_ID = Guid.Parse(MunicipalID);

                    var prefixes = await AuthProvider.GetProgrammPrefixes();

                    var CurrentMunicipality = prefixes.FirstOrDefault(p => p.Prefix != null && p.AUTH_Municipality_ID == Guid.Parse(MunicipalID));

                    if (CurrentMunicipality != null)
                    {
                        if (NavManager.BaseUri.Contains("localhost"))
                        {
                            NavManager.NavigateTo("https://localhost:7149/LoginError/" + ErrorCode, true);
                        }
                        else if (CurrentMunicipality.Prefix != null && !NavManager.BaseUri.Contains(CurrentMunicipality.Prefix))
                        {
                            NavManager.NavigateTo("https://" + CurrentMunicipality.Prefix + ".comunix.bz.it" + "/LoginError/" + ErrorCode, true);
                        }
                    }
                }

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private async void BackToLogin()
        {
            if (!string.IsNullOrEmpty(RedirectURL))
            {
                await SessionStorage.SetItemAsync("RedirectURL", RedirectURL);
                NavManager.NavigateTo("/Login/ReturnUrl=" + Uri.EscapeDataString(RedirectURL), true);
                return;
            }

            NavManager.NavigateTo("/Login", true);
        }
    }
}
