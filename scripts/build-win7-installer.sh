#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
MAKENSIS_BIN="${MAKENSIS_BIN:-$PROJECT_DIR/.tools/makensis/bin/makensis}"
NSIS_DIR="${NSISDIR:-$PROJECT_DIR/.tools/makensis/share/nsis}"
OUTPUT_FILE="$PROJECT_DIR/releases/服装进销存批发管理-Windows7-安装版.exe"

KB_FILE="$PROJECT_DIR/installer/prerequisites/Windows6.1-KB3063858-x64.msu"
VC_FILE="$PROJECT_DIR/installer/prerequisites/VC_redist.x64.exe"
KB_SHA256="6fec4e38cdcbdaa334937a2ef38bad6800e9c80cb513183451b4049e84479a85"
VC_SHA256="003063723b2131da23f40e2063fb79867bae275f7b5c099dbd1792e25845872b"

if [[ ! -x "$MAKENSIS_BIN" ]]; then
  echo "未找到 makensis：$MAKENSIS_BIN"
  exit 1
fi

verify_sha256() {
  local file="$1"
  local expected="$2"
  local actual
  actual="$(shasum -a 256 "$file" | awk '{print $1}')"
  if [[ "$actual" != "$expected" ]]; then
    echo "依赖文件校验失败：$file"
    exit 1
  fi
}

verify_sha256 "$KB_FILE" "$KB_SHA256"
verify_sha256 "$VC_FILE" "$VC_SHA256"

"$PROJECT_DIR/scripts/publish-windows.sh"

NSISDIR="$NSIS_DIR" "$MAKENSIS_BIN" \
  -DPROJECT_DIR="$PROJECT_DIR" \
  -DOUTPUT_FILE="$OUTPUT_FILE" \
  "$PROJECT_DIR/installer/GarmentWholesaleManager.Win7.nsi"

echo "Windows 7 一键安装包位于：$OUTPUT_FILE"
