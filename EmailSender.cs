using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace sendbulkemail
{
    public class EmailSender
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly bool _useSSL;

        public EmailSender(string smtpServer, int smtpPort, string fromEmail, string password, bool useSSL = true)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _fromEmail = fromEmail;
            _password = password;
            _useSSL = useSSL;
        }

        public async Task SendEmailAsync(EmailData emailData)
        {
            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = _useSSL,
                Credentials = new NetworkCredential(_fromEmail, _password)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, "Your Name"),
                Subject = emailData.Subject,
                Body = emailData.GetFormattedBody(),
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(emailData.EmailAddress!, emailData.Name));

            // Add attachments if any
            foreach (var attachment in emailData.Attachments)
            {
                try
                {
                    if (File.Exists(attachment.FilePath))
                    {
                        var mailAttachment = new Attachment(attachment.FilePath, attachment.ContentType);
                        mailAttachment.ContentDisposition.FileName = attachment.FileName;
                        mailMessage.Attachments.Add(mailAttachment);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Attachment file not found: {attachment.FilePath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding attachment {attachment.FileName}: {ex.Message}");
                }
            }

            await client.SendMailAsync(mailMessage);
        }

        public async Task SendBulkEmailsAsync(List<EmailData> emailDataList)
        {
            foreach (var emailData in emailDataList)
            {
                try
                {
                    await SendEmailAsync(emailData);
                    Console.WriteLine($"Email sent successfully to: {emailData.Name} ({emailData.EmailAddress})");
                    // Add a small delay between emails to avoid overwhelming the SMTP server
                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email to {emailData.Name} ({emailData.EmailAddress}): {ex.Message}");
                }
            }
        }
    }
}

