using System.Threading.Tasks;

namespace OrderTrackingApp.Services
{
    public interface IEmailService
    {
        Task SendEmailWithAttachment(
            string toEmail,
            string subject,
            string bodyHtml,
            byte[] attachmentBytes,
            string attachmentName
        );
    }
}
