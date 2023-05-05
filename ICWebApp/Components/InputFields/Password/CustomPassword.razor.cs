using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.InputFields.Password
{
    public partial class CustomPassword
    {
        [Parameter] public string Value { get; set; }
        [Parameter] public EventCallback<string> ValueChanged { get; set; }
        [Parameter] public string? PlaceHolder { get; set; }

        private bool HidePassword { get; set; } = true;
        private string Password 
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
                ValueChanged.InvokeAsync(value);
            } 
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
        private async void OnShowPassword()
        {
            HidePassword = false;
            StateHasChanged();
            await Task.Delay(2000);
            HidePassword = true;
            StateHasChanged();
        }
    }
}
