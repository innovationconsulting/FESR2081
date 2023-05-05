using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace ICWebApp.Components.InfoPage;

public partial class InfoPageComponent
{

    [Inject] public IINFO_PAGEProvider InfoPageProvider { get; set; }
    [Inject] private ISessionWrapper SessionWrapper { get; set; }

    [Inject] private IAUTHProvider AuthProvider { get; set; }
    [Inject] private NavigationManager NavManager { get; set; }
    [Parameter] public string ParentID { get; set; }
    [Parameter] public string MenuID { get; set; }

    [Parameter] public string PageUrl { get; set; }

    private Guid _menuID;
    private Guid? _parentID;
    private Guid? _municipalityID;
    private List<INFO_Page> pagelist = new();
    private List<AUTH_UserRoles> UserRights = new List<AUTH_UserRoles>();
    private bool isAdmin = false;


    protected override async Task OnInitializedAsync()
    {
        _menuID = Guid.Parse(MenuID);
        _parentID = null;
        if (ParentID != null && ParentID != "") _parentID = Guid.Parse(ParentID);
        _municipalityID = SessionWrapper.AUTH_Municipality_ID;

        await LoadPages(_menuID, _parentID);
        

        isAdmin = false;

        if (AuthProvider.HasUserRole(AuthRoles.Developer)) //DEVELOPER
        {
            isAdmin = true;
        }
        
        await base.OnInitializedAsync();
        StateHasChanged();
    }


    private async Task<bool> LoadPages(Guid menuID, Guid? parentID)
    {
        pagelist = await InfoPageProvider.GetPages(menuID, parentID, null ,PageUrl);
        pagelist = pagelist.OrderByDescending(o => o.SortOrder).ToList();

        if (pagelist.Count() == 0)
        {
            var p1 = new INFO_Page();
            p1.MenuID = menuID;
            p1.SubPageUrl = PageUrl;
            p1.Language = "de-DE";
            p1.LanguageID = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");
            p1.MunicipalityID = SessionWrapper.AUTH_Municipality_ID;
            p1.UpdateDate = DateTime.Now;
            await InfoPageProvider.UpdatePage(p1);

            var p2 = new INFO_Page();
            p2.MenuID = menuID;
            p2.SubPageUrl = PageUrl;
            p2.Language = "it-IT";
            p2.LanguageID = Guid.Parse("e450421a-baff-493e-a390-71b49be6485f");
            p2.MunicipalityID = SessionWrapper.AUTH_Municipality_ID;
            p2.UpdateDate = DateTime.Now;
            await InfoPageProvider.UpdatePage(p2);

            pagelist = await InfoPageProvider.GetPages(menuID, parentID, null, PageUrl);
            pagelist = pagelist.OrderByDescending(o => o.SortOrder).ToList();
        }

        return true;
    }

    private async void EditText(INFO_Page item)
    {
        StateHasChanged();
        NavManager.NavigateTo("/Admin/InfoPage/Edit/"+ _menuID.ToString() + "/"+item.SubPageUrl.Replace("/","-") );
    }
}