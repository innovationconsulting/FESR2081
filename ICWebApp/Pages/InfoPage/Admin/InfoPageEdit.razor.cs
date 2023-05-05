using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components.Editor;
using Telerik.Blazor.Components;
using DocumentFormat.OpenXml.Office2010.Excel;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Helper;
using ICWebApp.Application.Settings;

namespace ICWebApp.Pages.InfoPage.Admin
{
    public partial class InfoPageEdit
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IINFO_PAGEProvider InfoPageProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }

        [Inject] private IAUTHProvider AuthProvider { get; set; }

        [CascadingParameter] public DialogFactory Dialogs { get; set; }

        private bool IsDataBusy { get; set; } = true;
        private Guid? CurrentLanguage { get; set; }
        private bool IsTabBusy { get; set; } = true;

        private bool Italian
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = Guid.Parse("e450421a-baff-493e-a390-71b49be6485f");
                    StateHasChanged();
                }
            }
        }

        private bool isAdmin = false;
        private bool German
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"))
                {
                   
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");
                    StateHasChanged();
                }
            }
        }
        private Guid _menuID = Guid.Empty;


        private List<IEditorTool> Tools { get; set; } =
            new List<IEditorTool>()
            {
                new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
                new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
                new UnorderedList(),
                new EditorButtonGroup(new CreateLink(), new Unlink()),
                new InsertTable(),
                new DeleteTable(),
                new Format(),
                new EditorButtonGroup(new AddRowBefore(), new AddRowAfter(), new DeleteRow(), new MergeCells(), new SplitCell())
            };
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font" };
        private int CurrentTab { get; set; } = 0;

        private  List<INFO_Page> Data = new List<INFO_Page>();


        [Parameter] public string MenuID { get; set; }
        [Parameter] public string PageUrl { get; set; }


        protected override async Task OnInitializedAsync()
        {
            //_menuID = Guid.Parse(MenuID);
            _menuID = Guid.Parse("332ef969-1401-4650-bea2-c6af6e44ab71");
            CurrentLanguage = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");
            await GetData();

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            await base.OnInitializedAsync();
        }

        private async Task<bool> GetData()
        {

            var UserRights = AuthProvider.GetUserRoles();
            if (UserRights.Select(p => p.AUTH_RolesID).Contains(AuthRoles.Administrator))
            {
                isAdmin = true;
            }

            _menuID = Guid.Parse(MenuID);

            isAdmin = true;

            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null && isAdmin)
            {
               
                Data = await InfoPageProvider.GetInfoPagesList(_menuID,  null);

                Data = Data.Where(a => a.SubPageUrl == PageUrl.Replace("-","/")).ToList();
            }

            return true;
        }

        private async void ReturnToPreviousPage()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            NavManager.NavigateTo(PageUrl.Replace("-", "/"));
        }

        private async void SaveForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            foreach (var info in Data)
            {
                await InfoPageProvider.UpdatePage(info);
            }

            NavManager.NavigateTo(PageUrl.Replace("-", "/"));
        }

    }
}