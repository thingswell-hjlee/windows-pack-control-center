# build-installer.ps1 - Build the Windows Installer package
# Prerequisites: Inno Setup 6.x installed (iscc.exe in PATH or at default location)
# This script:
# 1. Builds the React frontend
# 2. Copies frontend to gateway wwwroot
# 3. Publishes .NET Gateway as self-contained single-file
# 4. Compiles the Inno Setup installer

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$WebDir = Join-Path $ProjectRoot "web"
$GatewayDir = Join-Path $ProjectRoot "gateway" "src"
$WwwrootDir = Join-Path $GatewayDir "wwwroot"
$PublishDir = Join-Path $ProjectRoot "dist" "publish"
$InstallerDir = Join-Path $ProjectRoot "installer" "inno-setup"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Control Center Installer Build" -ForegroundColor Cyan  
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build React frontend
Write-Host "[1/4] Building React frontend..." -ForegroundColor Yellow
Push-Location $WebDir
try {
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
} finally {
    Pop-Location
}
Write-Host "  Frontend built." -ForegroundColor Green

# Step 2: Copy frontend to wwwroot
Write-Host "[2/4] Copying frontend to gateway/src/wwwroot/..." -ForegroundColor Yellow
$WebDistDir = Join-Path $WebDir "dist"
if (Test-Path $WwwrootDir) { Remove-Item -Recurse -Force $WwwrootDir }
Copy-Item -Recurse $WebDistDir $WwwrootDir
Write-Host "  Copied." -ForegroundColor Green

# Step 3: Publish .NET Gateway
Write-Host "[3/4] Publishing .NET Gateway (self-contained, single-file, $Runtime)..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
Push-Location $GatewayDir
try {
    dotnet publish -c $Configuration -r $Runtime --self-contained -p:PublishSingleFile=true -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
} finally {
    Pop-Location
}
Write-Host "  Published." -ForegroundColor Green

# Step 4: Compile Inno Setup installer
Write-Host "[4/4] Compiling Inno Setup installer..." -ForegroundColor Yellow
$SetupScript = Join-Path $InstallerDir "setup.iss"

if (Test-Path $InnoSetupPath) {
    & $InnoSetupPath $SetupScript
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
    Write-Host "  Installer compiled." -ForegroundColor Green
} else {
    Write-Host "  WARNING: Inno Setup not found at $InnoSetupPath" -ForegroundColor Yellow
    Write-Host "  Skipping installer compilation. Install Inno Setup 6.x to build the installer." -ForegroundColor Yellow
    Write-Host "  Download: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Published files: $PublishDir" -ForegroundColor White
Write-Host "Installer output: $ProjectRoot\dist\installer\" -ForegroundColor White
Write-Host ""
