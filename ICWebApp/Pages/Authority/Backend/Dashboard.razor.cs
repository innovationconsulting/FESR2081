using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Authority.Backend
{
    public partial class Dashboard
    {

        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get;set;}
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFormAdministrationHelper FormAdministrationHelper { get; set; }
        [Parameter] public string AuthorityID { get; set; }


        private AUTH_Authority? Authority { get; set; }
        private int ApplicationCount { get; set; } = 0;
        private List<V_FORM_Definition_Statistik>? DefinitionStatistik { get; set; }
        private List<V_FORM_Application_Priority_Statistik>? PriorityStatistik { get; set; }
        private List<V_FORM_Application_Status_Statistik>? StatusStatistik { get; set; }
        private V_AUTH_Authority_Statistik? AuthorityStatistik { get; set; }
        private List<FORM_Application_Status>? StatusList { get; set; }
        private List<FORM_Application_Priority>? PriorityList { get; set; }

        private Administration_Filter_Item Filter = new Administration_Filter_Item();

        


        protected override async Task OnParametersSetAsync()
        {
            if (AuthorityID == null)
            {
                NavManager.NavigateTo("/Backend/Landing");
                StateHasChanged();
                return;
            }

            Authority = await GetData();

            if (Authority == null)
            {
                NavManager.NavigateTo("/Backend/Landing");
                StateHasChanged();
                return;
            }

            StatusList = GetStatusList();
            PriorityList = await GetPriorityList();

            DefinitionStatistik = await GetDefinitionStatistik();
            PriorityStatistik = await GetApplicationPriorityStatistik();
            StatusStatistik = await GetApplicationStatusStatistik();
            AuthorityStatistik = await GetAuthorityStatistik();

            if (PriorityList != null && PriorityStatistik != null)
            {

                foreach (var prio in PriorityList.OrderBy(p => p.SortOrder))
                {
                    var priorityStat = PriorityStatistik.FirstOrDefault(p => p.ID == prio.ID);

                    if(priorityStat != null)
                    {
                        prio.Amount =  priorityStat.ApplicationCount;
                    }
                    else
                    {
                        prio.Amount = 0;
                    }

                    if(prio.Amount == PriorityStatistik.Max(p => p.ApplicationCount))
                    {
                        prio.Explode = true;
                    }
                }
            }
            
            SessionWrapper.PageTitle = TextProvider.Get(Authority.TEXT_SystemText_Code);

            BusyIndicatorService.IsBusy = false;
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
        private async Task<List<V_FORM_Application_Priority_Statistik>?> GetApplicationPriorityStatistik()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return await FormApplicationProvider.GetPriorityStatistik(SessionWrapper.AUTH_Municipality_ID.Value, Guid.Parse(AuthorityID));
            }

            return null;
        }
        private async Task<List<V_FORM_Application_Status_Statistik>?> GetApplicationStatusStatistik()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return await FormApplicationProvider.GetStatusStatistik(SessionWrapper.AUTH_Municipality_ID.Value, Guid.Parse(AuthorityID));
            }

            return null;
        }
        private async Task<List<V_FORM_Definition_Statistik>?> GetDefinitionStatistik()
        {
            if (AuthorityID != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                return await FormDefinitionProvider.GetDefintionStatistik(SessionWrapper.AUTH_Municipality_ID.Value, Guid.Parse(AuthorityID));
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
        private void ShowListByStatus(Guid CurrentStatusID)
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
                Filter.FORM_Application_Status_ID = new List<Guid>() { CurrentStatusID };

                FormAdministrationHelper.Filter = Filter;

                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Backend/Form/Administration");
                StateHasChanged();
            }
        }
        private void ShowListByPriority(Guid CurrentPriorityID)
        {
            if (Authority != null)
            {
                GetOrCreateFilter();

                Filter.Text = null;
                Filter.SubmittedFrom = null;
                Filter.SubmittedTo = null;
                Filter.Archived = false;
                Filter.Auth_User_ID = null;

                if (StatusList != null)
                {
                    var statList = StatusList.Select(p => p.ID).ToList();
                    Filter.FORM_Application_Status_ID = new List<Guid>(statList);
                }

                Filter.DeadlineFrom = null;
                Filter.DeadlineTo = null;
                Filter.EskalatedTasks = false;
                Filter.ManualInput = false;
                Filter.MyTasks = false;

                Filter.AUTH_Authority_ID = new List<Guid>() { Authority.ID };
                Filter.FORM_Application_Priority_ID = new List<Guid>() { CurrentPriorityID };

                FormAdministrationHelper.Filter = Filter;

                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Backend/Form/Administration");
                StateHasChanged();
            }
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

                if(Group == 1)
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
            if(FormAdministrationHelper.Filter != null)
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
    }
}
