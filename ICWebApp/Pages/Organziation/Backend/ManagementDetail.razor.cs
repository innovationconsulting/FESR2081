using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Organziation.Backend
{
    public partial class ManagementDetail
    {

        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IEnviromentService EnviromentService { get; set; }
        [Inject] IMessageService MessageService { get; set; }   
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string ID { get; set; }

        private AUTH_Users? CurrentOrganization;
        private AUTH_Users_Anagrafic? CurrentOrganizationAnagrafic;
        private List<V_AUTH_Users_Organizations> CurrentOrgUsers = new List<V_AUTH_Users_Organizations>();
        private AUTH_Municipality? Municipality { get; set; }
        private bool IsDataBusy { get; set; } = true;
        private bool ShowEditAnagrafic { get; set; } = false;
        private bool ShowEditRole { get; set; } = false;
        private bool ShowAddNewSubstitute { get; set; } = false;

        private List<V_AUTH_Company_Type>? CompanyTypeList;
        private List<V_AUTH_Company_LegalForm>? CompanyLegalFormList = new List<V_AUTH_Company_LegalForm>();
        private V_AUTH_Company_Type? SelectedCompanyType;
        private V_AUTH_Company_LegalForm? SelectedCompanyLegalForm;
        private AUTH_Users_Anagrafic? GesetzlicherVertreter;
        private List<V_AUTH_ORG_Role> RoleList = new List<V_AUTH_ORG_Role>();
        private AUTH_ORG_Users? SelectedOrgUser;
        private ORG_New_Substitute? NewSubstitute;
        private List<MSG_Message_Parameters> MessageParameters = new List<MSG_Message_Parameters>();
        private V_AUTH_BolloFree_Reason? BolloFreeReason = null;

        protected override async Task OnInitializedAsync()
        {
            if (ID == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Backend/Management/List");
                StateHasChanged();
                return;
            }

            BusyIndicatorService.IsBusy = true;
            IsDataBusy = true;
            StateHasChanged();

            CurrentOrganization = await AuthProvider.GetUser(Guid.Parse(ID));

            if (CurrentOrganization == null)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Backend/Management/List");
                StateHasChanged();
                return;
            }

            CompanyTypeList = await GetCompanyType();
            CompanyLegalFormList = await AuthProvider.GetCompanyLegalForms();

            if (CurrentOrganization != null && CompanyTypeList != null && CompanyLegalFormList != null)
            {
                SelectedCompanyType = CompanyTypeList.FirstOrDefault(p => p.ID == CurrentOrganization.AUTH_Company_Type_ID);
                SelectedCompanyLegalForm = CompanyLegalFormList.FirstOrDefault(p => p.ID == CurrentOrganization.AUTH_Company_LegalForm_ID);
            }

            if (CurrentOrganization == null || CurrentOrganization.IsOrganization == false)
            {
                BusyIndicatorService.IsBusy = true;
                NavManager.NavigateTo("/Organization/Backend/Management/List");
                StateHasChanged();
                return;
            }

            SessionWrapper.PageTitle = TextProvider.Get("AUTH_ORG_MANAGEMENT") + " " + CurrentOrganization.Firstname + " " + CurrentOrganization.Lastname;

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Organization/Backend/Management/List", "MAINMENU_BACKEND_SUBSTITUTES_MANAGEMENT", null, null);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), SessionWrapper.PageTitle, null, null, true);

            if (CurrentOrganization != null)
            {
                CurrentOrgUsers = await GetOrganizationUserList();

                CurrentOrganizationAnagrafic = await AuthProvider.GetAnagraficByUserID(CurrentOrganization.ID);

                if (CurrentOrganizationAnagrafic != null && CurrentOrganizationAnagrafic.GV_AUTH_Users_ID != null)
                {
                    GesetzlicherVertreter = await AuthProvider.GetAnagraficByUserID(CurrentOrganizationAnagrafic.GV_AUTH_Users_ID.Value);
                }
            }

            if (CurrentOrganizationAnagrafic != null)
            {
                MessageParameters.Add(new MSG_Message_Parameters()
                {
                    Code = "{Organisation}",
                    Message = CurrentOrganizationAnagrafic.FirstName + " " + CurrentOrganizationAnagrafic.LastName,
                });
            }

            RoleList = await AuthProvider.GetVOrgRoles();
            if (CurrentOrganizationAnagrafic != null && CurrentOrganizationAnagrafic.AUTH_BolloFree_Reason_ID != null)
            {
                BolloFreeReason = (await AuthProvider.GetVBolloFreeReasons()).FirstOrDefault(e =>
                    e.ID == CurrentOrganizationAnagrafic.AUTH_BolloFree_Reason_ID);
            }

            BusyIndicatorService.IsBusy = false;
            IsDataBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<V_AUTH_Company_Type>?> GetCompanyType()
        {
            var result = await AuthProvider.GetVCompanyType();

            return result.Where(p => p.CanBeRepresentation == true).ToList();
        }
        private async Task<List<V_AUTH_Users_Organizations>> GetOrganizationUserList()
        {
            return await AuthProvider.GetOrganizationsUsers(CurrentOrganization.ID);
        }     
        private void BackToPrevious()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Organization/Backend/Management/List");
            StateHasChanged();            
        }
        private async void Remove(V_AUTH_Users_Organizations OrgItem)
        {
            if (OrgItem != null && OrgItem.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("USER_REMOVE_DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                var CurrentOrg = await AuthProvider.GetUserOrganization(OrgItem.ID);

                if (CurrentOrg != null)
                {
                    await AuthProvider.RemoveUserOrganization(CurrentOrg);
                }

                CurrentOrgUsers = await GetOrganizationUserList();
                StateHasChanged();
            }
        }
        private void EditAnagrafic()
        {
            ShowEditAnagrafic = true;
            StateHasChanged();
        }
        private async void OnEditAnagraficCancel()
        {
            if (CurrentOrganization != null)
            {
                CurrentOrganizationAnagrafic = await AuthProvider.GetAnagraficByUserID(CurrentOrganization.ID);
            }

            ShowEditAnagrafic = false;
            StateHasChanged();
        }
        private async void OnEditAnagraficSave()
        {
            if (CurrentOrganization != null)
            {
                CurrentOrganizationAnagrafic = await AuthProvider.GetAnagraficByUserID(CurrentOrganization.ID);

                if (CurrentOrganization != null && CompanyTypeList != null && CompanyLegalFormList != null)
                {
                    SelectedCompanyType = CompanyTypeList.FirstOrDefault(p => p.ID == CurrentOrganization.AUTH_Company_Type_ID);
                    SelectedCompanyLegalForm = CompanyLegalFormList.FirstOrDefault(p => p.ID == CurrentOrganization.AUTH_Company_LegalForm_ID);
                }
            }

            ShowEditAnagrafic = false;
            StateHasChanged();
        }
        private async void EditRole(V_AUTH_Users_Organizations OrgItem)
        {
            if (OrgItem != null && OrgItem.ID != null)
            {
                SelectedOrgUser = await AuthProvider.GetUserOrganization(OrgItem.ID);
                ShowEditRole = true;
                StateHasChanged();
            }
        }
        private async void Deaktivate(V_AUTH_Users_Organizations OrgItem)
        {
            if (OrgItem != null && OrgItem.ID != null)
            {
                var orgUser = await AuthProvider.GetUserOrganization(OrgItem.ID);

                if(orgUser != null)
                {
                    if (orgUser.DeaktivatedAt == null)
                    {
                        orgUser.DeaktivatedAt = DateTime.Now;

                        var msg = await MessageService.GetMessage(orgUser.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_SUBSTITUTION_DEAKTIVATED_TEXT", "NOTIF_SUBSTITUTION_DEAKTIVATED_SHORTTEXT", "NOTIF_SUBSTITUTION_DEAKTIVATED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, MessageParameters);

                        if (msg != null)
                        {
                            await MessageService.SendMessage(msg, NavManager.BaseUri + "/Organization/Dashboard");
                        }
                    }
                    else
                    {
                        orgUser.DeaktivatedAt = null;
                    }

                    await AuthProvider.SetUserOrganization(orgUser);

                }

                CurrentOrgUsers = await GetOrganizationUserList();

                StateHasChanged();
            }
        }
        private async void Confirm(V_AUTH_Users_Organizations OrgItem)
        {
            if (OrgItem != null && OrgItem.ID != null)
            {
                var orgUser = await AuthProvider.GetUserOrganization(OrgItem.ID);

                if (orgUser != null)
                {
                    orgUser.ConfirmedAt = DateTime.Now;

                    var msg = await MessageService.GetMessage(orgUser.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_SUBSTITUTION_ACCESS_GRANTED_TEXT", "NOTIF_SUBSTITUTION_ACCESS_GRANTED_SHORTTEXT", "NOTIF_SUBSTITUTION_ACCESS_GRANTED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, MessageParameters);

                    if (msg != null)
                    {
                        await MessageService.SendMessage(msg, NavManager.BaseUri + "/Organization/Dashboard");
                    }

                    await AuthProvider.SetUserOrganization(orgUser);
                }

                CurrentOrgUsers = await GetOrganizationUserList();

                StateHasChanged();
            }
        }
        private void CancelRole()
        {
            ShowEditRole = false;
            StateHasChanged();
        }
        private async void SaveRole()
        {
            if (SelectedOrgUser != null)
            {
                await AuthProvider.SetUserOrganization(SelectedOrgUser);

                var existingItem = MessageParameters.FirstOrDefault(p => p.Code == "{Role}");

                if (existingItem != null)
                {
                    MessageParameters.Remove(existingItem);
                }

                var userSettings = await AuthProvider.GetSettings(SelectedOrgUser.AUTH_Users_ID.Value);
                var role = RoleList.FirstOrDefault(p => p.ID == SelectedOrgUser.AUTH_ORG_Role_ID.Value);

                if (role != null)
                {
                    if (userSettings != null)
                    {
                        MessageParameters.Add(new MSG_Message_Parameters()
                        {
                            Code = "{Role}",
                            Message = TextProvider.Get(role.TEXT_SystemTexts_Code, userSettings.LANG_Languages_ID),
                        });
                    }
                }

                var msg = await MessageService.GetMessage(SelectedOrgUser.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_SUBSTITUTION_ROLE_CHANGED_TEXT", "NOTIF_SUBSTITUTION_ROLE_CHANGED_SHORTTEXT", "NOTIF_SUBSTITUTION_ROLE_CHANGED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, MessageParameters);

                if (msg != null)
                {
                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Organization/Dashboard");
                }

                CurrentOrgUsers = await GetOrganizationUserList();
            }

            ShowEditRole = false;
            StateHasChanged();
        }
        private void AddNewSubstitute()
        {
            NewSubstitute = new ORG_New_Substitute();
            ShowAddNewSubstitute = true;
            StateHasChanged();
        }
        private void CancelNewSubstitute()
        {
            ShowAddNewSubstitute = false;
            NewSubstitute = null;
            StateHasChanged();
        }
        private async void SaveNewSubstitute()
        {
            if (NewSubstitute != null && SessionWrapper.AUTH_Municipality_ID != null && CurrentOrganization != null && !string.IsNullOrEmpty(NewSubstitute.FiscalNumber))
            {
                AUTH_Users? existingUser = null;

                if (!string.IsNullOrEmpty(NewSubstitute.FiscalNumber))
                {
                    existingUser = await AuthProvider.GetUser(NewSubstitute.FiscalNumber, null, SessionWrapper.AUTH_Municipality_ID.Value, false);

                    if (existingUser == null)
                    {
                        NewSubstitute.Error = TextProvider.Get("ORG_USER_NOT_FOUND_EXCEPTION");
                        StateHasChanged();
                        return;
                    }
                }
                else
                {
                    NewSubstitute.Error = null;
                }

                var existingOrg = await AuthProvider.GetUserOrganization(CurrentOrganization.ID, existingUser.ID);

                if(existingOrg != null)
                {
                    NewSubstitute.Error = TextProvider.Get("ORG_USER_ALREADY_ADDED");
                    StateHasChanged();
                    return;
                }
                else
                {
                    NewSubstitute.Error = null;
                }

                var newUserOrg = new AUTH_ORG_Users();

                newUserOrg.ID = Guid.NewGuid();
                newUserOrg.AUTH_Users_ID = existingUser.ID;
                newUserOrg.ORG_AUTH_Users_ID = CurrentOrganization.ID;
                newUserOrg.CreationDate = DateTime.Now;
                newUserOrg.ConfirmedAt = DateTime.Now;
                newUserOrg.AUTH_ORG_Role_ID = Guid.Parse("d4213886-97eb-45aa-a93a-0e920a657364");

                await AuthProvider.SetUserOrganization(newUserOrg);

                var msg = await MessageService.GetMessage(newUserOrg.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_SUBSTITUTION_ADDED_TEXT", "NOTIF_SUBSTITUTION_ADDED_SHORTTEXT", "NOTIF_SUBSTITUTION_ADDED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, MessageParameters);

                if (msg != null)
                {
                    await MessageService.SendMessage(msg, NavManager.BaseUri + "/Organization/Dashboard");
                }

                CurrentOrgUsers = await GetOrganizationUserList();
            }

            ShowAddNewSubstitute = false;
            StateHasChanged();
        }
    }
}
