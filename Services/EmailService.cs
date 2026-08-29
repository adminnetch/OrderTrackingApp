using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace OrderTrackingApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailWithAttachment(
            string toEmail,
            string subject,
            string bodyHtml,
            byte[] attachmentBytes,
            string attachmentName)
        {
            // 🔒 Validazione configurazione email (risolve 4 warning CS8604)
            var from = _config["Email:From"] 
                ?? throw new InvalidOperationException("Configurazione email mancante: Email:From");
            var smtpServer = _config["Email:SmtpServer"] 
                ?? throw new InvalidOperationException("Configurazione email mancante: Email:SmtpServer");
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "25");
            var username = _config["Email:Username"] 
                ?? throw new InvalidOperationException("Configurazione email mancante: Email:Username");
            var password = _config["Email:Password"] 
                ?? throw new InvalidOperationException("Configurazione email mancante: Email:Password");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = bodyHtml };
            builder.Attachments.Add(attachmentName, attachmentBytes);

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, false);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}