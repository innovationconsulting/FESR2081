using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Anchor
{
    public partial class AnchorList
    {
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        private List<AnchorItem> Anchors = new List<AnchorItem>();
        protected override void OnInitialized()
        {
            AnchorService.OnAnchorChanged += AnchorService_Changed;
            SessionWrapper.OnPageTitleChanged += SessionWrapper_OnPageTitleChanged;

            Anchors = AnchorService.GetAnchors();
            StateHasChanged();

            base.OnInitialized();
        }
        private void SessionWrapper_OnPageTitleChanged()
        {
            StateHasChanged();
        }

        private void AnchorService_Changed()
        {
            Anchors = AnchorService.GetAnchors();

            StateHasChanged();
        }
    }
}
