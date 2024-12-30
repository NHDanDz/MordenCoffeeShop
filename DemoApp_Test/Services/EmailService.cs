using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace DemoApp_Test.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendResetPasswordEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Reset Your Password - NHDanDz Coffee Shop";
            var htmlBody = $@"
                <h2>Reset Your Password</h2>
                <p>Please click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email.</p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,</p>
                <p>NHDanDz Coffee Shop Team</p>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            try
            {
                var emailConfig = _configuration.GetSection("EmailConfiguration");
                var email = new MimeMessage();

                // Cấu hình email rõ ràng hơn
                email.From.Add(new MailboxAddress("NHDanDz Coffee Shop", emailConfig["From"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

                using var smtp = new SmtpClient();

                // Thêm log để debug
                Console.WriteLine($"Connecting to {emailConfig["SmtpServer"]}:{emailConfig["Port"]}");

                await smtp.ConnectAsync(
                    emailConfig["SmtpServer"],
                    emailConfig.GetValue<int>("Port"),
                    SecureSocketOptions.StartTls
                );

                // Log trước khi authenticate
                Console.WriteLine("Attempting to authenticate...");

                await smtp.AuthenticateAsync(
                    emailConfig["Username"],
                    emailConfig["Password"]
                );

                // Log sau khi authenticate thành công
                Console.WriteLine("Authentication successful, sending email...");

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi
                Console.WriteLine($"Error sending email: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
