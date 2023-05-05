using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Authorization
{
    public partial class FesrEfreComponent
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        private AUTH_Municipality? _municipality;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var _municipalityID = await SessionWrapper.GetMunicipalityID();

                if (SessionWrapper != null && _municipalityID != null && _municipalityID != Guid.Empty)
                {
                    _municipality = await AuthProvider.GetMunicipality(_municipalityID.Value);
                }
                else
                {
                    _municipality = null;
                }

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
