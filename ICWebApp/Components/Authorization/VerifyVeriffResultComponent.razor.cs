using Blazored.LocalStorage;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Components.CodeInput;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Timers;

namespace ICWebApp.Components.Authorization
{
    public partial class VerifyVeriffResultComponent
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IVeriffService VeriffService { get; set; }
        [Inject] ILocalStorageService _localStorageService { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        public AUTH_Users? User { get; set; }
        public AUTH_Users_Anagrafic? Anagrafic { get; set; }
        private bool CheckRunning = false;
        private AUTH_VeriffResponse? VeriffResponse;
        private int RepeatCount = 0;
        private int RepeatLimit = 30;
        private bool EditWindowVisible = false;
        private string RepeatVeriffMessage = "";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var token = await _localStorageService.GetItemAsync<string>("Comunix.Login.AuthToken");

                User = await AuthProvider.GetUserByLoginToken(Guid.Parse(token));

                SessionWrapper.AUTH_Municipality_ID = await SessionWrapper.GetMunicipalityID();

                if (User == null)
                {
                    NavManager.NavigateTo("/", true);
                    return;
                }

                if (User != null && User.VeriffConfirmed)
                {
                    NavManager.NavigateTo("/", true);
                    return;
                }

                if (User != null)
                {
                    Anagrafic = await AuthProvider.GetAnagraficByUserID(User.ID);
                }

                System.Timers.Timer t = new System.Timers.Timer(2000);
                t.Elapsed += CheckVeriffResponses;
                t.AutoReset = true;
                t.Start();

                StateHasChanged();
            }


            await base.OnAfterRenderAsync(firstRender);
        }
        private async void CheckVeriffResponses(object? sender, ElapsedEventArgs e)
        {
            if (!CheckRunning && User != null)
            {
                CheckRunning = true;

                VeriffResponse = await AuthProvider.GetVeriffResponse(User.ID);

                await InvokeAsync(() => StateHasChanged());

                CheckRunning = false;
            }

            if (VeriffResponse == null && RepeatCount <= RepeatLimit)
            {
                RepeatCount++;
                await InvokeAsync(() => StateHasChanged());
            }
        }
        private async void HandleLogout()
        {
            await AccountService.Logout();
            NavManager.NavigateTo("/", true);

            return;
        }        
        private void HandleAgain()
        {
            if (User != null)
            {
                NavManager.NavigateTo("/Veriff/" + User.ID, true);
            }
            else
            {
                NavManager.NavigateTo("/", true);
            }

            return;
        }
        private void ShowEditWindow()
        {
            EditWindowVisible = true;
            StateHasChanged();
        }
        private async void SaveData()
        {
            if (User != null && Anagrafic != null && VeriffResponse != null)
            {
                User.Firstname = Anagrafic.FirstName;
                User.Lastname = Anagrafic.LastName;

                await AuthProvider.UpdateUser(User);

                await AuthProvider.SetAnagrafic(Anagrafic);

                if (VeriffResponse.DateOfBirth != null && Anagrafic.DateOfBirth != null
                    && VeriffResponse.DateOfBirth.Value.Year == Anagrafic.DateOfBirth.Value.Year
                    && VeriffResponse.DateOfBirth.Value.Month == Anagrafic.DateOfBirth.Value.Month
                    && VeriffResponse.DateOfBirth.Value.Day == Anagrafic.DateOfBirth.Value.Day
                    && VeriffResponse.Firstname != null && Anagrafic.FirstName != null && VeriffResponse.Firstname.Trim().ToLower() == Anagrafic.FirstName.Trim().ToLower()
                    && VeriffResponse.Lastname != null && Anagrafic.LastName != null && VeriffResponse.Lastname.Trim().ToLower() == Anagrafic.LastName.Trim().ToLower())
                {

                    if (User != null)
                    {
                        User.VeriffConfirmed = true;

                        BusyIndicatorService.IsBusy = true;
                        StateHasChanged();
                        await AuthProvider.UpdateUser(User);
                        NavManager.NavigateTo("/", true);
                    }
                    else
                    {
                        RepeatVeriffMessage = TextProvider.Get("VERIFF_REPEAT_ERROR_MESSAGE");
                        EditWindowVisible = false;
                        StateHasChanged();
                    }
                }
                else
                {
                    RepeatVeriffMessage = TextProvider.Get("VERIFF_REPEAT_ERROR_MESSAGE");
                    EditWindowVisible = false;
                    StateHasChanged();
                }
            }
            else
            {
                RepeatVeriffMessage = TextProvider.Get("VERIFF_REPEAT_ERROR_MESSAGE");
                EditWindowVisible = false;
                StateHasChanged();
            }

            EditWindowVisible = false;
            StateHasChanged();
        }
        private async void HideEditWindow()
        {
            if (User != null)
            {
                await AuthProvider.GetAnagraficByUserID(User.ID);
            }

            EditWindowVisible = false;
            StateHasChanged();
        }
    }
}
