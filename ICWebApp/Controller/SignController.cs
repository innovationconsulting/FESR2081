using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Interface.Sessionless;
using ICWebApp.DataStore.MSSQL.Interfaces;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using ICWebApp.Application.Settings;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;

namespace ICWebApp.Controller
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
    public class SignController : ControllerBase
    {
        private ISignResponseService SignService;
        private ISignResponseSessionless SignServiceSessionless;
        private IFILEProvider FileProvider;
        private IUnitOfWork _unitOfWork;

        public SignController(ISignResponseService SignService, IFILEProvider FileProvider, ISignResponseSessionless SignServiceSessionless, IUnitOfWork _unitOfWork)
        {
            this.SignService = SignService;
            this.FileProvider = FileProvider;
            this.SignServiceSessionless = SignServiceSessionless;
            this._unitOfWork = _unitOfWork;
        }
        //https://comunix.bz.it/Sign/AgreementComplete
        public async Task<IActionResult> AgreementComplete()
        {
            try
            { 
                StreamReader reader = new StreamReader(Request.Body);
                string data = await reader.ReadToEndAsync();

                Response.Headers.Add("X-AdobeSign-ClientId", Request.Headers["X-AdobeSign-ClientId"].ToString());

                dynamic result = JsonConvert.DeserializeObject(data);

                FILE_Sign_Log Log = new FILE_Sign_Log();

                Log.ID = Guid.NewGuid();
                Log.Message = data;
                Log.Action = "AgreementCompleted";

                await FileProvider.SetLog(Log);

                if (result != null)
                {
                    if (result.agreement.id != null)
                    {
                        var AgreementID = (string)(result.agreement.id);

                        await FileProvider.SetLog(Log);

                        await SignService.SetSignedAgreement(AgreementID);

                    }
                }
            }
            catch (Exception ex)
            {
                FILE_Sign_Log Log = new FILE_Sign_Log();

                Log.ID = Guid.NewGuid();

                if (ex.InnerException != null) 
                {
                    Log.Message = ex.InnerException.ToString();
                } 

                Log.Message += "\n\n\n\n" + ex.StackTrace;
                Log.Action = "AgreementCompleted";
                Log.Error = ex.ToString();

                await FileProvider.SetLog(Log);
            }

            return Ok();
        }
        //https://comunix.bz.it/Sign/AgreementCreate
        public async Task<IActionResult> AgreementCreate()
        {
            try
            {
                StreamReader reader = new StreamReader(Request.Body);
                string data = await reader.ReadToEndAsync();

                Response.Headers.Add("X-AdobeSign-ClientId", Request.Headers["X-AdobeSign-ClientId"].ToString());

                dynamic result = JsonConvert.DeserializeObject(data);

                FILE_Sign_Log Log = new FILE_Sign_Log();

                Log.ID = Guid.NewGuid();
                Log.Message = data;
                Log.Action = "AgreementCreate";

                await FileProvider.SetLog(Log);

                if (result != null)
                {
                    if (result.agreement.id != null)
                    {
                        var AgreementID = (string)(result.agreement.id);

                        await SignService.SetAgreementCreated(AgreementID);

                    }
                }
            }
            catch (Exception ex)
            {
                FILE_Sign_Log Log = new FILE_Sign_Log();

                Log.ID = Guid.NewGuid();
                if (ex.InnerException != null)
                {
                    Log.Message = ex.InnerException.ToString();
                }

                Log.Message += "\n\n\n\n" + ex.StackTrace;
                Log.Action = "AgreementCreate";
                Log.Error = ex.ToString();

                await FileProvider.SetLog(Log);
            }

            return Ok();
        }
        //https://comunix.bz.it/Sign/AgreementComitted
        public async Task<IActionResult> AgreementComitted()
        {
            try
            {
                StreamReader reader = new StreamReader(Request.Body);
                string data = await reader.ReadToEndAsync();

                Response.Headers.Add("X-AdobeSign-ClientId", Request.Headers["X-AdobeSign-ClientId"].ToString());

                dynamic result = JsonConvert.DeserializeObject(data);

                FILE_Sign_Log Log = new FILE_Sign_Log();

                Log.ID = Guid.NewGuid();
                Log.Message = data;
                Log.Action = "AgreementComitted";

                await FileProvider.SetLog(Log);

                if (result != null)
                {
                    if (result.agreement.id != null && result.actingUserEmail != null)
                    {
                        var AgreementID = (string)(result.agreement.id);
                        var UserMail = (string)(result.actingUserEmail);

                        await SignService.SetAgreementComitted(AgreementID, UserMail);

                    }
                }
            }
            catch (Exception ex)
            {
                FILE_Sign_Log Log = new FILE_Sign_Log();

                Log.ID = Guid.NewGuid();
                if (ex.InnerException != null)
                {
                    Log.Message = ex.InnerException.ToString();
                }

                Log.Message += "\n\n\n\n" + ex.StackTrace;
                Log.Action = "AgreementComitted";
                Log.Error = ex.ToString();

                await FileProvider.SetLog(Log);
            }

            return Ok();
        }
        public async Task<IActionResult> ApiResponse()
        {
            try
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


                var conf = await _unitOfWork.Repository<CONF_Sign>().FirstOrDefaultAsync(p => p.AUTH_Municipality_ID == MunicipalityID);

                if (conf != null)
                {
                    StreamReader reader = new StreamReader(Request.Body);
                    string data = await reader.ReadToEndAsync();

                    string code = Request.Query["code"];
                    string state = Request.Query["state"];

                    if(!string.IsNullOrEmpty(state) && conf.State != null)
                    {
                        if (state == conf.State)
                        {
                            if (code != null && code != "")
                            {
                                var tokenresult = await SignServiceSessionless.SetAccessToken(MunicipalityID, (string)code);
                                return Content("Tokens erfolgreich gesetzt!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }

            return Content("Error!");
        }
    }
}
