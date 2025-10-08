using Octokit;

namespace CodeNex.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly GitHubClient _githubClient;
        private readonly string _organizationName;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(IConfiguration configuration, ILogger<GitHubService> logger)
        {
            _logger = logger;

            // Get configuration from environment variables
            var accessToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN") ??
                            configuration["GitHub:PersonalAccessToken"];
            
            _organizationName = Environment.GetEnvironmentVariable("GITHUB_ORGANIZATION_NAME") ??
                              configuration["GitHub:OrganizationName"] ??
                              "CodeNex-Premium";

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("GitHub Personal Access Token is not configured. Set GITHUB_PERSONAL_ACCESS_TOKEN in .env file.");
            }

            // Initialize GitHub client
            _githubClient = new GitHubClient(new ProductHeaderValue("CodeNex-App"))
            {
                Credentials = new Credentials(accessToken)
            };

            _logger.LogInformation($"GitHub Service initialized for organization: {_organizationName}");
        }

        public async Task<bool> InviteUserToRepositoryAsync(string githubUsername, string repositoryName, string? organizationName = null)
        {
            // Use provided organization name or fall back to default
            var orgName = organizationName ?? _organizationName;
            
            try
            {
                _logger.LogInformation($"Attempting to invite GitHub user '{githubUsername}' to repository '{orgName}/{repositoryName}'");

                // Add user as collaborator to the repository
                // Permission can be: pull, push, admin, maintain, triage
                await _githubClient.Repository.Collaborator.Add(
                    orgName,
                    repositoryName,
                    githubUsername
                );

                _logger.LogInformation($"✅ Successfully invited user '{githubUsername}' to repository '{orgName}/{repositoryName}'");
                _logger.LogInformation($"User '{githubUsername}' should receive an email invitation to access the repository.");
                return true;
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, $"❌ Repository '{orgName}/{repositoryName}' not found or user '{githubUsername}' doesn't exist");
                _logger.LogError($"Please verify: 1) Repository {orgName}/{repositoryName} exists and is private, 2) User {githubUsername} exists on GitHub");
                return false;
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"❌ GitHub API error while inviting user '{githubUsername}': {ex.Message}");
                _logger.LogError($"API Status: {ex.StatusCode}, Headers: {string.Join(", ", ex.Headers?.Keys ?? new string[0])}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Unexpected error inviting user '{githubUsername}' to repository");
                return false;
            }
        }

        public async Task<bool> CheckUserAccessAsync(string githubUsername, string repositoryName, string? organizationName = null)
        {
            // Use provided organization name or fall back to default
            var orgName = organizationName ?? _organizationName;
            
            try
            {
                _logger.LogInformation($"Checking access for user '{githubUsername}' to repository '{orgName}/{repositoryName}'");

                // Check if user is a collaborator
                var isCollaborator = await _githubClient.Repository.Collaborator.IsCollaborator(
                    orgName,
                    repositoryName,
                    githubUsername
                );

                _logger.LogInformation($"User '{githubUsername}' access status for '{orgName}/{repositoryName}': {isCollaborator}");
                return isCollaborator;
            }
            catch (NotFoundException)
            {
                _logger.LogWarning($"Repository '{orgName}/{repositoryName}' not found or user '{githubUsername}' doesn't exist");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking user access for '{githubUsername}'");
                return false;
            }
        }

        public async Task<bool> RevokeUserAccessAsync(string githubUsername, string repositoryName, string? organizationName = null)
        {
            // Use provided organization name or fall back to default
            var orgName = organizationName ?? _organizationName;
            
            try
            {
                _logger.LogInformation($"Revoking access for user '{githubUsername}' from repository '{orgName}/{repositoryName}'");

                // Remove user as collaborator
                await _githubClient.Repository.Collaborator.Delete(
                    orgName,
                    repositoryName,
                    githubUsername
                );

                _logger.LogInformation($"Successfully revoked access for user '{githubUsername}' from repository '{orgName}/{repositoryName}'");
                return true;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, $"Repository or user not found when revoking access for '{githubUsername}'");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking access for user '{githubUsername}'");
                return false;
            }
        }

        public async Task<bool> VerifyGitHubUsernameAsync(string githubUsername)
        {
            try
            {
                _logger.LogInformation($"Verifying GitHub username: '{githubUsername}'");

                // Try to get the user
                var user = await _githubClient.User.Get(githubUsername);

                _logger.LogInformation($"GitHub username '{githubUsername}' verified successfully");
                return user != null;
            }
            catch (NotFoundException)
            {
                _logger.LogWarning($"GitHub username '{githubUsername}' not found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying GitHub username '{githubUsername}'");
                return false;
            }
        }
    }
}
