using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Reporting;

namespace ICWebApp.Pages.Organziation.Frontend
{
    public partial class Application_Sign
    {
        [Inject] ID3Helper D3Helper { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Parameter] public string ID { get; set; }

        private ORG_Request? Data { get; set; }
        private FILE_FileInfo? File_FileInfo { get; set; }

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (ID == null)
            {
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();
                return;
            }

            Data = await GetRequest();

            if (Data == null)
            {
                NavManager.NavigateTo("/User/Services");
                StateHasChanged();
                return;
            }

            if (Data.SignetAt != null)
            {
                NavManager.NavigateTo("/Organization/Detail/" + Data.ID);
                StateHasChanged();
                return;
            }

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Services", "ORG_REQUEST_DASHBOARD_TITLE", null);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), "ORG_REQUEST_NEW_TITLE", null, null, true);

            SessionWrapper.PageTitle = TextProvider.Get("ORG_REQUEST_NEW_TITLE");
            Data.CreationDate = DateTime.Now;
            await OrgProvider.SetRequest(Data);

            File_FileInfo = await OrgProvider.CreateFile(Data);

            Data.SubmitAt = DateTime.Now;

            await OrgProvider.SetRequest(Data);

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<ORG_Request?> GetRequest()
        {
            return await OrgProvider.GetRequest(Guid.Parse(ID));
        }
        private async void DocumentSigned()
        {
            //HIER STATUS SETZEN

            if (Data != null)
            {
                Data.ORG_Request_Status_ID = Guid.Parse("d09bfdf6-406b-44b8-9def-d37481b0828a");    //Comitted
                Data.SignetAt = DateTime.Now;

                var lastNumber = OrgProvider.GetLatestProgressivNumber(Data.AUTH_Municipality_ID.Value, DateTime.Now.Year);

                if (Data.ProgressivNumber == null || Data.ProgressivNumber == 0)
                {
                    Data.ProgressivYear = DateTime.Now.Year;

                    Data.ProgressivNumber = lastNumber + 1;
                }                

                await OrgProvider.SetRequest(Data);

                var Parameters = new List<MSG_Message_Parameters>()
                {
                    new MSG_Message_Parameters()
                    {
                        Code = "{FormName}",
                        Message = TextProvider.Get("ORG_REPORT_TITLE")
                    },
                    new MSG_Message_Parameters()
                    {
                        Code="{SubstituteName}",
                        Message = Data.Firstname + " " + Data.Lastname
                    }
                };

                if (Data.AUTH_Users_ID != null && Data.AUTH_Municipality_ID != null)
                {
                    var msg = await MessageService.GetMessage(Data.AUTH_Users_ID.Value, Data.AUTH_Municipality_ID.Value, "SUBSTITUTION_COMITTED_TEXT", "SUBSTITUTION_COMITTED_SHORTTEXT", "SUBSTITUTION_COMITTED_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true, Parameters);

                    if (msg != null)
                    {

                        await MessageService.SendMessage(msg, NavManager.BaseUri + "/User/Services");
                    }

                    //SEND TO AUTHORITY
                    var authorityMsg = await MessageService.GetMessage(Data.AUTH_Users_ID.Value, Data.AUTH_Municipality_ID.Value, "MUN_SUBSTITUTION_COMITTED_TEXT", "MUN_SUBSTITUTION_COMITTED_SHORTTEXT", "MUN_SUBSTITUTION_COMITTED_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true, Parameters);

                    var authorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null, null);

                    var subsititutionAuthority = authorityList.FirstOrDefault(p => p.IsSubstitution == true);

                    if (authorityMsg != null && subsititutionAuthority != null)
                    {
                        await MessageService.SendMessageToAuthority(subsititutionAuthority.ID, authorityMsg, NavManager.BaseUri + "/Organization/Backend/Application/Detail/" + Data.ID);
                    }
                    NavManager.NavigateTo("/Organization/Application/Comitted/" + Data.ID);
                    StateHasChanged();
                }
                //D3! organization
                Task.Run(async () => await D3Helper.ProtocolNewOrganization(Data)).ConfigureAwait(false);
            }
        }
    }
}
