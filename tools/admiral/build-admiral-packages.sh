#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: tools/admiral/build-admiral-packages.sh [--configuration Debug|Release] [--output <dir>]

Runs the Admiral package pipeline:
  1. Build and pack AdmiralRadzen.Icons, AdmiralRadzen.Themes and AdmiralRadzen.Components.
  2. Verify package contents.
USAGE
}

configuration="Debug"
package_output="artifacts/packages/admiral"

while (($#)); do
  case "$1" in
    --configuration)
      configuration="${2:-}"
      if [ -z "$configuration" ]; then
        echo "--configuration requires a value." >&2
        exit 2
      fi
      shift 2
      ;;
    --output)
      package_output="${2:-}"
      if [ -z "$package_output" ]; then
        echo "--output requires a value." >&2
        exit 2
      fi
      shift 2
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

case "$configuration" in
  Debug|Release)
    ;;
  *)
    echo "Unsupported configuration: $configuration" >&2
    exit 2
    ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
output_dir="$repo_root/$package_output"

cd "$repo_root"

if ! command -v rg >/dev/null 2>&1; then
  echo "ripgrep (rg) is required for package checks." >&2
  exit 1
fi

if ! command -v unzip >/dev/null 2>&1; then
  echo "unzip is required for package checks." >&2
  exit 1
fi

mkdir -p "$output_dir"

echo "Building Admiral packages..."
dotnet build Radzen.AdmiralIcons/Radzen.AdmiralIcons.csproj -f net10.0 -c "$configuration"
dotnet build Radzen.AdmiralThemes/Radzen.AdmiralThemes.csproj -f net10.0 -c "$configuration"
dotnet build Radzen.AdmiralComponents/Radzen.AdmiralComponents.csproj -f net10.0 -c "$configuration"

echo "Packing Admiral packages into $package_output..."
dotnet pack Radzen.AdmiralIcons/Radzen.AdmiralIcons.csproj -c "$configuration" -p:TargetFramework=net10.0 -o "$package_output"
dotnet pack Radzen.AdmiralThemes/Radzen.AdmiralThemes.csproj -c "$configuration" -p:TargetFramework=net10.0 -o "$package_output"
dotnet pack Radzen.AdmiralComponents/Radzen.AdmiralComponents.csproj -c "$configuration" -p:TargetFramework=net10.0 -o "$package_output"

icons_package="$(find "$output_dir" -maxdepth 1 -type f -name 'AdmiralRadzen.Icons.*.nupkg' | sort -V | tail -1)"
themes_package="$(find "$output_dir" -maxdepth 1 -type f -name 'AdmiralRadzen.Themes.*.nupkg' | sort -V | tail -1)"
components_package="$(find "$output_dir" -maxdepth 1 -type f -name 'AdmiralRadzen.Components.*.nupkg' | sort -V | tail -1)"

if [ -z "$icons_package" ]; then
  echo "AdmiralRadzen.Icons package was not created." >&2
  exit 1
fi

if [ -z "$themes_package" ]; then
  echo "AdmiralRadzen.Themes package was not created." >&2
  exit 1
fi

if [ -z "$components_package" ]; then
  echo "AdmiralRadzen.Components package was not created." >&2
  exit 1
fi

source_icon_count="$(find Radzen.AdmiralIcons/icons -type f -name '*.svg' | wc -l | tr -d ' ')"
manifest_icon_count="$(wc -l < Radzen.AdmiralIcons/wwwroot/admiral-icons/icons.txt | tr -d ' ')"
package_icon_count="$(unzip -l "$icons_package" | rg '\sstaticwebassets/admiral-icons/.+\.svg$' | wc -l | tr -d ' ')"
package_theme_css_count="$(unzip -l "$themes_package" | rg '\sstaticwebassets/css/admiral.*\.css$' | wc -l | tr -d ' ')"
package_theme_scss_count="$(unzip -l "$themes_package" | rg '\sthemes/admiral.*\.scss$' | wc -l | tr -d ' ')"
package_theme_font_count="$(unzip -l "$themes_package" | rg '\sstaticwebassets/fonts/(MaterialSymbolsOutlined\.woff2|SourceSans3VF-(Italic|Upright)\.ttf\.woff2|cjk/NotoSans(SC|KR)-variable\.woff2)$' | wc -l | tr -d ' ')"
package_components_dll_count="$(unzip -l "$components_package" | rg '\slib/net10\.0/Radzen\.Admiral\.Components\.dll$' | wc -l | tr -d ' ')"

if [ "$source_icon_count" != "$manifest_icon_count" ]; then
  echo "Icon manifest count mismatch: source=$source_icon_count manifest=$manifest_icon_count" >&2
  exit 1
fi

if [ "$source_icon_count" != "$package_icon_count" ]; then
  echo "Icon package count mismatch: source=$source_icon_count package=$package_icon_count" >&2
  exit 1
fi

if [ "$package_theme_css_count" != "6" ]; then
  echo "Expected 6 Admiral CSS files in themes package, got $package_theme_css_count" >&2
  exit 1
fi

if [ "$package_theme_scss_count" != "6" ]; then
  echo "Expected 6 Admiral SCSS files in themes package, got $package_theme_scss_count" >&2
  exit 1
fi

if [ "$package_theme_font_count" != "5" ]; then
  echo "Expected 5 font files in themes package, got $package_theme_font_count" >&2
  exit 1
fi
if [ "$package_components_dll_count" != "1" ]; then
  echo "Expected the Admiral components assembly in the components package, got $package_components_dll_count" >&2
  exit 1
fi

if rg -q '/_content/Radzen\.Blazor/fonts/' Radzen.AdmiralThemes/wwwroot/css/*.css; then
  echo "Admiral CSS must not reference fonts owned by Radzen.Blazor." >&2
  exit 1
fi

echo "Admiral package pipeline completed."
echo "Icons package:  $icons_package"
echo "Themes package: $themes_package"
echo "Components:     $components_package"
echo "Icon count:     $source_icon_count"
echo "Theme fonts:    $package_theme_font_count"
