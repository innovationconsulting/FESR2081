using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin.SubPages
{
    public partial class AdditionalFormAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string DefinitionID { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string WizardIndex { get; set; }
        [Parameter] public string ActiveIndex { get; set; }

        private FORM_Definition_Additional_FORM Data { get; set; }
        private List<FORM_Definition> FormList = new List<FORM_Definition>();
        protected override async Task OnInitializedAsync()
        {
            if(DefinitionID == null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                NavManager.NavigateTo("/Form/Definition");
            }

            if (ID == "New")
            {
                Data = new FORM_Definition_Additional_FORM();
                Data.ID = Guid.NewGuid();
                Data.FORM_Definition_ID = Guid.Parse(DefinitionID);

                await FormDefinitionProvider.SetDefinitionAdditionalFORM(Data);
            }
            else
            {
                Data = await FormDefinitionProvider.GetDefinitionAdditionalFORM(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }
            }

            if (SessionWrapper != null && SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                FormList = await FormDefinitionProvider.GetDefinitionList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value);

                FormList = FormList.Where(p => p.Enabled && p.FORM_Definition_Category_ID == FORMCategories.Applications).OrderBy(p => p.SortOrder).ToList();
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void ReturnToPreviousPage()
        {
            if (ID == "New" && Data != null)
            {
                await FormDefinitionProvider.RemoveDefinitionUpload(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await FormDefinitionProvider.SetDefinitionAdditionalFORM(Data);
            NavManager.NavigateTo("/Form/Definition/Add/" + DefinitionID + "/" + WizardIndex + "/" + ActiveIndex);
        }
    }
}
