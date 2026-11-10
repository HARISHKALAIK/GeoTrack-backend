using MailKit.Net.Smtp;
using MimeKit;

namespace GeoTrack.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string email, string otp)
        {
            // Get email configuration
            var senderEmail = _configuration["EmailSettings:Email"];
            var appPassword = _configuration["EmailSettings:AppPassword"];
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portValue = _configuration["EmailSettings:Port"];

            // Validate configuration
            if (string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(appPassword) ||
                string.IsNullOrWhiteSpace(smtpServer) ||
                !int.TryParse(portValue, out int port))
            {
                throw new InvalidOperationException(
                    "Email configuration is missing or invalid."
                );
            }

            // Create email
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress("GeoTrack", senderEmail)
            );

            message.To.Add(
                new MailboxAddress("", email)
            );

            message.Subject = "GeoTrack - Email Verification OTP";

            message.Body = new TextPart("plain")
            {
                Text = $@"
Hello,

Your GeoTrack email verification OTP is:

{otp}

This OTP is valid for 5 minutes.

Please do not share this OTP with anyone.

Regards,
GeoTrack Team
"
            };

            // Connect to Gmail SMTP
            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                smtpServer,
                port,
                MailKit.Security.SecureSocketOptions.StartTls
            );

            // Authenticate Gmail
            await smtp.AuthenticateAsync(
                senderEmail,
                appPassword
            );

            // Send email
            await smtp.SendAsync(message);

            // Disconnect
            await smtp.DisconnectAsync(true);
        }
    }
}