#requires -Version 6.0
<#
.SYNOPSIS
    Installs the GitHub Copilot CLI plugins used by the Stella.FeatureManagement.Dashboard team.

.DESCRIPTION
    Copilot CLI plugins are installed at the user level (under
    %USERPROFILE%\.copilot\installed-plugins\), so they are NOT committed to
    the repository. This script installs the canonical set of plugins used by
    the team so every developer gets the same agents, skills, and instructions
    in their Copilot CLI sessions.

    Run this once after installing the Copilot CLI, and re-run it whenever the
    plugin list below changes.

.PREREQUISITES
    - GitHub Copilot CLI installed and authenticated (`copilot --version`).
    - PowerShell 6+ (Windows PowerShell 5 is not supported).

.EXAMPLE
    pwsh ./scripts/setup-copilot-cli.ps1

.EXAMPLE
    pwsh ./scripts/setup-copilot-cli.ps1 -UpdateExisting
#>

[CmdletBinding()]
param(
    [switch] $UpdateExisting
)

$ErrorActionPreference = 'Stop'

# Canonical plugin set for Stella.FeatureManagement.Dashboard.
# Format: 'plugin-name@marketplace'
$Plugins = @(
    'awesome-copilot@awesome-copilot',
    'software-engineering-team@awesome-copilot',
    'copilot-sdk@awesome-copilot',
    'csharp-dotnet-development@awesome-copilot',
    'microsoft-docs',
    'dotnet@dotnet-agent-skills',
    'dotnet-data@dotnet-agent-skills',
    'dotnet-diag@dotnet-agent-skills',
    'dotnet-upgrade@dotnet-agent-skills',
    'dotnet-msbuild@dotnet-agent-skills',
    'dotnet-ai@dotnet-agent-skills'
)

if (-not (Get-Command copilot -ErrorAction SilentlyContinue)) {
    throw "GitHub Copilot CLI is not installed or not on PATH. See https://docs.github.com/copilot/concepts/agents/about-copilot-cli"
}

Write-Host "Installing GitHub Copilot CLI plugins for Stella.FeatureManagement.Dashboard..." -ForegroundColor Cyan

$installed = (& copilot plugin list 2>&1) -join "`n"

foreach ($plugin in $Plugins) {
    $shortName = $plugin.Split('@')[0]
    $alreadyInstalled = $installed -match [regex]::Escape($shortName)

    if ($alreadyInstalled) {
        if ($UpdateExisting) {
            Write-Host "  Updating $plugin..." -ForegroundColor Yellow
            & copilot plugin update $plugin
        }
        else {
            Write-Host "  Skipping $plugin (already installed; pass -UpdateExisting to refresh)" -ForegroundColor DarkGray
        }
    }
    else {
        Write-Host "  Installing $plugin..." -ForegroundColor Green
        & copilot plugin install $plugin
    }
}

Write-Host ""
Write-Host "Done. Run '/restart' inside an active Copilot CLI session to load the new plugins." -ForegroundColor Cyan
