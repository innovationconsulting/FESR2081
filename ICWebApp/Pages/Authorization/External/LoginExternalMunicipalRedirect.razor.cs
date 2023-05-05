using Blazored.LocalStorage;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Web;

namespace ICWebApp.Pages.Authorization.External
{
    public partial class LoginExternalMunicipalRedirect
    {
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAccountService AccountService {get;set;}
        [Inject] IAUTHProvider AuthProvider {get;set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] Blazored.SessionStorage.ISessionStorageService SessionStorage { get; set; }
        [Inject] ILocalStorageService LocalStorage { get; set; }
        [Inject] ISPIDService SpidService { get; set; }
        [Inject] IMETAProvider MetaProvider { get; set; }

        [Parameter] public string FiscalCode { get; set; }
        [Parameter] public string LoginToken { get; set; }
        [Parameter] public string Municipality { get; set; }
        [Parameter] public string TargetUrl { get; set; }

        private bool ShowEmailEditWindow = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (Municipality == null)
                {
                    NavManager.NavigateTo("/Login", true);
                    return;
                }

                SpidService.SetLog(Guid.Parse(Municipality), "Comunix Login - Verification", "");
                SessionWrapper.AUTH_Municipality_ID = Guid.Parse(Municipality);

                if (Municipality == null || FiscalCode == null || LoginToken == null)
                {
                    NavManager.NavigateTo("/Login", true);
                    return;
                }

                var externalVerif = await AuthProvider.GetVerification(FiscalCode, Guid.Parse(LoginToken));

                if (externalVerif == null)
                {
                    NavManager.NavigateTo("/Login", true);
                    return;
                }

                SpidService.SetLog(Guid.Parse(Municipality), "Comunix Login - User saving", "");

                externalVerif.CompletedAt = DateTime.Now;
                await AuthProvider.SetVerification(externalVerif);

                var anagrafic = new AUTH_Users_Anagrafic();

                anagrafic.FirstName = externalVerif.FirstName;
                anagrafic.LastName = externalVerif.LastName;
                anagrafic.FiscalNumber = externalVerif.FiscalNumber;

                if (string.IsNullOrEmpty(anagrafic.Email))
                {
                    anagrafic.Email = externalVerif.Email;
                }

                if (anagrafic.CountyOfBirth != null)
                {
                    anagrafic.CountyOfBirth = externalVerif.CountyOfBirth.ToUpper();
                }

                var placeOfBirth = await MetaProvider.GetMunicipality(externalVerif.PlaceOfBirth);

                if (placeOfBirth != null)
                {
                    anagrafic.PlaceOfBirth = placeOfBirth.NameIT;
                    anagrafic.SelectedMunicipality = placeOfBirth.ID;
                }
                else
                {
                    placeOfBirth = await MetaProvider.GetMunicipalityByName(externalVerif.PlaceOfBirth);

                    if (placeOfBirth != null)
                    {
                        anagrafic.PlaceOfBirth = placeOfBirth.NameIT;
                        anagrafic.SelectedMunicipality = placeOfBirth.ID;
                    }
                    else
                    {
                        var countryOfBirth = await MetaProvider.GetCountry(externalVerif.PlaceOfBirth);

                        if (countryOfBirth != null)
                        {
                            anagrafic.PlaceOfBirth = countryOfBirth.NameIT;
                            anagrafic.SelectedMunicipality = countryOfBirth.ID;
                        }
                        else
                        {
                            anagrafic.PlaceOfBirth = externalVerif.PlaceOfBirth;
                        }
                    }
                }

                if (anagrafic.SelectedMunicipality == null)
                {
                    anagrafic.SelectedMunicipality = Guid.Parse("2A9141CE-8188-48BF-9A5C-58118C1045B8");
                }

                anagrafic.DateOfBirth = externalVerif.DateOfBirth;
                anagrafic.Address = externalVerif.Address;
                anagrafic.DomicileMunicipality = externalVerif.DomicileMunicipality;
                anagrafic.DomicileNation = externalVerif.DomicileNation;
                anagrafic.DomicilePostalCode = externalVerif.DomicilePostalCode;
                anagrafic.DomicileProvince = externalVerif.DomicileProvince;
                anagrafic.DomicileStreetAddress = externalVerif.DomicileStreetAddress;
                anagrafic.Gender = externalVerif.Gender;

                if(externalVerif.ServiceID != null)
                {
                    anagrafic.IgnoreDataVerification = true;
                }

                if (string.IsNullOrEmpty(anagrafic.MobilePhone))
                {
                    anagrafic.MobilePhone = externalVerif.MobilePhone;
                } 

                anagrafic.RegisteredOffice = externalVerif.RegisteredOffice;

                SpidService.SetLog(Guid.Parse(Municipality), "Comunix Login - User registration", "");

                var dbUser = await AccountService.RegisterExternal(anagrafic, "spid", SessionWrapper.AUTH_Municipality_ID);

                if (dbUser != null)
                {
                    var success = await AccountService.LoginExternal(dbUser.ID.ToString(), dbUser.LastLoginToken.ToString());

                    SpidService.SetLog(Guid.Parse(Municipality), "Comunix Login - User registration success", "");

                    if (success)
                    {
                        LocalRedirect();

                        return;
                    }
                }
                else
                {
                    dbUser = await AccountService.RegisterExternal(anagrafic, "custom", SessionWrapper.AUTH_Municipality_ID);

                    if (dbUser != null)
                    {
                        var success = await AccountService.LoginExternal(dbUser.ID.ToString(), dbUser.LastLoginToken.ToString());

                        SpidService.SetLog(Guid.Parse(Municipality), "Comunix Login - User registration success", "");

                        if (success)
                        {
                            LocalRedirect();

                            return;
                        }
                    }
                    else
                    {
                        NavManager.NavigateTo("/Login", true);
                        StateHasChanged();
                    }
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async void LocalRedirect()
        {
            var result = await SessionStorage.GetItemAsync<string>("RedirectURL");

            if (result != null)
            {
                NavManager.NavigateTo(result, true);
                StateHasChanged();
            }
            else if (TargetUrl != null)
            {
                NavManager.NavigateTo(HttpUtility.UrlDecode(TargetUrl), true);
                StateHasChanged();
            }
            else
            {
                NavManager.NavigateTo("/", true);
                StateHasChanged();
            }
        }
    }
}
