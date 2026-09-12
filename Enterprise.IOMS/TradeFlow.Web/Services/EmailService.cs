using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TradeFlow.Web.Services
{

    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "IOMS System";
        public bool EnableSsl { get; set; } = true;
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

#pragma warning disable CA1416, SYSLIB0006
                using var client = new SmtpClient(_settings.Host, _settings.Port);
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                client.EnableSsl = _settings.EnableSsl;
                await client.SendMailAsync(message);
#pragma warning restore CA1416, SYSLIB0006
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }
    }

}