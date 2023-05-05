using Blazored.SessionStorage;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Canteen.Frontend;

public partial class LandingPageCanteen
{
    [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
    [Inject] ISessionWrapper SessionWrapper { get; set; }
    [Inject] ICANTEENProvider CanteenProvider { get; set; }
    [Inject] ITEXTProvider TextProvider { get; set; }
    [Inject] NavigationManager NavManager { get; set; }
    [Inject] IBreadCrumbService CrumbService { get; set; }
    [Inject] ISessionStorageService SessionStorage { get; set; }
    [Inject] IFILEProvider FileProvider { get; set; }
    [Inject] IEnviromentService EnviromentService { get; set; }
    [Inject] IMyCivisService MyCivisService { get; set; }
    [Inject] ILANGProvider LangProvider { get; set; }

    [Parameter] public string Infopage { get; set; }

    private decimal CurrentBalance = 0;
    private List<CANTEEN_Subscriber_Movements> LatestMovements = new();
    private List<CANTEEN_Subscriber> Subscribers = new();
    private CANTEEN_User CurrentCanteenUser = new();
    private List<CANTEEN_Ressources>? DataRessources = new List<CANTEEN_Ressources>();
    private List<CANTEEN_Ressources_Extended> DataResourceExtendeds = new List<CANTEEN_Ressources_Extended>();
    private List<CANTEEN_Property>? DataProperties;
    private bool IsDataBusy { get; set; }
    private bool ShowContent { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        BusyIndicatorService.IsBusy = true;
        SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_CANTEEN");
        SessionWrapper.PageSubTitle = TextProvider.Get("MAINMENU_CANTEEN_SERVICE_DESCRIPTION");

        CrumbService.ClearBreadCrumb();

        if (MyCivisService.Enabled == true)
        {
            CrumbService.AddBreadCrumb("/Canteen/MyCivis", "MAINMENU_CANTEEN", null);
        }
        else
        {
            CrumbService.AddBreadCrumb("/Canteen", "MAINMENU_CANTEEN", null);
        }

        SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

        if (SessionWrapper != null && SessionWrapper.CurrentUser != null && SessionWrapper.CurrentSubstituteUser == null)
        {
            Subscribers = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);

            if (Infopage == null && Subscribers.Count > 0)
            {
                ContinueToService();
                return;
            }

            await LoadData();
        }

        DataRessources = await CanteenProvider.GetRessourceList(SessionWrapper.AUTH_Municipality_ID.Value);
        foreach (var res in DataRessources)
        {
            var extended = (await CanteenProvider.GetRessourceExtendedList(res.ID, LangProvider.GetCurrentLanguageID())).FirstOrDefault();
            if (extended != null)
                DataResourceExtendeds.Add(extended);
        }
        DataProperties = await CanteenProvider.GetPropertyList(SessionWrapper.AUTH_Municipality_ID.Value);

        if (MyCivisService.Enabled == true)
        {
            await SessionStorage.SetItemAsync("RedirectURL", "/Canteen/MyCivis/Service");
        }
        else
        {
            await SessionStorage.SetItemAsync("RedirectURL", "/Canteen/Service");
        }

        BusyIndicatorService.IsBusy = false;
        ShowContent = true;
        StateHasChanged();

        if (SessionWrapper != null && SessionWrapper.CurrentUser != null && SessionWrapper.CurrentSubstituteUser == null)
        {
            SessionWrapper.PageButtonAction = ContinueToService;
            SessionWrapper.PageButtonActionTitle = TextProvider.Get("CANTEEN_DASHBOARD_ONLINE_SERVICE");
        }
        else if (SessionWrapper != null && SessionWrapper.CurrentUser == null)
        {
            SessionWrapper.PageButtonAction = RedirectToLogin;
            SessionWrapper.PageButtonActionTitle = TextProvider.Get("CANTEEN_LOGIN");
        }

        StateHasChanged();
        await base.OnInitializedAsync();
    }
    private void SessionWrapper_OnCurrentUserChanged()
    {
        StateHasChanged();
    }
    private async Task<bool> LoadData()
    {
        IsDataBusy = true;

        Subscribers = await CanteenProvider.GetSubscribersByUserID(SessionWrapper.CurrentUser.ID);

        if (Infopage == null && Subscribers.Count > 0) ContinueToService();

        IsDataBusy = false;

        return true;
    }
    private async void RedirectToLogin()
    {
        if (MyCivisService.Enabled == true)
        {
            var RedirectURL = NavManager.Uri + "/Service/MyCivis";
            await SessionStorage.SetItemAsync("RedirectURL", RedirectURL);
            NavManager.NavigateTo("/Login/ReturnUrl=" + Uri.EscapeDataString(RedirectURL), true);
        }
        else
        {
            var RedirectURL = NavManager.Uri + "/Service";
            await SessionStorage.SetItemAsync("RedirectURL", RedirectURL);
            NavManager.NavigateTo("/Login/ReturnUrl=" + Uri.EscapeDataString(RedirectURL), true);
        }
    }
    private void ContinueToService()
    {
        BusyIndicatorService.IsBusy = true;

        if (MyCivisService.Enabled == true)
        {
            NavManager.NavigateTo("/Canteen/MyCivis/Service");
        }
        else
        {
            NavManager.NavigateTo("/Canteen/Service");
        }

        StateHasChanged();
    }
    private async void DownloadRessource(Guid FILE_Fileinfo_ID, string? Name)
    {
        var fileToDownload = await FileProvider.GetFileInfoAsync(FILE_Fileinfo_ID);

        if (fileToDownload != null)
        {
            FILE_FileStorage? blob = null;
            if (fileToDownload.FILE_FileStorage != null && fileToDownload.FILE_FileStorage.Count() > 0)
            {
                blob = fileToDownload.FILE_FileStorage.FirstOrDefault();
            }
            else
            {
                blob = await FileProvider.GetFileStorageAsync(fileToDownload.ID);
            }

            if (blob != null && blob.FileImage != null)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    await EnviromentService.DownloadFile(blob.FileImage, fileToDownload.FileName + fileToDownload.FileExtension);
                }
                else
                {
                    await EnviromentService.DownloadFile(blob.FileImage, Name + fileToDownload.FileExtension);
                }
            }
        }

        StateHasChanged();
    }
}