#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
package_root="$(cd "$script_dir/../.." && pwd)"
radzen_root="${RADZEN_SOURCE_ROOT:-$package_root/../radzen/radzen-blazor}"
radzen_themes="$radzen_root/Radzen.Blazor/themes"
admiral_themes="$package_root/Radzen.AdmiralThemes/themes"
admiral_css="$package_root/Radzen.AdmiralThemes/wwwroot/css"

if [[ ! -f "$radzen_themes/_variables.scss" || ! -d "$radzen_themes/components" ]]; then
  echo "Radzen theme sources were not found at $radzen_themes" >&2
  exit 1
fi

stage_dir="$(mktemp -d /tmp/admiral-theme-stage.XXXXXX)"
cleanup() {
  if [[ "$stage_dir" == /tmp/admiral-theme-stage.* && -d "$stage_dir" ]]; then
    rm -r -- "$stage_dir"
  fi
}
trap cleanup EXIT

cp "$admiral_themes"/*.scss "$stage_dir/"
for partial in _variables.scss _mixins.scss _fonts.scss _components.scss _css-variables.scss; do
  ln -s "$radzen_themes/$partial" "$stage_dir/$partial"
done
ln -s "$radzen_themes/components" "$stage_dir/components"

dotnet build "$script_dir/AdmiralThemeCompile.csproj" \
  -p:ThemeStage="$stage_dir" --nologo

for variant in admiral-base admiral-dark-base admiral admiral-dark; do
  compiled_css="$stage_dir/$variant.css"
  if [[ ! -s "$compiled_css" ]]; then
    echo "Sass did not generate $compiled_css" >&2
    exit 1
  fi
  # Keep font URLs relative to the packaged stylesheet. This resolves within
  # _content/Radzen.Admiral.Themes/ at both the site root and a host base path.
done

if ! rg -q --fixed-strings '.rz-grid-table tbody tr.rz-data-row.rz-state-focused' "$stage_dir/admiral-base.css" ||
   ! rg -q --fixed-strings -- '--rz-series-color:' "$stage_dir/admiral-base.css" ||
   ! rg -q --fixed-strings -- '--admiral-font-family' "$stage_dir/admiral-base.css"; then
  echo "The generated theme is missing required Radzen or Admiral styles." >&2
  exit 1
fi

for variant in admiral-base admiral-dark-base admiral admiral-dark; do
  cp "$stage_dir/$variant.css" "$admiral_css/$variant.css"
done

echo "Rebuilt Admiral CSS from local Radzen SCSS without modifying $radzen_root"
