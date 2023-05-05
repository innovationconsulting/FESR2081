using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Reporting;
using Telerik.Reporting.Processing;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class Application_ComittedView
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Parameter] public string ID { get; set; }
        private FORM_Application? Data { get; set; }
        private FORM_Definition? Definition { get; set; }
        private List<FORM_Definition_Additional_FORM>? FORM_AdditionalForms { get; set; }
        private List<FORM_Definition> FORM_AdditionalDefinitions { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (ID == null)
                {
                    NavManager.NavigateTo("/");
                    StateHasChanged();
                    return;
                }

                Data = await GetApplication();
                Definition = await GetDefinition();

                if (Data == null || Definition == null)
                {
                    NavManager.NavigateTo("/");
                    StateHasChanged();
                    return;
                }

                CrumbService.ClearBreadCrumb();
                CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_MY_APPLICATIONS", null);
                CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), "FRONTEND_APPLICATION_COMITTED_VIEW", null);

                FORM_AdditionalForms = await GetAdditionalForms();

                FORM_AdditionalDefinitions = new List<FORM_Definition>();

                if (FORM_AdditionalForms != null)
                {
                    foreach (var form in FORM_AdditionalForms)
                    {
                        if (form.FORM_Definition_Additional_ID != null)
                        {
                            var item = await FormDefinitionProvider.GetDefinition(form.FORM_Definition_Additional_ID.Value);

                            if (item != null)
                            {
                                FORM_AdditionalDefinitions.Add(item);
                            }
                        }
                    }
                }

                SessionWrapper.PageTitle = Definition.FORM_Name;

                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        private async Task<FORM_Application?> GetApplication()
        {
            if (ID != null)
            {
                var data = await FormApplicationProvider.GetApplication(Guid.Parse(ID));

                return data;
            }

            return null;
        }
        private async Task<FORM_Definition?> GetDefinition()
        {
            if (Data != null && Data.FORM_Definition_ID != null)
            {
                var data = await FormDefinitionProvider.GetDefinition(Data.FORM_Definition_ID.Value);

                return data;
            }

            return null;
        }
        private async Task<List<FORM_Definition_Additional_FORM>?> GetAdditionalForms()
        {
            if (Definition != null)
            {
                var list = await FormDefinitionProvider.GetDefinitionAdditionalFORMList(Definition.ID);

                return list;
            }

            return null;
        }
        private void BackToData()
        {
            NavManager.NavigateTo("/User/Services");
            StateHasChanged();
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
    }
}
