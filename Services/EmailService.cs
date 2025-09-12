using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using CodeNex.Models;

namespace CodeNex.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationUrl)
        {
            var subject = "Verify Your Email - Codenex Solutions";
            var htmlContent = GetEmailVerificationTemplate(firstName, verificationUrl);
            
            return await SendEmailAsync(email, subject, htmlContent);
        }

        public async Task<bool> SendPasswordResetAsync(string email, string firstName, string resetUrl)
        {
            var subject = "Reset Your Password - Codenex Solutions";
            var htmlContent = GetPasswordResetTemplate(firstName, resetUrl);
            
            return await SendEmailAsync(email, subject, htmlContent);
        }

        public async Task<bool> SendContactFormNotificationAsync(string adminEmail, string senderName, string senderEmail, string subject, string message)
        {
            var emailSubject = $"New Contact Form Submission: {subject}";
            var htmlContent = GetContactFormNotificationTemplate(senderName, senderEmail, subject, message);
            
            return await SendEmailAsync(adminEmail, emailSubject, htmlContent);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {Email} from {FromEmail}", to, _emailSettings.FromEmail);
                _logger.LogDebug("SMTP Config - Host: {Host}, Port: {Port}, Username: {Username}, PasswordLength: {PasswordLength}", 
                    _emailSettings.Host, _emailSettings.Port, _emailSettings.Username, _emailSettings.Password.Length);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlContent
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                _logger.LogDebug("Connecting to SMTP server...");
                // Connect to the SMTP server - use SSL for port 465, StartTLS for port 587
                var secureOptions = _emailSettings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, secureOptions);
                
                _logger.LogDebug("Authenticating with SMTP server...");
                // Authenticate
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                
                _logger.LogDebug("Sending email...");
                // Send the email
                await client.SendAsync(message);
                
                // Disconnect
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. Host: {Host}, Port: {Port}, From: {FromEmail}", 
                    to, _emailSettings.Host, _emailSettings.Port, _emailSettings.FromEmail);
                return false;
            }
        }

        private static string GetEmailVerificationTemplate(string firstName, string verificationUrl)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 30px; border-radius: 10px;'>
                        <h2 style='color: #333; text-align: center; margin-bottom: 30px;'>
                            Welcome to Codenex Solutions!
                        </h2>
                        
                        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                            Hi {firstName},
                        </p>
                        
                        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                            Thank you for registering with Codenex Solutions! To complete your registration, 
                            please verify your email address by clicking the button below:
                        </p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{verificationUrl}' 
                               style='background-color: #007bff; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;
                                      font-weight: bold;'>
                                Verify Email Address
                            </a>
                        </div>
                        
                        <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                            If the button doesn't work, copy and paste this link into your browser:
                            <br>
                            <a href='{verificationUrl}' style='color: #007bff; word-break: break-all;'>
                                {verificationUrl}
                            </a>
                        </p>
                        
                        <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                            If you didn't create this account, please ignore this email.
                        </p>
                        
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        
                        <p style='color: #999; font-size: 12px; text-align: center;'>
                            This email was sent by Codenex Solutions. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";
        }

        private static string GetPasswordResetTemplate(string firstName, string resetUrl)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 30px; border-radius: 10px;'>
                        <h2 style='color: #333; text-align: center; margin-bottom: 30px;'>
                            Password Reset Request
                        </h2>
                        
                        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                            Hi {firstName},
                        </p>
                        
                        <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                            You requested to reset your password for your Codenex Solutions account. 
                            Click the button below to reset your password:
                        </p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' 
                               style='background-color: #dc3545; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;
                                      font-weight: bold;'>
                                Reset Password
                            </a>
                        </div>
                        
                        <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                            If the button doesn't work, copy and paste this link into your browser:
                            <br>
                            <a href='{resetUrl}' style='color: #dc3545; word-break: break-all;'>
                                {resetUrl}
                            </a>
                        </p>
                        
                        <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                            If you didn't request this password reset, please ignore this email. 
                            Your password will remain unchanged.
                        </p>
                        
                        <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                            This link will expire in 24 hours for security reasons.
                        </p>
                        
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        
                        <p style='color: #999; font-size: 12px; text-align: center;'>
                            This email was sent by Codenex Solutions. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";
        }

        private static string GetContactFormNotificationTemplate(string senderName, string senderEmail, string subject, string message)
        {
            // Format the timestamp
            var timestamp = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' h:mm tt 'UTC'");
            
            // Escape HTML in user input to prevent XSS
            senderName = System.Net.WebUtility.HtmlEncode(senderName);
            senderEmail = System.Net.WebUtility.HtmlEncode(senderEmail);
            subject = System.Net.WebUtility.HtmlEncode(subject);
            message = System.Net.WebUtility.HtmlEncode(message);
            
            // Convert line breaks to HTML breaks
            message = message.Replace("\n", "<br>").Replace("\r", "");
            
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 30px; border-radius: 10px;'>
                        <h2 style='color: #333; text-align: center; margin-bottom: 30px; border-bottom: 3px solid #007bff; padding-bottom: 15px;'>
                            🔔 New Contact Form Submission
                        </h2>
                        
                        <div style='background: white; padding: 25px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); margin-bottom: 20px;'>
                            <h3 style='color: #007bff; margin-top: 0; margin-bottom: 20px; font-size: 18px;'>Contact Details</h3>
                            
                            <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                                <tr style='border-bottom: 1px solid #eee;'>
                                    <td style='padding: 10px 0; font-weight: bold; color: #555; width: 30%;'>Name:</td>
                                    <td style='padding: 10px 0; color: #333;'>{senderName}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #eee;'>
                                    <td style='padding: 10px 0; font-weight: bold; color: #555;'>Email:</td>
                                    <td style='padding: 10px 0; color: #333;'>
                                        <a href='mailto:{senderEmail}' style='color: #007bff; text-decoration: none;'>{senderEmail}</a>
                                    </td>
                                </tr>
                                <tr style='border-bottom: 1px solid #eee;'>
                                    <td style='padding: 10px 0; font-weight: bold; color: #555;'>Subject:</td>
                                    <td style='padding: 10px 0; color: #333; font-weight: 600;'>{subject}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px 0; font-weight: bold; color: #555;'>Received:</td>
                                    <td style='padding: 10px 0; color: #777; font-size: 14px;'>{timestamp}</td>
                                </tr>
                            </table>
                        </div>
                        
                        <div style='background: white; padding: 25px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h3 style='color: #007bff; margin-top: 0; margin-bottom: 15px; font-size: 18px;'>Message</h3>
                            <div style='background: #f8f9fa; padding: 20px; border-radius: 6px; border-left: 4px solid #007bff;'>
                                <p style='color: #333; font-size: 16px; line-height: 1.6; margin: 0;'>
                                    {message}
                                </p>
                            </div>
                        </div>
                        
                        <div style='text-align: center; margin: 30px 0; padding: 20px; background: linear-gradient(135deg, #007bff, #0056b3); border-radius: 8px;'>
                            <p style='color: white; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;'>Quick Actions</p>
                            <a href='mailto:{senderEmail}?subject=Re: {System.Net.WebUtility.UrlEncode(subject)}' 
                               style='background-color: white; color: #007bff; padding: 12px 24px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;
                                      font-weight: bold; margin: 0 10px; font-size: 14px;'>
                                📧 Reply to {senderName}
                            </a>
                        </div>
                        
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        
                        <p style='color: #999; font-size: 12px; text-align: center; margin: 0;'>
                            This notification was sent by the Codenex Solutions contact form system.<br>
                            Visit your <a href='#' style='color: #007bff;'>admin dashboard</a> to manage all contact submissions.
                        </p>
                    </div>
                </body>
                </html>";
        }
    }
}
