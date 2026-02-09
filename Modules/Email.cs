using InstallationAPPNonUnify.Areas.CMS.Models;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace InstallationAPPNonUnify.Modules
{
    public class Email
    {
        private LanguagePackage _languageRescource;
        private Dictionary<string, string> _companyInfo;
        private string _companyName;
        private string _companyTel;
        private string _companyEmail;
        private string _companyWebsite;
        private string _companyWebsite2;
        private Dictionary<string, string> _smtpSetting;
        private string _mailAdmin;
        private string _mailAdminSender;
        private int _mailPort;
        private string _mailIp;
        private string _mailId;
        private string _mailPassword;
        private bool _testMode;
        private string _shortenLink;
        private string _organization;

        public bool IsSuccessed { get; set; }

        public Email(string countryCode = "en-US")
        {
            _languageRescource = new LanguagePackage(countryCode);
            GetInfo();
        }

        public void SendRequest(EmailUsViewModel euvm)
        {
            IsSuccessed = false;
            
            MailMessage message = new MailMessage(_mailAdminSender, euvm.EmailAddress.Replace(";", ","));
            string body = WriteRequestBody(euvm);
            string subject = _languageRescource.getContentWithNoPrefix("EmailTitle");
            if (SendEmail(message, subject, body)) {
                var emailArray = euvm.EmailAddress.Split(',',';');
                var sender = emailArray[0];
                subject = _languageRescource.getContentWithNoPrefix("EmailQueryTitle") + " " + DateTime.Now.ToString();
                //不能用客戶的Email當成寄件者寄信，因為這是偽造信件，所以還是以 _mailAdminSender 為寄件者
                //message = new MailMessage(sender, _mailAdmin.Replace(";", ","), subject, body);
                message = new MailMessage(_mailAdminSender, _mailAdmin.Replace(";", ","), subject, body);
                if (SendEmail(message, subject, body)) {
                    IsSuccessed = true;
                }
            }
        }

        public bool SendEmail(MailMessage message, string subject, string body, string attachment = "")
        {
            SmtpClient client = new SmtpClient();

            message.IsBodyHtml = true;
            message.Body = body;
            message.Subject = subject;

            if ( !string.IsNullOrEmpty(_mailId) && !string.IsNullOrEmpty(_mailPassword)) {
                client.Credentials = new System.Net.NetworkCredential(_mailId, _mailPassword);
            }
            
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = _mailIp;
            client.Port = _mailPort;

            if (_testMode) {
                client.EnableSsl = true;
            } else {
                client.UseDefaultCredentials = false;
            }

            if (!String.IsNullOrEmpty(attachment)) {
                var Mailattachment = new Attachment(attachment);
                Mailattachment.Name = System.IO.Path.GetFileName(attachment);
                Mailattachment.NameEncoding = Encoding.GetEncoding("utf-8");
                Mailattachment.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                // 設定該附件為一個內嵌附件(Inline Attachment)
                Mailattachment.ContentDisposition.Inline = true;
                Mailattachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                message.Attachments.Add(Mailattachment);
            }
           
            try
            {
                client.Send(message);
            }
            catch
            {
                return false ;
            }
            return true;
        }

        private void GetInfo()
        {
            BackendInfo bi = new BackendInfo();
            //company info
            _companyInfo = bi.ReadCompanyInfo();
            _companyInfo.TryGetValue("CompanyName", out _companyName);
            _companyInfo.TryGetValue("Tel", out _companyTel);
            _companyInfo.TryGetValue("Email", out _companyEmail);
            _companyInfo.TryGetValue("WebSite", out _companyWebsite);
            _companyInfo.TryGetValue("WebSite2", out _companyWebsite2);
            _companyInfo.TryGetValue("ShortenLink", out _shortenLink);

            //smtp setting
            _smtpSetting = bi.ReadSmtpInfo();
            _smtpSetting.TryGetValue("Admin", out _mailAdmin);
            _smtpSetting.TryGetValue("AdminSender", out _mailAdminSender);
            _smtpSetting.TryGetValue("Ip", out _mailIp);
            _smtpSetting.TryGetValue("Password", out _mailPassword);
            _smtpSetting.TryGetValue("Id", out _mailId);

            string tempString = string.Empty;
            _smtpSetting.TryGetValue("Port", out tempString);
            try
            {
                _mailPort = int.Parse(tempString);
            }
            catch
            {
                _mailPort = 25;
            }

            _testMode = bi.isTestMode;
            _organization = bi.GetSetting("Organization").ToLower();
        }

        private string WriteRequestBody(EmailUsViewModel euvm)
        {
            var body =
                _languageRescource.getContentWithNoPrefix("Dear") + " " + euvm.FullName + ",<br>"
                + _languageRescource.getContentWithNoPrefix("EmailContent1stPhrase")
                + "<br><b>" + _languageRescource.getContentWithNoPrefix("MachineDetail") + "</b><br>"
                + _languageRescource.getContentWithNoPrefix("Product") + " : " + euvm.Product + "<br>"
                + _languageRescource.getContentWithNoPrefix("Model") + " : " + euvm.Model + "<br>"
                + _languageRescource.getContentWithNoPrefix("SerialNumber") + " : " + euvm.SerialNumber + "<br><br>"
                + "<b>" + _languageRescource.getContentWithNoPrefix("RequestContext") + " : </b><br>"
                + euvm.RequestContext + "<br><br>"
                + "<b>" + _languageRescource.getContentWithNoPrefix("YourDetails") + "</b><br>"
                + _languageRescource.getContentWithNoPrefix("Company") + " : " + euvm.Company + "<br>"
                + _languageRescource.getContentWithNoPrefix("FullName") + " : " + euvm.FullName + "<br>"
                + _languageRescource.getContentWithNoPrefix("TelephoneNumber") + " : " + euvm.TelephoneNumber + "<br>"
                + _languageRescource.getContentWithNoPrefix("MobileNumber") + " : " + euvm.MobileNumber + "<br>"
                + _languageRescource.getContentWithNoPrefix("EmailAddress") + " : " + euvm.EmailAddress + "<br>"
                + _languageRescource.getContentWithNoPrefix("Address") + " : " + euvm.Address + "<br>"
                + _languageRescource.getContentWithNoPrefix("PostCode") + " : " + euvm.PostCode + "<br><br><br>";

            body = body + signature();  //簽名檔

            return body.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&apos;", "'");
        }

        public bool SendChangePasswordMail(ResetPasswordModel rpm)
        {
            IsSuccessed = false;

            MailMessage message = new MailMessage(_mailAdminSender, rpm.Email);
            string body = WriteResetPasswordBody(rpm);
            string subject = _languageRescource.getContentWithNoPrefix("ResetPasswordTitle");

            IsSuccessed = SendEmail(message, subject, body);

            return IsSuccessed;
        }

        public bool SendActivateAccountMail(ResetPasswordModel rpm)
        {
            IsSuccessed = false;

            MailMessage message = new MailMessage(_mailAdminSender, rpm.Email);
            string body = WriteActivateAccountBody(rpm);
            string subject = _languageRescource.getContentWithNoPrefix("ActivateAccountTitle");

            IsSuccessed = SendEmail(message, subject, body);

            return IsSuccessed;
        }

        private string WriteActivateAccountBody(ResetPasswordModel rpm)
        {
            string website = string.IsNullOrEmpty(_shortenLink) || string.IsNullOrWhiteSpace(_shortenLink) ? rpm.Website : _shortenLink;

            var body =
                _languageRescource.getContentWithNoPrefix("Hi") + " " + rpm.AccountName + ",<br><br>"
                + _languageRescource.getContentWithNoPrefix("ActivateAccountContent2") + "<br>"
                + _languageRescource.getContentWithNoPrefix("ActivateAccountContent3") + "<br><br>"
                + "<a href='" + rpm.Script + "'"
                + " style='background:#d6d9db; border:1px solid #6c757d; color:#3b4044; border-radius:30px; text-align:center; padding:12px; font-size:16px; display:inline-block; text-decoration-line:none;' "
                + ">" + _languageRescource.getContentWithNoPrefix("SetUpPassword")
                + "</a><br><br><br>"
                + _languageRescource.getContentWithNoPrefix("ActivateAccountContent4") + "<br>"
                + _languageRescource.getContentWithNoPrefix("ActivateAccountContent5")
                + " <a href='" + website + "'>" + website + "</a><br>"
                + _languageRescource.getContentWithNoPrefix("UserId") + ": " + rpm.PortalAccount + "<br><br>";

            body = body + signature();  //簽名檔

            return body.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&apos;", "'");
        }

        private string WriteResetPasswordBody(ResetPasswordModel rpm)
        {
            var body =
                _languageRescource.getContentWithNoPrefix("Hi") + " " + rpm.AccountName + ",<br><br>"
                + _languageRescource.getContentWithNoPrefix("ResetPasswordContent1") + "<br>"
                + _languageRescource.getContentWithNoPrefix("ResetPasswordContent2") + "<br><br>"
                + "<a href='" + rpm.Script + "'"
                + " style='background:#d6d9db; border:1px solid #6c757d; color:#3b4044; border-radius:30px; text-align:center; padding:12px; font-size:16px; display:inline-block; text-decoration-line:none;' "
                + ">" + _languageRescource.getContentWithNoPrefix("ResetPassword")
                + "</a><br><br><br>";

            body = body + signature();  //簽名檔

            return body.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&apos;", "'");
        }

        private string signature() { 
            string signature = string.Empty;

            switch(_organization){
                default:
                    signature =_languageRescource.getContentWithNoPrefix("Regarding") + "<br>"
                                + _companyName + "<br><br>"
                                + " <span> T: </span>" + _companyTel + "<br>"
                                + " <span> E: </span><a href='mailto:" + _companyEmail + "'> " + _companyEmail + "</a> <br>"
                                + " <span> W: </span><a href='" + _companyWebsite + "'>" + _companyWebsite + "</a> <br> ";

                    if (!string.IsNullOrEmpty(_companyWebsite2) || !string.IsNullOrWhiteSpace(_companyWebsite2))
                        signature = signature + " <span> W: </span><a href='" + _companyWebsite2 + "'>" + _companyWebsite2 + "</a> <br>";
                    break;
            }
            return signature;
        }

        public bool SendTestMail(SmtpSettingModel ssm, string type)
        {
            IsSuccessed = true;

            //載入資訊
            _mailAdmin = ssm.Admin;
            _mailAdminSender = ssm.AdminSender;
            _mailIp = ssm.Ip;
            _mailPassword = ssm.Password;
            _mailId = ssm.Id;
            _mailPort = int.Parse(ssm.Port);

            MailMessage message = new MailMessage();
            message.From = new MailAddress(_mailAdminSender);
            message.To.Add(_mailAdmin.Replace(";", ","));

            message.Subject = "Test Mail From " + _companyName;
            message.Body = "Send Time: " + DateTime.Now;
            message.IsBodyHtml = true;

            SmtpClient client = new SmtpClient();
            if (!string.IsNullOrEmpty(_mailId) && !string.IsNullOrEmpty(_mailPassword))
            {
                client.Credentials = new System.Net.NetworkCredential(_mailId, _mailPassword);
            }

            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = _mailIp;
            client.Port = _mailPort;

            if (type.ToLower().Equals("smtpsetting"))
            {
                client.UseDefaultCredentials = false;
            }
            else
            {
                client.EnableSsl = true;
            }

            try
            {
                client.Send(message);
            }
            catch(Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return false;
            }

            return IsSuccessed;
        }
    }
}