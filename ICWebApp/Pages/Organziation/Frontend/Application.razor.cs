using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Classes.Validation;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ICWebApp.Application.Settings;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ICWebApp.Pages.Organziation.Frontend
{
    public partial class Application
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] IORGProvider OrgProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IPRIVProvider PrivProvider { get; set; }
        [Inject] IFILEProvider FileProvider { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] IMETAProvider MetaProvider { get; set; }
        [Inject] IAnchorService AnchorService { get; set; }

        private ORG_Request_Parameter? ORG_Request_Parameter { get; set; } = new ORG_Request_Parameter();   
        private List<V_AUTH_Organizations>? OrganizationList { get; set; }
        private bool IsDataBusy { get; set; } = true;
        private bool NewOrganization { get; set; } = false;
        private ORG_Request NewOrgRequest { get; set; } = new ORG_Request();
        private ORG_Request_User NewUserRequest { get; set; } = new ORG_Request_User();
        private List<V_AUTH_Company_Type>? CompanyTypeList;
        private List<V_AUTH_Company_LegalForm>? CompanyLegalFormList = new List<V_AUTH_Company_LegalForm>();
        private V_AUTH_Company_Type? SelectedCompanyType;
        private V_AUTH_Company_LegalForm? SelectedCompanyLegalForm;
        private List<V_AUTH_BolloFree_Reason>? BolloFreeReasons;
        private V_AUTH_BolloFree_Reason? CurrentReason;
        private EditContext editContext { get; set; }
        private bool MaleSelected
        {
            get
            {

                if (NewOrgRequest != null && NewOrgRequest.GV_GenderReq == "M")
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (NewOrgRequest != null && value == true)
                {
                    NewOrgRequest.GV_GenderReq = "M";
                    StateHasChanged();
                }
            }
        }
        private bool FemaleSelected
        {
            get
            {

                if (NewOrgRequest != null && NewOrgRequest.GV_GenderReq == "W")
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (NewOrgRequest != null && value == true)
                {
                    NewOrgRequest.GV_GenderReq = "W";
                    StateHasChanged();
                }
            }
        }
        private string validPhoneNumberCSS
        {
            get
            {
                if (editContext != null && editContext.GetValidationMessages(new FieldIdentifier(NewOrgRequest, "MobilePhoneReq")).Count() > 0)
                {
                    return "outline: 1px solid red !important;";
                }

                return "";
            }
        }
        private CustomValidation customValidation;
        private List<FILE_FileInfo> DocumentsOfProof = new List<FILE_FileInfo>();
        private string? DocumentsOfProofError;
        private string? PrivacyErrorCSS;
        private bool FormError = false;
        private bool FiscalCodeChecked = false;
        private PRIV_Privacy? Privacy { get; set; }
        private Guid SelectedMunicipality
        {
            get
            {
                if (NewOrgRequest != null && NewOrgRequest.SelectedMunicipality != null)
                    return NewOrgRequest.SelectedMunicipality.Value;
                
                return Guid.Empty;
            }
            set
            {
                if (value != Guid.Empty)
                {
                    UpdateAddressData(value);
                }

                if (NewOrgRequest != null)
                {
                    NewOrgRequest.SelectedMunicipality = value;
                }
            }
        }
        private bool AddressNotFound { get; set; } = false;
        private List<META_IstatComuni>? MunicipalitiesList { get; set; }
        private Guid GV_SelectedMunicipality
        {
            get
            {
                if (NewOrgRequest != null && NewOrgRequest.GV_SelectedMunicipality != null)
                    return NewOrgRequest.GV_SelectedMunicipality.Value;

                return Guid.Empty;
            }
            set
            {
                if (value != Guid.Empty)
                {
                    GV_UpdateAddressData(value);
                }

                if (NewOrgRequest != null)
                {
                    NewOrgRequest.GV_SelectedMunicipality = value;
                }
            }
        }
        private bool GV_AddressNotFound { get; set; } = false;
        private string? VatNumberError;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get("ORG_REQUEST_DASHBOARD_TITLE");
            editContext = new EditContext(NewOrgRequest);

            CrumbService.ClearBreadCrumb();
            CrumbService.AddBreadCrumb("/Organization/Dashboard", "ORG_REQUEST_DASHBOARD_TITLE", null, null, true);

            CompanyTypeList = await GetCompanyType();

            Privacy = await PrivProvider.GetPrivacy(SessionWrapper.AUTH_Municipality_ID.Value);
            BolloFreeReasons = await AuthProvider.GetVBolloFreeReasons();

            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            IsDataBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void CheckFiscalNumber()
        {
            if (ORG_Request_Parameter != null && !string.IsNullOrEmpty(ORG_Request_Parameter.FiscalCode) && ORG_Request_Parameter.FiscalCode.Length > 5 && SessionWrapper != null && SessionWrapper.AUTH_Municipality_ID != null)
            {
                IsDataBusy = true;
                StateHasChanged();

                OrganizationList = await AuthProvider.GetUserOrganizations(ORG_Request_Parameter.FiscalCode, SessionWrapper.AUTH_Municipality_ID.Value);

                FiscalCodeChecked = true;
                IsDataBusy = false;
                StateHasChanged();
            }
        }
        private async Task<List<V_AUTH_Company_Type>?> GetCompanyType()
        {
            var result = await AuthProvider.GetVCompanyType();

            return result.Where(p => p.CanBeRepresentation == true).ToList();
        }
        private async Task<List<V_AUTH_Company_LegalForm>?> GetCompanyFormList(Guid AUTH_Company_Type_ID)
        {
            return await AuthProvider.GetCompanyLegalForms(AUTH_Company_Type_ID);
        }
        private void ShowNewOrganisation()
        {
            AnchorService.ClearAnchors();

            NewOrgRequest.ID = new Guid();
            NewOrgRequest.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

            if (ORG_Request_Parameter != null)
            {
                NewOrgRequest.FiscalNumberReq = ORG_Request_Parameter.FiscalCode;
            }

            NewOrgRequest.GV_GenderReq = "W";

            NewOrganization = true;
            StateHasChanged();
        }
        private void HideNewOrganisation()
        {
            AnchorService.ClearAnchors();
            NewOrganization = false;
            StateHasChanged();
        }
        private async void CompanyInputChanged()
        {
            if (NewOrgRequest.AUTH_Company_Type_ID != null && CompanyTypeList != null)
            {
                SelectedCompanyType = CompanyTypeList.FirstOrDefault(p => p.ID == NewOrgRequest.AUTH_Company_Type_ID);

                CompanyLegalFormList = await GetCompanyFormList(NewOrgRequest.AUTH_Company_Type_ID.Value);
            }
            else
            {
                SelectedCompanyType = null;
                CompanyLegalFormList = new List<V_AUTH_Company_LegalForm>();
            }

            if (BolloFreeReasons != null)
            {
                CurrentReason = BolloFreeReasons.FirstOrDefault(p => p.ID == NewOrgRequest.AUTH_BolloFree_Reason_ID);
            }

            customValidation.ClearErrors();
            DocumentsOfProofError = null;
            PrivacyErrorCSS = null;
            FormError = false;
            StateHasChanged();
        }
        private void LegalFormInputChanged()
        {
            if(NewOrgRequest.AUTH_Company_LegalForm_ID != null)
            {
                SelectedCompanyLegalForm = CompanyLegalFormList.FirstOrDefault(p => p.ID == NewOrgRequest.AUTH_Company_LegalForm_ID);
            }
            else
            {
                SelectedCompanyLegalForm = null;
            }

            StateHasChanged();
        }
        private async void SubmitRequest()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            var result = ValidateForm();

            if(DocumentsOfProof == null || DocumentsOfProof.Count == 0)
            {
                DocumentsOfProofError = TextProvider.Get("ORG_DOCUMENT_OF_PROOF_ERROR");
                result = false;
            }
            else
            {
                DocumentsOfProofError = null;
            }

            if(NewOrgRequest.PrivacyConfirmed != true)
            {
                PrivacyErrorCSS = "error";
                result = false;
            }
            else
            {
                PrivacyErrorCSS = null;
            }

            FormError = !result;

            if (!result)
            {
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
                return;
            }

            var CurrentUserAnagrafic = await AuthProvider.GetAnagraficByUserID(SessionWrapper.CurrentUser.ID);

            if(NewOrgRequest.GVEqualsUser == true)
            {
                if (CurrentUserAnagrafic != null) 
                {
                    NewOrgRequest.GV_AUTH_Users_ID = SessionWrapper.CurrentUser.ID;

                    NewOrgRequest.GV_Firstname = CurrentUserAnagrafic.FirstName;
                    NewOrgRequest.GV_Lastname = CurrentUserAnagrafic.LastName;
                    NewOrgRequest.GV_FiscalNumber = CurrentUserAnagrafic.FiscalNumber;
                    NewOrgRequest.GV_Email = CurrentUserAnagrafic.Email;
                    NewOrgRequest.GV_DomicileMunicipality = CurrentUserAnagrafic.DomicileMunicipality;
                    NewOrgRequest.GV_DomicileNation = CurrentUserAnagrafic.DomicileNation;
                    NewOrgRequest.GV_DomicilePostalCode = CurrentUserAnagrafic.DomicilePostalCode;
                    NewOrgRequest.GV_DomicileProvince = CurrentUserAnagrafic.DomicileProvince;
                    NewOrgRequest.GV_DomicileStreetAddress = CurrentUserAnagrafic.DomicileStreetAddress;
                    NewOrgRequest.GV_Phone = CurrentUserAnagrafic.MobilePhone;
                    NewOrgRequest.GV_CountyOfBirth = CurrentUserAnagrafic.CountyOfBirth;
                    NewOrgRequest.GV_PlaceOfBirth = CurrentUserAnagrafic.PlaceOfBirth;
                    NewOrgRequest.GV_DateOfBirth = CurrentUserAnagrafic.DateOfBirth;
                    NewOrgRequest.GV_Gender = CurrentUserAnagrafic.Gender;
                }
            }

            NewOrgRequest.ID = Guid.NewGuid();
            NewOrgRequest.CreationDate = DateTime.Now;
            NewOrgRequest.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
            NewOrgRequest.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID;
            NewOrgRequest.ORG_Request_Status_ID = Guid.Parse("75b8b15c-9fda-4748-a7a2-1f2d409a521e"); //TO-SIGN

            //Comitting User

            if (CurrentUserAnagrafic != null)
            {
                NewOrgRequest.User_Firstname = CurrentUserAnagrafic.FirstName;
                NewOrgRequest.User_Lastname = CurrentUserAnagrafic.LastName;
                NewOrgRequest.User_FiscalNumber = CurrentUserAnagrafic.FiscalNumber;
                NewOrgRequest.User_Email = CurrentUserAnagrafic.Email;
                NewOrgRequest.User_DomicileMunicipality = CurrentUserAnagrafic.DomicileMunicipality;
                NewOrgRequest.User_DomicileNation = CurrentUserAnagrafic.DomicileNation;
                NewOrgRequest.User_DomicilePostalCode = CurrentUserAnagrafic.DomicilePostalCode;
                NewOrgRequest.User_DomicileProvince = CurrentUserAnagrafic.DomicileProvince;
                NewOrgRequest.User_DomicileStreetAddress = CurrentUserAnagrafic.DomicileStreetAddress;
                NewOrgRequest.User_Phone = CurrentUserAnagrafic.MobilePhone;
                NewOrgRequest.User_CountyOfBirth = CurrentUserAnagrafic.CountyOfBirth;
                NewOrgRequest.User_PlaceOfBirth = CurrentUserAnagrafic.PlaceOfBirth;
                NewOrgRequest.User_DateOfBirth = CurrentUserAnagrafic.DateOfBirth;
                NewOrgRequest.User_Gender = CurrentUserAnagrafic.Gender;
            }

            await OrgProvider.SetRequest(NewOrgRequest);

            ORG_Request_Attachment attachment = new ORG_Request_Attachment();

            if (DocumentsOfProof != null && DocumentsOfProof.Count > 0)
            {
                foreach (var doc in DocumentsOfProof)
                {
                    attachment.ID = Guid.NewGuid();
                    attachment.ORG_Request_ID = NewOrgRequest.ID;
                    attachment.FILE_FileInfo_ID = doc.ID;

                    await FileProvider.SetFileInfo(doc);

                    await OrgProvider.SetRequestAttachment(attachment);
                }
            }

            await OrgProvider.SetRequest(NewOrgRequest);

            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Organization/Application/Sign/" + NewOrgRequest.ID);

            StateHasChanged();
        }
        private bool ValidateForm()
        {
            customValidation.ClearErrors();

            var errors = new Dictionary<string, List<string>>();

            if (NewOrgRequest.AUTH_Company_Type_IDReq == null)
            {
                errors.Add(nameof(NewOrgRequest.AUTH_Company_Type_IDReq), new()
                {
                    "required"
                });
            }
            if (SelectedCompanyType != null && SelectedCompanyType.HasLegalForm == true && CompanyLegalFormList != null && CompanyLegalFormList.Count() > 0)
            {
                if (NewOrgRequest.AUTH_Company_LegalForm_IDReq == null)
                {
                    errors.Add(nameof(NewOrgRequest.AUTH_Company_LegalForm_IDReq), new()
                    {
                        "required"
                    });
                }
            }

            if (string.IsNullOrEmpty(NewOrgRequest.FirstnameReq))
            {
                errors.Add(nameof(NewOrgRequest.FirstnameReq), new()
                {
                    "required"
                });
            }
            if (string.IsNullOrEmpty(NewOrgRequest.FiscalNumberReq))
            {
                errors.Add(nameof(NewOrgRequest.FiscalNumberReq), new()
                {
                    "required"
                });
            }

            var fiscalRegex = new Regex(@"^[A-Z]{6}[0-9]{2}[A-Z][0-9]{2}[A-Z][0-9]{3}[A-Z]$|^[0-9]{11}$");

            if (!string.IsNullOrEmpty(NewOrgRequest.FiscalNumberReq) && !fiscalRegex.IsMatch(NewOrgRequest.FiscalNumberReq))
            {
                errors.Add(nameof(NewOrgRequest.FiscalNumberReq), new()
                {
                    "VALIDATION_REGEX"
                });
            }

            if (SelectedCompanyType != null && SelectedCompanyType.IsPrivate == true && SelectedCompanyType.ID == Guid.Parse("505f2b86-4301-4c55-8dbe-3ee494e5569d"))
            {
                if (string.IsNullOrEmpty(NewOrgRequest.LastnameReq))
                {
                    errors.Add(nameof(NewOrgRequest.LastnameReq), new()
                    {
                        "required"
                    });
                }
            }
            else
            {
                if (SelectedCompanyType != null && SelectedCompanyType.IsAssociation == true)
                {
                    if (string.IsNullOrEmpty(NewOrgRequest.VatNumberReq))
                    {
                        errors.Add(nameof(NewOrgRequest.VatNumberReq), new()
                        {
                            "required"
                        });
                    }
                }

                if (SelectedCompanyType != null && (SelectedCompanyType.IsAssociation == true || SelectedCompanyType.IsSport == true || SelectedCompanyType.IsOnulus == true))
                {
                    var fiscalGVRegex = new Regex(@"^[0-9]{11}$");

                    if (!string.IsNullOrEmpty(NewOrgRequest.VatNumberVal) && !fiscalGVRegex.IsMatch(NewOrgRequest.VatNumberVal))
                    {
                        if (SelectedCompanyLegalForm != null && SelectedCompanyLegalForm.ID == Guid.Parse("fc0a0e03-9685-4ddb-9f5c-61b9fe08bb11"))
                        { 
                            errors.Add(nameof(NewOrgRequest.VatNumberVal), new()
                            {
                                "VALIDATION_REGEX"
                            });
                        }
                        else
                        {
                            if (SelectedCompanyType.IsAssociation == true)
                            {
                                errors.Add(nameof(NewOrgRequest.VatNumberReq), new()
                                {
                                    "VALIDATION_REGEX"
                                });
                            }
                        }
                    }

                    if (SelectedCompanyType.IsAssociation == true)
                    {
                        if (string.IsNullOrEmpty(NewOrgRequest.PECEmailReq))
                        {
                            errors.Add(nameof(NewOrgRequest.PECEmailReq), new()
                        {
                            "required"
                        });
                        }

                        var mailpecAttribute = new EmailAddressAttribute();

                        if (!string.IsNullOrEmpty(NewOrgRequest.PECEmailReq) && !mailpecAttribute.IsValid(NewOrgRequest.PECEmailReq))
                        {
                            errors.Add(nameof(NewOrgRequest.PECEmailReq), new()
                            {
                                "VALIDATION_EMAIL"
                            });
                        }

                        if (string.IsNullOrEmpty(NewOrgRequest.CodiceDestinatarioReq))
                        {
                            errors.Add(nameof(NewOrgRequest.CodiceDestinatarioReq), new()
                            {
                                "required"
                            });
                        }
                    }
                }

                if (NewOrgRequest.BolloFree && NewOrgRequest.AUTH_BolloFree_Reason_ID == Guid.Parse("C93266FF-D027-4D8B-8A08-7D7025172AC8")) //Sonstige
                {
                    if(string.IsNullOrEmpty(NewOrgRequest.BolloFreeCustomReasonDesc))
                    {
                        errors.Add(nameof(NewOrgRequest.BolloFreeCustomReasonDesc), new() { "required" });
                    }
                }

                if (SelectedCompanyType != null && (SelectedCompanyType.IsOnulus) && CompanyTypes
                        .GetRecognizedAssociationLegalFormIds().Contains(SelectedCompanyLegalForm.ID) && NewOrgRequest.AUTH_BolloFree_Reason_ID == Guid.Parse("01146256-0697-49AE-9DC1-810C1E1D53D3"))
                {
                    if (string.IsNullOrEmpty(NewOrgRequest.EintragungNrReq))
                    {
                        errors.Add(nameof(NewOrgRequest.EintragungNrReq), new() { "required" });
                    }

                    if (NewOrgRequest.EintragungDatumReq == null)
                    {
                        errors.Add(nameof(NewOrgRequest.EintragungDatumReq), new() { "required" });
                    }
                    
                }

                if (SelectedCompanyType.IsSport && CompanyTypes.GetRecognizedAssociationLegalFormIds().Contains(SelectedCompanyLegalForm.ID) && NewOrgRequest.AUTH_BolloFree_Reason_ID == Guid.Parse("01146256-0697-49AE-9DC1-810C1E1D53D3"))
                {
                    if (string.IsNullOrEmpty(NewOrgRequest.EintragungNrReq))
                    {
                        errors.Add(nameof(NewOrgRequest.EintragungNrReq), new() { "required" });
                    }

                    if (NewOrgRequest.EintragungDatumReq == null)
                    {
                        errors.Add(nameof(NewOrgRequest.EintragungDatumReq), new() { "required" });
                    }
                }

                    if (SelectedCompanyType != null && SelectedCompanyType.IsAssociation == true)
                {
                    if (string.IsNullOrEmpty(NewOrgRequest.HandelskammerEintragungReq))
                    {
                        errors.Add(nameof(NewOrgRequest.HandelskammerEintragungReq), new()
                        {
                            "required"
                        });
                    }
                }
            }

            if (string.IsNullOrEmpty(NewOrgRequest.DomicileStreetAddressReq))
            {
                errors.Add(nameof(NewOrgRequest.DomicileStreetAddressReq), new()
                {
                    "required"
                });
            }
            if (string.IsNullOrEmpty(NewOrgRequest.DomicilePostalCodeReq))
            {
                errors.Add(nameof(NewOrgRequest.DomicilePostalCodeReq), new()
                {
                    "required"
                });
            }
            if (string.IsNullOrEmpty(NewOrgRequest.DomicileMunicipalityReq))
            {
                errors.Add(nameof(NewOrgRequest.DomicileMunicipalityReq), new()
                {
                    "required"
                });
            }
            if (string.IsNullOrEmpty(NewOrgRequest.DomicileProvinceReq))
            {
                errors.Add(nameof(NewOrgRequest.DomicileProvinceReq), new()
                {
                    "required"
                });
            }
            if (string.IsNullOrEmpty(NewOrgRequest.DomicileNationReq))
            {
                errors.Add(nameof(NewOrgRequest.DomicileNationReq), new()
                {
                    "required"
                });
            }
            if (string.IsNullOrEmpty(NewOrgRequest.EmailReq))
            {
                errors.Add(nameof(NewOrgRequest.EmailReq), new()
                {
                    "required"
                });
            }

            var mailAttribute = new EmailAddressAttribute();

            if (!string.IsNullOrEmpty(NewOrgRequest.EmailReq) && !mailAttribute.IsValid(NewOrgRequest.EmailReq))
            {
                errors.Add(nameof(NewOrgRequest.EmailReq), new()
                {
                    "VALIDATION_EMAIL"
                });
            }

            if (string.IsNullOrEmpty(NewOrgRequest.Phone) && string.IsNullOrEmpty(NewOrgRequest.MobilePhone))
            {
                errors.Add(nameof(NewOrgRequest.Phone), new()
                {
                    "required"
                });
            }

            if (NewOrgRequest.GVEqualsUser != true && SelectedCompanyType != null && SelectedCompanyType.IsPrivate != true)
            {
                if (string.IsNullOrEmpty(NewOrgRequest.GV_FirstnameReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_FirstnameReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_LastnameReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_LastnameReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_GenderReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_GenderReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_CountyOfBirthReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_CountyOfBirthReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_PlaceOfBirthReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_PlaceOfBirthReq), new()
                    {
                        "required"
                    });
                }
                if (NewOrgRequest.GV_DateOfBirthReq == null)
                {
                    errors.Add(nameof(NewOrgRequest.GV_DateOfBirthReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_FiscalNumberReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_FiscalNumberReq), new()
                    {
                        "required"
                    });
                }

                var fiscalGVRegex = new Regex(@"^[A-Z]{6}[0-9]{2}[A-Z][0-9]{2}[A-Z][0-9]{3}[A-Z]$");

                if (!string.IsNullOrEmpty(NewOrgRequest.GV_FiscalNumberReq) && !fiscalGVRegex.IsMatch(NewOrgRequest.GV_FiscalNumberReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_FiscalNumberReq), new()
                    {
                        "VALIDATION_REGEX"
                    });
                }

                if (string.IsNullOrEmpty(NewOrgRequest.GV_DomicileStreetAddressReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_DomicileStreetAddressReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_DomicilePostalCodeReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_DomicilePostalCodeReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_DomicileMunicipalityReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_DomicileMunicipalityReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_DomicileProvinceReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_DomicileProvinceReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_DomicileNationReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_DomicileNationReq), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(NewOrgRequest.GV_EmailReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_EmailReq), new()
                    {
                        "required"
                    });
                }

                var mailGVAttribute = new EmailAddressAttribute();

                if (!string.IsNullOrEmpty(NewOrgRequest.GV_EmailReq) && !mailGVAttribute.IsValid(NewOrgRequest.GV_EmailReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_EmailReq), new()
                    {
                        "VALIDATION_EMAIL"
                    });
                }

                if (string.IsNullOrEmpty(NewOrgRequest.GV_PhoneReq))
                {
                    errors.Add(nameof(NewOrgRequest.GV_PhoneReq), new()
                    {
                        "required"
                    });
                }
            }

            if (errors.Any())
            {
                customValidation?.DisplayErrors(errors);
                return false;
            }
            
            return true;
        }
        private void ReturnToDashboard()
        {
            BusyIndicatorService.IsBusy = true;
            NavManager.NavigateTo("/Organization/Dashboard");
            StateHasChanged();
        }
        private async void ApplyForOrg(V_AUTH_Organizations Org)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            if (SessionWrapper.CurrentUser != null && Org != null && Org.ID != null)
            {
                Org.RequestMessage = null;
                Org.RequestErrorMessage = null;

                var UserOrg = await AuthProvider.GetUserOrganization(Org.ID, SessionWrapper.CurrentUser.ID);

                if(UserOrg == null)
                {
                    AUTH_ORG_Users newOrgUser = new AUTH_ORG_Users();

                    newOrgUser.ID = Guid.NewGuid();
                    newOrgUser.ORG_AUTH_Users_ID = Org.AUTH_Users_ID.Value;
                    newOrgUser.AUTH_Users_ID = SessionWrapper.CurrentUser.ID;
                    newOrgUser.AUTH_ORG_Role_ID = Guid.Parse("d4213886-97eb-45aa-a93a-0e920a657364"); // Nur Vertreter                    

                    await AuthProvider.SetUserOrganization(newOrgUser);

                    var existingOrgUsers = await AuthProvider.GetOrganizationsUsers(Org.ID);

                    var orgAdmins = existingOrgUsers.Where(p =>
                        p.AUTH_ORG_Role_ID == Guid.Parse("76724c77-9b1e-4f8f-9444-057f7894783f")
                        || p.AUTH_ORG_Role_ID == Guid.Parse("da2a52c6-7c67-4f8e-9afd-5af026ab445e")).ToList();
                    if (orgAdmins.Count > 0) //SEND TO OTHER USERS
                    {                       
                        foreach (var orgUser in orgAdmins)
                        {
                            var msg = await MessageService.GetMessage(orgUser.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_SUBSTITUTION_ACCESS_TEXT",
                                "NOTIF_SUBSTITUTION_ACCESS_SHORTTEXT", "NOTIF_SUBSTITUTION_ACCESS_CHANGED_TITLE",
                                Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, FirstName: SessionWrapper.CurrentUser.Firstname, LastName: SessionWrapper.CurrentUser.Lastname);

                            if (msg != null)
                            {
                                msg.Messagetext = msg.Messagetext.Replace("{Link}",
                                    NavManager.BaseUri + "/Organization/Management/" + orgUser.ORG_AUTH_Users_ID);
                                await MessageService.SendMessage(msg, NavManager.BaseUri + "/Organization/Management/" + orgUser.ORG_AUTH_Users_ID);
                            }
                        }
                    }
                    else //SEND TO MUNICIPALITY
                    {
                        var authorityList = await AuthProvider.GetAuthorityList(SessionWrapper.AUTH_Municipality_ID.Value, null, null);

                        var subsititutionAuthority = authorityList.FirstOrDefault(p => p.IsSubstitution == true);

                        if (subsititutionAuthority != null)
                        {
                            var msg = await MessageService.GetMessage(newOrgUser.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_MUN_SUBSTITUTION_ACCESS_TEXT", "NOTIF_MUN_SUBSTITUTION_ACCESS_SHORTTEXT", "NOTIF_MUN_SUBSTITUTION_ACCESS_TITLE", Guid.Parse("7d03e491-5826-4131-a6a1-06c99be991c9"), true);
                            if (msg != null)
                            {
                                msg.Messagetext = msg.Messagetext.Replace("{OrgName}", Org.Fullname);
                                msg.Messagetext = msg.Messagetext.Replace("{OrgFiscalCode}", Org.FiscalNumber);
                                await MessageService.SendMessageToAuthority(subsititutionAuthority.ID, msg, NavManager.BaseUri + "/Organization/Backend/Management/Detail/" + Org.AUTH_Users_ID);
                            }
                        }
                    }

                    Org.RequestMessage = TextProvider.Get("ORG_REQUEST_SUCCESSFULL");
                }
                else
                {
                    Org.RequestErrorMessage = TextProvider.Get("ORG_REQUEST_ALREADY_DONE");
                }
            }

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
        private void PrivacyChanged()
        {
            if (NewOrgRequest.PrivacyConfirmed != true)
            {
                PrivacyErrorCSS = "error";
            }
            else
            {
                PrivacyErrorCSS = null;
            }

            StateHasChanged();
        }
        private void UploadChanged()
        {
            if (DocumentsOfProof == null || DocumentsOfProof.Count == 0)
            {
                DocumentsOfProofError = TextProvider.Get("ORG_DOCUMENT_OF_PROOF_ERROR");
            }
            else
            {
                DocumentsOfProofError = null;
            }

            StateHasChanged();
        }
        private void AddressNotFoundClick()
        {
            AddressNotFound = true;
            StateHasChanged();
        }
        private void SearchAddressClick()
        {
            AddressNotFound = false;
            StateHasChanged();
        }
        private async void UpdateAddressData(Guid Municipality_ID)
        {
            var data = await MetaProvider.GetMunicipality(Municipality_ID);

            if (data != null)
            {
                NewOrgRequest.DomicilePostalCode = data.Cap;
                NewOrgRequest.DomicileMunicipality = data.NameDE;
                NewOrgRequest.DomicileProvince = data.RegionCity;
                NewOrgRequest.DomicileNation = "IT";

                StateHasChanged();
            }
        }
        private void GV_AddressNotFoundClick()
        {
            GV_AddressNotFound = true;
            StateHasChanged();
        }
        private void GV_SearchAddressClick()
        {
            GV_AddressNotFound = false;
            StateHasChanged();
        }
        private async void GV_UpdateAddressData(Guid Municipality_ID)
        {
            var data = await MetaProvider.GetMunicipality(Municipality_ID);

            if (data != null)
            {
                NewOrgRequest.GV_DomicilePostalCode = data.Cap;
                NewOrgRequest.GV_DomicileMunicipality = data.NameDE;
                NewOrgRequest.GV_DomicileProvince = data.RegionCity;
                NewOrgRequest.GV_DomicileNation = "IT";

                StateHasChanged();
            }
        }

        private void BolloFreeChangedHandler(object value)
        {
            bool val = (bool)value;
            if (!val)
            {
                NewOrgRequest.AUTH_BolloFree_Reason_ID = null;
            }
        }
    }
}
