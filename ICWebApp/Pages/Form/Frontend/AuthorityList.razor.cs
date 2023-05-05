using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class AuthorityList
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Parameter] public string ID { get; set; }

        private List<FORM_Definition> DefinitionList { get; set; }
        private List<FORM_Definition> DefinitionListAll { get; set; }
        private AUTH_Authority? Data { get; set; }
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

        protected override async Task OnParametersSetAsync()
        {
            if (ID == null)
            {
                NavManager.NavigateTo("/");
                return;
            }

            await GetData();

            if (Data == null)
            {
                NavManager.NavigateTo("/");
                return;
            }

            if (Data != null)
            {
                SessionWrapper.PageTitle = Data.Description;
                SessionWrapper.PageSubTitle = Data.ShortText;
            }

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Form", "FRONTEND_AUTHORITY_FORM_LIST", null);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), null, null, Data.Description);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async Task<bool> GetData()
        {
            if(SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                Data = await AuthProvider.GetAuthority(Guid.Parse(ID));

                DefinitionList = await FormDefinitionProvider.GetDefinitionListByAuthoriy(SessionWrapper.AUTH_Municipality_ID.Value, Guid.Parse(ID));

                var data = await FormDefinitionProvider.GetDefinitionListOnline(SessionWrapper.AUTH_Municipality_ID.Value);

                DefinitionListAll = data.Where(p => p.FORM_Definition_Category_ID == FORMCategories.Applications).ToList();
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
