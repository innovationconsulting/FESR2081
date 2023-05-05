using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace ICWebApp.Components.FormRendererMunicipalView
{
    public partial class Container
    {
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFormRendererHelper FormRendererHelper { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }

        [Parameter] public FORM_Application Application { get; set; }
        [Parameter] public List<FORM_Application_Field_Data> Fields { get; set; }
        private List<FORM_Definition_Field> DefinitionFields { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await GetData();
            Fields = Fields.OrderBy(e => e.SortOrder).ToList();
            StateHasChanged();
            await base.OnInitializedAsync();
        }
        private async Task<bool> GetData()
        {
            if (Application != null && Application.FORM_Definition_ID != null)
            {
                DefinitionFields = await FormDefinitionProvider.GetDefinitionFieldList(Application.FORM_Definition_ID.Value);
            }
            return true;
        }
    }
}
