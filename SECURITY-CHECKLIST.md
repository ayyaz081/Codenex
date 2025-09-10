# Security Checklist for Production Deployment

## 🔐 Pre-Deployment Security Checklist

### Authentication & Authorization
- [ ] JWT secret key is at least 256 bits long and cryptographically secure
- [ ] JWT expiration time is appropriately set (recommended: 24 hours or less)
- [ ] JWT issuer and audience are properly configured
- [ ] Password requirements are enforced (8+ chars, mixed case, digits)
- [ ] Account lockout is enabled (5 failed attempts, 5-minute lockout)
- [ ] Email confirmation is enabled in production (`REQUIRE_EMAIL_CONFIRMATION=true`)

### API Security
- [ ] CORS is configured with specific allowed origins (no wildcards in production)
- [ ] HTTPS is enforced (`REQUIRE_HTTPS=true`)
- [ ] HSTS headers are enabled with appropriate max-age
- [ ] Security headers are configured (X-Content-Type-Options, X-Frame-Options, etc.)
- [ ] Content Security Policy (CSP) is properly configured
- [ ] Rate limiting is implemented and configured

### Database Security
- [ ] Database connection string uses SSL/TLS encryption
- [ ] Database credentials are stored securely (environment variables, not code)
- [ ] Database user has minimum required permissions
- [ ] Connection pooling is configured with appropriate limits
- [ ] Sensitive data logging is disabled in production

### External Services
- [ ] Email service uses app-specific passwords or OAuth
- [ ] API keys for external services are stored securely
- [ ] External API calls use HTTPS
- [ ] Timeouts are configured for external service calls

### Environment & Configuration
- [ ] All secrets are stored in environment variables, not configuration files
- [ ] Production configuration files don't contain sensitive data
- [ ] Environment variables are properly protected in deployment environment
- [ ] Debug information is disabled in production
- [ ] Detailed error messages are disabled for external users
- [ ] JWT_KEY environment variable is set in production (not using development default)

## 🔒 Post-Deployment Security Verification

### SSL/TLS Configuration
```bash
# Test SSL configuration
curl -I https://yourdomain.com
# Should return proper security headers

# Check SSL certificate
openssl s_client -connect yourdomain.com:443 -servername yourdomain.com
```

### Security Headers Verification
```bash
# Check security headers
curl -I https://yourdomain.com/health

# Expected headers:
# Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
# Content-Security-Policy: [your CSP policy]
# Referrer-Policy: strict-origin-when-cross-origin
```

### CORS Testing
```javascript
// Test CORS from browser console on your frontend domain
fetch('https://yourdomain.com/api/health', {
  method: 'GET',
  credentials: 'include'
}).then(response => console.log('CORS OK:', response.status));

// Should succeed from allowed origins, fail from others
```

### Authentication Testing
```bash
# Test JWT authentication
curl -X POST https://yourdomain.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"invalidpassword"}'

# Should return proper error without sensitive information
```

## 🛡️ Security Monitoring

### Health Check Monitoring
```bash
# Monitor security-related health checks
curl https://yourdomain.com/health | jq '.checks[] | select(.name | contains("jwt") or contains("email"))'
```

### Log Monitoring
Monitor logs for:
- Failed authentication attempts
- JWT validation failures
- CORS violations
- Rate limit violations
- Database connection errors
- Unauthorized access attempts

### Security Scanning
```bash
# Run security scan (example with OWASP ZAP)
docker run -t owasp/zap2docker-stable zap-baseline.py -t https://yourdomain.com

# Or use online tools like:
# - SSL Labs SSL Test
# - Security Headers.com
# - Mozilla Observatory
```

## 🔧 Security Configuration Examples

### Environment Variables (Production)
```bash
# JWT Configuration
JWT_KEY="your-256-bit-cryptographically-secure-key-here"
JWT_ISSUER="https://yourdomain.com"
JWT_AUDIENCE="https://yourdomain.com"
JWT_EXPIRY_HOURS="24"

# CORS Configuration
CORS_ALLOWED_ORIGINS="https://yourfrontend.com,https://www.yourfrontend.com"

# Security Settings
REQUIRE_HTTPS="true"
USE_HSTS="true"
HSTS_MAX_AGE="31536000"
REQUIRE_EMAIL_CONFIRMATION="true"

# Email Security
EMAIL_HOST="smtp.gmail.com"
EMAIL_PORT="587"
EMAIL_ENABLE_SSL="true"
EMAIL_USERNAME="your-email@gmail.com"
EMAIL_PASSWORD="your-app-specific-password"
```

### Content Security Policy (CSP)
```bash
# Example CSP for API-only backend
CSP_DIRECTIVES="default-src 'self'; script-src 'none'; object-src 'none'; style-src 'none'; img-src 'none'; media-src 'none'; frame-src 'none'; font-src 'none'; connect-src 'self';"
```

## ⚠️ Common Security Mistakes to Avoid

### Configuration Mistakes
- ❌ Using wildcard (`*`) in CORS origins for production
- ❌ Storing secrets in configuration files or code
- ❌ Using weak or default JWT secret keys
- ❌ Not enabling HTTPS redirection
- ❌ Exposing detailed error messages to clients

### Authentication Mistakes
- ❌ Not implementing proper password policies
- ❌ Not implementing account lockout
- ❌ JWT tokens with no expiration or very long expiration
- ❌ Not validating JWT issuer and audience
- ❌ Storing JWTs in localStorage (if applicable to frontend)

### Database Mistakes
- ❌ Using database admin credentials for application
- ❌ Not using SSL for database connections
- ❌ Enabling sensitive data logging in production
- ❌ Not implementing connection limits

## 📋 Security Incident Response

### If Security Issue is Detected
1. **Immediate Actions:**
   - Rotate compromised secrets immediately
   - Block suspicious IP addresses if necessary
   - Check logs for extent of potential breach
   - Document the incident

2. **Investigation:**
   - Analyze logs to understand the scope
   - Identify affected users/data
   - Determine root cause

3. **Recovery:**
   - Apply security patches
   - Update security configurations
   - Force password resets if necessary
   - Notify affected users if required

4. **Prevention:**
   - Update security procedures
   - Implement additional monitoring
   - Conduct security review
   - Update documentation

## 🔍 Regular Security Maintenance

### Weekly
- [ ] Review authentication failure logs
- [ ] Check SSL certificate expiration dates
- [ ] Verify health check statuses

### Monthly
- [ ] Update dependencies and security patches
- [ ] Review and rotate API keys if necessary
- [ ] Audit user accounts and permissions
- [ ] Run security scans

### Quarterly
- [ ] Comprehensive security audit
- [ ] Penetration testing (if applicable)
- [ ] Review and update security policies
- [ ] Security training and awareness

## 📞 Security Resources

### Tools for Security Testing
- OWASP ZAP (Security scanning)
- SSL Labs SSL Test (SSL configuration)
- Security Headers.com (Security headers)
- Mozilla Observatory (Security assessment)
- Postman/curl (API security testing)

### Security References
- OWASP Top 10 Security Risks
- Microsoft Security Best Practices
- NIST Cybersecurity Framework
- Azure Security Best Practices (if using Azure)
- AWS Security Best Practices (if using AWS)

Remember: Security is an ongoing process, not a one-time setup. Regularly review and update your security configurations!
