# Gmail SMTP Authentication Troubleshooting

## Current Error
```
535: 5.7.8 Username and Password not accepted
```

This error indicates that Gmail is rejecting the credentials.

## Step-by-Step Troubleshooting

### 1. Verify App Password Generation
1. Go to [Google Account Settings](https://myaccount.google.com/security)
2. Ensure **2-Step Verification** is ON
3. Go to **App passwords** section
4. If you see the existing "Portfolio" app password, **delete it**
5. Create a **new app password**:
   - Select "Mail"
   - Select "Other (custom name)" → type "Portfolio Backend"
   - Copy the NEW 16-character password (it will look like: `abcd efgh ijkl mnop`)

### 2. Test Gmail Settings Manually

Try logging into Gmail SMTP with a tool like:
- **Thunderbird**
- **Outlook** 
- **Or test with curl**:

```bash
curl --url 'smtps://smtp.gmail.com:465' --ssl-reqd \
  --mail-from 'ayyaz081@gmail.com' \
  --mail-rcpt 'test@example.com' \
  --user 'ayyaz081@gmail.com:YOUR_APP_PASSWORD' \
  --upload-file - << EOF
From: ayyaz081@gmail.com
To: test@example.com
Subject: Test

Test email
EOF
```

### 3. Common Issues

**Issue**: Account security settings
**Solution**: Check if Gmail is blocking "less secure app access"

**Issue**: Wrong app password format
**Solution**: App password should be exactly 16 characters, no spaces

**Issue**: 2FA not properly enabled
**Solution**: Disable and re-enable 2-Step Verification

**Issue**: Account locked or flagged
**Solution**: Check Gmail security tab for any alerts

### 4. Alternative SMTP Settings to Try

**Option 1: Gmail SSL (Port 465)**
```json
{
  "Host": "smtp.gmail.com",
  "Port": 465,
  "EnableSsl": true
}
```

**Option 2: Gmail Legacy (if available)**
- Enable "Less secure app access" (deprecated but might work for testing)

### 5. Verify Current Settings

1. Email: `ayyaz081@gmail.com`
2. Current password in config: `ffywdwvrjarpmwf`
3. Is this the exact password from Google? **Please verify**

## Next Steps

1. **Generate a fresh app password** from Google
2. **Test it manually** with curl or email client
3. **Update the config** with the new password
4. **Try the alternative port 465** if 587 still fails
