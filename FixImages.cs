using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioBackend.Data;

Console.WriteLine("Fixing image URLs in database...");

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// Update Products with existing images
var products = await context.Products.ToListAsync();
var productImageMappings = new Dictionary<string, string>
{
    ["E-Commerce Platform"] = "/content/web-ecommerce-demo.jpg",
    ["Task Management Mobile App"] = "/content/mobile-fitness-demo.jpg", 
    ["AI-Powered Chatbot"] = "/content/ai-chatbot-demo.jpg",
    ["Data Analytics Dashboard"] = "/content/ai-analytics-demo.jpg",
    ["Cloud Infrastructure Automation"] = "/content/cloud-devops-demo.jpg"
};

foreach (var product in products)
{
    if (productImageMappings.ContainsKey(product.Title))
    {
        product.ImageUrl = productImageMappings[product.Title];
        Console.WriteLine($"Updated {product.Title} -> {product.ImageUrl}");
    }
}

// Update Publications with existing images
var publications = await context.Publications.ToListAsync();
var publicationImageMappings = new Dictionary<string, string>
{
    ["Modern Web Development Practices: A Comprehensive Guide"] = "/content/products-hero.jpg",
    ["Machine Learning in Mobile Applications: Challenges and Solutions"] = "/content/ai.jpg",
    ["Cloud Security Best Practices for Enterprise Applications"] = "/content/cybersecurity.jpg"
};

foreach (var publication in publications)
{
    if (publicationImageMappings.ContainsKey(publication.Title))
    {
        publication.ThumbnailUrl = publicationImageMappings[publication.Title];
        Console.WriteLine($"Updated {publication.Title} -> {publication.ThumbnailUrl}");
    }
}

// Update Solutions with existing images
var solutions = await context.Solutions.ToListAsync();
var solutionImageMappings = new Dictionary<string, string>
{
    ["Smart Inventory Management"] = "/content/solution-inventory-demo.jpg",
    ["Remote Team Collaboration Hub"] = "/content/solution-webapp-demo.jpg",
    ["Automated Code Review System"] = "/content/solution-api-demo.jpg",
    ["Smart Energy Management"] = "/content/solution-iot-demo.jpg"
};

foreach (var solution in solutions)
{
    if (solutionImageMappings.ContainsKey(solution.Title))
    {
        solution.DemoImageUrl = solutionImageMappings[solution.Title];
        Console.WriteLine($"Updated {solution.Title} -> {solution.DemoImageUrl}");
    }
}

// Save changes
await context.SaveChangesAsync();
Console.WriteLine("✅ All image URLs have been updated to use existing images!");
