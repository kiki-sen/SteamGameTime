#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up the development environment for SteamGameTime backend
.DESCRIPTION
    This script helps set up your local development environment by copying the appsettings template
    and prompting for your Steam API key.
.EXAMPLE
    .\setup-dev.ps1
#>

param(
    [string]$ApiKey = $null,
    [switch]$UseUserSecrets = $false,
    [switch]$Help = $false
)

if ($Help) {
    Get-Help $MyInvocation.MyCommand.Definition -Full
    exit 0
}

Write-Host "🎮 SteamGameTime Development Environment Setup" -ForegroundColor Cyan
Write-Host "=" * 50

$steamApiProjectPath = Join-Path $PSScriptRoot "Steam API"
$templateFile = Join-Path $steamApiProjectPath "appsettings.Development.json.template"
$devSettingsFile = Join-Path $steamApiProjectPath "appsettings.Development.json"

# Check if template exists
if (-not (Test-Path $templateFile)) {
    Write-Error "Template file not found: $templateFile"
    Write-Host "Please make sure you're running this script from the backend directory."
    exit 1
}

# Get API key from user if not provided
if (-not $ApiKey) {
    Write-Host ""
    Write-Host "You need a Steam Web API key to run the application locally." -ForegroundColor Yellow
    Write-Host "Get one at: https://steamcommunity.com/dev/apikey" -ForegroundColor Blue
    Write-Host ""
    
    do {
        $ApiKey = Read-Host "Enter your Steam API key (or 'skip' to set it manually later)"
        if ($ApiKey -eq 'skip') {
            $ApiKey = "YOUR_STEAM_API_KEY_HERE"
            break
        }
    } while ([string]::IsNullOrWhiteSpace($ApiKey))
}

if ($UseUserSecrets) {
    Write-Host "Setting up User Secrets..." -ForegroundColor Green
    
    Push-Location $steamApiProjectPath
    try {
        # Initialize user secrets if not already done
        $initResult = dotnet user-secrets init 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ User secrets initialized"
        }
        
        # Set the API key
        $setResult = dotnet user-secrets set "Steam:ApiKey" $ApiKey 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Steam API key set in user secrets"
            Write-Host ""
            Write-Host "Your API key is now securely stored in user secrets." -ForegroundColor Green
        } else {
            Write-Error "Failed to set user secret: $setResult"
            exit 1
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "Creating appsettings.Development.json..." -ForegroundColor Green
    
    # Read template and replace placeholder
    $templateContent = Get-Content $templateFile -Raw
    $devContent = $templateContent -replace "YOUR_STEAM_API_KEY_HERE", $ApiKey
    
    # Write to development settings
    $devContent | Set-Content $devSettingsFile -Encoding UTF8
    Write-Host "✅ Created $devSettingsFile"
}

Write-Host ""
Write-Host "Testing configuration..." -ForegroundColor Yellow

Push-Location $steamApiProjectPath
try {
    $buildResult = dotnet build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
    } else {
        Write-Warning "Build failed. Please check your configuration."
        Write-Host $buildResult
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "🎉 Setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run the application: " -NoNewline
Write-Host "dotnet run --project `"Steam API`" --environment Development" -ForegroundColor Yellow
Write-Host "2. Run tests: " -NoNewline  
Write-Host "dotnet test `"Steam API Tests`"" -ForegroundColor Yellow
Write-Host ""
Write-Host "📖 See DEVELOPMENT-SETUP.md for more detailed instructions."