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
    }
}
