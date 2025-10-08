-- Quick fix for Repository ID 5
-- Update the values below as needed

UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = 29.99,  -- Change this to your desired price
    GitHubRepoFullName = 'CodeNex-Premium/test-repo',  -- IMPORTANT: Change this to your actual GitHub org/repo
    UpdatedAt = GETUTCDATE()
WHERE Id = 5;

-- Verify the update
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
