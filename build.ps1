# build.ps1 - Windows Pack Control Center Build Script
# Builds the React frontend, copies to gateway wwwroot, and publishes as a self-contained single-file win-x64 application.
# Output: dist/ folder containing the complete deployment package.
# Startup time target: < 5 seconds (self-contained single-file exe)

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$WebDir = Join-Path $ProjectRoot "web"
$GatewayDir = Join-Path $ProjectRoot "gateway" "src"
$WwwrootDir = Join-Path $GatewayDir "wwwroot"
$OutputPath = Join-Path $ProjectRoot $OutputDir

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Control Center Build Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean output directory
Write-Host "[1/4] Cleaning output directory..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
}

# Step 2: Build React frontend
Write-Host "[2/4] Building React frontend..." -ForegroundColor Yellow
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

# Step 3: Copy frontend build to gateway wwwroot
Write-Host "[3/4] Copying frontend assets to gateway/src/wwwroot/..." -ForegroundColor Yellow
$WebDistDir = Join-Path $WebDir "dist"
if (-not (Test-Path $WebDistDir)) {
    throw "Frontend build output not found at $WebDistDir"
}
# Clean existing wwwroot and copy fresh build
if (Test-Path $WwwrootDir) {
    Remove-Item -Recurse -Force $WwwrootDir
}
Copy-Item -Recurse $WebDistDir $WwwrootDir
Write-Host "  Frontend assets copied to wwwroot." -ForegroundColor Green

# Step 4: Publish .NET Gateway (self-contained single-file)
Write-Host "[4/4] Publishing .NET Gateway ($Configuration, $Runtime, self-contained, single-file)..." -ForegroundColor Yellow
Push-Location $GatewayDir
try {
    dotnet publish -c $Configuration -r $Runtime --self-contained -p:PublishSingleFile=true -o $OutputPath
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
Write-Host "  - Single-file executable: ControlCenter.Gateway.exe" -ForegroundColor White
Write-Host "  - Self-contained: No .NET runtime required on target machine" -ForegroundColor White
Write-Host "  - Target: Windows x64" -ForegroundColor White
Write-Host "  - Startup time target: < 5 seconds" -ForegroundColor White
Write-Host ""
Write-Host "To run the application:" -ForegroundColor White
Write-Host "  cd $OutputDir" -ForegroundColor Gray
Write-Host "  .\ControlCenter.Gateway.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "Access the dashboard at: http://localhost:8088" -ForegroundColor White
