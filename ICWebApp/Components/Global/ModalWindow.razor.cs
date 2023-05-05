using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using static ICWebApp.Domain.Models.ModalWindowParameters;

namespace ICWebApp.Components.Global
{
    public partial class ModalWindow
    {
        [Inject] public IEnviromentService EnviromentService { get; set; }
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public RenderFragment? Content { get; set; }
        [Parameter] public ModalWindowParameters? Parameters { get; set; }
        [Parameter] public EventCallback OnWindowClosed { get; set; }
        private WindowState WindowStateParameter
        {
            get
            {
                if (EnviromentService.IsMobile)
                {
                    return WindowState.Maximized;
                }

                if (Parameters != null && Parameters.StartupState == StartupStateMode.Minimized)
                {
                    return WindowState.Minimized;
                }
                else if(Parameters != null && Parameters.StartupState == StartupStateMode.Maximized)
                {
                    return WindowState.Maximized;
                }

                return WindowState.Default;
            } 
        }

        protected override void OnInitialized()
        {
            EnviromentService.OnIsMobileChanged += EnviromentService_OnIsMobileChanged;

            StateHasChanged();
            base.OnInitialized();
        }
        private void EnviromentService_OnIsMobileChanged()
        {
            StateHasChanged();
        }
        private void OnHandleClose()
        {
            IsVisible = false;
            StateHasChanged();
            OnWindowClosed.InvokeAsync();
        }
    }
}
