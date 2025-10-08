namespace CodeNex.Services
{
    public interface IGitHubService
    {
        /// <summary>
        /// Invites a GitHub user to a private repository in the organization
        /// </summary>
        Task<bool> InviteUserToRepositoryAsync(string githubUsername, string repositoryName);

        /// <summary>
        /// Checks if a user has access to a repository
        /// </summary>
        Task<bool> CheckUserAccessAsync(string githubUsername, string repositoryName);

        /// <summary>
        /// Revokes user's access to a repository (for refunds/cancellations)
        /// </summary>
        Task<bool> RevokeUserAccessAsync(string githubUsername, string repositoryName);

        /// <summary>
        /// Verifies if a GitHub username exists
        /// </summary>
        Task<bool> VerifyGitHubUsernameAsync(string githubUsername);
    }
}
