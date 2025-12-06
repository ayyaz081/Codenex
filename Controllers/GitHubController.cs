using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codenex.Services;

namespace Codenex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitHubController : ControllerBase
    {
        private readonly IGitHubService _githubService;
        private readonly ILogger<GitHubController> _logger;

        public GitHubController(IGitHubService githubService, ILogger<GitHubController> logger)
        {
            _githubService = githubService;
            _logger = logger;
        }

        // GET: api/github/repository/{owner}/{repo}
        [HttpGet("repository/{owner}/{repo}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<GitHubRepositoryDetails>> GetRepositoryDetails(
            [FromRoute] string owner,
            [FromRoute] string repo)
        {
            try
            {
                _logger.LogInformation($"Fetching GitHub repository details for {owner}/{repo}");

                var details = await _githubService.GetRepositoryDetailsAsync(owner, repo);

                if (details == null)
                {
                    return NotFound(new { message = $"Repository {owner}/{repo} not found" });
                }

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching repository details for {owner}/{repo}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
