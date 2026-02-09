using InstallationAPPNonUnify.ReportService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.Configuration;
using System.Web.Script.Serialization;

namespace InstallationAPPNonUnify.Modules
{
    public static class ReportService
    {
        public static byte[] DownLoadPDF(string salesOrderId, string objectId, string requestDeliveryBy)
        {
            var baseDate = DateTime.Parse(requestDeliveryBy.Trim()).Date;
            var beforeDate = baseDate.AddDays(-1);
            var nextDate = baseDate.AddDays(1);
            ParameterValue[] para = new ParameterValue[5];
            para[0] = new ParameterValue { Name = "SalesOrderId", Value = salesOrderId };
            para[1] = new ParameterValue { Name = "ObjectId", Value = objectId };
            para[2] = new ParameterValue { Name = "RequestDeliveryBy", Value = requestDeliveryBy };
            para[3] = new ParameterValue { Name = "StartDate", Value = beforeDate.ToString("yyyy-MM-dd") };
            para[4] = new ParameterValue { Name = "EndDate", Value = nextDate.ToString("yyyy-MM-dd") };

            WebClient client = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            client.Headers.Add(HttpRequestHeader.ContentType, "text/plain");
            string userinfoJson = client.DownloadString(ConfigurationManager.AppSettings["CredentialWebApiUrl"]);
            //string userinfoJson = "9KWZ79U+p8KeZE3fuX022YsdRFBAjSrh/Iup8W7N/zKZ132YclWVWk+Q3pOhZWxu6SEk5/pTR2ZydqtB1kVSyr7BXbio8DEMOtpC9HTnstA=";
            SystemUserInfo usrInfo = new JavaScriptSerializer().Deserialize<SystemUserInfo>(Security.aesDecryptBase64(userinfoJson));
            NetworkCredential authority = new NetworkCredential(usrInfo.UserName, usrInfo.UserPwd, usrInfo.DomainName);

            return ReportExport("/JHTNLQAS_MSCRM/InstallConfirmation", para, "pdf", authority);
        }

        public static bool SendEmailbyCase(List<byte[]> pdfList, string NewName, string ClubManagerName, string salesOrderId, string objectId, string closureDate, string mail, string mail2)
        {
            JHT.myFileIO io = new JHT.myFileIO();
            byte[] mergeFile = io.MergePdfForms(pdfList);

            string strMonth = GetEnglishMonth(closureDate);
            closureDate = closureDate.Substring(0, 4) + "-" + strMonth + "-" + closureDate.Substring(closureDate.LastIndexOf("-") + 1, 2);
            string fileName = NewName + "_" + ClubManagerName + "_" + closureDate + ".pdf";

            if (mergeFile.Length > 0)
            {
                try
                {
                    string mailTo;
                    string cc;
                    string subject;

                    if (ConfigurationManager.AppSettings["TestMode"] == "Y")
                    {
                        mailTo = ConfigurationManager.AppSettings["TestModeEmail2"];
                        cc = string.Join(",", new[]
                        {
                            ConfigurationManager.AppSettings["TestModeEmail"],
                            ConfigurationManager.AppSettings["TestModeEmail3"],
                            string.IsNullOrWhiteSpace(mail2) ? null : mail2
                        }.Where(x => !string.IsNullOrWhiteSpace(x)));
                        
                        subject = ConfigurationManager.AppSettings["Subject"] + NewName + "-" + ClubManagerName + "-" + closureDate + " -Just Test, Please Ignore";
                    }
                    else
                    {
                        // 補充正式模式處理（若有）
                        mailTo = mail;
                        cc = "stanleychao@johnsonfitness.com";
                        subject = ConfigurationManager.AppSettings["Subject"] + NewName + "-" + ClubManagerName + "-" + closureDate + " -Just Test, Please Ignore";
                    }

                    string body = GetEmailBody();

                    MailSender mailMessage = new MailSender(ConfigurationManager.AppSettings["MailUserFrom"], mailTo, cc, "", subject, body, "", true);
                    mailMessage.Attachments(mergeFile, fileName);
                    //WriteLog(caseNo + " prepare to send mail.....");
                    mailMessage.Send();
                    //WriteLog(caseNo + " send mail successful!!!");
                }
                catch (Exception ex)
                {
                    MailSender errMail = new MailSender("stanleychao@johnsonfitness.com", "[NLQAS]-Send Mail Error", objectId + " Send mail error:" + ex.ToString());
                    errMail.Send();
                    return false;
                }
            }
            return true;
        }

        private static string GetEnglishMonth(string closureDate)
        {
            string month = closureDate.Substring(closureDate.IndexOf("-") + 1, 2);
            switch (month)
            {
                case "01": return "Jan";
                case "02": return "Feb";
                case "03": return "Mar";
                case "04": return "Apr";
                case "05": return "May";
                case "06": return "Jun";
                case "07": return "Jul";
                case "08": return "Aug";
                case "09": return "Sep";
                case "10": return "Oct";
                case "11": return "Nov";
                case "12": return "Dec";
                default: return "Unknown";
            }
        }

        private static string GetEmailBody()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">");
            sb.Append("<HTML><HEAD><META http-equiv=Content-Type content=\"text/html; charset=iso-8859-1\">");
            sb.Append("</HEAD><BODY><DIV><font face='Calibri'>Dear Sir/Madam, <BR><BR>");
            sb.Append("Please find the relevant order installation information attached for your reference. Thank you!. <BR>");
            sb.Append("Please refer to the attachment for more details. <BR><BR>");
            sb.Append("Attachment: <BR>");
            sb.Append("<BR>Regards,</font><BR><BR><font color='#FF0000'><b>Johnson Customer Service</b></font><BR><BR>");
            sb.Append("</DIV></BODY></HTML>");
            return sb.ToString();
        }

        public static byte[] ReportExport(string reportPath, ParameterValue[] para, string fileFormat, NetworkCredential authority)
        {
            byte[] result = null;
            try
            {
                ReportExecutionService rs = new ReportExecutionService
                {
                    Credentials = authority,
                    Timeout = 9999999
                };

                rs.ExecutionHeaderValue = new ExecutionHeader();
                ExecutionInfo execInfo = rs.LoadReport(reportPath, null);
                rs.SetExecutionParameters(para, "en-us");

                string extension, encoding, mimeType;
                Warning[] warnings;
                string[] streamIDs;

                result = rs.Render(fileFormat, "<DeviceInfo><Toolbar>False</Toolbar></DeviceInfo>", out extension, out encoding, out mimeType, out warnings, out streamIDs);
            }
            catch (Exception ex)
            {
                //WriteLog("Report export failed: " + ex.Message);
            }
            return result;
        }
    }
}