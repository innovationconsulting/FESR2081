using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Components.Collapsible
{
    public partial class Collapsible
    {
        [Inject] IJSRuntime JSRuntime { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string TitleCSS { get; set; }
        [Parameter] public bool CollapsedAtStartup { get; set; } = false;
        [Parameter] public bool IsAnchor { get; set; }
        [Parameter] public int AnchorCount { get; set; } = 5;

        IJSObjectReference? _module;
        private Guid ID = Guid.NewGuid();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_module == null)
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/Collapsable/Helper.js");
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        public async void ToggleCollapse()
        {
            if (_module != null)
            {
                await _module.InvokeVoidAsync("ToggleCollapse", ID);
                StateHasChanged();
            }
        }
    }
}
