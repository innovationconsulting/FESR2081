using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.User
{
    public partial class UserCard
    {
        [Inject] IAUTHProvider AUTHProvider { get; set; }
        [Inject] IFILEProvider FILEProvider { get; set; }

        [Parameter] public AUTH_Users User { get; set; }
        [Parameter] public bool SmallStyle { get; set; } = false;

        private AUTH_Users_Anagrafic? UserAnagrafic;
        private FILE_FileStorage UserLogo { get; set; }

        protected override void OnParametersSet()
        {
            if (User != null)
            {
                if (User.Logo_FILE_FileInfo_ID != null) 
                {
                    UserLogo = FILEProvider.GetFileStorage(User.Logo_FILE_FileInfo_ID.Value);
                }
            }

            StateHasChanged();
            base.OnParametersSet();
        }
    }
}
