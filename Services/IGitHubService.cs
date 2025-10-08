namespace CodeNex.Services
{
    public interface IGitHubService
    {
        /// <summary>
        /// Invites a GitHub user to a private repository in the organization
        /// </summary>
        /// <param name="githubUsername">The GitHub username to invite</param>
        /// <param name="repositoryName">The repository name</param>
        /// <param name="organizationName">Optional organization name (uses default if not provided)</param>
        Task<bool> InviteUserToRepositoryAsync(string githubUsername, string repositoryName, string? organizationName = null);

        /// <summary>
        /// Checks if a user has access to a repository
        /// </summary>
        /// <param name="githubUsername">The GitHub username to check</param>
        /// <param name="repositoryName">The repository name</param>
        /// <param name="organizationName">Optional organization name (uses default if not provided)</param>
        Task<bool> CheckUserAccessAsync(string githubUsername, string repositoryName, string? organizationName = null);

        /// <summary>
        /// Revokes user's access to a repository (for refunds/cancellations)
        /// </summary>
        /// <param name="githubUsername">The GitHub username to revoke</param>
        /// <param name="repositoryName">The repository name</param>
        /// <param name="organizationName">Optional organization name (uses default if not provided)</param>
        Task<bool> RevokeUserAccessAsync(string githubUsername, string repositoryName, string? organizationName = null);

        /// <summary>
        /// Verifies if a GitHub username exists
        /// </summary>
        Task<bool> VerifyGitHubUsernameAsync(string githubUsername);
    }
}
