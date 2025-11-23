# reCAPTCHA v3 Setup Guide

## Overview
reCAPTCHA v3 has been implemented for the contact form to prevent spam and bot submissions. It's invisible to users and scores their interactions.

## Setup Instructions

### 1. Register Your Site
1. Go to https://www.google.com/recaptcha/admin
2. Click "+" to add a new site
3. Fill in:
   - **Label**: Neelsol Technologies Contact Form
   - **reCAPTCHA type**: Select "reCAPTCHA v3"
   - **Domains**: 
     - `neelsol.com`
     - `www.neelsol.com`
     - `localhost` (for testing)
4. Accept terms and click "Submit"
5. You'll receive:
   - **Site Key** (public, used in frontend)
   - **Secret Key** (private, used in backend)

### 2. Configure Environment Variables

#### For Development (`.env.development`):
```env
RECAPTCHA_SITE_KEY=your_site_key_here
RECAPTCHA_SECRET_KEY=your_secret_key_here
RECAPTCHA_MIN_SCORE=0.5
```

#### For Production (Hosting Control Panel):
Add these environment variables:
```
RECAPTCHA_SITE_KEY=your_site_key_here
RECAPTCHA_SECRET_KEY=your_secret_key_here
RECAPTCHA_MIN_SCORE=0.5
```

### 3. Update Contact.html
Set the site key in the data attribute:
```html
<script id="recaptcha-config" data-site-key="YOUR_SITE_KEY_HERE" defer></script>
```

Or set it globally in your config:
```javascript
window.RECAPTCHA_SITE_KEY = 'your_site_key_here';
```

## How It Works

### Frontend Flow:
1. User fills out contact form
2. When user clicks "Send Message":
   - reCAPTCHA automatically generates a token
   - Token is added to form data
   - Form submits to backend with token

### Backend Flow:
1. Receives form data with reCAPTCHA token
2. Sends token to Google's API for verification
3. Google returns score (0.0 to 1.0):
   - **1.0** = Very likely human
   - **0.5** = Threshold (configurable)
   - **0.0** = Very likely bot
4. If score >= threshold: Accept submission
5. If score < threshold: Reject with security error

## Testing

### Test Submission:
1. Fill out the contact form
2. Click "Send Message"
3. Check browser console for:
   ```
   reCAPTCHA loaded successfully
   reCAPTCHA verified (score: 0.9) for contact form from user@example.com
   ```

### Test Bot Protection:
Automated scripts will receive low scores and be blocked.

## Score Thresholds

Recommended values for `RECAPTCHA_MIN_SCORE`:

- **0.3**: Very lenient (allows almost all users)
- **0.5**: Balanced (recommended for production)
- **0.7**: Strict (may block some legitimate users)
- **0.9**: Very strict (recommended only for high-security)

## Monitoring

### Check Logs:
Backend logs will show:
- reCAPTCHA verification attempts
- Scores received
- Blocked submissions (low scores)

### Google reCAPTCHA Admin:
Visit https://www.google.com/recaptcha/admin to see:
- Request volume
- Score distribution
- Suspicious activity alerts

## Troubleshooting

### "reCAPTCHA site key not configured":
- Site key not set in environment or Contact.html
- Solution: Set `RECAPTCHA_SITE_KEY` environment variable

### "Security verification failed":
- User scored below minimum threshold
- Could be legitimate user in some cases
- Consider lowering `RECAPTCHA_MIN_SCORE` if too many false positives

### reCAPTCHA not loading:
- Check browser console for errors
- Verify CSP allows Google domains
- Check site key is correct

## Graceful Degradation

The system is designed to fail open:
- If reCAPTCHA is not configured: Form works without CAPTCHA
- If Google API is down: Form works without CAPTCHA
- If token generation fails: Form continues without token

This ensures the contact form always works, even if CAPTCHA fails.

## Security Notes

1. **Never expose Secret Key**: Only use in backend, never in frontend
2. **Use HTTPS**: reCAPTCHA requires HTTPS in production
3. **Monitor scores**: Adjust threshold based on spam levels
4. **Rate limiting**: Consider adding rate limiting in addition to CAPTCHA

## Rollback

If you need to disable reCAPTCHA:
1. Remove `RECAPTCHA_SECRET_KEY` from environment
2. Form will work without CAPTCHA verification
3. No code changes needed - graceful degradation built-in
