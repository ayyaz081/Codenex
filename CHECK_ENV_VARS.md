# Quick Environment Variable Check

The site is failing to start. This means one of these exceptions is being thrown during startup:

## Most Likely Issues:

### 1. JWT_KEY is too short
```
Error: JWT_KEY is too short (X characters)!
Required: At least 32 characters
```

**How to check in site4now:**
- Log into site4now control panel
- Go to Environment Variables
- Find `JWT_KEY`
- Count the characters - must be **at least 32**

**Example valid JWT_KEY (64 characters):**
```
abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ab
```

**Generate a new one in PowerShell:**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

### 2. JWT_KEY variable name is wrong
The variable MUST be exactly: `JWT_KEY` (case-sensitive in some systems)

**Wrong:**
- `JwtKey`
- `Jwt:Key`
- `JWT-KEY`
- `jwtkey`

**Correct:**
- `JWT_KEY`

### 3. Database connection string is missing
The application looks for these variables in order:
1. `DATABASE_CONNECTION_STRING`
2. `CONNECTION_STRING`
3. DefaultConnection in appsettings.json

Make sure at least ONE of these is set.

### 4. Application pool needs restart
After adding or changing environment variables:
1. Go to site4now control panel
2. Find your application pool
3. Click "Recycle" or "Restart"
4. Wait 30-60 seconds for full restart

## To View Application Logs in Site4Now:

1. Log into site4now control panel
2. Click on "Web Sites" → Your site
3. Click "Logs" or "Error Logs"
4. Look for recent errors with timestamps matching deployment
5. The error message will show exactly which variable is missing

You should see one of these messages:
- `❌ STARTUP ERROR: JWT_KEY is missing!`
- `❌ STARTUP ERROR: JWT_KEY is too short (X characters)!`
- `❌ STARTUP ERROR: Database connection string is missing!`

## Required Environment Variables Checklist:

Copy this and verify each one in site4now control panel:

**Critical (App won't start without these):**
- [ ] `JWT_KEY` (minimum 32 characters, no maximum)
- [ ] `DATABASE_CONNECTION_STRING` OR `CONNECTION_STRING`

**Important (Features won't work without these):**
- [ ] `JWT_ISSUER` (can be "CodeNexAPI")
- [ ] `JWT_AUDIENCE` (can be "CodeNexAPI")
- [ ] `JWT_EXPIRY_HOURS` (can be "24")
- [ ] `ADMIN_CREATION_SECRET` (for creating admin users)
- [ ] `ADMIN_EMAIL` (for contact form notifications)

**Optional (Email features):**
- [ ] `EmailSettings__Host`
- [ ] `EmailSettings__Port`
- [ ] `EmailSettings__FromEmail`
- [ ] `EmailSettings__FromName`
- [ ] `EmailSettings__Username`
- [ ] `EmailSettings__Password`
- [ ] `EmailSettings__EnableSsl`

## Test After Fixing:

```bash
curl -I http://neelsol-002-site1.anytempurl.com/
# Should return HTTP 200 or 301, not connection reset

curl http://neelsol-002-site1.anytempurl.com/api/diagnostics
# Should return JSON with configuration status
```

## Still Not Working?

The application logs in site4now will show the EXACT error message. Please check those logs and share the error message.
