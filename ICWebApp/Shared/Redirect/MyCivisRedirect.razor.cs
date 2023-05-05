using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Shared.Redirect
{
    public partial class MyCivisRedirect
    {
        [Parameter] public string ServiceID { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        protected override void OnInitialized()
        {
            if (SessionWrapper.AUTH_Municipality_ID == ComunixSettings.TestMunicipalityID)
            {
                NavManager.NavigateTo("https://sso.civis.bz.it/api/Auth/login?targetUrl=" + Uri.EscapeDataString("https://test.comunix.bz.it/MyCivis/Success"), true);
            }
            else
            {
                NavManager.NavigateTo("https://sso.civis.bz.it/api/Auth/login?targetUrl=" + Uri.EscapeDataString(NavManager.BaseUri + "MyCivis/Success"), true);
            }

            StateHasChanged();
            base.OnInitialized();
        }
    }
}
