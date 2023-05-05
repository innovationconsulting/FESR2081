using Blazored.SessionStorage;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Services;
using ICWebApp.Application.Settings;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SendGrid.Helpers.Mail;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ICWebApp.Controller
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
    public class SpidController : ControllerBase
    {
        private IAUTHProvider _AuthProvider;
        private ISPIDService _SpidService;
        private NavigationManager _navigationManager;
        private IUnitOfWork _unitOfWork;
        public SpidController(ISPIDService _SpidService, IAUTHProvider _AuthProvider, NavigationManager _navigationManager, IUnitOfWork _unitOfWork)
        {
            this._AuthProvider = _AuthProvider;
            this._navigationManager = _navigationManager;
            this._SpidService = _SpidService;
            this._unitOfWork = _unitOfWork;
        }

        public async Task<IActionResult> Return()
        {
            var token = Request.Query["SpidToken"][0];

            if (token != null) 
            {
                var prefixes = await _unitOfWork.Repository<AUTH_Municipality>().ToListAsync();
                var result = new List<MunicipalityDomainSelectableItem>();
                var Languages = await _unitOfWork.Repository<LANG_Languages>().ToListAsync();

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        foreach (var d in prefixes)
                        {
                            var text = _unitOfWork.Repository<TEXT_SystemTexts>().FirstOrDefault(p => p.Code == d.Prefix_Text_SystemTexts_Code && p.LANG_LanguagesID == l.ID);

                            if (text != null)
                            {
                                result.Add(new MunicipalityDomainSelectableItem()
                                {
                                    Prefix = text.Text,
                                    AUTH_Municipality_ID = d.ID
                                });
                            }
                        }
                    }
                }

                Guid MunicipalityID = Guid.Empty;
                var CurrentMunicipality = result.Where(p => p.Prefix != null && Request.Host.Value.Contains(p.Prefix)).FirstOrDefault();

                if (CurrentMunicipality != null && CurrentMunicipality.AUTH_Municipality_ID != null)
                {
                    MunicipalityID = CurrentMunicipality.AUTH_Municipality_ID.Value;
                }
                else if (Request.Host.Value.Contains("localhost"))
                {
                    MunicipalityID = ComunixSettings.TestMunicipalityID;
                }

                _SpidService.SetLog(MunicipalityID, "Login Token Response", token);

                var profileData = await _SpidService.GetUserData(MunicipalityID, token); 
                
                if (profileData != null)
                {
                    var LoginToken = Guid.NewGuid();
                    var externalVerif = new AUTH_External_Verification();

                    externalVerif.FirstName = profileData.name;
                    externalVerif.LastName = profileData.familyName;

                    if (profileData.fiscalNumber != null)
                    {
                        externalVerif.FiscalNumber = profileData.fiscalNumber.Replace("TINIT-", "");
                    }

                    externalVerif.PlaceOfBirth = profileData.placeOfBirth;
                    externalVerif.CountyOfBirth = profileData.countyOfBirth;

                    if (profileData.dateOfBirth != null)
                    {
                        externalVerif.DateOfBirth = DateTime.Parse(profileData.dateOfBirth);
                    }

                    externalVerif.Gender = profileData.gender;
                    externalVerif.Email = profileData.email;
                    externalVerif.MobilePhone = profileData.mobilePhone;
                    externalVerif.Address = profileData.address;
                    externalVerif.RegisteredOffice = profileData.registeredOffice;
                    externalVerif.SpidCode = profileData.spidCode;
                    externalVerif.IvaCode = profileData.ivaCode;
                    externalVerif.IDCard = profileData.idCard;
                    externalVerif.DomicileStreetAddress = profileData.domicileStreetAddress;
                    externalVerif.DomicileMunicipality = profileData.domicileMunicipality;
                    externalVerif.DomicilePostalCode = profileData.domicilePostalCode;
                    externalVerif.DomicileProvince = profileData.domicileProvince;
                    externalVerif.DomicileNation = profileData.domicileNation;

                    if (profileData.expirationDate != null)
                    {
                        externalVerif.ExpirationDate = DateTime.Parse(profileData.expirationDate);
                    }

                    externalVerif.ID = Guid.NewGuid();
                    externalVerif.CreatedAt = DateTime.Now;
                    externalVerif.Timeout = DateTime.Now.AddSeconds(30);
                    externalVerif.LoginToken = LoginToken;

                    await _AuthProvider.SetVerification(externalVerif);

                    var FiscalNumber = externalVerif.FiscalNumber;

                    _SpidService.SetLog(MunicipalityID, "Login Comunix Redirect", token);

                    return LocalRedirect("/Login/" + MunicipalityID + "/" + FiscalNumber + "/" + LoginToken);
                }
            }

            return LocalRedirect("/LoginError/SPID_DEFAULT_ERROR");
        }
        public async Task<IActionResult> InternalSuccess(Guid VeriffID)
        {
            var externalVerif = await _AuthProvider.GetVerification(VeriffID);

            if (externalVerif != null)
            {
                externalVerif.LoginToken = Guid.NewGuid();
                externalVerif.Timeout = DateTime.Now.AddSeconds(30);

                externalVerif = await _AuthProvider.SetVerification(externalVerif);

                if (externalVerif != null)
                {
                    return LocalRedirect("/Login/" + externalVerif.FiscalNumber + "/" + externalVerif.LoginToken);
                }
            }

            return LocalRedirect("/LoginError/SPID_DEFAULT_ERROR");
        }
        [AllowAnonymous]
        public async Task<IActionResult> SpidError()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            string errorMessage = string.Empty;

            if (exceptionHandlerPathFeature?.Error != null)
            {
                var messages = FromHierarchy(exceptionHandlerPathFeature?.Error, ex => ex.InnerException)
                    .Select(ex => ex.Message)
                    .ToList();
                errorMessage = String.Join(" ", messages);
            }

            string ErrorCode = "DEFAULT_ERROR";

            if (errorMessage.Contains("Autenticazione fallita per ripetuta sottomissione di credenziali errate (superato numero  tentativi secondo le policy adottate)"))
            {
                ErrorCode = "SPID_TO_MANY_TRYS";
            }
            else if (errorMessage.Contains("Utente privo di credenziali compatibili con il livello richiesto dal fornitore del servizio"))
            {
                ErrorCode = "SPID_REQUIRED_LEVEL_NOT_REACHED";
            }
            else if (errorMessage.Contains("Timeout durante l’autenticazione utente"))
            {
                ErrorCode = "SPID_TIMEOUT_ON_AUTHENTICATION";
            }
            else if (errorMessage.Contains("Utente nega il consenso all’invio di dati al SP in caso di sessione vigente"))
            {
                ErrorCode = "SPID_DATA_NO_CONSENT";
            }
            else if (errorMessage.Contains("Utente con identità sospesa/revocata o con credenziali bloccate"))
            {
                ErrorCode = "SPID_USER_BLOCKED";
            }
            else if (errorMessage.Contains("Processo di autenticazione annullato dall’utente"))
            {
                ErrorCode = "SPID_USER_CANCELED";
            }

            var externalError = new AUTH_External_Error();

            externalError.ID = Guid.NewGuid();
            externalError.CreationDate = DateTime.Now;
            externalError.ErrorMessage = errorMessage;
            externalError.ErrorCode = ErrorCode;

            await _AuthProvider.SetVerificationError(externalError);

            return LocalRedirect("/LoginError/" + ErrorCode);
        }
        private IEnumerable<TSource> FromHierarchy<TSource>(TSource source, Func<TSource, TSource> nextItem, Func<TSource, bool> canContinue)
        {
            for (var current = source; canContinue(current); current = nextItem(current))
            {
                yield return current;
            }
        }
        private IEnumerable<TSource> FromHierarchy<TSource>(TSource source, Func<TSource, TSource> nextItem)
            where TSource : class
        {
            return FromHierarchy(source, nextItem, s => s != null);
        }
    }
}
