using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Messaging
{
    public partial class SystemMessageComponent
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Inject] public ISessionWrapper SessionWrapper { get; set; }
        [Parameter] public MSG_SystemMessages Message { get; set; }

        protected override void OnInitialized()
        {
            StateHasChanged();

            base.OnInitialized();
        }
    }
}
