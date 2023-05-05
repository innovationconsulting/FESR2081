using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Canteen.Admin.SubPages
{
    public partial class SchoolsAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Parameter] public string ID { get; set; }

        private CANTEEN_School? Data { get; set; }
        private List<V_CANTEEN_SchoolType>? SchoolTypeList { get; set; } = new List<V_CANTEEN_SchoolType>();
        private List<CANTEEN_School_AdditionalPersonal> AdditionalPersonal = new List<CANTEEN_School_AdditionalPersonal>();

        protected override async Task OnInitializedAsync()
        {
            if (ID == "New")
            {
                Data = new CANTEEN_School();
                Data.ID = Guid.NewGuid();
                if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
                {
                    Data.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value;
                }
            }
            else
            {
                Data = await CanteenProvider.GetSchool(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }
            }

            SchoolTypeList = await CanteenProvider.GetSchoolTypeList();

            AdditionalPersonal = await CanteenProvider.GetAdditionalPersonalList(Data.ID);

            if (AdditionalPersonal.FirstOrDefault(p => p.CANTEEN_School_ID == Data.ID) == null)
            {
                AdditionalPersonal.Add(new CANTEEN_School_AdditionalPersonal()
                {
                    ID = Guid.NewGuid(),
                    CANTEEN_School_ID = Data.ID,
                    MO = 0,
                    DI = 0,
                    MI = 0,
                    DO = 0,
                    FR = 0,
                    MOPP = 0,
                    DIPP = 0,
                    MIPP = 0,
                    DOPP = 0,
                    FRPP = 0
                });
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo("/Admin/Canteen/SchoolManagement");
        }
        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (Data != null)
            {
                await CanteenProvider.SetSchool(Data);

                foreach (var add in AdditionalPersonal)
                {
                    await CanteenProvider.SetAdditionalPersonal(add);
                }
            }

            NavManager.NavigateTo("/Admin/Canteen/SchoolManagement");
        }
    }
}
