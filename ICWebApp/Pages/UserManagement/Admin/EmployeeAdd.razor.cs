using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace ICWebApp.Pages.UserManagement.Admin
{
    public partial class EmployeeAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] IMSGProvider MSGProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        private AUTH_Users? Data { get; set; }
        private EditContext editContext { get; set; }
        private bool validEmail = true;
        private bool ShowWindow { get; set; } = false;
        private List<AUTH_Municipality> MunicipalityList = new List<AUTH_Municipality>();
        private List<AUTH_Authority> AuthorityList = new List<AUTH_Authority>();
        private AUTH_User_Authority? CurrentUserAuthority = null;
        private List<AUTH_User_Authority> UserAuthorities = new List<AUTH_User_Authority>();
        private List<FILE_FileInfo> ProfilePicture = new List<FILE_FileInfo>();
        private string validEmailCSS
        {
            get
            {
                if (!validEmail || (editContext != null && editContext.GetValidationMessages(new FieldIdentifier(Data, "DA_Email")).Count() > 0))
                {
                    return "outline: 1px solid red !important;";
                }

                return "";
            }
        }
        private string valEmail
        {
            get
            {
                return Data.DA_Email;
            }
            set
            {
                Data.DA_Email = value;
                editContext.Validate();
                ValidateEmail();
            }
        }
        private bool IsValidPassword = false;
        private PasswordHelper pwhelper = new PasswordHelper();
        private string PasswordQuality { get; set; } = "";
        private string Password
        {
            get
            {
                return Data.Password;
            }
            set
            {

                Data.Password = value;
                PasswordQuality = pwhelper.GetPasswordStrength(value).ToString();
                IsValidPassword = pwhelper.IsValidPassword(value);
                StateHasChanged();
            }
        }

        protected override async Task OnInitializedAsync()
        {

            if (SessionWrapper.CurrentUser == null || SessionWrapper.CurrentUser.AUTH_Municipality_ID == null)
            {
                if (!AuthProvider.HasUserRole(AuthRoles.Developer))  //DEVELOPER
                {
                    BackToPreviousPage();
                }
            }

            if (ID == "New")
            {
                Data = new AUTH_Users();
                editContext = new EditContext(Data);
                Data.ID = Guid.NewGuid();

                if (!AuthProvider.HasUserRole(AuthRoles.Developer))  //DEVELOPER
                { 
                    Data.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID;
                }
            }
            else
            {
                Data = await AuthProvider.GetUser(Guid.Parse(ID));

                if (Data == null)
                {
                    BackToPreviousPage();
                }

                editContext = new EditContext(Data);

                UserAuthorities = await AuthProvider.GetUserAuthorities(Data.ID);

                if (Data.Logo_FILE_FileInfo_ID != null)
                {
                    var FileInfo = await FileProvider.GetFileInfoAsync(Data.Logo_FILE_FileInfo_ID.Value);

                    if (FileInfo != null)
                    {
                        ProfilePicture = new List<FILE_FileInfo>() { FileInfo };
                    }
                }
            }

            if (AuthProvider.HasUserRole(AuthRoles.Developer))  //DEVELOPER
            {
                MunicipalityList = await AuthProvider.GetMunicipalityList();
                Data.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID;
            }

            AuthorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null, null);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void HandleValidSubmit()
        {
            if (Data != null && validEmail && (IsValidPassword || (string.IsNullOrEmpty(Data.Password) && (string.IsNullOrEmpty(Data.ConfirmPassword)))))
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();

                if (ID == "New")
                {
                    Data.RegistrationMode = "Gemeinde Backend";

                    if(Data.DA_Email != null)
                    {
                        Data.Email = Data.DA_Email;
                    }

                    Data.Username = Data.Email;
                    Data.EmailConfirmed = true;
                    Data.ForceEmailVerification = false;

                    AUTH_Users_Anagrafic anagrafic = new AUTH_Users_Anagrafic();

                    anagrafic.ID = Guid.NewGuid();
                    anagrafic.AUTH_Users_ID = Data.ID;
                    anagrafic.FirstName = Data.Firstname;
                    anagrafic.LastName = Data.Lastname;
                    anagrafic.Email = Data.Email;
                    anagrafic.Phone = Data.PhoneNumber;

                    await AccountService.Register(Data, anagrafic);
                    await AuthProvider.SetAnagrafic(anagrafic);
                    await AuthProvider.SetRole(Data.ID, AuthRoles.Employee);    //EMPLOYEE
                }
                else
                {
                    var anagrafic = await AuthProvider.GetAnagraficByUserID(Data.ID);

                    if(anagrafic == null)
                    {
                        anagrafic = new AUTH_Users_Anagrafic();

                        anagrafic.ID = Guid.NewGuid();
                        anagrafic.AUTH_Users_ID = Data.ID;
                        anagrafic.FirstName = Data.Firstname;
                        anagrafic.LastName = Data.Lastname;
                        anagrafic.Email = Data.Email;
                        anagrafic.Phone = Data.PhoneNumber;
                        anagrafic.MobilePhone = Data.PhoneNumber;

                        await AuthProvider.SetAnagrafic(anagrafic);
                    }

                    anagrafic.FirstName = Data.Firstname;
                    anagrafic.LastName = Data.Lastname;
                    anagrafic.Email = Data.Email;
                    anagrafic.MobilePhone = Data.PhoneNumber;

                    await AuthProvider.SetAnagrafic(anagrafic);

                    Data.Username = Data.Email;

                    PasswordHelper pwd = new PasswordHelper();

                    if(Data.Password != null && Data.ConfirmPassword != null)
                    {
                        Data.PasswordHash = pwd.CreateMD5Hash(Data.Password);
                        Data.LastLoginToken = Guid.NewGuid();
                    }

                    await AuthProvider.UpdateUser(Data);
                }

                foreach(var r in UserAuthorities)
                {
                    await AuthProvider.SetUserAuthority(r);                    
                }

                if (ProfilePicture != null && ProfilePicture.Count() > 0)
                {
                    var file = await FileProvider.SetFileInfo(ProfilePicture.FirstOrDefault());

                    if (file != null)
                    {
                        Data.Logo_FILE_FileInfo_ID = file.ID;
                        await AuthProvider.UpdateUser(Data);
                    }
                }

                NavManager.NavigateTo("/User/Management");
            }
        }
        private void BackToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/User/Management");
        }
        private void ValidateEmail()
        {
            validEmail = AccountService.IsEmailUnique(Data.DA_Email, Data.ID);
            StateHasChanged();
        }
        private void AddAuthority(AUTH_Authority Authority)
        {
            if (Authority != null)
            {
                CurrentUserAuthority = new AUTH_User_Authority();
                CurrentUserAuthority.ID = Guid.NewGuid();
                CurrentUserAuthority.AUTH_Users_ID = Data.ID;
                CurrentUserAuthority.AUTH_Authority_ID = Authority.ID;
                CurrentUserAuthority.EnableNotifications = true;

                if (CurrentUserAuthority != null)
                {
                    var role = UserAuthorities.FirstOrDefault(p => p.ID == CurrentUserAuthority.ID);

                    if (role == null)
                    {
                        var selectedRole = AuthorityList.FirstOrDefault(p => p.ID == CurrentUserAuthority.AUTH_Authority_ID);

                        if (selectedRole != null)
                        {
                            CurrentUserAuthority.AuthorityName = selectedRole.Name;
                        }

                        UserAuthorities.Add(CurrentUserAuthority);
                    }
                    else if (role.Removed)
                    {
                        role.Removed = false;
                    }
                }

                StateHasChanged();
            }
        }
        private async void DeleteAuthority(AUTH_User_Authority target)
        {
            if (target != null)
            {
                target.Removed = true;
                await AuthProvider.RemoveUserAuthority(Data.ID, target.AUTH_Authority_ID.Value);
                UserAuthorities.Remove(target);
                StateHasChanged();
            }
        }
        private async void RemoveImage(Guid File_Info_ID)
        {
            if (Data != null)
            {
                Data.Logo_FILE_FileInfo_ID = null;
                await FileProvider.RemoveFileInfo(File_Info_ID);

                StateHasChanged();
            }
        }
    }
}
