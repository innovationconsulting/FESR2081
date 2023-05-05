using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Canteen.Admin
{
    public partial class CanteenDetailPage

    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string AktiveIndex { get; set; }
        private int AktiveTabIndex { get; set; } = 0;
        private List<CANTEEN_Property> Properties { get; set; }
        private List<CANTEEN_Ressources> Ressources { get; set; }


        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_BACKEND_CANTEEN_DETAILPAGE");

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb(NavManager.Uri, "MAINMENU_BACKEND_CANTEEN_DETAILPAGE", null, null, true);

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Properties = await CanteenProvider.GetPropertyList(SessionWrapper.AUTH_Municipality_ID.Value);
                Ressources = await CanteenProvider.GetRessourceList(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void AddProperty()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            NavManager.NavigateTo("/Canteen/Property/Add/New/" + AktiveTabIndex);
        }
        private void EditProperty(CANTEEN_Property Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            NavManager.NavigateTo("/Canteen/Property/Add/" + Item.ID + "/" + AktiveTabIndex);
        }
        private async void DeleteProperty(CANTEEN_Property Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await CanteenProvider.RemoveProperty(Item.ID);
                Properties = await CanteenProvider.GetPropertyList(SessionWrapper.AUTH_Municipality_ID.Value);

                StateHasChanged();
            }
        }
        private async void MoveUpProperty(CANTEEN_Property opt)
        {
            if (Properties != null && Properties.Count() > 0)
            {
                await ReOrderProperties();
                var newPos = Properties.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await CanteenProvider.SetProperty(opt);
                    await CanteenProvider.SetProperty(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownProperty(CANTEEN_Property opt)
        {
            if (Properties != null && Properties.Count() > 0)
            {
                await ReOrderProperties();
                var newPos = Properties.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await CanteenProvider.SetProperty(opt);
                    await CanteenProvider.SetProperty(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderProperties()
        {
            int count = 1;

            foreach (var d in Properties.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await CanteenProvider.SetProperty(d);

                count++;
            }

            return true;
        }
        private void AddRessource()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            NavManager.NavigateTo("/Canteen/Ressource/Add/New/" + AktiveTabIndex);
        }
        private void EditRessource(CANTEEN_Ressources Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            NavManager.NavigateTo("/Canteen/Ressource/Add/" + Item.ID + "/" + AktiveTabIndex);
        }
        private async void DeleteRessource(CANTEEN_Ressources Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await CanteenProvider.RemoveRessource(Item.ID);
                Ressources = await CanteenProvider.GetRessourceList(SessionWrapper.AUTH_Municipality_ID.Value);

                StateHasChanged();
            }
        }
        private async void MoveUpRessources(CANTEEN_Ressources opt)
        {
            if (Ressources != null && Ressources.Count() > 0)
            {
                await ReOrderRessources();
                var newPos = Ressources.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await CanteenProvider.SetRessource(opt);
                    await CanteenProvider.SetRessource(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownRessources(CANTEEN_Ressources opt)
        {
            if (Ressources != null && Ressources.Count() > 0)
            {
                await ReOrderRessources();
                var newPos = Ressources.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await CanteenProvider.SetRessource(opt);
                    await CanteenProvider.SetRessource(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderRessources()
        {
            int count = 1;

            foreach (var d in Ressources.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await CanteenProvider.SetRessource(d);

                count++;
            }

            return true;
        }
    }
}
