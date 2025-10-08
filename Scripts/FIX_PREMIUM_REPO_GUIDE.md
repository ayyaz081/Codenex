# Fix Premium Repository Payment Issue

## Problem
You're getting an error: **"Repository is not premium or price is not set"** when trying to purchase a repository.

## Root Cause
The repository in your database is missing:
1. **Price** value (or it's set to 0 or NULL)
2. **GitHubRepoFullName** value (or it's NULL)

The PaymentController validates these at line 87-90:
```csharp
if (!repository.IsPremium || repository.Price == null || repository.Price <= 0)
{
    return BadRequest("Repository is not premium or price is not set");
}
```

## What I Fixed
✅ Added `Price` and `GitHubRepoFullName` fields to `RepositoryDto` and `RepositoryCreateDto`
✅ Updated `RepositoryController` to handle these fields in Create and Update operations
✅ Rebuilt the application successfully

## How to Fix Your Repository Data

### Option 1: Using PowerShell Script (Recommended)

1. **Make sure your API is running** at `http://localhost:7150`

2. **Get your auth token:**
   - Log in to your application as admin
   - Open browser DevTools (F12)
   - Go to Application/Storage > Local Storage
   - Copy the 'token' value

3. **Run the PowerShell script:**
```powershell
cd C:\Users\Az\source\repos\ayyaz081\Codenex\Scripts
.\UpdateRepository.ps1 -RepositoryId 1 -Price 29.99 -GitHubRepoFullName "CodeNex-Premium/your-repo-name" -AuthToken "YOUR_TOKEN_HERE"
```

Replace:
- `1` with your actual repository ID
- `29.99` with your desired price
- `CodeNex-Premium/your-repo-name` with your actual GitHub organization and repository name
- `YOUR_TOKEN_HERE` with the token you copied

### Option 2: Using SQL Directly

1. **Connect to your Azure SQL Database**
   - Server: `codenex.database.windows.net`
   - Database: `codenex`

2. **Check current repository data:**
```sql
SELECT Id, Title, IsPremium, IsFree, Price, GitHubRepoFullName, IsActive
FROM Repositories
WHERE IsActive = 1
ORDER BY Id;
```

3. **Update your repository:**
```sql
-- Replace the values below with your actual data
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = 29.99,
    GitHubRepoFullName = 'CodeNex-Premium/your-repo-name',
    UpdatedAt = GETUTCDATE()
WHERE Id = 1; -- Replace 1 with your repository ID
```

4. **Verify the update:**
```sql
SELECT Id, Title, IsPremium, IsFree, Price, GitHubRepoFullName
FROM Repositories
WHERE Id = 1; -- Replace 1 with your repository ID
```

### Option 3: Using Postman or Curl

1. **Get your auth token** (same as Option 1)

2. **Update the repository:**
```bash
curl -X PUT "http://localhost:7150/api/repository/1" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "IsPremium": true,
    "IsFree": false,
    "Price": 29.99,
    "GitHubRepoFullName": "CodeNex-Premium/your-repo-name"
  }'
```

## Required Values

For a premium repository to work with Stripe checkout, you MUST set:

1. **IsPremium**: `true`
2. **IsFree**: `false` (optional, but recommended)
3. **Price**: A decimal value greater than 0 (e.g., `29.99`)
4. **GitHubRepoFullName**: The full repository name in the format `Organization/repo-name`
   - This is used to invite the buyer to the private GitHub repository
   - Example: `CodeNex-Premium/premium-project`

## Testing

After updating your repository, test the purchase flow:

1. **Restart your application** (if it's running):
```powershell
# Stop the current instance (Ctrl+C)
dotnet run --project C:\Users\Az\source\repos\ayyaz081\Codenex\Codenex.csproj
```

2. **Open your repository page** in the browser

3. **Click the Purchase button** and enter a GitHub username

4. **You should now see the Stripe checkout** instead of the error

## Troubleshooting

### Still getting the error?
- Check the browser console for the exact repository ID being sent
- Verify that repository ID exists in your database
- Ensure Price is greater than 0
- Ensure GitHubRepoFullName is not null

### Can't connect to database?
- Check your .env file has the correct ConnectionStrings__DefaultConnection
- Verify your Azure SQL firewall rules allow your IP

### API endpoint not found?
- Make sure you rebuild the application: `dotnet build`
- Restart the application
- Check the API is running at `http://localhost:7150`

## Next Steps

After fixing the repository data:
1. ✅ Test the purchase flow end-to-end
2. ✅ Verify Stripe webhook is working
3. ✅ Test that GitHub invitations are sent correctly
4. ✅ Check that the payment is recorded in the database

## Need More Help?

Check the application logs for detailed error messages:
- Backend logs will show in the console where you run `dotnet run`
- Browser console (F12) will show frontend errors
- Check Stripe dashboard for payment-related issues
