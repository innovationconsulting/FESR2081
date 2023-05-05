using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace ICWebApp.Components.Dashboard
{
    public partial class AuthorityContainer
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFormAdministrationHelper FormAdministrationHelper { get; set; }
        [Parameter] public string AuthorityID { get; set; }
        private AUTH_Authority? Authority { get; set; }
        private V_AUTH_Authority_Statistik? AuthorityStatistik { get; set; }
        private List<FORM_Application_Status>? StatusList { get; set; }
        private List<FORM_Application_Priority>? PriorityList { get; set; }
        private Administration_Filter_Item Filter = new Administration_Filter_Item();
        private List<V_FORM_Application> Applications = new List<V_FORM_Application>();
        private bool IsDataBusy { get; set; } = true;

        protected override async Task OnParametersSetAsync()
        {
            IsDataBusy = true;
            StateHasChanged();

            Authority = await GetData();

            StatusList = GetStatusList();
            PriorityList = await GetPriorityList();

            AuthorityStatistik = await GetAuthorityStatistik();

            Applications = await GetApplications();

            IsDataBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }
        private async Task<AUTH_Authority?> GetData()
        {
            if (AuthorityID != null)
            {
                return await AuthProvider.GetAuthority(Guid.Parse(AuthorityID));
            }

            return null;
        }
        private async Task<V_AUTH_Authority_Statistik?> GetAuthorityStatistik()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return await AuthProvider.GetAuthorityStatistik(Guid.Parse(AuthorityID), SessionWrapper.AUTH_Municipality_ID);
            }

            return null;
        }
        private List<FORM_Application_Status>? GetStatusList()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            return null;
        }
        private async Task<List<FORM_Application_Priority>?> GetPriorityList()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return await FormApplicationProvider.GetPriorities();
            }

            return null;
        }
        private async Task<List<V_FORM_Application>> GetApplications()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return await FormApplicationProvider.GetApplications(Guid.Parse(AuthorityID), SessionWrapper.AUTH_Municipality_ID.Value, 6);
            }

            return null;
        }
        private void ShowListByStatusGroup(int Group)
        {
            if (Authority != null)
            {
                GetOrCreateFilter();

                Filter.Text = null;
                Filter.SubmittedFrom = null;
                Filter.SubmittedTo = null;
                Filter.Archived = false;
                Filter.Auth_User_ID = null;

                if (PriorityList != null)
                {
                    var prioList = PriorityList.Select(p => p.ID).ToList();
                    Filter.FORM_Application_Priority_ID = new List<Guid>(prioList);
                }

                Filter.DeadlineFrom = null;
                Filter.DeadlineTo = null;
                Filter.EskalatedTasks = false;
                Filter.ManualInput = false;
                Filter.MyTasks = false;

                Filter.AUTH_Authority_ID = new List<Guid>() { Authority.ID };

                if (Group == 1)
                {
                    if (StatusList != null)
                    {
                        var statList = StatusList.Where(p => p.FinishedStatus == null).Select(p => p.ID).ToList();
                        Filter.FORM_Application_Status_ID = new List<Guid>(statList);
                    }
                }
                else if (Group == 2)
                {
                    Filter.FORM_Application_Status_ID = new List<Guid>() { FORMStatus.Comitted };
                }
                else if (Group == 3)
                {
                    if (StatusList != null)
                    {
                        var statList = StatusList.Where(p => p.FinishedStatus != null && p.ID != FORMStatus.Comitted).Select(p => p.ID).ToList();
                        Filter.FORM_Application_Status_ID = new List<Guid>(statList);
                    }
                }
                else
                {
                    var municipalStatusList = FormApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
                    Filter.FORM_Application_Status_ID = new List<Guid>();

                    foreach (var item in municipalStatusList.Where(p => p.Selectable == true))
                    {
                        Filter.FORM_Application_Status_ID.Add(item.ID);
                    }

                    Filter.FORM_Application_Status_ID.Add(FORMStatus.Comitted);
                }

                FormAdministrationHelper.Filter = Filter;

                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Backend/Form/Administration");
                StateHasChanged();
            }
        }
        private void GetOrCreateFilter()
        {
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

                Filter.FORM_Application_Priority_ID = new List<Guid>();
                Filter.FORM_Application_Priority_ID.Add(Guid.Parse("f1af3d5a-7a02-4faa-8845-34d2a5a66785"));
                Filter.FORM_Application_Priority_ID.Add(Guid.Parse("4318613b-17f7-4e15-a613-8801d4d5ae65"));
                Filter.FORM_Application_Priority_ID.Add(Guid.Parse("80dadb40-fa35-43eb-b2d5-871b8dc40913"));
                Filter.FORM_Application_Priority_ID.Add(Guid.Parse("f47f39b8-bfaf-4b71-9fd0-f3c978a4d1a4"));
            }
        }
        private void ShowDetailPage(Guid ApplicationID)
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Backend/Form/Detail/" + ApplicationID);
            StateHasChanged();
        }
        private async Task<bool> OnRowClick(GridRowClickEventArgs Args)
        {
            var item = (V_FORM_Application)Args.Item; ;

            if (item != null)
            {
                ShowDetailPage(item.ID);
            }

            return true;
        }

    }
}
