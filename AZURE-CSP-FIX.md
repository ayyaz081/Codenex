# Azure CSP Configuration Fix

If you're experiencing issues with header/footer not loading or Font Awesome CSS being blocked in Azure, follow these steps:

## Issue
The Content Security Policy in `web.config` might be too restrictive and override the correct CSP from `Program.cs`.

## Solution 1: Azure App Configuration (Recommended)

Add this environment variable in Azure Portal → Configuration → Application Settings:

```
CSP_DIRECTIVES = default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://js.stripe.com; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; style-src-elem 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; img-src 'self' data: https: blob:; font-src 'self' data: https://cdnjs.cloudflare.com https://fonts.gstatic.com; connect-src 'self' https: wss: https://api.stripe.com https://r.stripe.com https://errors.stripe.com; media-src 'self' https:; object-src 'none'; child-src https://*.google.com https://*.googleapis.com https://*.gstatic.com; frame-src 'self' https://*.google.com https://*.googleapis.com https://*.gstatic.com https://*.youtube.com https://www.youtube.com https://js.stripe.com https://checkout.stripe.com; frame-ancestors 'none'; form-action 'self' https://checkout.stripe.com; base-uri 'self'; manifest-src 'self';
```

**Note:** Remove `upgrade-insecure-requests;` for Azure if HTTP redirects are handled by Azure App Service.

## Solution 2: Verify web.config

The `web.config` has been updated to remove the restrictive CSP header so that `Program.cs` CSP takes precedence.

Current `web.config` CSP headers section should be empty:
```xml
<httpProtocol>
    <customHeaders>
    </customHeaders>
</httpProtocol>
```

## Solution 3: Restart App Service

After making changes:
1. Azure Portal → Your App Service
2. Click **Restart**
3. Wait 30-60 seconds
4. Test the site

## Verification

After applying the fix, check browser console:
- ✅ No CSP violations
- ✅ Font Awesome loads successfully
- ✅ Header and footer appear correctly

## Additional Troubleshooting

If issues persist:

1. **Check Azure Logs:**
   ```bash
   az webapp log tail --resource-group codenex-rg --name codenex-app
   ```

2. **Verify Static Files:**
   ```bash
   curl https://your-app.azurewebsites.net/components/header.html
   curl https://your-app.azurewebsites.net/components/footer.html
   ```

3. **Browser Console:**
   - Open Developer Tools (F12)
   - Check Console tab for JavaScript errors
   - Check Network tab to see if files are loading

## Common Issues

### Issue: "Element not found" errors
**Cause:** JavaScript trying to access DOM before it's ready
**Fix:** Already handled in `shared-components.js` with proper async/await and delays

### Issue: CSP blocking external resources
**Cause:** Restrictive CSP in web.config or missing Azure environment variable
**Fix:** Apply Solution 1 or 2 above

### Issue: Header/footer not appearing
**Possible causes:**
1. Static files not deployed
2. CSP blocking resources
3. JavaScript errors in console

**Debug steps:**
1. Check if `/components/header.html` and `/components/footer.html` exist
2. Check browser console for errors
3. Verify `shared-components.js` is loaded
