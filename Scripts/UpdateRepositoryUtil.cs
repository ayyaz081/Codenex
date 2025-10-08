using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

// Quick utility to update repository settings
// Usage: Run this after logging in as admin to update a repository

public class UpdateRepositoryUtil
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string BASE_URL = "http://localhost:7150";

    public static async Task Main(string[] args)
    {
        try
        {
            // CONFIGURATION - Update these values
            var repositoryId = 1; // Change this to your repository ID
            var price = 29.99m; // Change this to your desired price
            var githubRepoFullName = "CodeNex-Premium/your-repo-name"; // Change this to your GitHub org/repo
            var authToken = "YOUR_JWT_TOKEN_HERE"; // Replace with your actual JWT token after logging in

            Console.WriteLine("CodeNex Repository Update Utility");
            Console.WriteLine("=================================\n");
            
            // Set authorization header
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            // Create update payload
            var updatePayload = new
            {
                IsPremium = true,
                IsFree = false,
                Price = price,
                GitHubRepoFullName = githubRepoFullName
            };

            Console.WriteLine($"Updating Repository ID: {repositoryId}");
            Console.WriteLine($"Setting Price: ${price}");
            Console.WriteLine($"Setting GitHub Repo: {githubRepoFullName}");
            Console.WriteLine($"Setting IsPremium: true\n");

            // Send PUT request
            var response = await httpClient.PutAsJsonAsync(
                $"{BASE_URL}/api/repository/{repositoryId}", 
                updatePayload
            );

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✓ Repository updated successfully!");
                
                // Fetch and display the updated repository
                var getResponse = await httpClient.GetAsync($"{BASE_URL}/api/repository/{repositoryId}");
                if (getResponse.IsSuccessStatusCode)
                {
                    var repoJson = await getResponse.Content.ReadAsStringAsync();
                    Console.WriteLine("\nUpdated Repository Data:");
                    Console.WriteLine(JsonSerializer.Serialize(
                        JsonSerializer.Deserialize<JsonElement>(repoJson), 
                        new JsonSerializerOptions { WriteIndented = true }
                    ));
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Failed to update repository: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
