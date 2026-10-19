#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
DOTNET_DIR="$PROJECT_DIR/.tools/dotnet"

if [[ ! -x "$DOTNET_DIR/dotnet" ]]; then
  mkdir -p "$PROJECT_DIR/.tools"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$PROJECT_DIR/.tools/dotnet-install.sh"
  bash "$PROJECT_DIR/.tools/dotnet-install.sh" --channel 10.0 --install-dir "$DOTNET_DIR"
fi

export DOTNET_CLI_HOME="$PROJECT_DIR/.tools/dotnet-home"
export NUGET_PACKAGES="$PROJECT_DIR/.tools/nuget-packages"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"
"$DOTNET_DIR/dotnet" restore "$PROJECT_DIR/Shanquan.Inventory.slnx"

echo "开发环境已准备完成。"

