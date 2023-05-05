using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Frontend.Mantainance
{
    public partial class MantainanceList
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }

        private List<FORM_Definition> DefinitionList { get; set; }
        private List<AUTH_Authority> AuthorityList { get; set; }
        private string? _keyword;
        private string? Keyword
        {
            get
            {
                return _keyword;
            }
            set
            {
                _keyword = value;
                ShowCount = 6;
                StateHasChanged();
            }
        }
        private int ShowCount { get; set; } = 6;
        protected override async Task OnInitializedAsync()
        {
            StateHasChanged();

            await GetData();

            SessionWrapper.PageTitle = TextProvider.Get("FRONTEND_MANTAINANCE_LIST");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Mantainance", "FRONTEND_MANTAINANCE_LIST", null);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            if(SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                var def = await FormDefinitionProvider.GetDefinitionListByCategory(SessionWrapper.AUTH_Municipality_ID.Value, FORMCategories.Maintenance);

                DefinitionList = def.Where(p => p.Enabled == true).ToList();
            }

            return true;
        }
        private void NavigateTo(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (!NavManager.Uri.EndsWith(url))
                {
                    BusyIndicatorService.IsBusy = true;
                    NavManager.NavigateTo(url);
                    StateHasChanged();
                }
            }
        }
        private void IncreaseShowCount()
        {
            ShowCount += 6;
            StateHasChanged();
        }
        private async void ScrollToTop()
        {
            await EnviromentService.ScrollToTop();
        }
    }
}
