using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Collections;
using System.Net.Mime;
using System.IO;
/// <summary>
/// MailSender 的摘要描述
/// </summary>
public class MailSender
{
	 private MailMessage mailMessage;
        public string from = "admin@johnsonfitness.com";
        public string host = "jhtmail.jhtgroup.com";
        //private string host = "mail.star.co.uk";
        private int port = 25;

        /// <summary>
        /// Mail
        /// </summary>
        /// <param name="To">收件者</param>
        /// <param name="Title">主旨</param>
        /// <param name="Body">內文</param>
        public MailSender(string To, string Title, string Body)
        {
            MailMessage(To, "", "", Title, Body,"","",false);
        }

        /// <summary>
        /// Mail
        /// </summary>
        /// <param name="To">收件者</param>
        /// <param name="Cc">副本抄送</param>
        /// <param name="Title">主旨</param>
        /// <param name="Body">內文</param>
        public MailSender(string To, string Cc, string Title, string Body)
        {
            MailMessage(To, Cc, "", Title, Body,"","",false);
        }

        /// <summary>
        /// Mail
        /// </summary>
        /// <param name="To">收件者</param>
        /// <param name="Cc">副本抄送</param>
        /// <param name="Bcc">副本密送</param>
        /// <param name="Title">主旨</param>
        /// <param name="Body">內文</param>
        public MailSender(string To, string Cc, string Bcc, string Title, string Body)
        {
            MailMessage(To, Cc, Bcc, Title, Body, "","",false);
        }

        /// <summary>
        /// Mail
        /// </summary>
        /// <param name="form">寄件者</param>
        /// <param name="To">收件者</param>
        /// <param name="Cc">副本抄送</param>
        /// <param name="Bcc">副本密送</param>
        /// <param name="Title">主旨</param>
        /// <param name="Body">內文</param>
        public MailSender(string SenderAddress, string To, string Cc, string Bcc, string Title, string Body)
        {
            MailMessage(To, Cc, Bcc, Title, Body, SenderAddress,"",false);
        }

    /// <summary>
    /// Mail
    /// </summary>
    /// <param name="form">寄件者</param>
    /// <param name="To">收件者</param>
    /// <param name="Cc">副本抄送</param>
    /// <param name="Bcc">副本密送</param>
    /// <param name="Title">主旨</param>
    /// <param name="Body">內文</param>
    /// <param name="imagepath">signpicture</param>
    public MailSender(string SenderAddress, string To, string Cc, string Bcc, string Title, string Body,string imagepath,Boolean ishtml)
        {
            MailMessage(To, Cc, Bcc, Title, Body, SenderAddress, imagepath, ishtml);
        }



    /// <summary>
    /// Mail
    /// </summary>
    /// <param name="To">收件者</param>
    /// <param name="Cc">副本抄送</param>
    /// <param name="Bcc">副本密送</param>
    /// <param name="Title">主旨</param>
    /// <param name="Body">內文</param>
    private void MailMessage(string To, string Cc, string Bcc, string Title, string Body, string SenderAddress,string imagepath,Boolean ishtml)
        {
            mailMessage = new MailMessage();
            if (SenderAddress != "")
                mailMessage.From = new System.Net.Mail.MailAddress(SenderAddress, "Johnson Customer Service");
            else
                mailMessage.From = new System.Net.Mail.MailAddress(from);

            if (To != "")
            {
                mailMessage.To.Add(To.Replace(";", ","));
            }
            //string[] MailTo = To.Split(new char[]{";",","});


            if (Cc != "")
            {
                mailMessage.CC.Add(Cc.Replace(";", ","));
            }
            if (Bcc != "")
            {
                mailMessage.Bcc.Add(Bcc.Replace(";", ","));
            }
            mailMessage.Subject = Title;
           // mailMessage.Body = Body;
          //  mailMessage.IsBodyHtml = true;
         //   mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
         //   mailMessage.Priority = System.Net.Mail.MailPriority.Normal;

            if (ishtml)
            {
                ContentType mimeType = new System.Net.Mime.ContentType("text/html");
                // Add the alternate body to the message.

                AlternateView alternate = AlternateView.CreateAlternateViewFromString(Body, mimeType);
                if (imagepath != "")
                {
                


                    //Add image to HTML version
                    System.Net.Mail.LinkedResource imageResource = new System.Net.Mail.LinkedResource(imagepath);
                    imageResource.ContentId = "signpic";
                    imageResource.ContentType.MediaType = "image/jpeg";
                    alternate.LinkedResources.Add(imageResource);

                   
                }
                mailMessage.AlternateViews.Add(alternate);
            }
            else 
            {
                mailMessage.Body = Body;
                mailMessage.IsBodyHtml = true;
                mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
                mailMessage.Priority = System.Net.Mail.MailPriority.Normal;
            }



    }

        /// <summary>
        /// 設定該附件為一個內嵌附件
        /// </summary>
        /// <param name="strFilePath">檔案路徑</param>
        private void SetInlineAttachment(string strFilePath)
        {
            System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(strFilePath);
            attachment.Name = System.IO.Path.GetFileName(strFilePath);
            attachment.NameEncoding = System.Text.Encoding.GetEncoding("utf-8");
            attachment.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;

            attachment.ContentDisposition.Inline = true;
            attachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
            mailMessage.Attachments.Add(attachment);
        }

        /// <summary>
        /// 附件
        /// </summary>
        /// <param name="list">檔案清單</param>
        public void Attachments(ArrayList list)
        {
            Attachment data = null;
            ContentDisposition disposition;
            for (int i = 0; i < list.Count; i++)
            {
                data = new Attachment(list[i].ToString(), MediaTypeNames.Application.Octet);
                disposition = data.ContentDisposition;
                disposition.CreationDate = System.IO.File.GetCreationTime(list[i].ToString());
                disposition.ModificationDate = System.IO.File.GetLastWriteTime(list[i].ToString());
                disposition.ReadDate = System.IO.File.GetLastAccessTime(list[i].ToString());
                mailMessage.Attachments.Add(data);
            }
        }


        public void Attachments(byte[] by,string filename)
        {
            mailMessage.Attachments.Add(new Attachment(new MemoryStream(by), filename));
        }

        //非同步mail
        /*
        public void SendAsync(SendCompletedEventHandler CompletedMethod)
        {
            if (mailMessage != null)
            {
                smtpClient = new SmtpClient();
                smtpClient.Credentials = new System.Net.NetworkCredential(mailMessage.From.Address, password);
                smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                smtpClient.Host = "smtp." + mailMessage.From.Host;
                smtpClient.SendCompleted += new SendCompletedEventHandler(CompletedMethod); 
                smtpClient.SendAsync(mailMessage, mailMessage.Body);
            }
        }
        */

        /// <summary>
        /// 取得SmtpClient
        /// </summary>
        /// <returns>SmtpClient</returns>
        private SmtpClient GetSmtp()
        {
            SmtpClient smtp = new SmtpClient();
            smtp.Host = host;
            smtp.Port = port;
           // smtp.UseDefaultCredentials = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential("stanleychao", "jUioplk740228!");           
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            return smtp;
        }


        /// <summary>
        /// 發送mail
        /// </summary>
        public void Send()
        {
            if (mailMessage != null)
            {
                GetSmtp().Send(mailMessage);
            }
        }

        /// <summary>
        /// 發送mail夾帶檔案
        /// </summary>
        /// <param name="strFileName">檔案名稱</param>
        public void SendInlineAttache(string strFileName)
        {
            if (mailMessage != null)
            {
                SetInlineAttachment(strFileName);

                GetSmtp().Send(mailMessage);

                mailMessage.Dispose();
            }
        }
    }
