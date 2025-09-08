using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PortfolioBackend.Services
{
    public interface ICertificateGenerationService
    {
        X509Certificate2 GenerateOrLoadCertificate();
        string GetCertificatePath();
    }

    public class CertificateGenerationService : ICertificateGenerationService
    {
        private readonly ILogger<CertificateGenerationService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _certificatesDirectory;
        private readonly string _certificatePath;

        public CertificateGenerationService(ILogger<CertificateGenerationService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            
            // Store certificates in the application's directory
            _certificatesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ssl");
            _certificatePath = Path.Combine(_certificatesDirectory, "certificate.pfx");
            
            // Ensure the certificates directory exists
            if (!Directory.Exists(_certificatesDirectory))
            {
                Directory.CreateDirectory(_certificatesDirectory);
            }
        }

        public X509Certificate2 GenerateOrLoadCertificate()
        {
            try
            {
                // Try to load existing certificate first
                if (File.Exists(_certificatePath))
                {
                    var existingCert = LoadCertificate(_certificatePath);
                    if (existingCert != null && IsCertificateValid(existingCert))
                    {
                        _logger.LogInformation("Using existing SSL certificate from {Path}", _certificatePath);
                        
                        // Ensure the existing certificate is also trusted
                        if (!IsCertificateInTrustedStore(existingCert))
                        {
                            _logger.LogInformation("Existing certificate not in trusted store, installing it");
                            InstallCertificateInTrustedStore(existingCert);
                        }
                        
                        return existingCert;
                    }
                    else
                    {
                        _logger.LogWarning("Existing certificate is invalid or expired, generating new one");
                        File.Delete(_certificatePath);
                    }
                }

                // Generate new certificate
                _logger.LogInformation("Generating new SSL certificate at {Path}", _certificatePath);
                return GenerateNewCertificate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate or load SSL certificate");
                throw;
            }
        }

        public string GetCertificatePath()
        {
            return _certificatePath;
        }

        private X509Certificate2? LoadCertificate(string path)
        {
            try
            {
                // Using empty password as we generate certificates without password for simplicity
                return new X509Certificate2(path, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load certificate from {Path}", path);
                return null;
            }
        }

        private bool IsCertificateValid(X509Certificate2 certificate)
        {
            // Check if certificate is not expired and has at least 30 days remaining
            return certificate.NotAfter > DateTime.Now.AddDays(30);
        }

        private X509Certificate2 GenerateNewCertificate()
        {
            // Generate the certificate programmatically
            using var rsa = RSA.Create(2048);
            
            var request = new CertificateRequest(
                "CN=localhost, O=Portfolio Backend, OU=Development",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            // Add Subject Alternative Names for localhost and 127.0.0.1
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddIpAddress(IPAddress.Loopback);  // 127.0.0.1
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);  // ::1
            request.CertificateExtensions.Add(sanBuilder.Build());

            // Add Key Usage
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: true
                )
            );

            // Add Extended Key Usage for server authentication
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                    critical: true
                )
            );

            // Add Basic Constraints
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false)
            );

            // Create the certificate (valid for 1 year)
            var certificate = request.CreateSelfSigned(
                DateTime.Now.AddDays(-1),
                DateTime.Now.AddYears(1)
            );

            // Export to PFX format and save to disk
            var pfxBytes = certificate.Export(X509ContentType.Pfx, "");
            File.WriteAllBytes(_certificatePath, pfxBytes);

            _logger.LogInformation("Generated new SSL certificate with thumbprint: {Thumbprint}", certificate.Thumbprint);
            _logger.LogInformation("Certificate valid from {NotBefore} to {NotAfter}", 
                certificate.NotBefore, certificate.NotAfter);

            // Try to install the certificate in the trusted root store so browsers trust it
            InstallCertificateInTrustedStore(certificate);

            return certificate;
        }

        private void InstallCertificateInTrustedStore(X509Certificate2 certificate)
        {
            try
            {
                // First try to install in LocalMachine store (requires admin)
                if (TryInstallCertificate(certificate, StoreLocation.LocalMachine))
                {
                    _logger.LogInformation("Successfully installed SSL certificate in Local Machine trusted store");
                    return;
                }
                
                // If that fails, install in CurrentUser store
                if (TryInstallCertificate(certificate, StoreLocation.CurrentUser))
                {
                    _logger.LogInformation("Successfully installed SSL certificate in Current User trusted store");
                    _logger.LogWarning("Certificate installed in user store only. For system-wide trust, run as administrator");
                    return;
                }
                
                _logger.LogWarning("Could not install certificate in trusted store. Browsers may show security warnings");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to install certificate in trusted store. Browsers may show security warnings");
            }
        }

        private bool TryInstallCertificate(X509Certificate2 certificate, StoreLocation storeLocation)
        {
            try
            {
                using var store = new X509Store(StoreName.Root, storeLocation);
                store.Open(OpenFlags.ReadWrite);
                
                // Check if certificate already exists
                var existingCert = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
                if (existingCert.Count > 0)
                {
                    _logger.LogDebug("Certificate already exists in {StoreLocation} trusted store", storeLocation);
                    return true;
                }
                
                // Add the certificate to the trusted root store
                store.Add(certificate);
                store.Close();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to install certificate in {StoreLocation} store: {Message}", storeLocation, ex.Message);
                return false;
            }
        }

        private bool IsCertificateInTrustedStore(X509Certificate2 certificate)
        {
            // Check both LocalMachine and CurrentUser stores
            return IsCertificateInStore(certificate, StoreLocation.LocalMachine) ||
                   IsCertificateInStore(certificate, StoreLocation.CurrentUser);
        }

        private bool IsCertificateInStore(X509Certificate2 certificate, StoreLocation storeLocation)
        {
            try
            {
                using var store = new X509Store(StoreName.Root, storeLocation);
                store.Open(OpenFlags.ReadOnly);
                var existingCert = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
                return existingCert.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
