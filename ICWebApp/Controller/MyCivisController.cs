using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Settings;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RestSharp;
using ICWebApp.Domain.Models.MyCivis;
using System.Drawing.Text;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Authentication;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Localization;
using ICWebApp.Domain.Models.Spid;

namespace ICWebApp.Controller
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
    public class MyCivisController : ControllerBase
    {
        private IAUTHProvider _AuthProvider;
        private ICONFProvider _ConfProvider;
        private IUnitOfWork _UnitOfWork;
        private ITEXTProvider _TextProvider;

        public MyCivisController(IAUTHProvider _AuthProvider, ICONFProvider _ConfProvider, IUnitOfWork _UnitOfWork, ITEXTProvider _TextProvider)
        {
            this._AuthProvider = _AuthProvider;
            this._ConfProvider = _ConfProvider;
            this._UnitOfWork = _UnitOfWork;
            this._TextProvider = _TextProvider;
        }        
        [HttpGet("/services")]
        public async Task<IActionResult> Services()
        {
            var municipality = await GetMunicipality();

            if (municipality != null && municipality.AUTH_Municipality_ID != null)
            {
                var services = await _ConfProvider.GetMyCivisConfiguration(municipality.AUTH_Municipality_ID.Value);

                if (services != null)
                {
                    foreach (var service in services)
                    {
                        var authenticated = await HandleAuthentication(service);

                        if (authenticated != null && authenticated.Succeeded != true)
                        {
                            if (authenticated.Failure != null)
                            {
                                return Content(authenticated.Failure.Message);
                            }

                            return Content("Error");
                        }
                    }
                }

                if (services != null && services.Count() > 0)
                {
                    var serviceDtoList = new List<ServiceDto>();

                    foreach(var service in services)
                    {
                        serviceDtoList.Add(new ServiceDto()
                        {
                            ServiceUid = service.ServiceUid,
                            Name = new MultiLang()
                            {
                                It = service.ServiceName_IT,
                                De = service.ServiceName_DE
                            },
                            Url = new MultiLang()
                            {
                                It = service.Url_IT,
                                De = service.Url_DE
                            },
                            ValidFrom = service.ValidFrom,
                            ValidTill = service.ValidTill
                        });
                    }

                    var content = JsonSerializer.Serialize<List<ServiceDto>>(serviceDtoList);

                    return Content(content);
                }
            }

            return Content("");
        }
        [HttpGet("/services/uids")]
        public async Task<IActionResult> ServicesUids()
        {
            var municipality = await GetMunicipality();

            if (municipality != null && municipality.AUTH_Municipality_ID != null)
            {
                var services = await _ConfProvider.GetMyCivisConfiguration(municipality.AUTH_Municipality_ID.Value);

                if (services != null)
                {
                    foreach (var service in services)
                    {
                        var authenticated = await HandleAuthentication(service);

                        if (authenticated != null && authenticated.Succeeded != true)
                        {
                            if(authenticated.Failure != null)
                            {
                                return Content(authenticated.Failure.Message);
                            }

                            return Content("Error");
                        }
                    }
                }

                if (services != null && services.Count() > 0)
                {
                    var serviceDtoList = new List<string>();

                    foreach (var service in services)
                    {
                        serviceDtoList.Add(service.ServiceUid);
                    }

                    var content = JsonSerializer.Serialize<List<string>>(serviceDtoList);

                    return Content(content);
                }
            }

            return Content("");
        }
        [HttpGet("/services/positions")]
        public async Task<IActionResult> ServicesPositions(string lang, string? fiscalNumber = null, string? vatCode = null, string? iamPersonId = null, string? serviceUids = null)
        {
            var municipality = await GetMunicipality();

            if (municipality != null && lang != null && municipality.AUTH_Municipality_ID != null)
            {
                var services = await _ConfProvider.GetMyCivisConfiguration(municipality.AUTH_Municipality_ID.Value);

                if (services != null)
                {
                    foreach (var service in services)
                    {
                        var authenticated = await HandleAuthentication(service);

                        if (authenticated != null && authenticated.Succeeded != true)
                        {
                            if (authenticated.Failure != null)
                            {
                                return Content(authenticated.Failure.Message);
                            }

                            return Content("Error");
                        }
                    }
                }

                if (services != null && services.Where(p => serviceUids == null || serviceUids.Contains(p.ServiceUid)).Count() > 0)
                {
                    var serviceDtoList = new List<PositionDto>();

                    foreach (var service in services.Where(p => serviceUids == null || serviceUids.Contains(p.ServiceUid)))
                    {
                        var newPositionDto = new PositionDto();

                        newPositionDto.ServiceUid = service.ServiceUid;

                        if (lang.Contains("de")) 
                        {
                            newPositionDto.ServiceName = service.ServiceName_DE;
                            newPositionDto.ServiceUrl = service.Url_DE;
                            newPositionDto.ContentHtml = _TextProvider.Get("MYCIVIS_SERVICE_POSITION_DETAIL", LanguageSettings.German);
                        }
                        else
                        {
                            newPositionDto.ServiceName = service.ServiceName_IT;
                            newPositionDto.ServiceUrl = service.Url_IT;
                            newPositionDto.ContentHtml = _TextProvider.Get("MYCIVIS_SERVICE_POSITION_DETAIL", LanguageSettings.Italian);
                        }

                        newPositionDto.LogoUrl = "https://demo.comunix.bz.it/Content/Logo.png";



                        serviceDtoList.Add(newPositionDto);
                    }

                    var content = JsonSerializer.Serialize<List<PositionDto>>(serviceDtoList);

                    return Content(content);
                }
            }

            return Content("");
        }
        public async Task<IActionResult> Success(string token)
        {
            var municipality = await GetMunicipality();

            if (municipality != null)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    if (!Request.Host.Value.Contains("localhost"))
                    {
                        RestClient CommitClient = new RestClient("https://sso.civis.bz.it/");

                        CommitClient.AddDefaultHeader("User-Agent", "Comunix");

                        string CommitAddress = "/api/Auth/Profile/";

                        RestRequest CommitRequest = new RestRequest(CommitAddress + token, Method.GET);

                        var Commitresponse = await CommitClient.ExecuteAsync(CommitRequest, Method.GET);

                        System.IO.File.WriteAllText("D:/SPIDResponse.txt", Commitresponse.Content);

                        if (Commitresponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            dynamic? data = Newtonsoft.Json.JsonConvert.DeserializeObject(Commitresponse.Content);

                            if (data != null && data.owner != null)
                            {
                                var LoginToken = Guid.NewGuid();
                                var externalVerif = new AUTH_External_Verification();

                                externalVerif.FirstName = data.owner.firstname;
                                externalVerif.LastName = data.owner.lastname;
                                externalVerif.FiscalNumber = data.owner.fiscalCode;
                                externalVerif.PlaceOfBirth = data.owner.birthPlace.italianMunicipality.nameDe;

                                if (data.owner.BirthDate != null)
                                {
                                    externalVerif.DateOfBirth = DateTime.Parse(data.owner.birthDate.ToString());
                                }

                                externalVerif.Gender = data.owner.gender;

                                externalVerif.ID = Guid.NewGuid();
                                externalVerif.CreatedAt = DateTime.Now;
                                externalVerif.Timeout = DateTime.Now.AddSeconds(30);
                                externalVerif.LoginToken = LoginToken;
                                externalVerif.ServiceID = "74AFF5A9-A1C3-4D2B-B2E4-018A6BD5C5C3";
                                externalVerif.DomicileStreetAddress = data.owner.domicileStreetAddress;
                                externalVerif.DomicileMunicipality = data.owner.domicileMunicipality;
                                externalVerif.DomicilePostalCode = data.owner.domicilePostalCode;
                                externalVerif.DomicileProvince = data.owner.domicileProvince;
                                externalVerif.DomicileNation = data.owner.domicileNation;
                                externalVerif.RegisteredOffice = data.owner.registeredOffice;

                                await _AuthProvider.SetVerification(externalVerif);

                                var FiscalNumber = externalVerif.FiscalNumber;

                                var currentLang = await _UnitOfWork.Repository<LANG_Languages>().FirstOrDefaultAsync(p => p.ID == municipality.LANG_Language_ID);

                                if (currentLang != null)
                                {
                                    HttpContext.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(currentLang.Code, currentLang.Code)));
                                }

                                return LocalRedirect("/Login/" + municipality.AUTH_Municipality_ID + "/" + FiscalNumber + "/" + LoginToken + "/" + HttpUtility.UrlEncode("/Canteen/MyCivis/Service"));
                            }
                        }
                    }
                    else
                    {
                        return LocalRedirect("/Login/" + municipality.AUTH_Municipality_ID + "/ZNLMRK95M27B160L/D9812FDF-555F-42AE-B173-EF589C14F20B/" + HttpUtility.UrlEncode("/Canteen/MyCivis/Service"));
                    }
                }
            }

            return LocalRedirect("/LoginError/SPID_DEFAULT_ERROR");
        }
        private async Task<MunicipalityDomainSelectableItem?> GetMunicipality()
        {
            var prefixes = await _UnitOfWork.Repository<AUTH_Municipality>().ToListAsync();
            var result = new List<MunicipalityDomainSelectableItem>();
            var Languages = await _UnitOfWork.Repository<LANG_Languages>().ToListAsync();

            if (Languages != null)
            {
                foreach (var l in Languages)
                {
                    foreach (var d in prefixes)
                    {
                        var text = _UnitOfWork.Repository<TEXT_SystemTexts>().FirstOrDefault(p => p.Code == d.Prefix_Text_SystemTexts_Code && p.LANG_LanguagesID == l.ID);

                        if (text != null)
                        {
                            result.Add(new MunicipalityDomainSelectableItem()
                            {
                                Prefix = text.Text,
                                AUTH_Municipality_ID = d.ID,
                                LANG_Language_ID = text.LANG_LanguagesID
                            });
                        }
                    }
                }
            }

            var CurrentMunicipality = result.Where(p => p.Prefix != null && Request.Host.Value.Contains(p.Prefix)).FirstOrDefault();

            if (CurrentMunicipality != null)
            {
                return CurrentMunicipality;
            }
            else if (Request.Host.Value.Contains("localhost"))
            {
                return new MunicipalityDomainSelectableItem()
                {
                    Prefix = "localhost",
                    AUTH_Municipality_ID = ComunixSettings.TestMunicipalityID,
                    LANG_Language_ID = LanguageSettings.German
                }; 
            }

            return null;
        }
        private async Task<AuthenticateResult> HandleAuthentication(CONF_MyCivis Config)
        {
            Response.Headers.Add("WWW-Authenticate", "Basic");

            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Authorization header missing.");
            }

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var authHeaderRegex = new Regex(@"Basic (.*)");

            if (!authHeaderRegex.IsMatch(authorizationHeader))
            {
                return AuthenticateResult.Fail("Authorization code not formatted properly.");
            }

            var authBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderRegex.Replace(authorizationHeader, "$1")));
            var authSplit = authBase64.Split(Convert.ToChar(":"), 2);
            var authUsername = authSplit[0];
            var authPassword = authSplit.Length > 1 ? authSplit[1] : throw new Exception("Unable to get password");

            if (authUsername != Config.API_Username || authPassword != Config.API_Password)
            {
                return AuthenticateResult.Fail("The username or password is not correct.");
            }

            var authenticatedUser = new Classes.MyCivis.AuthenticatedUser("BasicAuthentication", true, authUsername);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(authenticatedUser));

            return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, "Comunix"));
        }
    }
}