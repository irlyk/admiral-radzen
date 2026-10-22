#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: tools/admiral/verify-admiral-assets.sh [--sync] [--sync-themes]

Builds Radzen.Blazor theme CSS, verifies key Admiral generated CSS tokens/selectors,
and checks that demo CSS is synchronized. Pass --sync to copy generated Admiral CSS
into RadzenBlazorDemos/wwwroot/css before comparing hashes. Pass --sync-themes to
copy generated Admiral CSS and Admiral SCSS source into Radzen.AdmiralThemes.
USAGE
}

sync_demo=false
sync_themes=false

while (($#)); do
  case "$1" in
    --sync)
      sync_demo=true
      shift
      ;;
    --sync-themes)
      sync_themes=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"

cd "$repo_root"

if ! command -v rg >/dev/null 2>&1; then
  echo "ripgrep (rg) is required for CSS checks." >&2
  exit 1
fi

css_files=(
  admiral-base.css
  admiral.css
  admiral-dark-base.css
  admiral-dark.css
  admiral-wcag.css
  admiral-dark-wcag.css
)

echo "Building Radzen.Blazor theme CSS..."
dotnet build Radzen.Blazor/Radzen.Blazor.csproj -f net10.0

if [ "$sync_demo" = true ]; then
  echo "Syncing generated Admiral CSS into demo..."
  for css in "${css_files[@]}"; do
    cp "Radzen.Blazor/wwwroot/css/$css" "RadzenBlazorDemos/wwwroot/css/$css"
  done
fi

if [ "$sync_themes" = true ]; then
  echo "Syncing generated Admiral CSS and SCSS source into Radzen.AdmiralThemes..."
  for css in "${css_files[@]}"; do
    cp "Radzen.Blazor/wwwroot/css/$css" "Radzen.AdmiralThemes/wwwroot/css/$css"
  done

  for scss in Radzen.Blazor/themes/admiral*.scss; do
    cp "$scss" "Radzen.AdmiralThemes/themes/$(basename "$scss")"
  done
fi

echo "Checking generated Admiral CSS tokens/selectors..."

require_fixed() {
  local file="$1"
  local text="$2"

  if ! rg --fixed-strings --quiet -- "$text" "$file"; then
    echo "Missing expected CSS in $file:" >&2
    echo "  $text" >&2
    exit 1
  fi
}

light_css="Radzen.Blazor/wwwroot/css/admiral-base.css"
dark_css="Radzen.Blazor/wwwroot/css/admiral-dark-base.css"

require_fixed "$light_css" "--rz-input-border: var(--rz-border-width) solid var(--rz-base-500);"
require_fixed "$light_css" "--rz-input-hover-border: var(--rz-border-width) solid var(--rz-primary);"
require_fixed "$light_css" "--rz-input-focus-shadow: inset 0 0 0 1px var(--rz-primary);"
require_fixed "$light_css" ".rz-datepicker:not(.rz-state-disabled):hover>.rz-inputtext:not(:disabled):not(.rz-state-disabled){box-shadow:var(--rz-input-focus-shadow);border:var(--rz-input-focus-border);border-block-end:var(--rz-input-focus-border-block-end)}"

require_fixed "$dark_css" "--rz-input-border: var(--rz-border-width) solid var(--rz-base-400);"
require_fixed "$dark_css" "--rz-input-hover-border: var(--rz-border-width) solid var(--rz-primary);"
require_fixed "$dark_css" "--rz-input-focus-shadow: inset 0 0 0 1px var(--rz-primary);"
require_fixed "$dark_css" "--rz-skeleton-background-color: var(--rz-base-700);"
require_fixed "$dark_css" "--rz-skeleton-animation-color: var(--rz-base-600);"
require_fixed "$dark_css" ".rz-datepicker:not(.rz-state-disabled):hover>.rz-inputtext:not(:disabled):not(.rz-state-disabled){box-shadow:var(--rz-input-focus-shadow);border:var(--rz-input-focus-border);border-block-end:var(--rz-input-focus-border-block-end)}"
require_fixed "$dark_css" ".rz-button.rz-variant-outlined.rz-dark.rz-shade-default{box-shadow:inset 0 0 0 var(--rz-border-width) var(--rz-text-title-color);color:var(--rz-text-title-color)}"
require_fixed "$dark_css" ".rz-button.rz-variant-text.rz-dark{color:var(--rz-text-title-color)}"
require_fixed "$dark_css" ".rz-alert.rz-variant-outlined.rz-dark{box-shadow:inset 0 0 0 var(--rz-border-width) var(--rz-text-title-color)}"

echo "Comparing generated CSS with demo CSS..."

for css in "${css_files[@]}"; do
  src="Radzen.Blazor/wwwroot/css/$css"
  demo="RadzenBlazorDemos/wwwroot/css/$css"

  if [ ! -f "$src" ]; then
    echo "Missing generated CSS: $src" >&2
    exit 1
  fi

  if [ ! -f "$demo" ]; then
    echo "Missing demo CSS: $demo" >&2
    exit 1
  fi

  src_hash="$(shasum "$src" | awk '{print $1}')"
  demo_hash="$(shasum "$demo" | awk '{print $1}')"

  if [ "$src_hash" != "$demo_hash" ]; then
    echo "Demo CSS is out of sync: $css" >&2
    echo "  generated: $src_hash" >&2
    echo "  demo:      $demo_hash" >&2
    echo "Run with --sync to update demo CSS." >&2
    exit 1
  fi
done

if [ -d "Radzen.AdmiralThemes/wwwroot/css" ]; then
  echo "Comparing generated CSS with Radzen.AdmiralThemes CSS..."

  for css in "${css_files[@]}"; do
    src="Radzen.Blazor/wwwroot/css/$css"
    theme="Radzen.AdmiralThemes/wwwroot/css/$css"

    if [ ! -f "$theme" ]; then
      echo "Missing themes package CSS: $theme" >&2
      exit 1
    fi

    src_hash="$(shasum "$src" | awk '{print $1}')"
    theme_hash="$(shasum "$theme" | awk '{print $1}')"

    if [ "$src_hash" != "$theme_hash" ]; then
      echo "Themes package CSS is out of sync: $css" >&2
      echo "  generated: $src_hash" >&2
      echo "  package:   $theme_hash" >&2
      echo "Run with --sync-themes to update Radzen.AdmiralThemes." >&2
      exit 1
    fi
  done
fi

echo "Admiral assets look synchronized."
