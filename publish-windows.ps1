# UORespawn Windows Publish Script
# This creates a self-contained .NET 10 Windows executable package

$version = "2.0.0.1"
$projectPath = "UORespawnApp"
$outputBase = ".\Releases"
$publishOutput = "$outputBase\windows-x64"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "UORespawn v$version - Windows Publisher" -ForegroundColor Cyan
Write-Host "  .NET 10 Self-Contained Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Create output directory
Write-Host "Creating output directory..." -ForegroundColor Yellow
New-Item -Path $outputBase -ItemType Directory -Force | Out-Null

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "$projectPath\bin") {
    Remove-Item "$projectPath\bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "$projectPath\obj") {
    Remove-Item "$projectPath\obj" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $publishOutput) {
    Remove-Item $publishOutput -Recurse -Force -ErrorAction SilentlyContinue
}

# Publish Windows x64 (Self-contained with CoreCLR runtime)
Write-Host ""
Write-Host "Publishing Windows x64 (Self-Contained)..." -ForegroundColor Green
Write-Host "This may take a few minutes..." -ForegroundColor Yellow

dotnet publish $projectPath `
-f net10.0-windows10.0.19041.0 `
-c Release `
-r win-x64 `
--self-contained true `
-p:RuntimeIdentifierOverride=win10-x64 `
-p:UseMonoRuntime=false `
-o $publishOutput

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Publish failed! Check errors above." -ForegroundColor Red
    exit 1
}

# Verify publish output
$publishPath = $publishOutput


# Verify publish output
$publishPath = $publishOutput

if (-not (Test-Path $publishPath)) {
    Write-Host ""
    Write-Host "? Publish directory not found!" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "$publishPath\UORespawnApp.exe")) {
    Write-Host ""
    Write-Host "? UORespawnApp.exe not found!" -ForegroundColor Red
    exit 1
}

# Create ZIP package
Write-Host ""
Write-Host "Creating distribution package..." -ForegroundColor Green

$zipName = "UORespawn-v$version-Windows.zip"
$zipPath = Join-Path $outputBase $zipName

# Remove old ZIP if exists
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Create ZIP
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -CompressionLevel Optimal

# Get file size
$fileSize = (Get-Item $zipPath).Length / 1MB

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "? SUCCESS!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package: $zipName" -ForegroundColor Cyan
Write-Host "Size: $("{0:N2}" -f $fileSize) MB" -ForegroundColor Cyan
Write-Host "Framework: .NET 10.0 (Self-Contained)" -ForegroundColor Cyan
Write-Host "Runtime: CoreCLR (Windows)" -ForegroundColor Cyan
Write-Host "Location: $(Resolve-Path $zipPath)" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Ready to upload to GitHub Releases!" -ForegroundColor Yellow
Write-Host ""

