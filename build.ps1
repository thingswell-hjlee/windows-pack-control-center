# build.ps1 - Windows Pack Control Center Build Script
# Builds the React frontend and publishes the .NET gateway as a self-contained win-x64 application.
# Output: dist/ folder containing the complete deployment package.

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$WebDir = Join-Path $ProjectRoot "web"
$GatewayDir = Join-Path $ProjectRoot "gateway" "src"
$OutputPath = Join-Path $ProjectRoot $OutputDir

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Control Center Build Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean output directory
Write-Host "[1/3] Cleaning output directory..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
}

# Step 2: Build React frontend
Write-Host "[2/3] Building React frontend..." -ForegroundColor Yellow
Push-Location $WebDir
try {
    Write-Host "  Installing npm dependencies..."
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }

    Write-Host "  Building frontend..."
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
}
finally {
    Pop-Location
}
Write-Host "  Frontend built successfully." -ForegroundColor Green

# Step 3: Publish .NET Gateway
Write-Host "[3/3] Publishing .NET Gateway ($Configuration, $Runtime, self-contained)..." -ForegroundColor Yellow
Push-Location $GatewayDir
try {
    dotnet publish -c $Configuration -r $Runtime --self-contained -o $OutputPath
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
}
finally {
    Pop-Location
}
Write-Host "  Gateway published successfully." -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output: $OutputPath" -ForegroundColor White
Write-Host ""
Write-Host "To run the application:" -ForegroundColor White
Write-Host "  cd $OutputDir" -ForegroundColor Gray
Write-Host "  .\ControlCenter.Gateway.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "Access the dashboard at: http://localhost:8088" -ForegroundColor White
