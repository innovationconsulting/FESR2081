using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Classes.Validation;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;

namespace ICWebApp.Components.Organization
{
    public partial class EditOrganization
    {
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IMessageService MessageService { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IMETAProvider MetaProvider { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public AUTH_Users_Anagrafic Anagrafic { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        [Parameter] public bool IsBackend { get; set; } = false;
        
        private AUTH_Users_Anagrafic? StartAnagrafic;
        private AUTH_Users? StartUser;
        private AUTH_Users? User { get; set; }
        private List<V_AUTH_Company_Type>? CompanyTypeList = new List<V_AUTH_Company_Type>();
        private List<V_AUTH_Company_LegalForm>? CompanyLegalFormList = new List<V_AUTH_Company_LegalForm>();
        private V_AUTH_Company_Type? SelectedCompanyType;
        private V_AUTH_Company_LegalForm? SelectedCompanyLegalForm;
        private CustomValidation? customValidation;
        private CustomValidation? customGVValidation;
        private AUTH_Users_Anagrafic? GesetzlicherVertreter { get; set; }
        
        private List<V_AUTH_BolloFree_Reason>? BolloFreeReasons;
        private string validPhoneNumberCSS
        {
            get
            {
                if (editContext != null && editContext.GetValidationMessages(new FieldIdentifier(Anagrafic, "ReqMobilePhone")).Count() > 0)
                {
                    return "outline: 1px solid red !important;";
                }

                return "";
            }
        }
        private EditContext editContext { get; set; }
        private EditContext editGVContext { get; set; }
        private bool MaleSelected
        {
            get
            {

                if (GesetzlicherVertreter != null && GesetzlicherVertreter.Gender == "M")
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (GesetzlicherVertreter != null && value == true)
                {
                    GesetzlicherVertreter.Gender = "M";
                    StateHasChanged();
                }
            }
        }
        private bool FemaleSelected
        {
            get
            {

                if (GesetzlicherVertreter != null && GesetzlicherVertreter.Gender == "W")
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (GesetzlicherVertreter != null && value == true)
                {
                    GesetzlicherVertreter.Gender = "W";
                    StateHasChanged();
                }
            }
        }
        private bool FormError = false;
        private string? GVStartFiscalNumber;
        private Guid SelectedMunicipality
        {
            get
            {
                if(Anagrafic != null && Anagrafic.SelectedMunicipality != null)
                    return Anagrafic.SelectedMunicipality.Value;

                return Guid.Empty;
            }
            set
            {
                if (value != Guid.Empty)
                {
                    UpdateAddressData(value);
                }

                if (Anagrafic != null)
                {
                    Anagrafic.SelectedMunicipality = value;
                }
            }
        }
        private bool AddressNotFound { get; set; } = false;
        private List<META_IstatComuni>? MunicipalitiesList { get; set; }
        private Guid _GV_selectedMunicipality;
        private Guid GV_SelectedMunicipality
        {
            get
            {
                if (GesetzlicherVertreter != null && GesetzlicherVertreter.SelectedMunicipality != null)
                    return GesetzlicherVertreter.SelectedMunicipality.Value;

                return Guid.Empty;
            }
            set
            {
                if (value != Guid.Empty)
                {
                    GV_UpdateAddressData(value);
                }

                if (GesetzlicherVertreter != null)
                {
                    GesetzlicherVertreter.SelectedMunicipality = value;
                }
            }
        }
        private bool GV_AddressNotFound { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            if (Anagrafic != null && Anagrafic.AUTH_Users_ID != null)
            {
                CompanyTypeList = await GetCompanyType();
                CompanyLegalFormList = await AuthProvider.GetCompanyLegalForms();
                BolloFreeReasons = await AuthProvider.GetVBolloFreeReasons();

                editContext = new EditContext(Anagrafic);

                User = await AuthProvider.GetUser(Anagrafic.AUTH_Users_ID.Value);

                if (User != null && CompanyTypeList != null && CompanyLegalFormList != null)
                {
                    SelectedCompanyType = CompanyTypeList.FirstOrDefault(p => p.ID == User.AUTH_Company_Type_ID);
                    SelectedCompanyLegalForm = CompanyLegalFormList.FirstOrDefault(p => p.ID == Anagrafic.AUTH_Company_LegalForm_ID);
                }

                if (User != null && Anagrafic.GV_AUTH_Users_ID != null)
                {
                    GesetzlicherVertreter = await AuthProvider.GetAnagraficByUserID(Anagrafic.GV_AUTH_Users_ID.Value);
                }

                if (GesetzlicherVertreter == null)
                {
                    GesetzlicherVertreter = new AUTH_Users_Anagrafic();
                }

                if (GesetzlicherVertreter != null)
                {
                    GVStartFiscalNumber = GesetzlicherVertreter.FiscalNumber;
                }

                editGVContext = new EditContext(GesetzlicherVertreter);
            }

            StartAnagrafic = Anagrafic;
            StartUser = User;

            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async Task<List<V_AUTH_Company_Type>?> GetCompanyType()
        {
            var result = await AuthProvider.GetVCompanyType();

            return result.Where(p => p.CanBeRepresentation == true).ToList();
        }
        private bool ValidateForm()
        {
            if (Anagrafic != null && User != null)
            {
                if (customValidation != null)
                {
                    customValidation.ClearErrors();
                }
                if (customGVValidation != null)
                {
                    customGVValidation.ClearErrors();
                }

                var errors = new Dictionary<string, List<string>>();
                var gvErrors = new Dictionary<string, List<string>>();

                if (string.IsNullOrEmpty(Anagrafic.FirstName))
                {
                    errors.Add(nameof(Anagrafic.FirstName), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(Anagrafic.FiscalNumber))
                {
                    errors.Add(nameof(Anagrafic.FiscalNumber), new()
                    {
                        "required"
                    });
                }

                if (SelectedCompanyType != null && SelectedCompanyType.IsPrivate == true && SelectedCompanyType.ID == Guid.Parse("505f2b86-4301-4c55-8dbe-3ee494e5569d"))
                {
                    if (string.IsNullOrEmpty(Anagrafic.LastName))
                    {
                        errors.Add(nameof(Anagrafic.LastName), new()
                        {
                            "required"
                        });
                    }
                }
                else
                {
                    if (SelectedCompanyType != null && SelectedCompanyType.IsAssociation == true)
                    {
                        if (string.IsNullOrEmpty(Anagrafic.VatNumber))
                        {
                            errors.Add(nameof(Anagrafic.VatNumber), new()
                            {
                                "required"
                            });
                        }
                    }

                    if (SelectedCompanyType != null && (SelectedCompanyType.IsAssociation == true || SelectedCompanyType.IsSport == true || SelectedCompanyType.IsOnulus == true))
                    {
                        var fiscalGVRegex = new Regex(@"[0-9]{11}");

                        if (!string.IsNullOrEmpty(Anagrafic.VatNumber) && !fiscalGVRegex.IsMatch(Anagrafic.VatNumber))
                        {
                            errors.Add(nameof(Anagrafic.VatNumber), new()
                            {
                                "VALIDATION_REGEX"
                            });
                        }


                        /*if (string.IsNullOrEmpty(Anagrafic.PECEmail))
                        {
                            errors.Add(nameof(Anagrafic.PECEmail), new()
                            {
                                "required"
                            });
                        }*/

                        var mailpecAttribute = new EmailAddressAttribute();

                        if (!string.IsNullOrEmpty(Anagrafic.PECEmail) && !mailpecAttribute.IsValid(Anagrafic.PECEmail))
                        {
                            errors.Add(nameof(Anagrafic.PECEmail), new()
                            {
                                "VALIDATION_EMAIL"
                            });
                        }

                        /*if (string.IsNullOrEmpty(Anagrafic.CodiceDestinatario))
                        {
                            errors.Add(nameof(Anagrafic.CodiceDestinatario), new()
                            {
                                "required"
                            });
                        }*/
                    }

                    if (SelectedCompanyType != null && SelectedCompanyType.IsAssociation == true)
                    {
                        if (string.IsNullOrEmpty(Anagrafic.HandelskammerEintragung))
                        {
                            errors.Add(nameof(Anagrafic.HandelskammerEintragung), new()
                            {
                                "required"
                            });
                        }
                    }
                }

                if (Anagrafic.BolloFree == true)
                {
                    if (Anagrafic.AUTH_BolloFree_Reason_ID == null)
                    {
                        errors.Add(nameof(Anagrafic.AUTH_BolloFree_Reason_ID), new()
                        {
                            "required"
                        });
                    }
                }
                if (string.IsNullOrEmpty(Anagrafic.DomicileStreetAddress))
                {
                    errors.Add(nameof(Anagrafic.DomicileStreetAddress), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(Anagrafic.DomicilePostalCode))
                {
                    errors.Add(nameof(Anagrafic.DomicilePostalCode), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(Anagrafic.DomicileMunicipality))
                {
                    errors.Add(nameof(Anagrafic.DomicileMunicipality), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(Anagrafic.DomicileProvince))
                {
                    errors.Add(nameof(Anagrafic.DomicileProvince), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(Anagrafic.DomicileNation))
                {
                    errors.Add(nameof(Anagrafic.DomicileNation), new()
                    {
                        "required"
                    });
                }
                if (string.IsNullOrEmpty(Anagrafic.Email))
                {
                    errors.Add(nameof(Anagrafic.Email), new()
                    {
                        "required"
                    });
                }

                var mailAttribute = new EmailAddressAttribute();

                if (!string.IsNullOrEmpty(Anagrafic.Email) && !mailAttribute.IsValid(Anagrafic.Email))
                {
                    errors.Add(nameof(Anagrafic.Email), new()
                    {
                        "VALIDATION_EMAIL"
                    });
                }

                if (SelectedCompanyType != null && GesetzlicherVertreter != null && SelectedCompanyType.IsPrivate != true)
                {
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_FirstName))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_FirstName), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_LastName))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_LastName), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_Gender))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_Gender), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_CountyOfBirth))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_CountyOfBirth), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_PlaceOfBirth))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_PlaceOfBirth), new()
                        {
                            "required"
                        });
                    }
                    if (GesetzlicherVertreter.GV_DateOfBirth == null)
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_DateOfBirth), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_FiscalNumber))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_FiscalNumber), new()
                        {
                            "required"
                        });
                    }

                    var fiscalGVRegex = new Regex(@"^[A-Z]{6}[0-9]{2}[A-Z][0-9]{2}[A-Z][0-9]{3}[A-Z]$");

                    if (!string.IsNullOrEmpty(GesetzlicherVertreter.GV_FiscalNumber) && !fiscalGVRegex.IsMatch(GesetzlicherVertreter.GV_FiscalNumber))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_FiscalNumber), new()
                        {
                            "VALIDATION_REGEX"
                        });
                    }

                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_DomicileStreetAddress))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_DomicileStreetAddress), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_DomicilePostalCode))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_DomicilePostalCode), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_DomicileMunicipality))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_DomicileMunicipality), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_DomicileProvince))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_DomicileProvince), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_DomicileNation))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_DomicileNation), new()
                        {
                            "required"
                        });
                    }
                    if (string.IsNullOrEmpty(GesetzlicherVertreter.GV_Email))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_Email), new()
                        {
                            "required"
                        });
                    }

                    var mailGVAttribute = new EmailAddressAttribute();

                    if (!string.IsNullOrEmpty(GesetzlicherVertreter.GV_Email) && !mailGVAttribute.IsValid(GesetzlicherVertreter.GV_Email))
                    {
                        gvErrors.Add(nameof(GesetzlicherVertreter.GV_Email), new()
                        {
                            "VALIDATION_EMAIL"
                        });
                    }
                }

                if (errors.Any() && customValidation != null)
                {
                    customValidation.DisplayErrors(errors);
                }
                if (gvErrors.Any() && customGVValidation != null)
                {
                    customGVValidation.DisplayErrors(gvErrors);
                }

                if(errors.Any() || gvErrors.Any())
                {
                    return false;
                }

                return true;
            }

            return false;
        }
        private async void Cancel()
        {
            await OnCancel.InvokeAsync();
            StateHasChanged();
        }
        private async void Save()
        {
            var result = ValidateForm();

            FormError = !result;

            if (!result)
            {
                BusyIndicatorService.IsBusy = false;
                StateHasChanged();
                return;
            }

            if (GesetzlicherVertreter != null && GesetzlicherVertreter.FiscalNumber != GVStartFiscalNumber)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("CHANGE_GV_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

            }

            await UpdateOrCreateGV();

            if (User != null)
            {
                User.Firstname = Anagrafic.FirstName;
                User.Lastname = Anagrafic.LastName;
                Anagrafic.AUTH_Company_LegalForm_ID = User.AUTH_Company_LegalForm_ID;

                await AuthProvider.UpdateUser(User);
            }

            await AuthProvider.SetAnagrafic(Anagrafic);

            var OrgUsers = await AuthProvider.GetOrganizationsUsers(User.ID);

            foreach (var user in OrgUsers.Where(p => p.AUTH_ORG_Role_ID == Guid.Parse("76724c77-9b1e-4f8f-9444-057f7894783f") || p.AUTH_ORG_Role_ID == Guid.Parse("da2a52c6-7c67-4f8e-9afd-5af026ab445e")).ToList())
            {
                var userSettings = await AuthProvider.GetSettings(user.AUTH_Users_ID.Value);

                List<MSG_Message_Parameters>? MessageParameters = null;

                if (StartAnagrafic != null && StartUser != null && User != null)
                {
                    MessageParameters = GetDifferentFields(StartAnagrafic, Anagrafic, StartUser, User, userSettings.LANG_Languages_ID.Value);
                }

                if (StartAnagrafic != null && MessageParameters != null && MessageParameters.Count() > 0)
                {
                    MessageParameters.Add(new MSG_Message_Parameters()
                    {
                        Code = "{Organisation}",
                        Message = Anagrafic.FirstName + " " + Anagrafic.LastName,
                    });

                    var msgAll = await MessageService.GetMessage(user.AUTH_Users_ID.Value, SessionWrapper.AUTH_Municipality_ID.Value, "NOTIF_SUBSTITUTION_DATA_CHANGED_TEXT", "NOTIF_SUBSTITUTION_DATA_CHANGED_SHORTTEXT", "NOTIF_SUBSTITUTION_DATA_CHANGED_TITLE", Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true, MessageParameters);

                    if (msgAll != null)
                    {
                        await MessageService.SendMessage(msgAll, NavManager.BaseUri + "/Organization/Dashboard");
                    }
                }
            }

            await OnSave.InvokeAsync();
            StateHasChanged();
        }
        private async Task<bool> UpdateOrCreateGV()
        {
            if (GesetzlicherVertreter != null && GesetzlicherVertreter.FiscalNumber != null && SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                var GV = await AuthProvider.GetUser(GesetzlicherVertreter.FiscalNumber, null, SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, true);

                if (GV != null)
                {
                    GV.Firstname = GesetzlicherVertreter.FirstName;
                    GV.Lastname = GesetzlicherVertreter.LastName;
                    GV.PhoneNumber = GesetzlicherVertreter.MobilePhone;
                    GV.Email = GesetzlicherVertreter.Email;

                    await AuthProvider.SetAnagrafic(GesetzlicherVertreter);
                    await AuthProvider.UpdateUser(GV);

                    Anagrafic.GV_AUTH_Users_ID = GV.ID;
                }
                else
                {
                    AUTH_Users newUser = new AUTH_Users();

                    newUser.ID = Guid.NewGuid();
                    newUser.Firstname = GesetzlicherVertreter.FirstName;
                    newUser.Lastname = GesetzlicherVertreter.LastName;
                    newUser.Username = GesetzlicherVertreter.FiscalNumber;
                    newUser.Email = GesetzlicherVertreter.Email;
                    newUser.PhoneNumber = GesetzlicherVertreter.Phone;
                    newUser.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value;
                    newUser.RegistrationMode = "Citizen Backend";
                    newUser.IsOrganization = false;

                    var anagrafic = new AUTH_Users_Anagrafic();

                    anagrafic.ID = Guid.NewGuid();
                    anagrafic.AUTH_Users_ID = newUser.ID;
                    anagrafic.FirstName = GesetzlicherVertreter.FirstName;
                    anagrafic.LastName = GesetzlicherVertreter.LastName;
                    anagrafic.FiscalNumber = GesetzlicherVertreter.FiscalNumber;
                    anagrafic.Email = GesetzlicherVertreter.Email;
                    anagrafic.CountyOfBirth = GesetzlicherVertreter.CountyOfBirth;
                    anagrafic.PlaceOfBirth = GesetzlicherVertreter.PlaceOfBirth;
                    anagrafic.DateOfBirth = GesetzlicherVertreter.DateOfBirth;
                    anagrafic.Address = GesetzlicherVertreter.DomicileStreetAddress + ", " + GesetzlicherVertreter.DomicilePostalCode + " " + GesetzlicherVertreter.DomicileMunicipality;
                    anagrafic.DomicileMunicipality = GesetzlicherVertreter.DomicileMunicipality;
                    anagrafic.DomicileNation = GesetzlicherVertreter.DomicileNation;
                    anagrafic.DomicilePostalCode = GesetzlicherVertreter.DomicilePostalCode;
                    anagrafic.DomicileProvince = GesetzlicherVertreter.DomicileProvince;
                    anagrafic.DomicileStreetAddress = GesetzlicherVertreter.DomicileStreetAddress;
                    anagrafic.Gender = GesetzlicherVertreter.Gender;
                    anagrafic.MobilePhone = GesetzlicherVertreter.Phone;
                    anagrafic.RegisteredOffice = "Comunix Substitution";

                    await AuthProvider.RegisterUser(newUser, anagrafic);

                    await AuthProvider.SetAnagrafic(anagrafic);

                    Anagrafic.GV_AUTH_Users_ID = newUser.ID;
                }
            }

            return true;
        }
        private List<MSG_Message_Parameters> GetDifferentFields(AUTH_Users_Anagrafic Start, AUTH_Users_Anagrafic End, AUTH_Users StartUser, AUTH_Users EndUser, Guid LANG_Language_ID)
        {
            var result = new List<MSG_Message_Parameters>();

            bool DifferentValue = false;

            string resultString = "<table>";

            resultString += "<tr><th>" + TextProvider.Get("ORG_TABLE_OLD_VALUE", LANG_Language_ID) + "</th><th>" + TextProvider.Get("ORG_TABLE_NEW_VALUE", LANG_Language_ID) + "</th></tr>";

            resultString += GetTableRow(Start.FirstName, End.FirstName, ref DifferentValue);
            resultString += GetTableRow(Start.LastName, End.LastName, ref DifferentValue);
            resultString += GetTableRow(Start.FiscalNumber, End.FiscalNumber, ref DifferentValue);
            resultString += GetTableRow(Start.VatNumber, End.VatNumber, ref DifferentValue);
            resultString += GetTableRow(Start.Email, End.Email, ref DifferentValue);
            resultString += GetTableRow(Start.CountyOfBirth, End.CountyOfBirth, ref DifferentValue);
            resultString += GetTableRow(Start.PlaceOfBirth, End.PlaceOfBirth, ref DifferentValue);

            string StartDateOfBirth = "";
            string EndDateOfBirth = "";

            if (Start.DateOfBirth != null)
            {
                StartDateOfBirth = Start.DateOfBirth.Value.ToString("dd.MM.yyyy");

            }
            if (End.DateOfBirth != null)
            {
                EndDateOfBirth = End.DateOfBirth.Value.ToString("dd.MM.yyyy");
            }

            resultString += GetTableRow(StartDateOfBirth, EndDateOfBirth, ref DifferentValue);

            resultString += GetTableRow(Start.DomicileNation, End.DomicileNation, ref DifferentValue);
            resultString += GetTableRow(Start.DomicilePostalCode, End.DomicilePostalCode, ref DifferentValue);
            resultString += GetTableRow(Start.DomicileProvince, End.DomicileProvince, ref DifferentValue);
            resultString += GetTableRow(Start.DomicileStreetAddress, End.DomicileStreetAddress, ref DifferentValue);
            resultString += GetTableRow(Start.MobilePhone, End.MobilePhone, ref DifferentValue);
            resultString += GetTableRow(Start.PECEmail, End.PECEmail, ref DifferentValue);
            resultString += GetTableRow(Start.CodiceDestinatario, End.CodiceDestinatario, ref DifferentValue);
            resultString += GetTableRow(Start.Phone, End.Phone, ref DifferentValue);

            if (CompanyTypeList != null)
            {
                var startCompanyType = CompanyTypeList.FirstOrDefault(p => p.ID == StartUser.AUTH_Company_Type_ID);
                var EndCompanyType = CompanyTypeList.FirstOrDefault(p => p.ID == EndUser.AUTH_Company_Type_ID);

                if (startCompanyType != null && startCompanyType.TEXT_System_Texts_Code != null && EndCompanyType != null && EndCompanyType.TEXT_System_Texts_Code != null)
                {
                    resultString += GetTableRow(TextProvider.Get(startCompanyType.TEXT_System_Texts_Code, LANG_Language_ID), TextProvider.Get(EndCompanyType.TEXT_System_Texts_Code, LANG_Language_ID), ref DifferentValue);
                }
            }

            if (CompanyLegalFormList != null)
            {
                var startCompanyLF = CompanyLegalFormList.FirstOrDefault(p => p.ID == StartUser.AUTH_Company_LegalForm_ID);
                var EndCompanyLF = CompanyLegalFormList.FirstOrDefault(p => p.ID == EndUser.AUTH_Company_LegalForm_ID);

                if (startCompanyLF != null && startCompanyLF.Text_SystemText_Code != null && EndCompanyLF != null && EndCompanyLF.Text_SystemText_Code != null)
                {
                    resultString += GetTableRow(TextProvider.Get(startCompanyLF.Text_SystemText_Code, LANG_Language_ID), TextProvider.Get(EndCompanyLF.Text_SystemText_Code, LANG_Language_ID), ref DifferentValue);
                }
            }

            resultString += "</table>";

            if (DifferentValue)
            {
                result.Add(new MSG_Message_Parameters() { Code = "{ChangedData}", Message = resultString });
            }

            return result;
        }
        private string? GetTableRow(string? StartValue, string? EndValue, ref bool Changed)
        {
            if (StartValue == null)
                StartValue = "";
            if (EndValue == null)
                EndValue = "";

            if (StartValue != EndValue)
            {
                Changed = true;
                return "<tr><td>" + StartValue + "</td><td>" + EndValue + "</td></tr>";
            }

            return null;
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
        private async Task<DataEnvelope<META_IstatComuni>> GetRemoteMunicipalities(ComboBoxReadEventArgs args)
        {
            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            var result = await MunicipalitiesList.ToDataSourceResultAsync(args.Request);

            var dataToReturn = new DataEnvelope<META_IstatComuni>
            {
                Data = result.Data.Cast<META_IstatComuni>().ToList(),
                Total = result.Total
            };

            args.Data = result.Data;
            args.Total = result.Total;

            return await Task.FromResult(dataToReturn);
        }
        private async Task<META_IstatComuni> GetModelFromValue(Guid selectedValue)
        {
            if (MunicipalitiesList == null)
            {
                MunicipalitiesList = await MetaProvider.GetMunicipalities();
            }

            return MunicipalitiesList.FirstOrDefault(p => p.ID == selectedValue);
        }
        private async void UpdateAddressData(Guid Municipality_ID)
        {
            var data = await MetaProvider.GetMunicipality(Municipality_ID);

            if (data != null)
            {
                Anagrafic.DomicilePostalCode = data.Cap;
                Anagrafic.DomicileMunicipality = data.NameDE;
                Anagrafic.DomicileProvince = data.RegionCity;
                Anagrafic.DomicileNation = "IT";

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

            if (data != null && GesetzlicherVertreter != null)
            {
                GesetzlicherVertreter.DomicilePostalCode = data.Cap;
                GesetzlicherVertreter.DomicileMunicipality = data.NameDE;
                GesetzlicherVertreter.DomicileProvince = data.RegionCity;
                GesetzlicherVertreter.DomicileNation = "IT";

                StateHasChanged();
            }
        }
        
        private void BolloFreeChangedHandler(object value)
        {
            bool val = (bool)value;
            if (!val)
            {
                Anagrafic.AUTH_BolloFree_Reason_ID = null;
            }
        }
    }
}