using System.Threading.Tasks;

namespace DemoApp_Test.Services
{
    public interface IEmailService
    {
        Task SendResetPasswordEmailAsync(string toEmail, string resetLink);
        Task SendEmailAsync(string to, string subject, string htmlMessage);
    }
}