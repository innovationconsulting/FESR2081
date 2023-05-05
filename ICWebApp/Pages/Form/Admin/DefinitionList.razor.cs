using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Form.Admin
{
    public partial class DefinitionList
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set;}
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Parameter] public string PageRefreshParam { get; set; }

        private List<FORM_Definition> Data = new List<FORM_Definition>();
        private bool IsDataBusy { get; set; } = true;

        protected override async Task OnParametersSetAsync()
        {
            if (NavManager.Uri.Contains("Application"))
            {
                SessionWrapper.PageTitle = TextProvider.Get("FORM_DEFINITION_BACKEND_FORM");
                SessionWrapper.PageSubTitle = TextProvider.Get("FORM_DEFINITION_PAGE_TITLE");
                CrumbService.ClearBreadCrumb();
                CrumbService.AddBreadCrumb("/Form/Definition", "FORM_DEFINITION_PAGE_TITLE", null, null, true);
                StateHasChanged();
            }
            else if (NavManager.Uri.Contains("Mantainance"))
            {
                SessionWrapper.PageTitle = TextProvider.Get("FORM_DEFINITION_BACKEND_FORM");
                SessionWrapper.PageSubTitle = TextProvider.Get("FORM_DEFINITION_PAGE_MANTAINANCE_TITLE");
                CrumbService.ClearBreadCrumb();
                CrumbService.AddBreadCrumb("/Form/Definition", "FORM_DEFINITION_PAGE_MANTAINANCE_TITLE", null, null, true);
                StateHasChanged();
            }

            await GetData();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnParametersSetAsync();
        }

        private async Task<bool> GetData()
        {
            if (SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                IsDataBusy = true;
                StateHasChanged();

                if (NavManager.Uri.Contains("Application"))
                {
                    Data = await FormDefinitionProvider.GetDefinitionListByCategory(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"));
                    Data = Data.Union(await FormDefinitionProvider.GetDefinitionListByCategory(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"), true)).ToList();
                }
                else if (NavManager.Uri.Contains("Mantainance"))
                {
                    Data = await FormDefinitionProvider.GetDefinitionListByCategory(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, FORMCategories.Maintenance);
                    Data = Data.Union(await FormDefinitionProvider.GetDefinitionListByCategory(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, FORMCategories.Maintenance, true)).ToList();
                }

                IsDataBusy = false;
                StateHasChanged();
            }

            return true;
        }
        private void Add()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged(); 

            if (NavManager.Uri.Contains("Application"))
            {
                NavManager.NavigateTo("/Form/Definition/Add/Application/New");
            }
            else
            {
                NavManager.NavigateTo("/Form/Definition/Add/Mantainance/New");
            }
        }
        private void Edit(FORM_Definition Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Form/Definition/Add/" + Item.ID);
        }
        private async void Delete(FORM_Definition Item)
        {
            if(Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinition(Item.ID);
                await GetData();
                StateHasChanged();
            }
        }
        private async void MoveUp(FORM_Definition opt)
        {
            if (Data != null && Data.Count() > 0 && opt.ID != null)
            {
                await ReOrder();

                var startField = await FormDefinitionProvider.GetDefinition(opt.ID);

                if (startField != null)
                {

                    var newPos = Data.FirstOrDefault(p => p.SortOrder == startField.SortOrder - 1);

                    if (newPos != null && newPos.ID != null)
                    {
                        var newPosDB = await FormDefinitionProvider.GetDefinition(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder - 1;
                            newPosDB.SortOrder = newPosDB.SortOrder + 1;
                            await FormDefinitionProvider.SetDefinition(startField);
                            await FormDefinitionProvider.SetDefinition(newPosDB);
                        }
                    }
                }
            }

            if (Data != null)
            {
                await GetData();
            }

            StateHasChanged();
        }
        private async void MoveDown(FORM_Definition opt)
        {
            if (Data != null && Data.Count() > 0 && opt.ID != null)
            {
                await ReOrder();

                var startField = await FormDefinitionProvider.GetDefinition(opt.ID);

                if (startField != null)
                {

                    var newPos = Data.FirstOrDefault(p => p.SortOrder == startField.SortOrder + 1);

                    if (newPos != null && newPos.ID != null)
                    {
                        var newPosDB = await FormDefinitionProvider.GetDefinition(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder + 1;
                            newPosDB.SortOrder = newPosDB.SortOrder - 1;
                            await FormDefinitionProvider.SetDefinition(startField);
                            await FormDefinitionProvider.SetDefinition(newPosDB);
                        }
                    }
                }
            }

            if (Data != null)
            {
                await GetData();
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrder()
        {
            int count = 1;

            foreach (var d in Data.OrderBy(p => p.SortOrder))
            {
                if (d != null && d.ID != null)
                {
                    var field = await FormDefinitionProvider.GetDefinition(d.ID);

                    if (field != null)
                    {
                        field.SortOrder = count;

                        await FormDefinitionProvider.SetDefinition(field);
                    }
                }
                count++;
            }

            return true;
        }
    }
}
