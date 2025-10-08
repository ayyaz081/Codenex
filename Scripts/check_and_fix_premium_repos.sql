-- Script to check and fix premium repository settings
-- Run this against your codenex database

-- 1. Check current repository data
SELECT 
    Id, 
    Title, 
    IsPremium, 
    IsFree,
    Price, 
    GitHubRepoFullName,
    IsActive
FROM Repositories
WHERE IsActive = 1
ORDER BY Id;

-- 2. Find repositories marked as Premium but missing Price or GitHubRepoFullName
SELECT 
    Id, 
    Title, 
    IsPremium,
    Price,
    GitHubRepoFullName,
    CASE 
        WHEN IsPremium = 1 AND (Price IS NULL OR Price <= 0) THEN 'Missing Price'
        WHEN IsPremium = 1 AND GitHubRepoFullName IS NULL THEN 'Missing GitHub Repo Name'
        ELSE 'OK'
    END AS Issue
FROM Repositories
WHERE IsActive = 1 
    AND IsPremium = 1 
    AND (Price IS NULL OR Price <= 0 OR GitHubRepoFullName IS NULL);

-- 3. EXAMPLE: Update a specific repository to be premium with price
-- Replace [REPO_ID] with the actual repository ID you want to make premium
-- Replace [PRICE] with the price (e.g., 29.99)
-- Replace [GITHUB_REPO_FULL_NAME] with the full GitHub repo name (e.g., 'YourOrg/repo-name')
/*
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = [PRICE],
    GitHubRepoFullName = '[GITHUB_REPO_FULL_NAME]',
    UpdatedAt = GETUTCDATE()
WHERE Id = [REPO_ID];
*/

-- 4. EXAMPLE: Update multiple repositories at once
-- Uncomment and modify as needed
/*
-- Example: Make repository with ID 1 premium with $19.99 price
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = 19.99,
    GitHubRepoFullName = 'CodeNex-Premium/repository-name',
    UpdatedAt = GETUTCDATE()
WHERE Id = 1;

-- Example: Make repository with ID 2 premium with $29.99 price
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = 29.99,
    GitHubRepoFullName = 'CodeNex-Premium/another-repo',
    UpdatedAt = GETUTCDATE()
WHERE Id = 2;
*/

-- 5. Verify the updates
SELECT 
    Id, 
    Title, 
    IsPremium, 
    IsFree,
    Price, 
    GitHubRepoFullName,
    UpdatedAt
FROM Repositories
WHERE IsActive = 1
ORDER BY UpdatedAt DESC;
