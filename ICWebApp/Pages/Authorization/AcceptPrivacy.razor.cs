using Microsoft.AspNetCore.Components;
using System.Net.NetworkInformation;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;
using ICWebApp.Domain.DBModels;

namespace ICWebApp.Pages.Authorization
{
    public partial class AcceptPrivacy
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IUnitOfWork UnitOfWork { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Parameter] public string UserId { get; set; }

        private PRIV_Privacy? Privacy { get; set; }
        private bool _displayPrivacyNotAcceptedError = false;
        private bool _privacyAccepted = false;
        private bool _privacyMuniciplityAccepted = false;
        private bool _privacyCommunicationsAccepted = false;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.GetOrCreate("PRIVACY_POLICY_PAGE_TITLE");

            Privacy = await PrivProvider.GetPrivacy(SessionWrapper.AUTH_Municipality_ID.Value);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }

        private async Task Submit()
        {
            if (_privacyAccepted && _privacyMuniciplityAccepted && _privacyCommunicationsAccepted)
            {
                var dbUser = await AuthProvider.GetUser(Guid.Parse(UserId));
                if (dbUser != null)
                {
                    dbUser.PrivacyAccepted = DateTime.Now;
                    await UnitOfWork.Repository<AUTH_Users>().UpdateAsync(dbUser);
                    NavManager.NavigateTo("/");
                }
            }
            else
            {
                _displayPrivacyNotAcceptedError = true;
            }
        }
    }
}
