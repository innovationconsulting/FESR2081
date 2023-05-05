using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models.Textvorlagen;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Pages.Templates.Admin
{
    public partial class Administration
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IFORMApplicationProvider ApplicationProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] IRoomProvider RoomProvider { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }

        private List<AUTH_MunicipalityApps>? AktiveApplications;
        private Guid? CurrentApplication;
        private List<StatusItem> StatusList = new List<StatusItem>();
        private bool IsDataBusy = true;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_TEXTTEMLPATE_LIST");

            AktiveApplications = await AuthProvider.GetMunicipalityApps();

            if (AktiveApplications != null && AktiveApplications.Select(p => p.APP_Application_ID).Contains(Applications.Forms)) 
            {
                CurrentApplication = Applications.Forms;
            }
            else if (AktiveApplications != null && AktiveApplications.Select(p => p.APP_Application_ID).Contains(Applications.Canteen))
            {
                CurrentApplication = Applications.Canteen;
            }
            else if (AktiveApplications != null && AktiveApplications.Select(p => p.APP_Application_ID).Contains(Applications.Rooms))
            {
                CurrentApplication = Applications.Rooms;
            }
            else if (AktiveApplications != null && AktiveApplications.Select(p => p.APP_Application_ID).Contains(Applications.Mantainences))
            {
                CurrentApplication = Applications.Mantainences;
            }
            else if (AktiveApplications != null && AktiveApplications.Select(p => p.APP_Application_ID).Contains(Applications.Organisations))
            {
                CurrentApplication = Applications.Organisations;
            }


            await LoadStatus();

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void ChangeCurrentApplication(Guid Application_ID)
        {
            IsDataBusy = true;
            StateHasChanged();


            CurrentApplication = Application_ID;
            await LoadStatus();

            IsDataBusy = false;
            StateHasChanged();
        }
        private async Task<bool> LoadStatus()
        {
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                StatusList.Clear();
                long SortOrder = 1;

                if (CurrentApplication == Applications.Forms)
                {
                    var statuslist = ApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);


                    foreach (var status in statuslist.OrderBy(p => p.SortOrder).ToList())
                    {
                        StatusList.Add(new StatusItem()
                        {
                            ID = status.ID.ToString(),
                            Description = TextProvider.Get(status.TEXT_SystemTexts_Code),
                            Icon = status.Icon,
                            Context = "FORMS",
                            SortOrder = SortOrder
                        });

                        SortOrder++;
                    }
                }
                else if (CurrentApplication == Applications.Canteen)
                {
                    var statuslist = await CanteenProvider.GetSubscriberStatuses();

                    foreach (var status in statuslist.OrderBy(p => p.SortOrder).ToList())
                    {
                        StatusList.Add(new StatusItem()
                        {
                            ID = status.ID.ToString(),
                            Description = TextProvider.Get(status.TEXT_SystemTexts_Code),
                            Icon = status.Icon,
                            Context = "CANTEEN",
                            SortOrder = SortOrder
                        });

                        SortOrder++;
                    }
                }
                else if (CurrentApplication == Applications.Rooms)
                {
                    var statuslist = await RoomProvider.GetBookingStatusList();

                    foreach (var status in statuslist.OrderBy(p => p.SortOrder).ToList())
                    {
                        StatusList.Add(new StatusItem()
                        {
                            ID = status.ID.ToString(),
                            Description = status.Description,
                            Icon = status.IconCSS,
                            Context = "ROOMS",
                            SortOrder = SortOrder
                        });

                        SortOrder++;
                    }
                }
                else if (CurrentApplication == Applications.Mantainences)
                {
                    var statuslist = ApplicationProvider.GetStatusListByMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                    foreach (var status in statuslist.Where(p => p.UserSelectable == true).OrderBy(p => p.SortOrder).ToList())
                    {
                        StatusList.Add(new StatusItem()
                        {
                            ID = status.ID.ToString(),
                            Description = TextProvider.Get(status.TEXT_SystemTexts_Code),
                            Icon = status.Icon,
                            Context = "MANTAINENCES",
                            SortOrder = SortOrder
                        });

                        SortOrder++;
                    }
                }
                else if (CurrentApplication == Applications.Organisations)
                {
                    var statuslist = await OrgProvider.GetStatusList();

                    if (statuslist != null)
                    {
                        foreach (var status in statuslist.Where(p => p.UserSelectable == true).OrderBy(p => p.SortOrder).ToList())
                        {
                            StatusList.Add(new StatusItem()
                            {
                                ID = status.ID.ToString(),
                                Description = TextProvider.Get(status.TEXT_SystemText_Code),
                                Icon = status.Icon,
                                Context = "ORGANISATIONS",
                                SortOrder = SortOrder
                            });

                            SortOrder++;
                        }
                    }
                }
            }

            return true;
        }
    }
}
