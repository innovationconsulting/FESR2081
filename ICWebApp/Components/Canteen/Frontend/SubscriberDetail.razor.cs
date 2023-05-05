using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ICWebApp.Components.Canteen.Frontend
{
    public partial class SubscriberDetail
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ICANTEENProvider CanteenProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBreadCrumbService CrumbService { get; set; }
        [Inject] IMyCivisService MyCivisService { get; set; }
        [Inject] IMailerService MailerService { get; set; }
        [Inject] ISMSService SMSService { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }

        [Parameter] public Guid ID { get; set; }
        private V_CANTEEN_Subscriber? Subscriber;
        private CANTEEN_User? CurrentCanteenUser;
        private EmailInput? _EMailInput;
        private PhoneInput? _PhoneInput;
        private bool ShowEmailWindow = false;
        private bool ShowSMSWindow = false;
        private EditForm EditformSMS;
        private AUTH_Municipality? Municipality;

        protected override async Task OnInitializedAsync()
        {
            BusyIndicatorService.IsBusy = true;

            SessionWrapper.PageTitle = TextProvider.Get("MAINMENU_CANTEEN_SERVICE");
            SessionWrapper.PageSubTitle = TextProvider.Get("MAINMENU_CANTEEN_SERVICE_DESCRIPTION");

            CrumbService.ClearBreadCrumb();

            if (MyCivisService.Enabled == true)
            {
                CrumbService.AddBreadCrumb("/Canteen/MyCivis/Service", "MAINMENU_CANTEEN", null, null);
            }
            else
            {
                CrumbService.AddBreadCrumb("/Canteen/Service", "MAINMENU_CANTEEN", null, null);
            }
            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                Subscriber = await CanteenProvider.GetVSubscriber(ID);
                CurrentCanteenUser = CanteenProvider.GetCanteenUserByID(SessionWrapper.CurrentUser.ID, SessionWrapper.AUTH_Municipality_ID.Value);
                Municipality = await AuthProvider.GetMunicipality(SessionWrapper.AUTH_Municipality_ID.Value);
            }

            SessionWrapper.OnCurrentSubUserChanged += SessionWrapper_OnCurrentUserChanged;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void SessionWrapper_OnCurrentUserChanged()
        {
            if (SessionWrapper != null && SessionWrapper.CurrentUser != null)
            {
                BusyIndicatorService.IsBusy = true;

                if (MyCivisService.Enabled == true)
                {
                    NavManager.NavigateTo("/Canteen/MyCivis");
                }
                else
                {
                    NavManager.NavigateTo("/Canteen");
                }

                StateHasChanged();
            }
        }
        private void SendMailWithData()
        {
            _EMailInput = new EmailInput();

            ShowEmailWindow = true;
            StateHasChanged();
        }
        private void SendSmSWithData()
        {
            _PhoneInput = new PhoneInput();

            ShowSMSWindow = true;
            StateHasChanged();
        }
        private void OnSendEmail(V_CANTEEN_Subscriber? sub)
        {
            if (_EMailInput != null && Municipality != null && _EMailInput.Email != null && SessionWrapper.AUTH_Municipality_ID != null && sub != null && CurrentCanteenUser != null)
            {
                MSG_Mailer mail = new MSG_Mailer();

                mail.ToAdress = _EMailInput.Email;

                mail.Subject = TextProvider.Get("CANTEEN_PHONE_STATE_DATA_SUBJECT");

                mail.Body = TextProvider.Get("CANTEEN_PHONE_STATE_DATA_TEXT");
                mail.Body = mail.Body.Replace("{MunicipalPhone}", Municipality.PhoneCanteenManagement);

                if (CurrentCanteenUser.TelPin != null && CurrentCanteenUser.TelPin.Length > 7)
                {
                    mail.Body = mail.Body.Replace("{ParentPin}", CurrentCanteenUser.TelPin.Substring(0, 2) + " " + CurrentCanteenUser.TelPin.Substring(2, 2) + " " + CurrentCanteenUser.TelPin.Substring(4, 2) + " " + CurrentCanteenUser.TelPin.Substring(6, 2));
                }

                if (sub.TelCode != null && sub.TelCode.Length > 3)
                {
                    mail.Body = mail.Body.Replace("{ChildPin}", sub.TelCode.Substring(0, 2) + " " + sub.TelCode.Substring(2, 2));
                }

                mail.Body = mail.Body.Replace("{ChildName}", sub.FirstName + " " + sub.LastName);

                MailerService.SendMail(mail, null, SessionWrapper.AUTH_Municipality_ID.Value);
            }

            ShowEmailWindow = false;
            StateHasChanged();
        }
        private void OnSendSMS(V_CANTEEN_Subscriber? sub)
        {
            if (_PhoneInput != null && Municipality != null && _PhoneInput.Phone != null && SessionWrapper.AUTH_Municipality_ID != null && sub != null && CurrentCanteenUser != null)
            {
                MSG_SMS sms = new MSG_SMS();

                sms.PhoneNumber = _PhoneInput.Phone;

                sms.Message = TextProvider.Get("CANTEEN_PHONE_SMS_STATE_DATA_TEXT");
                sms.Message = sms.Message.Replace("{MunicipalPhone}", Municipality.PhoneCanteenManagement);

                if (CurrentCanteenUser.TelPin != null && CurrentCanteenUser.TelPin.Length > 7)
                {
                    sms.Message = sms.Message.Replace("{ParentPin}", CurrentCanteenUser.TelPin.Substring(0, 2) + " " + CurrentCanteenUser.TelPin.Substring(2, 2) + " " + CurrentCanteenUser.TelPin.Substring(4, 2) + " " + CurrentCanteenUser.TelPin.Substring(6, 2));
                }

                if (sub.TelCode != null && sub.TelCode.Length > 3)
                {
                    sms.Message = sms.Message.Replace("{ChildPin}", sub.TelCode.Substring(0, 2) + " " + sub.TelCode.Substring(2, 2));
                }

                sms.Message = sms.Message.Replace("{ChildName}", sub.FirstName + " " + sub.LastName);

                SMSService.SendSMS(sms, SessionWrapper.AUTH_Municipality_ID.Value);
            }

            ShowSMSWindow = false;
            StateHasChanged();
        }
        private void BackToPrevious()
        {
            BusyIndicatorService.IsBusy = true;

            if (MyCivisService.Enabled)
            {
                NavManager.NavigateTo("/Canteen/MyCivis/Service");
            }
            else
            {
                NavManager.NavigateTo("/Canteen/Service");
            }

            StateHasChanged();
        }
    }
}
