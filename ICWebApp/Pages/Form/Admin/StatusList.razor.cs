using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Form.Admin
{
    public partial class StatusList
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }

        private List<FORM_Application_Status_Municipal>? MunicipalStatusList { get; set; }

        protected override void OnInitialized()
        {
            SessionWrapper.PageTitle = TextProvider.Get("FORM_DEFINITION_FORM_STATUS");
            SessionWrapper.PageSubTitle = TextProvider.Get("SETTINGS");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Form/Status/Settings", "FORM_DEFINITION_FORM_STATUS", null, null, true);

            MunicipalStatusList = GetData();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            base.OnInitialized();
        }
        private List<FORM_Application_Status_Municipal>? GetData()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                return FormApplicationProvider.GetOrCreateMunicipalStatusList(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return null;
        }

        private void SaveMunicipalStatus(FORM_Application_Status_Municipal item)
        {
            FormApplicationProvider.SetMunicipalStatus(item);
            StateHasChanged();
        }
        private void MoveUpStatus(FORM_Application_Status_Municipal opt)
        {
            if (MunicipalStatusList != null && MunicipalStatusList.Count() > 0)
            {
                var newPos = MunicipalStatusList.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    FormApplicationProvider.SetMunicipalStatus(opt);
                    FormApplicationProvider.SetMunicipalStatus(newPos);

                    MunicipalStatusList = GetData();
                }
            }

            StateHasChanged();
        }
        private void MoveDownStatus(FORM_Application_Status_Municipal opt)
        {
            if (MunicipalStatusList != null && MunicipalStatusList.Count() > 0)
            {
                var newPos = MunicipalStatusList.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    FormApplicationProvider.SetMunicipalStatus(opt);
                    FormApplicationProvider.SetMunicipalStatus(newPos);

                    MunicipalStatusList = GetData();
                }
            }

            StateHasChanged();
        }
    }
}
