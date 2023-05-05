using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Text.Json;
using System.Text.Json.Serialization;
using Syncfusion.Blazor.PdfViewerServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Syncfusion.Blazor.PdfViewer;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using System.Configuration;
using ICWebApp.Application.Settings;

namespace ICWebApp.Components.FormTemplateEditor
{
    public partial class Editor
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Parameter] public FORM_Definition_Template? Template { get; set; }
        [Parameter] public string Data { get; set; }

        public Dictionary<string, string>? _pdfData;
        private bool DataSmallLoaded = false;
        private bool DataLargeLoaded = false;
        private bool WindowVisible = false;
        private byte[] DocumentData;
        private SfPdfViewerServer? Viewer;
        private SfPdfViewerServer? ViewerLarge; 
        public PdfViewerToolbarSettings ToolbarSettings = new PdfViewerToolbarSettings()
        {
           ToolbarItems = new List<ToolbarItem>()
           {
                ToolbarItem.PageNavigationTool,
                ToolbarItem.MagnificationTool,
                ToolbarItem.SearchOption
            }
        };

        protected override void OnParametersSet()
        {
            if (Template != null && !string.IsNullOrEmpty(Template.FilePath))
            {
                DocumentData = System.IO.File.ReadAllBytes(Template.FilePath);

                DataSmallLoaded = false;
                DataLargeLoaded = false;

                StateHasChanged();
            }

            base.OnParametersSet();
        }
        private async Task OnPdfViewerCreated(object args)
        {
            if (Viewer != null)
            {
                await Viewer.LoadAsync(DocumentData);

                StateHasChanged();
            }
        }
        private async Task OnPdfSmallLoaded(LoadEventArgs args)
        {
            if (Viewer != null && !DataSmallLoaded)
            {
                await FillData();

                _pdfData = JsonSerializer.Deserialize<Dictionary<string, string>>(Data);

                if (_pdfData != null)
                {
                    await Viewer.ImportFormFieldsAsync(_pdfData);
                }

                DataSmallLoaded = true;
                StateHasChanged();
            }
        }
        public async Task<List<string>> Validate()
        {
            var result = await Viewer.ExportFormFieldsAsObjectAsync();

            var json = await SaveAsJson();

            List<string> errorMessages = new List<string>();

            foreach(var item in result)
            {
                if (item.Key.Contains("#P#") && string.IsNullOrEmpty(item.Value))
                {
                    errorMessages.Add(TextProvider.Get("FORM_TEMPLATE_REQUIRED_FIELD_ERROR").Replace("{0}", item.Key.Replace("#P#", "")));
                }
            }

            return errorMessages;
        }
        public async Task<byte[]> SaveAsPDF()
        {
            var data = await Viewer.GetDocumentAsync();

            PdfLoadedDocument loadedDocument = new PdfLoadedDocument(data);
            
            loadedDocument.Form.ReadOnly = true;

            var ms = new MemoryStream();

            loadedDocument.Save(ms);

            return ms.ToArray();
        }
        public async Task<string> SaveAsJson()
        {
            var result = await Viewer.ExportFormFieldsAsObjectAsync();

            var json = JsonSerializer.Serialize(result);

            return json;
        }
        private void ShowFullScreen()
        {
            DataSmallLoaded = false;
            WindowVisible = true;
            StateHasChanged();
        }
        private async Task OnPdfViewerLargeCreated(object args)
        {
            if (ViewerLarge != null)
            {
                await ViewerLarge.LoadAsync(DocumentData);

                StateHasChanged();
            }
        }
        private async Task PDFLargeLoaded(LoadEventArgs args)
        {
            if (Viewer != null && ViewerLarge != null && !DataLargeLoaded)
            {
                _pdfData = await Viewer.ExportFormFieldsAsObjectAsync();

                if (_pdfData != null)
                {
                    await ViewerLarge.ImportFormFieldsAsync(_pdfData);
                    StateHasChanged();
                }
                DataLargeLoaded = true;
            }
        }
        private async void HideFullScreen()
        {
            if (Viewer != null && ViewerLarge != null)
            {
                _pdfData = await ViewerLarge.ExportFormFieldsAsObjectAsync();

                if (_pdfData != null)
                {
                    await Viewer.ImportFormFieldsAsync(_pdfData);
                }
            }

            DataLargeLoaded = false;
            WindowVisible = false;
            StateHasChanged();
        }
        private async Task<bool> FillData()
        {
            if (!string.IsNullOrEmpty(Data))
            {
                if (SessionWrapper.CurrentUser != null)
                {
                    var userData = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentUser.ID);

                    if(userData != null)
                    {
                        Data = Data.Replace("{YearPart1}", DateTime.Now.Year.ToString().Substring(2, 1));
                        Data = Data.Replace("{YearPart2}", DateTime.Now.Year.ToString().Substring(3, 1));
                        Data = Data.Replace("{FiscalCode}", userData.FiscalNumber);
                        Data = Data.Replace("{PlaceOfBirth}", userData.PlaceOfBirth);
                        Data = Data.Replace("{CurrentDate}", DateTime.Now.ToString("dd.MM.yyyy"));
                        Data = Data.Replace("{Email}", userData.Email);

                        if (userData.MobilePhone != null)
                        {
                            Data = Data.Replace("{PhoneNumber}", userData.MobilePhone.Replace("+39", ""));
          
                            if (userData.MobilePhone.StartsWith("+39")) 
                            {
                                Data = Data.Replace("{PhonePrefix}", "+39");
                            }
                            else
                            {
                                Data = Data.Replace("{PhonePrefix}", "");
                            }
                        }
                        else
                        {
                            Data = Data.Replace("{PhonePrefix}", "");
                        }

                        Data = Data.Replace("{Gender}", userData.Gender);
                        Data = Data.Replace("{Surname}", userData.LastName);
                        Data = Data.Replace("{Name}", userData.FirstName);
                        Data = Data.Replace("{DomicileProv}", "");
                        Data = Data.Replace("{DomicileAddress}", userData.DomicileStreetAddress);
                        Data = Data.Replace("{DomicileCAP}", userData.DomicilePostalCode);
                        Data = Data.Replace("{DomicileMunicipality}", userData.DomicileMunicipality);
                        

                        if (userData.DateOfBirth != null)
                        {
                            Data = Data.Replace("{BrithDateYear}", userData.DateOfBirth.Value.Year.ToString());
                            Data = Data.Replace("{BrithDateMonth}", userData.DateOfBirth.Value.Month.ToString());
                            Data = Data.Replace("{BrithDateDay}", userData.DateOfBirth.Value.Day.ToString());
                        }

                        if (SessionWrapper.AUTH_Municipality_ID != null)
                        {
                            var CurrentMunicipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);

                            if(CurrentMunicipality != null)
                            {
                                Data = Data.Replace("{Municipality}", CurrentMunicipality.Name);
                            }
                        }
                    }
                }
            }

            return true;
        }
        //private void FormFieldClicked(FormFieldClickArgs args)
        //{
        //    args.Cancel = true;
        //}
    }
}
