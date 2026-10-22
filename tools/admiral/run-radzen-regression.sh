#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
package_root="$(cd "$script_dir/../.." && pwd)"
radzen_root="${RADZEN_SOURCE_ROOT:-$package_root/../radzen/radzen-blazor}"
demo_host="$radzen_root/RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj"
regression_scheme="${ADMIRAL_REGRESSION_SCHEME:-https}"
if [[ "$regression_scheme" != https && "$regression_scheme" != http ]]; then
  echo "ADMIRAL_REGRESSION_SCHEME must be https or http" >&2
  exit 2
fi
if [[ "$regression_scheme" == https ]]; then
  if ! dotnet dev-certs https --check >/dev/null; then
    echo "An ASP.NET HTTPS development certificate is required. Configure it with dotnet dev-certs https, then retry." >&2
    exit 1
  fi
else
  echo "Warning: recent Radzen demos block HTTP SVG masks through CSP. Use HTTPS for visual verification." >&2
fi

if [[ ! -f "$demo_host" ]]; then
  echo "Radzen demos were not found at $radzen_root" >&2
  exit 1
fi

for variant in admiral-base admiral-dark-base admiral-wcag admiral-dark-wcag; do
  if [[ ! -f "$package_root/Radzen.AdmiralThemes/wwwroot/css/$variant.css" ]]; then
    echo "Theme CSS was not found: $variant.css" >&2
    exit 1
  fi
done

regression_stage="$(mktemp -d /tmp/admiral-regression.XXXXXX)"
regression_stage="$(cd "$regression_stage" && pwd -P)"
cleanup() {
  if [[ ( "$regression_stage" == /tmp/admiral-regression.* || "$regression_stage" == /private/tmp/admiral-regression.* ) && -d "$regression_stage" ]]; then
    rm -r -- "$regression_stage"
  fi
}
trap cleanup EXIT
# Build a clean committed snapshot: upstream MSBuild can regenerate tracked CSS
# and JavaScript. Even those generated writes must never touch the source checkout.
upstream_ref="${RADZEN_SOURCE_REF:-HEAD}"
upstream_commit="$(git -C "$radzen_root" rev-parse --verify "$upstream_ref^{commit}")"
mkdir "$regression_stage/source"
git -C "$radzen_root" archive "$upstream_commit" | tar -xf - -C "$regression_stage/source"
demo_host="$regression_stage/source/RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj"
regression_css_dir="$regression_stage/source/RadzenBlazorDemos/wwwroot/css"
echo "Isolated Radzen snapshot: $upstream_commit (local changes are not included)"
cp "$package_root/Radzen.AdmiralThemes/wwwroot/css/admiral-base.css" "$regression_css_dir/admiral-regression-base.css"
cp "$package_root/Radzen.AdmiralThemes/wwwroot/css/admiral-dark-base.css" "$regression_css_dir/admiral-regression-dark-base.css"
cp "$package_root/Radzen.AdmiralThemes/wwwroot/css/admiral-wcag.css" "$regression_css_dir/admiral-regression-wcag.css"
cp "$package_root/Radzen.AdmiralThemes/wwwroot/css/admiral-dark-wcag.css" "$regression_css_dir/admiral-regression-dark-wcag.css"
# These two CSS files are copied outside their package directory for the demo.
# Restore package asset URLs only in the temporary copies; the shipped CSS keeps
# relative URLs so applications hosted below a base path resolve them correctly.
for variant in admiral-regression-base admiral-regression-dark-base; do
  perl -pi -e 's@\.\./fonts/@/_content/Radzen.Admiral.Themes/fonts/@g; s@\.\./\.\./Radzen\.Admiral\.Icons/admiral-icons/@/_content/Radzen.Admiral.Icons/admiral-icons/@g' "$regression_css_dir/$variant.css"
done

dotnet build "$demo_host" \
  -p:DirectoryBuildTargetsPath="$script_dir/radzen-regression.targets" \
  -p:AdmiralUpstreamTargets="$regression_stage/source/Directory.Build.targets" \
  -p:AdmiralPackageRoot="$package_root"

regression_port="${ADMIRAL_REGRESSION_PORT:-}"
if [[ -n "$regression_port" ]]; then
  if [[ ! "$regression_port" =~ ^[0-9]+$ ]] || (( regression_port < 1 || regression_port > 65535 )); then
    echo "ADMIRAL_REGRESSION_PORT must be a TCP port from 1 to 65535" >&2
    exit 2
  fi
  if lsof -nP -iTCP:"$regression_port" -sTCP:LISTEN >/dev/null 2>&1; then
    echo "Port $regression_port is already in use; choose another ADMIRAL_REGRESSION_PORT" >&2
    exit 1
  fi
else
  for candidate_port in {5084..5099}; do
    if ! lsof -nP -iTCP:"$candidate_port" -sTCP:LISTEN >/dev/null 2>&1; then
      regression_port="$candidate_port"
      break
    fi
  done
  if [[ -z "$regression_port" ]]; then
    echo "No free port found in 5084-5099; set ADMIRAL_REGRESSION_PORT" >&2
    exit 1
  fi
fi

echo "Radzen demos: $regression_scheme://localhost:$regression_port/?theme=admiral-regression"
echo "Dark theme:  $regression_scheme://localhost:$regression_port/?theme=admiral-regression-dark"
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$regression_scheme://localhost:$regression_port" \
  dotnet run --project "$demo_host" --no-build --no-launch-profile
