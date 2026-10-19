#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
DOTNET_BIN="$PROJECT_DIR/.tools/dotnet/dotnet"

if [[ ! -x "$DOTNET_BIN" ]]; then
  "$PROJECT_DIR/scripts/bootstrap-mac.sh"
fi

export DOTNET_CLI_HOME="$PROJECT_DIR/.tools/dotnet-home"
export NUGET_PACKAGES="$PROJECT_DIR/.tools/nuget-packages"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
"$DOTNET_BIN" test "$PROJECT_DIR/Shanquan.Inventory.slnx" --configuration Release

