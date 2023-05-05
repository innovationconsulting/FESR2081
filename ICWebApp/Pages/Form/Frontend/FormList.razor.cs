using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class FormList
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }

        private string? _keyword;
        private List<FORM_Definition> DefinitionList { get; set; }
        private List<AUTH_Authority> AuthorityList { get; set; }
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
            AnchorService.ForceShow = true;
            AnchorService.SkipForceReset = true;
            StateHasChanged();

            await GetData();

            SessionWrapper.PageTitle = TextProvider.Get("FRONTEND_AUTHORITY_FORM_LIST");
            SessionWrapper.ShowTitleSepparation = false;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Form", "FRONTEND_AUTHORITY_FORM_LIST", null);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            AnchorService.SkipForceReset = false;

            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {
            if(SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                var data = await FormDefinitionProvider.GetDefinitionListOnline(SessionWrapper.AUTH_Municipality_ID.Value);

                DefinitionList = data.Where(p => p.FORM_Definition_Category_ID == FORMCategories.Applications).ToList();

                var auth = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, true);

                AuthorityList = auth.Where(p => DefinitionList.Select(p => p.AUTH_Authority_ID).Distinct().Contains(p.ID)).ToList();
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
    }
}
