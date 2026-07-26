#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Add test projects here. Each entry is a path relative to SCRIPT_DIR.
ALL_PROJECTS=(
  "Get.RegexMachine.Test"
  # "Get.SourceGenerator.Test" # not supported yet
  # "Get.LangSupport.Test" # not supported yet - minor issues left on JSON serialization
  "Get.Lexer.Test"
  "Get.Parser.Test"
)

NO_BUILD=false
FILTER_PROJECT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-build)
      NO_BUILD=true
      shift
      ;;
    *)
      FILTER_PROJECT="$1"
      shift
      ;;
  esac
done

PROJECTS=()
if [[ -n "$FILTER_PROJECT" ]]; then
  for project in "${ALL_PROJECTS[@]}"; do
    if [[ "$project" == "$FILTER_PROJECT" || "$(basename "$project")" == "$FILTER_PROJECT" ]]; then
      PROJECTS+=("$project")
    fi
  done
  if [[ ${#PROJECTS[@]} -eq 0 ]]; then
    echo "ERROR: No matching project found for '$FILTER_PROJECT'"
    exit 1
  fi
else
  PROJECTS=("${ALL_PROJECTS[@]}")
fi

FAILED=()

for project in "${PROJECTS[@]}"; do
  echo "========================================"
  if [[ "$NO_BUILD" == false ]]; then
    echo "Publishing: $project"
  else
    echo "Testing: $project"
  fi
  echo "========================================"

  cd "$SCRIPT_DIR/$project"

  if [[ "$NO_BUILD" == false ]]; then
    dotnet publish -c Release
  fi

  # Find the publish output directory (linux-x64/publish)
  publish_dir=$(find bin/Release -type d -name "publish" | head -1)
  if [ -z "$publish_dir" ]; then
    echo "ERROR: Could not find publish directory for $project"
    FAILED+=("$project")
    continue
  fi

  # The native binary name matches the project name
  binary_name=$(basename "$project")
  binary_path="$publish_dir/$binary_name"

  if [ ! -f "$binary_path" ]; then
    echo "ERROR: Binary not found: $binary_path"
    FAILED+=("$project")
    continue
  fi

  echo "----------------------------------------"
  echo "Running: $binary_path"
  echo "----------------------------------------"

  if "$binary_path"; then
    echo "PASSED: $project"
  else
    echo "FAILED: $project (exit code $?)"
    FAILED+=("$project")
  fi

  cd "$SCRIPT_DIR"
  echo ""
done

echo "========================================"
echo "SUMMARY"
echo "========================================"
echo "Total: ${#PROJECTS[@]}"
echo "Passed: $((${#PROJECTS[@]} - ${#FAILED[@]}))"
echo "Failed: ${#FAILED[@]}"

if [ ${#FAILED[@]} -gt 0 ]; then
  echo ""
  echo "Failed projects:"
  for f in "${FAILED[@]}"; do
    echo "  - $f"
  done
  exit 1
fi
