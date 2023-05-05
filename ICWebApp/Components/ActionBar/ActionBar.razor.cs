using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.ActionBar
{
    public partial class ActionBar
    {
        [Inject] IActionBarService ActionBarService { get; set; }
        [Inject] ITEXTProvider TEXTProvider { get; set; }

        protected override void OnInitialized()
        {
            ActionBarService.OnActionBarChanged += ActionBarService_OnActionBarChanged;

            base.OnInitialized();
        }

        private void ActionBarService_OnActionBarChanged()
        {
            StateHasChanged();
        }
    }
}
