using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WebInterface.Services
{
    public interface IEmailService
    {
        Task SendInviteEmailAsync(string toEmail, string employeeName, string inviteLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendInviteEmailAsync(string toEmail, string employeeName, string inviteLink)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var senderEmail = emailSettings["SenderEmail"];
            var senderName = emailSettings["SenderName"];
            var appPassword = emailSettings["AppPassword"];
            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"]!);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(employeeName, toEmail));
            message.Subject = "You're Invited — Set Up Your Leave Management Account";

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #2c3e50;'>Welcome to 3S Solutions — Leave Management System</h2>
                    <p>Hi {employeeName},</p>
                    <p>You've been invited to set up your account on the Leave Management System. Click the button below to create your password and get started.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{inviteLink}' 
                           style='background-color: #2c7be5; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>
                           Set Up Your Account
                        </a>
                    </div>
                    <p style='font-size: 13px; color: #777;'>This link will expire in 24 hours. If you did not expect this invite, you can safely ignore this email.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #aaa;'>3S Solutions — Leave Management System</p>
                </div>";

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}