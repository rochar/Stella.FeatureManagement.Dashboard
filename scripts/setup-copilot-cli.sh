#!/usr/bin/env bash
# Installs the GitHub Copilot CLI plugins used by the Stella.FeatureManagement.Dashboard team.
#
# Copilot CLI plugins are installed at the user level (under
# ~/.copilot/installed-plugins/), so they are NOT committed to the repo.
# This script installs the canonical set so every developer gets the same
# agents, skills, and instructions.
#
# Usage:
#   ./scripts/setup-copilot-cli.sh                # install missing plugins
#   ./scripts/setup-copilot-cli.sh --update       # also update already-installed plugins

set -euo pipefail

PLUGINS=(
  "awesome-copilot@awesome-copilot"
  "software-engineering-team@awesome-copilot"
  "copilot-sdk@awesome-copilot"
  "csharp-dotnet-development@awesome-copilot"
  "microsoft-docs"
  "dotnet@dotnet-agent-skills"
  "dotnet-data@dotnet-agent-skills"
  "dotnet-diag@dotnet-agent-skills"
  "dotnet-upgrade@dotnet-agent-skills"
  "dotnet-msbuild@dotnet-agent-skills"
  "dotnet-ai@dotnet-agent-skills"
)

UPDATE_EXISTING=0
if [[ "${1:-}" == "--update" ]]; then
  UPDATE_EXISTING=1
fi

if ! command -v copilot >/dev/null 2>&1; then
  echo "ERROR: GitHub Copilot CLI is not installed or not on PATH." >&2
  echo "See https://docs.github.com/copilot/concepts/agents/about-copilot-cli" >&2
  exit 1
fi

echo "Installing GitHub Copilot CLI plugins for Stella.FeatureManagement.Dashboard..."

INSTALLED="$(copilot plugin list 2>&1 || true)"

for plugin in "${PLUGINS[@]}"; do
  short="${plugin%%@*}"
  if echo "$INSTALLED" | grep -q -- "$short"; then
    if [[ $UPDATE_EXISTING -eq 1 ]]; then
      echo "  Updating $plugin..."
      copilot plugin update "$plugin"
    else
      echo "  Skipping $plugin (already installed; pass --update to refresh)"
    fi
  else
    echo "  Installing $plugin..."
    copilot plugin install "$plugin"
  fi
done

echo ""
echo "Done. Run '/restart' inside an active Copilot CLI session to load the new plugins."
