#!/usr/bin/env dotnet-script
// Quick fix for Repository ID 5
// Run with: dotnet script FixRepo.csx

#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.0"
#r "nuget: Microsoft.Extensions.Configuration, 8.0.0"
#r "nuget: Microsoft.Extensions.Configuration.EnvironmentVariables, 8.0.0"
#r "nuget: DotNetEnv, 3.0.0"

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using DotNetEnv;

// Load .env file
var basePath = @"C:\Users\Az\source\repos\ayyaz081\Codenex";
Env.Load(Path.Combine(basePath, ".env"));

// Get connection string
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: Connection string not found!");
    return 1;
}

Console.WriteLine("Connecting to database...");
Console.WriteLine("");

// Simple SQL execution
using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
await connection.OpenAsync();

var repositoryId = 5;
var price = 29.99m;
var githubRepoFullName = "CodeNex-Premium/test-repo"; // CHANGE THIS!

var sql = $@"
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = @price,
    GitHubRepoFullName = @githubRepoFullName,
    UpdatedAt = GETUTCDATE()
WHERE Id = @repositoryId;

SELECT 
    Id, Title, IsPremium, IsFree, Price, GitHubRepoFullName
FROM Repositories
WHERE Id = @repositoryId;
";

using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
command.Parameters.AddWithValue("@price", price);
command.Parameters.AddWithValue("@githubRepoFullName", githubRepoFullName);
command.Parameters.AddWithValue("@repositoryId", repositoryId);

var rowsAffected = await command.ExecuteNonQueryAsync();

Console.WriteLine($"[SUCCESS] Updated {rowsAffected} row(s)");
Console.WriteLine("");
Console.WriteLine("Updated repository:");
Console.WriteLine($"  ID: {repositoryId}");
Console.WriteLine($"  Price: ${price}");
Console.WriteLine($"  GitHubRepoFullName: {githubRepoFullName}");
Console.WriteLine("");
Console.WriteLine("Try purchasing again!");

return 0;
