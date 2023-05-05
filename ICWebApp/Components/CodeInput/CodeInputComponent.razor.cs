using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICWebApp.Components.CodeInput
{
    public partial class CodeInputComponent
    {
        [Inject] IJSRuntime JSRuntime { get; set; }
        public async Task<string?> GetValue()
        {
            var result = await JSRuntime.InvokeAsync<string>("CodeInput_GetValue");

            if(result != null)
            {
                return result;
            }

            return null;
        }
    }
}
