using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Global
{
    public partial class DialogWindow : IDisposable
    {
        [Inject] IDialogService? DialogService { get; set; }

        protected override void OnInitialized()
        {
            if (DialogService != null)
            {
                DialogService.OnStateChange += DialogService_StateChanged;
            }

            base.OnInitialized();
        }

        private void DialogService_StateChanged(bool _showDialogWindow)
        {
            StateHasChanged();
        }
        public void Dispose()
        {
            if (DialogService != null)
            {
                DialogService.OnStateChange -= DialogService_StateChanged;
            }
        }
        public void ConfirmDialog()
        {
            if (DialogService != null)
            {
                DialogService.ConfirmDialog(true);
            }
        }
        public void CancelDialog()
        {
            if (DialogService != null)
            {
                DialogService.ConfirmDialog(false);
            }
        }
    }
}
