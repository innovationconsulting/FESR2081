using ICWebApp.DataStore.PagoPA.Classes;
using ICWebApp.DataStore.PagoPA.Interface;
using ICWebApp.DataStore.MSSQL.Interfaces;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Web;
using ICWebApp.Application.Helper;
using ICWebApp.Application.Settings;
using ICWebApp.DataStore.MSSQL.Repositories;
using ICWebApp.Application.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Application.Interface.Services;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ICWebApp.Controller
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
    public class PagoPAController : ControllerBase
    {
        private IPagoPaRepository _pagoPARepository;
        private IUnitOfWork _unitOfWork;
        private IMessageService _msgService;

        public PagoPAController(IPagoPaRepository _pagoPARepository, IUnitOfWork _unitOfWork, IMessageService _msgService)
        {
            this._pagoPARepository = _pagoPARepository;
            this._unitOfWork = _unitOfWork;
            this._msgService = _msgService;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Notification(string Family_ID, string ReturnUrl, string buffer)
        {
            if (string.IsNullOrEmpty(buffer) && ReturnUrl != null && ReturnUrl.ToLower().Contains("?buffer="))
            {
                try
                {
                    buffer = ReturnUrl.Split("?")[1].Replace("buffer=", "");
                }
                catch { }
            }
            if (string.IsNullOrEmpty(buffer))
            {
                buffer = HttpContext.Request.Query["buffer"];
            }

            try
            {
                var notificaitonlog = new PAY_PagoPA_Log();

                notificaitonlog.ID = Guid.NewGuid();
                notificaitonlog.CreationDate = DateTime.Now;
                notificaitonlog.XMLData = buffer + " || " + ReturnUrl;
                notificaitonlog.CallType = "Notification Incoming";
                notificaitonlog.FamilyID = Family_ID;
                notificaitonlog.Incoming = false;

                await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(notificaitonlog);
            }
            catch { }


            if (!string.IsNullOrEmpty(buffer))
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

                            if(text != null)
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

                var conf = await _unitOfWork.Repository<CONF_PagoPA>().FirstOrDefaultAsync(p => p.AUTH_Municipality_ID == MunicipalityID);

                if (conf != null)
                {
                    CoreCS cs = new CoreCS();

                    var pid = buffer;

                    var handshakeResult = await _pagoPARepository.NotificationHandshake(conf.BaseAddressIT, conf.IV, conf.Key, conf.PortaleID, pid, true);

                    if (handshakeResult != null && !string.IsNullOrEmpty(handshakeResult.Esito) && handshakeResult.Esito.ToLower() == "ok")
                    {
                        var Successlog = new PAY_PagoPA_Log();

                        Successlog.ID = Guid.NewGuid();
                        Successlog.CreationDate = DateTime.Now;
                        Successlog.XMLData = handshakeResult.CommitXml;
                        Successlog.FamilyID = Family_ID;
                        Successlog.PaymentData = handshakeResult.PaymentData;
                        Successlog.Esito = handshakeResult.Esito;
                        Successlog.CallType = "Notification";
                        Successlog.Incoming = false;

                        await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(Successlog);

                        try
                        {
                            var transactions = await _unitOfWork.Repository<PAY_Transaction>().Where(p => p.Family_ID == Guid.Parse(Family_ID)).ToListAsync();

                            if (transactions != null && transactions.Where(p => p.PaymentDate == null).Count() > 0)
                            {
                                foreach (var trans in transactions)
                                {
                                    if (trans != null)
                                    {
                                        trans.PagoPANotificationDate = DateTime.Now;
                                        trans.PaymentDate = DateTime.Now;

                                        if (handshakeResult != null)
                                        {
                                            trans.PagoPANotificationValue = handshakeResult.Esito;
                                        }

                                        await _unitOfWork.Repository<PAY_Transaction>().InsertOrUpdateAsync(trans);
                                    }
                                }

                                var MessageParameters = new List<MSG_Message_Parameters>();

                                if (transactions != null && transactions.Count() > 0 && transactions.Sum(p => p.TotalAmount) != null && transactions.FirstOrDefault().NotificationSendDate == null)
                                {
                                    MessageParameters.Add(new MSG_Message_Parameters()
                                    {
                                        Code = "{Totale}",
                                        Message = String.Format("{0:C}", transactions.Sum(p => p.TotalAmount).Value)
                                    });

                                    MessageParameters.Add(new MSG_Message_Parameters()
                                    {
                                        Code = "{Description}",
                                        Message = transactions.FirstOrDefault().Description
                                    });

                                    if (transactions.FirstOrDefault() != null && transactions.FirstOrDefault().PaymentDate != null)
                                    {
                                        MessageParameters.Add(new MSG_Message_Parameters()
                                        {
                                            Code = "{PaymentDate}",
                                            Message = transactions.FirstOrDefault().PaymentDate.Value.ToString("dd.MM.yyyy - HH:mm")
                                        });
                                    }
                                    else
                                    {
                                        MessageParameters.Add(new MSG_Message_Parameters()
                                        {
                                            Code = "{PaymentDate}",
                                            Message = DateTime.Now.ToString("dd.MM.yyyy - HH:mm")
                                        });
                                    }

                                    if (transactions.FirstOrDefault() != null && transactions.FirstOrDefault().AUTH_Users_ID != null)
                                    {
                                        var user = await _unitOfWork.Repository<AUTH_Users_Anagrafic>().FirstOrDefaultAsync(p => p.AUTH_Users_ID == transactions.FirstOrDefault().AUTH_Users_ID.Value);

                                        if (user != null)
                                        {
                                            MessageParameters.Add(new MSG_Message_Parameters()
                                            {
                                                Code = "{Fullname}",
                                                Message = user.FirstName + " " + user.LastName
                                            });
                                            MessageParameters.Add(new MSG_Message_Parameters()
                                            {
                                                Code = "{FiscalCode}",
                                                Message = user.FiscalNumber
                                            });
                                        }

                                        var msg = await GetTransactionSucceededMessage(transactions.FirstOrDefault());

                                        if (msg != null)
                                        {
                                            await _msgService.SendMessage(msg);
                                        }

                                        foreach(var trans in transactions)
                                        {
                                            trans.NotificationSendDate = DateTime.Now;

                                            await _unitOfWork.Repository<PAY_Transaction>().InsertOrUpdateAsync(trans);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var logError = new PAY_PagoPA_Log();

                            logError.ID = Guid.NewGuid();
                            logError.CreationDate = DateTime.Now;
                            logError.XMLData = ex.Message;
                            logError.CallType = "Notification - Error on Set Payed";
                            logError.FamilyID = Family_ID;
                            logError.Incoming = false;

                            await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logError);
                        }

                        return Content(handshakeResult.CommitXml);
                    }
                    else
                    {
                        var logNotOK = new PAY_PagoPA_Log();

                        logNotOK.ID = Guid.NewGuid();
                        logNotOK.CreationDate = DateTime.Now;
                        if (handshakeResult != null)
                        {
                            logNotOK.XMLData = handshakeResult.CommitXml;
                            logNotOK.Esito = handshakeResult.Esito;
                            logNotOK.PaymentData = handshakeResult.PaymentData;
                        }
                        logNotOK.FamilyID = Family_ID;
                        logNotOK.CallType = "Notification Not OK";
                        logNotOK.Incoming = false;

                        await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logNotOK);

                        try
                        {
                            var transactions = await _unitOfWork.Repository<PAY_Transaction>().Where(p => p.Family_ID == Guid.Parse(Family_ID)).ToListAsync();

                            if (transactions != null)
                            {
                                foreach (var trans in transactions)
                                {
                                    if (trans != null && trans.PagoPANotificationValue != "OK")
                                    {
                                        trans.PagoPANotificationDate = DateTime.Now;

                                        if (handshakeResult != null)
                                        {
                                            trans.PagoPANotificationValue = handshakeResult.Esito;
                                        }

                                        await _unitOfWork.Repository<PAY_Transaction>().InsertOrUpdateAsync(trans);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var logError = new PAY_PagoPA_Log();

                            logError.ID = Guid.NewGuid();
                            logError.CreationDate = DateTime.Now;
                            logError.XMLData = ex.Message;
                            logError.CallType = "Notification - Error on Set Payed";
                            logError.FamilyID = Family_ID;
                            logError.Incoming = false;

                            await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logError);
                        }
                                
                        if (handshakeResult != null)
                        {
                            return Content(handshakeResult.CommitXml);
                        }
                    }
                }
            }

            var log = new PAY_PagoPA_Log();

            log.ID = Guid.NewGuid();
            log.CreationDate = DateTime.Now;
            log.XMLData = "Error on Notification - Buffer Empty";
            log.FamilyID = Family_ID;
            log.CallType = "Notification";
            log.Incoming = false;

            await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(log);

            return Content("Error");
        }
        public async Task<IActionResult> Success(string Family_ID, string ReturnUrl, string buffer, string? esitopa = null)
        {
            try
            {
                var logEx = new PAY_PagoPA_Log();

                logEx.ID = Guid.NewGuid();
                logEx.CreationDate = DateTime.Now;
                logEx.XMLData = ReturnUrl;
                logEx.CallType = "PagoPa Return Call";
                logEx.FamilyID = Family_ID;
                logEx.Incoming = false;

                await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logEx);
            }
            catch { }

            if (!string.IsNullOrEmpty(ReturnUrl) && esitopa != "ERROR") 
            {
                try
                {
                    if (ReturnUrl.Contains("?Buffer="))
                    {
                        try
                        {
                            buffer = ReturnUrl.Split("?")[1].Replace("buffer=", "");
                        }
                        catch { }
                    }
                    if (string.IsNullOrEmpty(buffer))
                    {
                        try
                        {
                            buffer = HttpContext.Request.Query["buffer"];
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(buffer))
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
                        var CurrentMunicipality = result.Where(p => p.Prefix != null && Request.Host.Value.ToLower().Contains(p.Prefix.ToLower())).FirstOrDefault();

                        if (CurrentMunicipality != null && CurrentMunicipality.AUTH_Municipality_ID != null)
                        {
                            MunicipalityID = CurrentMunicipality.AUTH_Municipality_ID.Value;
                        }
                        else if (Request.Host.Value.Contains("localhost"))
                        {
                            MunicipalityID = ComunixSettings.TestMunicipalityID;
                        }

                        var munlog = new PAY_PagoPA_Log();

                        munlog.ID = Guid.NewGuid();
                        munlog.CreationDate = DateTime.Now;
                        if (MunicipalityID != null)
                        {
                            munlog.XMLData = MunicipalityID.ToString() + " || " + ReturnUrl;
                        }
                        munlog.FamilyID = Family_ID;
                        munlog.CallType = "Pago Pa Return Call - Municipaity";
                        munlog.Incoming = false;

                        await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(munlog);

                        var conf = await _unitOfWork.Repository<CONF_PagoPA>().FirstOrDefaultAsync(p => p.AUTH_Municipality_ID == MunicipalityID);

                        if (conf != null)
                        {
                            CoreCS cs = new CoreCS();

                            var pid = buffer; 

                            var handshakeResult = await _pagoPARepository.NotificationHandshake(conf.BaseAddressIT, conf.IV, conf.Key, conf.PortaleID, pid, true);

                            if (handshakeResult != null && !string.IsNullOrEmpty(handshakeResult.Esito) && handshakeResult.Esito.ToLower() == "ok")
                            {
                                var Successlog = new PAY_PagoPA_Log();

                                Successlog.ID = Guid.NewGuid();
                                Successlog.CreationDate = DateTime.Now;
                                Successlog.XMLData = handshakeResult.CommitXml + " || " + ReturnUrl;
                                Successlog.PaymentData = handshakeResult.PaymentData;
                                Successlog.Esito = handshakeResult.Esito;
                                Successlog.CallType = "Success";
                                Successlog.FamilyID = Family_ID;
                                Successlog.Incoming = false;

                                await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(Successlog);

                                return LocalRedirect("/PaymentSuccess/" + Family_ID + "/" + Uri.EscapeDataString(ReturnUrl));
                            }
                            else
                            {

                                var logEx = new PAY_PagoPA_Log();

                                logEx.ID = Guid.NewGuid();
                                logEx.CreationDate = DateTime.Now;
                                if (handshakeResult != null)
                                {
                                    logEx.XMLData = handshakeResult.CommitXml;
                                    logEx.PaymentData = handshakeResult.PaymentData;
                                    logEx.Esito = handshakeResult.Esito;
                                }
                                logEx.FamilyID = Family_ID;
                                logEx.CallType = "Not OK";
                                logEx.Incoming = false;

                                await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logEx);
                            }
                        }
                    }
                    else
                    {
                        var logEx = new PAY_PagoPA_Log();

                        logEx.ID = Guid.NewGuid();
                        logEx.CreationDate = DateTime.Now;
                        logEx.XMLData = ReturnUrl.ToString();
                        logEx.FamilyID = Family_ID;
                        logEx.CallType = "Error - Buffer Empty";
                        logEx.Incoming = false;

                        await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logEx);
                    }
                }
                catch (Exception ex)
                {
                    var logEx = new PAY_PagoPA_Log();

                    logEx.ID = Guid.NewGuid();
                    logEx.CreationDate = DateTime.Now;
                    logEx.XMLData = ex.ToString();
                    logEx.FamilyID = Family_ID;
                    logEx.CallType = "Error";
                    logEx.Incoming = false;

                    await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(logEx);
                }
            }

            var log = new PAY_PagoPA_Log();

            log.ID = Guid.NewGuid();
            log.CreationDate = DateTime.Now;
            log.XMLData = "Error on Handshake, after Return Commit" + " || " + ReturnUrl;
            log.FamilyID = Family_ID;
            log.CallType = "Error";
            log.Incoming = false;

            await _unitOfWork.Repository<PAY_PagoPA_Log>().InsertOrUpdateAsync(log);

            try
            {
                var returnurl = ReturnUrl.Split("?")[0];

                return LocalRedirect("/PaymentError/" + Family_ID + "/" + Uri.EscapeDataString(returnurl));
            }
            catch { }

            return LocalRedirect("/PaymentError/" + Family_ID + "/Home");
        } 
        private async Task<MSG_Message?> GetTransactionSucceededMessage(PAY_Transaction? transaction)
        {
            try
            {
                if (transaction == null || transaction.AUTH_Users_ID == null || transaction.AUTH_Municipality_ID == null)
                    return null;
                var msg = await _msgService.GetMessage(transaction.AUTH_Users_ID.Value, transaction.AUTH_Municipality_ID.Value,
                "NOTIF_PAYMENT_SUCCESS_TEXT", "NOTIF_PAYMENT_SUCCESS_SHORTTEXT", "NOTIF_PAYMENT_SUCCESS_TITLE",
                Guid.Parse("dcd04015-c1bd-4ad5-99e6-aeef7f35bfa4"), true);
                var formCreatorUserId = transaction.AUTH_Users_ID.Value;
                if (msg != null)
                {
                    var settings = await _unitOfWork.Repository<AUTH_UserSettings>()
                        .FirstOrDefaultAsync(e => e.AUTH_UsersID == formCreatorUserId);
                    var lang = settings?.LANG_Languages_ID ?? LanguageSettings.German;
                    
                    var transactionType = await _unitOfWork.Repository<PAY_Type>().FirstOrDefaultAsync(p => p.ID == (transaction.PAY_Type_ID ?? Guid.Empty));
                    var transactionTypeName = "";
                    if (transactionType != null)
                    {
                        transactionTypeName = (await _unitOfWork.Repository<TEXT_SystemTexts>().FirstOrDefaultAsync(e =>
                            e.Code == transactionType.TEXT_SystemTexts_Code && e.LANG_LanguagesID == lang))?.Text ?? "";
                    }
                    
                    msg.Messagetext = msg.Messagetext.Replace("{PayType}", transactionTypeName);
                    msg.Messagetext = msg.Messagetext.Replace("{TotalAmount}", transaction.TotalAmount?.ToString("C"));
                    msg.Messagetext = msg.Messagetext.Replace("{PaymentDate}", DateTime.Now.ToString("dd.MM.yyyy - HH:mm"));

                    var positionsString = "<table class='task-table'>{PositionContent}</table>";
                    var positions = await _unitOfWork.Repository<PAY_Transaction_Position>().Where(p => p.PAY_Transaction_ID == transaction.ID).ToListAsync();
                    var rows = "";
                    foreach (var pos in positions)
                    {
                        var row = "<tr><td style='padding-right: 40px'>{desc}</td><td valign='top' align='right'>{amount}</td></tr>";
                        row = row.Replace("{desc}", pos.Description);
                        row = row.Replace("{amount}", pos.Amount?.ToString("C"));
                        rows += row;
                    }
                    positionsString = positionsString.Replace("{PositionContent}", rows);
                    msg.Messagetext = msg.Messagetext.Replace("{PositionsTable}", positionsString);
                }

                return msg;
            }
            catch
            {
                return null;
            }
        }
    }
}
