using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;
using System.Security;

namespace ICWebApp.Components.Anchor
{
    public partial class Anchor
    {
        [Inject] IAnchorService AnchorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string SubTitle { get; set; }
        [Parameter] public string? CSS { get; set; } = "form-bold";
        [Parameter] public string ID { get; set; }
        [Parameter] public int Order { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public bool HasParagraph { get; set; } = true;
        [Parameter] public bool IsCard { get; set; } = false;
        [Parameter] public bool AddAnchor { get; set; } = true;
        [Parameter] public EventCallback OnModify { get; set; }
        [Parameter] public string? ModifyTextCode { get; set; }
        [Parameter] public bool EnableModify { get; set; } = true;
        [Parameter] public bool IsSmallTitle { get; set; } = false;
        [Parameter] public bool SectionSpace { get; set; } = true;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                if (AddAnchor && !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(ID))
                {
                    AnchorService.AddAnchor(Title, ID.Replace(" ", "-"), Order);
                }

                StateHasChanged();
            }

            base.OnAfterRender(firstRender);
        }
        private async void OnModifyClicked()
        {
            if (OnModify.HasDelegate)
            {
                await OnModify.InvokeAsync();
            }
        }
    }
}
