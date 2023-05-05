using System.Globalization;
using Blazored.SessionStorage;
using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Blazored.LocalStorage;
using ICWebApp.Application.Settings;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;

namespace ICWebApp;

public partial class App
{
    [Inject] ISessionWrapper SessionWrapper { get; set; }
    [Inject] AuthenticationStateProvider AuthenticationHelper { get; set; }
    [Inject] ILocalizationService LocalizationService { get; set; }
    [Inject] ILANGProvider LangProvider { get; set; }
    [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] ISYSProvider SYSProvider { get; set; }
    [Inject] NavigationManager NavManager { get; set; }
    [Inject] ICONFProvider ConfProvider { get; set; }
    [Inject] ISessionStorageService SessionStorage { get; set; }
    [Inject] IAccountService AccountService { get; set; }
    [Inject] IAUTHProvider AuthProvider { get; set; }
    [Inject] IEnviromentService EnviromentService { get; set; }
    [Inject] IJSRuntime JSRuntime { get; set; }
    [Inject] IAPPProvider APPProvider { get; set; }
    [Inject] ITEXTProvider TextProvider { get; set; }
    [Inject] ITASKService TaskService { get; set; }
    [Inject] IActionBarService ActionBarService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        SessionWrapper.Initializing = true;
        BusyIndicatorService.IsBusy = true;
        NavManager.LocationChanged += NavManager_LocationChanged;
        EnviromentService.OnIsMobileChanged += EnviromentService_OnIsMobileChanged;

        var prefixes = await AuthProvider.GetProgrammPrefixes();

        var CurrentMunicipality = prefixes.Where(p => p.Prefix != null && NavManager.BaseUri.Contains(p.Prefix))
            .FirstOrDefault();

        if (CurrentMunicipality != null && CurrentMunicipality.AUTH_Municipality_ID != null)
        {
            SessionWrapper.AUTH_Municipality_ID = CurrentMunicipality.AUTH_Municipality_ID;
        }
        else if (NavManager.BaseUri.Contains("localhost"))
        {
            SessionWrapper.AUTH_Municipality_ID = ComunixSettings.TestMunicipalityID;
        }
        else if (NavManager.BaseUri.Contains("status.comunix.bz.it"))
        {
            NavManager.NavigateTo("/PrtgDashboard", true);
        }
        else if (NavManager.BaseUri.Contains("192.168.77"))
        {
            SessionWrapper.AUTH_Municipality_ID = ComunixSettings.TestMunicipalityID;
        }
        else
        {
            if (!NavManager.BaseUri.Contains("spid")) NavManager.NavigateTo("https://innovation-consulting.it/", true);
        }

        StateHasChanged();

        await base.OnInitializedAsync();
    }
    private void EnviromentService_OnIsMobileChanged()
    {
        StateHasChanged();
    }
    private async void LogData()
    {
        var data = new SYS_Log();

        data.CreationDate = DateTime.Now;
        data.Url = NavManager.Uri;
        data.Type = "pagelog";

        if (SessionWrapper.CurrentUser != null)
        {
            data.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

            if (SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
                data.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID;
        }

        await SYSProvider.SetLog(data);
    }
    private async void NavManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        LogData();

        await VerifyAccess();

        CheckUser();

        TaskService.TASK_Context_ID = null;
        TaskService.ContextElementID = null;
        TaskService.ContextName = null;
        TaskService.ShowToolbar = true;

        ActionBarService.ClearActionBar();
    }
    private void CheckUser()
    {
        if (SessionWrapper.CurrentUser != null)
        {
            AuthProvider.CheckUserVerifications(SessionWrapper.CurrentUser.ID);
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            SessionWrapper.PageIsRendered = true;

            if (AuthenticationHelper != null)
            {
                SessionWrapper.Initializing = true;
                (AuthenticationHelper as AuthenticationHelper).Notify(); //HAS TO BE THE FIRST CALL HERE
            }

            var languages = await LangProvider.GetAll();
            var langId = await AuthProvider.GetLanguageIdFromDomain(NavManager.BaseUri);

            if (languages != null && langId != null && !await LangProvider.LanguageInitialized()) 
            {
                var lang = languages.FirstOrDefault(e => e.ID == langId);
                if (lang != null)
                {
                    await LangProvider.SetLanguage(lang.Code);
                }
            }
            
            if (languages != null && !await LangProvider.LanguageInitialized())
            {
                var CurrentBrowserLanguage = await LocalizationService.GetBrowserCulture();

                if (!CultureInfo.CurrentCulture.Name.Contains(CurrentBrowserLanguage))
                    await LangProvider.SetLanguage(CurrentBrowserLanguage);
            }
            
            try
            {
                var mobile = await JSRuntime.InvokeAsync<bool>("isDevice");
                EnviromentService.IsMobile = mobile;
            }
            catch
            {
            }

            if (JSRuntime != null && EnviromentService != null)
            {
                try
                {
                    var result = await JSRuntime.InvokeAsync<string>("enviromentHelper_CheckWidthOnStartup");
                    if (!string.IsNullOrEmpty(result))
                    {
                        var Width = 1600;

                        int.TryParse(result, out Width);

                        if (Width <= 1000)
                            EnviromentService.IsMobile = true;
                        else
                            EnviromentService.IsMobile = false;

                        if (Width <= 1700)
                            EnviromentService.IsTablet = true;
                        else
                            EnviromentService.IsTablet = false;
                    }
                }
                catch
                {
                }
            }

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                var municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                if (municipality != null)
                    try
                    {
                        await EnviromentService.SetPageTitle(TextProvider.Get("APPLICANT_MUNICIPALITY") + " " +
                                                             municipality.Name);
                    }
                    catch
                    {
                    }

                try
                {
                    await SessionStorage.SetItemAsync("municipality", SessionWrapper.AUTH_Municipality_ID);
                }
                catch
                {
                }

                SessionWrapper.MunicipalityApps = await AuthProvider.GetMunicipalityApps();
            }

            await VerifyAccess();
        }

        await base.OnAfterRenderAsync(firstRender);
    }
    private async Task<bool> VerifyAccess()
    {
        var lockedUrlList = await APPProvider.GetAppUrl();
        var allowed = true;

        if (lockedUrlList != null)
        {
            var mainUrl = NavManager.Uri;
            var urlsToCheck = lockedUrlList.Where(p => p.Url != null && mainUrl.EndsWith(p.Url)).ToList();

            if (urlsToCheck != null && urlsToCheck.Count() > 0)
            {
                if (SessionWrapper.MunicipalityApps == null || SessionWrapper.MunicipalityApps.Count() == 0)
                    SessionWrapper.MunicipalityApps = await AuthProvider.GetMunicipalityApps();

                var appIDs = SessionWrapper.MunicipalityApps.Select(p => p.APP_Application_ID);

                var locCheck = false;

                foreach (var UrlCheck in urlsToCheck)
                    if (UrlCheck.APP_Applications_ID != null && appIDs.Contains(UrlCheck.APP_Applications_ID.Value))
                    {
                        locCheck = true;
                        break;
                    }

                allowed = locCheck;
            }
        }

        if (!allowed)
        {
            NavManager.NavigateTo("/");
            StateHasChanged();

            return false;
        }

        return true;
    }
}