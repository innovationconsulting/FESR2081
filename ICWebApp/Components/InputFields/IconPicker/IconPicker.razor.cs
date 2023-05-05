using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Components.InputFields.IconPicker
{
    public partial class IconPicker
    {
        [Inject] IMETAProvider MetaProvider { get; set; }
        [Inject] private IJSRuntime _jsRuntime { get; set; }
        [Parameter] public string Value { get; set; }
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        private List<META_Icons> MetaList = new List<META_Icons>();

        private string _menuDisplayCss = "display: none;";
        protected override async Task OnInitializedAsync()
        {
            MetaList = await MetaProvider.GetIconList();

            StateHasChanged();
            await base.OnInitializedAsync();
        }
        protected override void OnParametersSet()
        {
            StateHasChanged();
            base.OnParametersSet();
        }

        private async void OnIconChanged(string Value)
        {
            await ValueChanged.InvokeAsync(Value);
            _menuDisplayCss = "display: none;";
            StateHasChanged();
        }

        private void ToggleMenu()
        {
            _menuDisplayCss = _menuDisplayCss == "display: none;" ? "display: flex;" : "display: none;";
            StateHasChanged();
        }
    }
}
