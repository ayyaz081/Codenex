namespace CodeNex.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationUrl);
        Task<bool> SendPasswordResetAsync(string email, string firstName, string resetUrl);
        Task<bool> SendEmailAsync(string to, string subject, string htmlContent);
    }
}
