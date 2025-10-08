# CodeNex Premium Repository Payment Fix - Summary

## Problem Identified
You were getting the error: **"Repository is not premium or price is not set"** when trying to purchase a repository through Stripe.

## Root Cause
The `PaymentController.cs` (lines 87-90) validates that premium repositories must have:
1. `IsPremium` = true
2. `Price` > 0 (not null, not zero)
3. `GitHubRepoFullName` set (required for inviting users after purchase)

Your repository was missing the `Price` and/or `GitHubRepoFullName` fields.

## What Was Fixed

### 1. Updated DTOs (`DTOs/RepositoryDto.cs`)
✅ Added `Price` field to `RepositoryDto`
✅ Added `GitHubRepoFullName` field to `RepositoryDto`  
✅ Added `Price` field to `RepositoryCreateDto`
✅ Added `GitHubRepoFullName` field to `RepositoryCreateDto`

### 2. Updated Repository Controller (`Controllers/RepositoryController.cs`)
✅ Updated `CreateRepository` to accept and save `Price` and `GitHubRepoFullName`
✅ Updated `UpdateRepository` to handle updating `Price` and `GitHubRepoFullName`

### 3. Rebuilt Application
✅ Application successfully compiled with the changes

### 4. Created Helper Tools
✅ `CheckRepository.ps1` - Diagnose repository configuration issues
✅ `UpdateRepository.ps1` - Update repository settings via API
✅ `check_and_fix_premium_repos.sql` - SQL scripts for direct database updates
✅ `FIX_PREMIUM_REPO_GUIDE.md` - Comprehensive troubleshooting guide

## Next Steps - WHAT YOU NEED TO DO

### Step 1: Start Your Application
```powershell
dotnet run --project C:\Users\Az\source\repos\ayyaz081\Codenex\Codenex.csproj
```

### Step 2: Check Your Repository Configuration
```powershell
cd C:\Users\Az\source\repos\ayyaz081\Codenex\Scripts
.\CheckRepository.ps1
```

This will show you which repositories are missing Price or GitHubRepoFullName.

### Step 3: Update Your Premium Repository

#### Option A: Using PowerShell (Easiest)

1. **Get your auth token:**
   - Open your browser and log in to your application as admin
   - Press F12 to open DevTools
   - Go to Application tab → Storage → Local Storage
   - Find and copy the value of the `token` key

2. **Run the update script:**
```powershell
.\UpdateRepository.ps1 `
  -RepositoryId 1 `
  -Price 29.99 `
  -GitHubRepoFullName "CodeNex-Premium/your-repo-name" `
  -AuthToken "paste_your_token_here"
```

Replace:
- `1` = your repository ID (from Step 2)
- `29.99` = your desired price in USD
- `CodeNex-Premium/your-repo-name` = your actual GitHub organization/repository
- `paste_your_token_here` = the token from step 1

#### Option B: Using SQL (Direct Database)

1. Connect to your Azure SQL database:
   - Server: codenex.database.windows.net
   - Database: codenex

2. Run this SQL:
```sql
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = 29.99,
    GitHubRepoFullName = 'CodeNex-Premium/your-repo-name',
    UpdatedAt = GETUTCDATE()
WHERE Id = 1;  -- Replace 1 with your repository ID
```

#### Option C: Using Postman/Thunder Client/Curl

```bash
curl -X PUT "http://localhost:7150/api/repository/1" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "IsPremium": true,
    "IsFree": false,
    "Price": 29.99,
    "GitHubRepoFullName": "CodeNex-Premium/your-repo-name"
  }'
```

### Step 4: Test the Purchase Flow

1. Open your repository page in the browser
2. Click the "Purchase" button
3. Enter a valid GitHub username
4. You should now be redirected to Stripe checkout (not an error!)

## Important Configuration Values

For each premium repository, you MUST set:

| Field | Type | Required | Example |
|-------|------|----------|---------|
| IsPremium | bool | Yes | `true` |
| IsFree | bool | No (recommended false) | `false` |
| Price | decimal | Yes (must be > 0) | `29.99` |
| GitHubRepoFullName | string | Yes | `CodeNex-Premium/my-repo` |

## Troubleshooting

### Still getting the "not premium or price not set" error?

1. **Check the repository ID**
   - Open browser console (F12) and check the network tab
   - Look for the POST request to `/api/payment/create-checkout-session`
   - Verify the `repositoryId` in the request body

2. **Verify database values**
   ```sql
   SELECT Id, Title, IsPremium, Price, GitHubRepoFullName 
   FROM Repositories 
   WHERE Id = YOUR_REPO_ID;
   ```

3. **Clear your browser cache**
   - The repository list might be cached
   - Hard refresh: Ctrl + F5

4. **Restart your application**
   - Stop the current instance (Ctrl + C)
   - Run `dotnet run` again

### Can't update via API?

- Make sure you're logged in as Admin or Manager
- Check that your auth token is valid (not expired)
- Use the SQL method instead (Option B above)

### GitHub username validation failing?

- Check your `.env` file has `GITHUB_TOKEN` set
- Verify the GitHub token has proper permissions
- Check your `GitHubService` is configured correctly

## Files Created/Modified

### Modified Files:
- `DTOs/RepositoryDto.cs` - Added Price and GitHubRepoFullName
- `Controllers/RepositoryController.cs` - Handle new fields

### New Files:
- `Scripts/CheckRepository.ps1` - Repository configuration checker
- `Scripts/UpdateRepository.ps1` - Repository updater script  
- `Scripts/check_and_fix_premium_repos.sql` - SQL helper scripts
- `Scripts/FIX_PREMIUM_REPO_GUIDE.md` - Detailed troubleshooting guide
- `Scripts/UpdateRepositoryUtil.cs` - C# utility (alternative)
- `Scripts/README_FIX_SUMMARY.md` - This file

## Testing Checklist

After fixing your repository, test these:

- [ ] Repository shows correct price on the frontend
- [ ] Purchase button is visible and clickable
- [ ] Entering GitHub username doesn't show validation errors
- [ ] Stripe checkout session opens successfully
- [ ] Test payment (use Stripe test card: 4242 4242 4242 4242)
- [ ] After payment, check webhook is called
- [ ] Verify payment record is created in database
- [ ] Verify user purchase record is created
- [ ] Check GitHub invitation is sent (if GITHUB_TOKEN is configured)

## Support

If you're still having issues:

1. Check the application logs (console output where you ran `dotnet run`)
2. Check browser console (F12) for JavaScript errors
3. Check Stripe dashboard for webhook errors
4. Review the detailed guide: `FIX_PREMIUM_REPO_GUIDE.md`

## Quick Reference Commands

```powershell
# Start application
dotnet run

# Check repositories
.\Scripts\CheckRepository.ps1

# Update repository
.\Scripts\UpdateRepository.ps1 -RepositoryId 1 -Price 29.99 -GitHubRepoFullName "Org/repo" -AuthToken "token"

# Rebuild application
dotnet build

# Check database migrations
dotnet ef migrations list

# Apply migrations (if needed)
dotnet ef database update
```

---

**Status:** ✅ Code fixed and ready. You just need to update your repository data!
