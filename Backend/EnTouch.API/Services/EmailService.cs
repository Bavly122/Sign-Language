using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace EnTouch.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otp)
        {
            var settings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                settings["SenderName"],
                settings["SenderEmail"]));
            message.To.Add(new MailboxAddress(fullName, toEmail));
            message.Subject = "EnTouch - Your Reset Verfication Code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div dir='rtl' style='font-family: Arial; padding: 20px;'>
                        <h2>Hello {fullName}</h2>
                        <p>Your Reset Verfication Code:</p>
                        <h1 style='color:#6200EA; letter-spacing:8px;'>{otp}</h1>
                        <p>Valid For <strong>10 Minutes</strong> only.</p>
                        <p>If you Don't Want is Just Ignore the Message.</p>
                    </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(
                settings["SmtpHost"],
                int.Parse(settings["SmtpPort"]!),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                settings["SenderEmail"],
                settings["SenderPassword"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}