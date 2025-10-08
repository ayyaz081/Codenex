-- IMMEDIATE FIX FOR REPOSITORY ID 5
-- Copy and paste this into Azure Data Studio, SSMS, or Azure Portal Query Editor

-- Connect to:
-- Server: codenex.database.windows.net
-- Database: codenex

-- 1. Check current state
SELECT 
    Id, 
    Title, 
    IsPremium, 
    IsFree,
    Price, 
    GitHubRepoFullName,
    IsActive
FROM Repositories
WHERE Id = 5;

-- 2. FIX IT NOW - Update Repository 5
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = 29.99,
    GitHubRepoFullName = 'CodeNex-Premium/test-repo',
    UpdatedAt = GETUTCDATE()
WHERE Id = 5;

-- 3. Verify the fix
SELECT 
    Id, 
    Title, 
    IsPremium, 
    IsFree,
    Price, 
    GitHubRepoFullName,
    UpdatedAt
FROM Repositories
WHERE Id = 5;

-- IMPORTANT: Change 'CodeNex-Premium/test-repo' to your actual GitHub organization and repository name!
-- Format: 'YourGitHubOrg/YourRepoName'
