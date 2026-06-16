using SendGrid;
using SendGrid.Helpers.Mail;
using WularItech_solutions.Interfaces;

namespace WularItech_solutions.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"]
                ?? throw new Exception("SendGrid:ApiKey not configured.");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("munizahzargar.iimun@gmail.com", "WularTech Solutions");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", body);
            var response = await client.SendEmailAsync(msg);
            Console.WriteLine("Email status: " + response.StatusCode);
        }
    }
}