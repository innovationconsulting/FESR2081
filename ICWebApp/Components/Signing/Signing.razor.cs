using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;

namespace ICWebApp.Components.Signing
{
    public partial class Signing
    {
        [Inject] ISignService SignService { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; } 
        [Inject] IFORMApplicationProvider FormApplicationProvider { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public Guid? File_FileInfoID { get; set; }
        [Parameter] public bool MultiSign { get; set; } = false;
        [Parameter] public List<SignerItem> NeededSigns { get; set; } = null;
        [Parameter] public bool NeedsMunicipalSigning { get; set; } = false;
        [Parameter] public EventCallback OnDocumentSigned { get; set; }
        [Parameter] public EventCallback OnBackToPrevious { get; set; }
        [Parameter] public bool ShowBackButton { get; set; } = false;
        private bool WindowVisible { get; set; } = false;
        private bool SigningWindowVisible { get; set; } = false;
        private bool NotAllComitted { get; set; } = false;
        private string? SigningURL { get; set; }
        private ModalWindowParameters? WindowParameters;
        private DotNetObjectReference<JSRuntimeEventHelper>? Reference;

        protected override void OnInitialized()
        {
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
            base.OnInitialized();
        }
        private async void HandleSubmit()
        {
            if (File_FileInfoID != null)
            {
                BusyIndicatorService.IsBusy = true;
                WindowVisible = false;
                StateHasChanged();

                SigningURL = await SignService.StartMultiSignProcess(File_FileInfoID.Value, NeededSigns, NeedsMunicipalSigning);
                await ShowAgreement(true);


                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private void CloseWindow()
        {
            WindowVisible = false;
            StateHasChanged();
        }
        private async Task<bool> ShowAgreement(bool SkipSigning = false)
        {
            if (!SkipSigning)
            {
                await Sign();
            }

            if (SigningURL != null)
            {
                SigningWindowVisible = true;
                StateHasChanged();

                await Task.Delay(300);

                Reference = DotNetObjectReference.Create(new JSRuntimeEventHelper(DocumentSigned));
                await JSRuntime.InvokeVoidAsync("AdobeSign_OpenWindow", Reference, SigningURL, TextProvider.Get("SIGNING_POPUP_WINDOW_NAME"));
            }

            return true;
        }
        private async Task<bool> Sign()
        {
            if (File_FileInfoID != null)
            {
                BusyIndicatorService.IsBusy = true;
                BusyIndicatorService.Description = TextProvider.Get("SIGNING_PREPARING");
                StateHasChanged();

                if (!MultiSign && (NeededSigns == null || NeededSigns.Count() == 0))
                {
                    SigningURL = await SignService.StartLocalSignProcess(File_FileInfoID.Value, NeedsMunicipalSigning);
                }
                else if (NeededSigns != null && NeededSigns.Count() == 1 && NeededSigns.FirstOrDefault(p => p.SelfSign) != null)
                {
                    SigningURL = await SignService.StartLocalSignProcess(File_FileInfoID.Value, NeedsMunicipalSigning, NeededSigns.FirstOrDefault(p => p.SelfSign));
                }
                else if(string.IsNullOrEmpty(SigningURL))
                {
                    var File = await FileProvider.GetFileInfoAsync(File_FileInfoID.Value);

                    if (File != null && File.AdobeSign_Agreement_ID != null && File.AgreementCreated == true && File.AgreementComitted == false)
                    {
                        var User = SessionWrapper.CurrentUser;

                        if (User != null)
                        {
                            SigningURL = await SignService.GetSigningUrl(File.AdobeSign_Agreement_ID, User.Email);
                        }
                    }
                    else if (File != null && File.AgreementComitted == true)
                    {
                        NotAllComitted = true;
                    }
                    else
                    {
                        if (NeededSigns != null && NeededSigns.Count() > 0)
                        {

                            WindowParameters = new ModalWindowParameters();
                            WindowParameters.Title = TextProvider.Get("SIGNING_PARTICIPANTS_TITLE");
                            WindowParameters.Width = "550px";

                            WindowVisible = true;
                        }
                    }
                }

                BusyIndicatorService.IsBusy = false;
                BusyIndicatorService.Description = "";
                StateHasChanged();
            }

            return true;
        }
        [JSInvokable]
        public async Task DocumentSigned(string val)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (File_FileInfoID != null)
            {
                var dbFile = await FileProvider.GetFileInfoAsync(File_FileInfoID.Value);

                if(dbFile != null)
                {
                    dbFile.AgreementComitted = true;
                    dbFile.SelfSigned = DateTime.Now;

                    if (!MultiSign && NeededSigns != null && NeededSigns.Count() == 0)
                    {
                        dbFile.Signed = true;
                    }
                    else if (NeededSigns != null && NeededSigns.Count() == 1 && NeededSigns.FirstOrDefault(p => p.SelfSign) != null)
                    {
                        dbFile.Signed = true;
                    }

                    await FileProvider.SetFileInfo(dbFile);
                }

                await OnDocumentSigned.InvokeAsync();

                SigningWindowVisible = false;
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
            }
        }
        private ValueTask<string> SetupSignedEvent(Func<string, Task> callback)
        {
            var result = JSRuntime.InvokeAsync<string>("AdobeSign_AddSignedEvent", Reference);

            return result;
        }
        private void BackToPrevious()
        {
            OnBackToPrevious.InvokeAsync();
        }
        private void HideSigningWindow()
        {
            SigningWindowVisible = false;
            StateHasChanged();
        }
    }
}
