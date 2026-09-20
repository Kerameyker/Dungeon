#!/bin/bash
# Compile all Core+Runtime sources against the hand-written UnityEngine stub (UnityCore.cs / UnityUI.cs).
# Variants: legacy input, ENABLE_INPUT_SYSTEM, and ENABLE_INPUT_SYSTEM against netstandard2.1 (Unity's real BCL surface).
cd "$(dirname "$0")"
OUT="${TMPDIR:-/tmp}/hollow_fullcheck"
run() { # variant defines tfm
  echo "=== variant: $1 [$2] tfm=$3 ==="
  rm -rf "$OUT/$1"
  dotnet build fullcheck.csproj -nologo -v q -p:Variant=$1 -p:ExtraDefines="$2" -p:TFM=$3 -p:NuGetAudit=false \
    -p:BaseIntermediateOutputPath="$OUT/$1/obj/" -p:BaseOutputPath="$OUT/$1/bin/" 2>&1 \
    | grep -E "warning|error|Warn|Error" | cut -c1-300 | sort -u
}
run legacy "" net8.0
run inputsystem "ENABLE_INPUT_SYSTEM" net8.0
run ns21 "ENABLE_INPUT_SYSTEM" netstandard2.1
