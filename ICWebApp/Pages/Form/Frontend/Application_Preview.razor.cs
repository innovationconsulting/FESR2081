using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using ICWebApp.Application.Interface.Sessionless;
using Stripe;
using System.Text.Json;
using Telerik.Blazor.Components.Stepper;
using ICWebApp.Application.Provider;

namespace ICWebApp.Pages.Form.Frontend
{
    public partial class Application_Preview
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] IFORM_ReportRendererHelper FormRendererHelper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] ILANGProvider LANGProvider { get; set; }
        [Inject] IFILEProvider FILEProvider { get; set; }
        [Inject] IFormApplicationService FormApplicationService { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string Payed { get; set; }
        private FORM_Application? Data { get; set; }
        private FORM_Definition? Definition { get; set; }
        private IDictionary<string, object> ReportParameters { get; set; }
        private string ReportName { get; set; }
        private Guid? File_FileInfoID;
        private List<SignerItem> Signings { get; set; }
        private FILE_FileInfo? File_FileInfo { get; set; }
        private List<FORM_Application_Transactions>? FORM_Application_Transactions { get; set; }

        private string ReturnUrl = "";
        private IFormApplicationService.Step NextStep = IFormApplicationService.Step.ShowPreview;
        private bool ShowPaymentPage = false;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (ID == null)
            {
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            Data = await GetApplication();
            Definition = await GetDefinition();

            if (Data == null || Definition == null)
            {
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            if (Data.SubmitAt != null)
            {
                NavManager.NavigateTo("/Form/Application/Comitted/" + ID);
                StateHasChanged();
                return;
            }

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/User/Services", "MAINMENU_MY_APPLICATIONS", null);
            CrumbService.AddBreadCrumb("/Form/Detail/" + Data.FORM_Definition_ID, "FRONTEND_AUTHORITY_FORM", null);
            CrumbService.AddBreadCrumb("/Form/Application/" + Data.FORM_Definition_ID + "/" + Data.ID, null, null, Definition.FORM_Name, true);
            CrumbService.AddBreadCrumb(NavManager.ToBaseRelativePath(NavManager.Uri), "FRONTEND_APPLICATION_PREVIEW", null);

            SessionWrapper.PageTitle = Definition.FORM_Name;

            if (Data != null)
            {
                Data = await FormApplicationService.CheckApplication(Data);

                NextStep = await FormApplicationService.NextStep(Data);

                if(NextStep == IFormApplicationService.Step.ToPay)
                {
                    if(Data.PreviewSeenAt == null)
                    {
                        ShowPaymentPage = false;
                    }
                    else
                    {
                        ShowPaymentPage = true;
                    }

                    FORM_Application_Transactions = await FormApplicationProvider.GetApplicationTransactionList(Data.ID);
                }
                else if(NextStep == IFormApplicationService.Step.ToSign)
                {
                    SendToSign();
                }
                else if(NextStep == IFormApplicationService.Step.ToCommit)
                {
                    Comitted();
                }
            }

            if (Definition.HasTemplate != true)
            {
                ReportName = await FormRendererHelper.ExecuteReport(Definition.ID, Data.ID);
                SetReportParameters();

                File_FileInfo = await FormApplicationProvider.GetOrCreateFileInfo(Data.ID, ReportName);
            }
            else
            {
                if (Data != null && Data.FILE_Fileinfo_ID != null)
                {
                    File_FileInfo = await FILEProvider.GetFileInfoAsync(Data.FILE_Fileinfo_ID.Value);

                    if (Definition != null && Definition.FORM_Definition_Template_ID != null && Data.TemplateJsonData != null)
                    {
                        var signings = await FormDefinitionProvider.GetDefinitionTemplateSignfields(Definition.FORM_Definition_Template_ID.Value);

                        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(Data.TemplateJsonData);

                        if (data != null)
                        {
                            if (Signings == null)
                                Signings = new List<SignerItem>();

                            foreach (var sign in signings)
                            {
                                string? Name = null;
                                string? Email = null;

                                if (!string.IsNullOrEmpty(sign.FirstnameFieldName))
                                {
                                    var item = data.FirstOrDefault(p => p.Key == sign.FirstnameFieldName);

                                    Name = item.Value;
                                }
                                if (!string.IsNullOrEmpty(sign.LastnameFieldName))
                                {
                                    var item = data.FirstOrDefault(p => p.Key == sign.LastnameFieldName);

                                    Name += " " + item.Value;
                                }
                                if (!string.IsNullOrEmpty(sign.EmailFieldName))
                                {
                                    var item = data.FirstOrDefault(p => p.Key == sign.EmailFieldName);

                                    Email += " " + item.Value;
                                }

                                if (!string.IsNullOrEmpty(Name))
                                {
                                    Signings.Add(new SignerItem()
                                    {
                                        Name = Name,
                                        Mail = Email,
                                        XPosition = sign.XPosition,
                                        YPosition = sign.YPosition,
                                        Width = sign.Width,
                                        Height = sign.Height,
                                        PageNumber = sign.PageNumber,
                                        SelfSign = sign.SelfSign
                                    });
                                }
                            }
                        }
                    }
                }
            }

            if (File_FileInfo != null && Data != null && Data.FILE_Fileinfo_ID == null)
            {
                Data.FILE_Fileinfo_ID = File_FileInfo.ID;
                File_FileInfoID = File_FileInfo.ID;

                await FormApplicationProvider.SetApplication(Data);
            }
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<FORM_Application?> GetApplication()
        {
            if (ID != null)
            {
                var data = await FormApplicationProvider.GetApplication(Guid.Parse(ID));

                return data;
            }

            return null;
        }
        private async Task<FORM_Definition?> GetDefinition()
        {
            if (Data != null && Data.FORM_Definition_ID != null)
            {
                var def = await FormDefinitionProvider.GetDefinition(Data.FORM_Definition_ID.Value);
                var definedSignings = await FormDefinitionProvider.GetDefinitionSigningList(Data.FORM_Definition_ID.Value);

                if (definedSignings != null && definedSignings.Count > 0 && def != null && def.HasMultiSigning == true)
                {
                    Signings = new List<SignerItem>();

                    foreach (var s in definedSignings.OrderBy(p => p.SortOrder))
                    {
                        Signings.Add(new SignerItem() { Description = s.Description });
                    }
                }

                var dynamicSignings = await FormApplicationProvider.GetSigningFields(Data.ID);

                if(dynamicSignings != null && dynamicSignings.Count() > 0)
                {
                    if(Signings == null)
                        Signings = new List<SignerItem>();

                    Signings.AddRange(dynamicSignings);
                }

                if (Data != null && Data.FILE_Fileinfo_ID != null)
                {
                    File_FileInfo = await FILEProvider.GetFileInfoAsync(Data.FILE_Fileinfo_ID.Value);
                }

                var data = await FormDefinitionProvider.GetDefinition(Data.FORM_Definition_ID.Value);
                return data;
            }

            return null;
        }
        private void SetReportParameters()
        {
            if (Data != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                ReportParameters = new Dictionary<string, object>();

                ReportParameters.Add("LanguageID", LANGProvider.GetCurrentLanguageID().ToString());
                ReportParameters.Add("MunicipalityID", SessionWrapper.AUTH_Municipality_ID.Value.ToString());
                ReportParameters.Add("ApplicationID", Data.ID.ToString());
            }
        }
        private async void SendToSign()
        {
            if (Definition != null && Data != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                FILE_FileInfo? fi = null;

                if (Definition.HasTemplate != true)
                {
                    if (string.IsNullOrEmpty(ReportName))
                    {
                        ReportName = await FormRendererHelper.ExecuteReport(Definition.ID, Data.ID);
                        SetReportParameters();
                    }

                    fi = await FormApplicationProvider.GetOrCreateFileInfo(Data.ID, ReportName);
                }
                else
                {
                    fi = File_FileInfo;
                }

                if (fi != null)
                {
                    if(Definition.HasSigning == false && (Signings == null || Signings.Count() == 0))
                    {
                        Data.SignedAt = DateTime.Now;
                        Comitted();
                        return;

                    }

                    File_FileInfoID = fi.ID;
                    StateHasChanged();
                }
            }
        }
        private void BackToPrevious()
        {
            if (Definition != null && Data != null)
            {
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();
                NavManager.NavigateTo("/Form/Application/" + Definition.ID + "/" + Data.ID);
            }
        }
        private void BackToPreviousPayment()
        {
            if (Definition != null && Data != null)
            {
                ShowPaymentPage = false;
                StateHasChanged();
            }
        }
        private void GoToPayment()
        {
            ShowPaymentPage = true;

            ReturnUrl = "/Form/Application/Preview/" + ID;
        }
        private async Task<bool> PaymentCompleted()
        {
            if (Data != null)
            {
                Data = await FormApplicationService.CheckApplication(Data);

                NextStep = await FormApplicationService.NextStep(Data);

                if (NextStep == IFormApplicationService.Step.ToSign)
                {
                    SendToSign();
                }
                else if (NextStep == IFormApplicationService.Step.ToCommit)
                {
                    Comitted();
                }

                StateHasChanged();
            }

            return true;
        }
        private async Task<bool> PaymentStarted()
        {
            if (Data != null)
            {
                Data.PaymentReminderSentAt = null;
                Data.PaymentStarted = DateTime.Now;
                await FormApplicationProvider.SetApplication(Data);
            }

            return true; 
        }
        private async void DocumentSigned()
        {
            if (Data != null)
            {
                Data = await FormApplicationService.CheckApplication(Data);

                NextStep = await FormApplicationService.NextStep(Data);

                if (NextStep == IFormApplicationService.Step.InSigningMultiSign)
                {
                    StateHasChanged();
                }
                else if(NextStep == IFormApplicationService.Step.InSigning)
                {
                    Comitted(true);
                }
                else if (NextStep == IFormApplicationService.Step.ToCommit)
                {
                    Comitted();
                }

                StateHasChanged();
            }
        }
        private async void Comitted(bool signatureProcessing = false)
        {
            if (Data != null)
            {
                Data.PreviewSeenAt = DateTime.Now;

                if(signatureProcessing) {
                    Data.FORM_Application_Status_ID = FORMStatus.SignatureProcessing;
                }
                
                Data = await FormApplicationService.CheckApplication(Data);

                NavManager.NavigateTo("/Form/Application/Comitted/" + Data.ID);
                BusyIndicatorService.IsBusy = true;
                StateHasChanged();                
            }
        }
        private void BackToData()
        {
            NavManager.NavigateTo("/User/Services");
            StateHasChanged();
        }
    }
}
