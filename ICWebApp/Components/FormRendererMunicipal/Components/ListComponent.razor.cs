using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.FormRendererMunicipal.Components
{
    public partial class ListComponent
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Parameter] public FORM_Application_Field_Data Field { get; set; }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
   
    }
}
