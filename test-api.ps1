# Test script for Azure Web App API endpoints
# Replace 'your-app-name' with your actual Azure Web App name

$webAppName = "codenex"  # Based on your deployment file
$baseUrl = "https://codenex.azurewebsites.net"

Write-Host "Testing API endpoints for: $baseUrl" -ForegroundColor Green

# Test 1: Health check endpoint
Write-Host "`n1. Testing Health Check..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -TimeoutSec 30
    Write-Host "Health Check Status: $($healthResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Response: $($healthResponse.Content)"
}
catch {
    Write-Host "Health Check Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: API base route
Write-Host "`n2. Testing API base route..." -ForegroundColor Yellow
try {
    $apiResponse = Invoke-WebRequest -Uri "$baseUrl/api" -Method GET -TimeoutSec 30
    Write-Host "API Base Status: $($apiResponse.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "API Base Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

# Test 3: Products endpoint (assuming it's public)
Write-Host "`n3. Testing Products endpoint..." -ForegroundColor Yellow
try {
    $productsResponse = Invoke-WebRequest -Uri "$baseUrl/api/products" -Method GET -TimeoutSec 30
    Write-Host "Products Status: $($productsResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Response Length: $($productsResponse.Content.Length)"
}
catch {
    Write-Host "Products Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

# Test 4: Check if it's returning HTML instead of API responses
Write-Host "`n4. Checking response content type..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/products" -Method GET -TimeoutSec 30
    Write-Host "Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Cyan
    if ($response.Headers.'Content-Type' -like "*text/html*") {
        Write-Host "WARNING: API is returning HTML - this suggests routing issues!" -ForegroundColor Red
    }
}
catch {
    Write-Host "Content type check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Test with specific headers
Write-Host "`n5. Testing with API headers..." -ForegroundColor Yellow
try {
    $headers = @{
        'Accept' = 'application/json'
        'Content-Type' = 'application/json'
    }
    $response = Invoke-WebRequest -Uri "$baseUrl/api/products" -Method GET -Headers $headers -TimeoutSec 30
    Write-Host "API with headers Status: $($response.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "API with headers Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTest completed!" -ForegroundColor Green
