using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Telerik.Blazor.Components;

namespace ICWebApp.Pages.Form.Backend
{
    public partial class Administration
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] ISETTProvider SettProvider { get; set; }
        [Inject] IFormAdministrationHelper FormAdministrationHelper { get; set; }

        private List<V_FORM_Application> Data = new List<V_FORM_Application>();
        private List<Guid> AllowedAuthorities = new List<Guid>();
        private bool IsDataBusy { get; set; } = true;
        private Administration_Filter_Item Filter = new Administration_Filter_Item();
        private bool ColumnSettingsWindowVisible { get; set; } = false;
        private bool HasCommitteeRights { get; set; } = false;

        private bool MunicipalityHasMantainances = true;
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_FORM_ADMINISTRATION");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Backend/Form/Administration", "MAINMENU_BACKEND_FORM_ADMINISTRATION", null, null, true);

            if (FormAdministrationHelper.Filter != null)
            {
                Filter = FormAdministrationHelper.Filter;
            }
            else
            {
                Filter.FORM_Application_Status_ID = new List<Guid>();

                var municipalStatusList = FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                foreach (var item in municipalStatusList.Where(p => p.Selectable == true))
                {
                    Filter.FORM_Application_Status_ID.Add(item.ID);
                }

                Filter.FORM_Application_Status_ID.Add(FORMStatus.Comitted);

                Filter.FORM_Application_Priority_ID = new List<Guid>
                {
                    Guid.Parse("f1af3d5a-7a02-4faa-8845-34d2a5a66785"),
                    Guid.Parse("4318613b-17f7-4e15-a613-8801d4d5ae65"),
                    Guid.Parse("80dadb40-fa35-43eb-b2d5-871b8dc40913"),
                    Guid.Parse("f47f39b8-bfaf-4b71-9fd0-f3c978a4d1a4")
                };
            }

            HasCommitteeRights = AuthProvider.HasUserRole(AuthRoles.Committee) || AuthProvider.HasUserRole(AuthRoles.Developer);
            AllowedAuthorities = await GetAuthorities();

            //ListSettings = await GetOrCreateListSettings();
            Data = await GetData(Filter);

            var MunicipalApps = await AuthProvider.GetMunicipalityApps();

            if(MunicipalApps != null)
            {
                MunicipalityHasMantainances = MunicipalApps.Any(e => e.APP_Application_ID == Applications.Mantainences);
            }

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<Guid>> GetAuthorities()
        {
            if (SessionWrapper.CurrentUser != null)
            {
                var userAuthorities = await AuthProvider.GetUserAuthorities(SessionWrapper.CurrentUser.ID);

                return userAuthorities.Where(p => p.AUTH_Authority_ID != null).Select(p => p.AUTH_Authority_ID.Value).ToList();
            }

            return new List<Guid>();
        }
        private async Task<List<V_FORM_Application>> GetData(Administration_Filter_Item? Filter)
        {
            if (SessionWrapper.AUTH_Municipality_ID != null && SessionWrapper.CurrentUser != null)
            {
                var applications = await FormApplicationProvider.GetApplications(SessionWrapper.AUTH_Municipality_ID.Value, SessionWrapper.CurrentUser.ID, AllowedAuthorities, Filter);

                FormAdministrationHelper.Filter = Filter;

                return applications;
            }

            return new List<V_FORM_Application>();
        }
        private void EditManualInput(Guid FORM_Application_ID)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/Form/Application/" + FORM_Application_ID);
        }
        private void AddApplication()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/Form/Application/New");
        }
        private void AddManteinance()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/Form/Manteinance/New");
        }
        private async void FilterSearch(Administration_Filter_Item Filter)
        {
            IsDataBusy = true;
            StateHasChanged();

            this.Filter = Filter;

            Data = await GetData(this.Filter);

            IsDataBusy = false;
            StateHasChanged();
        }
        private void ShowDetailPage(Guid FORM_Application_ID)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Backend/Form/Detail/" + FORM_Application_ID);

        }
        private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
        {
            var item = (V_FORM_Application)Args.Item; ;

            if (item != null)
            {
                if (item.IsManualInput == true && item.FORM_Application_Status_ID == FORMStatus.ToSign && item.ID != null) //TO SIGN
                {
                    EditManualInput(item.ID);
                }
                else if (item.ID != null) //COMITTED
                {
                    ShowDetailPage(item.ID);
                }
            }

            return true;
        } 
        private async Task OnStateInitHandler(GridStateEventArgs<V_FORM_Application> args)
        {
            try
            {
                var state = await SettProvider.GetUserState(SessionWrapper.CurrentUser.ID);

                if (state != null && !string.IsNullOrEmpty(state.FORM_Administration_State))
                {
                    args.GridState = JsonSerializer.Deserialize<GridState<V_FORM_Application>>(state.FORM_Administration_State);
                }

            }
            catch { }
        }
        private async void OnStateChangedHandler(GridStateEventArgs<V_FORM_Application> args)
        {
            var state = await SettProvider.GetUserState(SessionWrapper.CurrentUser.ID);
            var data = JsonSerializer.Serialize(args.GridState);

            if (state != null)
            {
                state.FORM_Administration_State = data;                

                await SettProvider.SetUserState(state);
            }
            else
            {
                state = new SETT_User_States();
                state.ID = Guid.NewGuid();
                state.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                state.FORM_Administration_State = data;

                await SettProvider.SetUserState(state);
            }
        }
    }
}
