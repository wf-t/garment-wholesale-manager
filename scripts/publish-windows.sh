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
"$DOTNET_BIN" publish "$PROJECT_DIR/src/Shanquan.Inventory.Desktop/Shanquan.Inventory.Desktop.csproj" \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output "$PROJECT_DIR/releases/win7-x64" \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  -p:DebugType=none \
  -p:DebugSymbols=false

echo "Windows 7 x64 发布文件位于 releases/win7-x64。"
