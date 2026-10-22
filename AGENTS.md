# Repository instructions

AdmiralRadzen supplies three standalone NuGet packages for using Admiral Design
System assets with Radzen Blazor. Keep package code independent of a Radzen
source checkout; consumers use normal `PackageReference` and packaged static
assets.

## Packages

- `Radzen.AdmiralIcons/` — SVG assets and the lazy Radzen icon adapter.
- `Radzen.AdmiralThemes/` — packaged CSS, SCSS sources and fonts. It does not
  depend on Radzen.Blazor.
- `Radzen.AdmiralComponents/` — Admiral-prefixed Razor components over Radzen.
- `samples/AdmiralConsumerSmoke/` — server-side package consumer and examples.
- `samples/AdmiralDocs/` — small WebAssembly documentation site.

Do not change package IDs, versions, public API or dependencies as a side effect
of documentation work. Do not publish packages or push Git changes without an
explicit request.

## Theme and components

Link the packaged CSS from `_content/Radzen.Admiral.Themes/css/`. For runtime
switching use `RadzenTheme` with names `admiral` and `admiral-dark`. The theme
uses a system text font by default and packaged Noto Sans SC/KR for CJK glyphs.
Consumers can set `--admiral-font-family` to use their own font; see
[`docs/fonts.md`](docs/fonts.md). Keep this variable integrated with
`--rz-text-font-family` so Admiral wrappers and direct Radzen controls share the
same typography.

Preferred consumer markup uses `Admiral*` components. Direct Radzen controls
remain supported by the theme. New public Admiral parameters and callbacks
should not expose Radzen-specific types. Keep SCSS and generated CSS in sync.

Documentation pages should show working Admiral examples and, where useful,
independent direct Radzen examples, copyable Razor, states and API details.
Keep Russian and English text and light/dark themes usable.

## Verification

Choose checks according to the changed files:

- Documentation only: check links and content, then `git diff --check`.
- Package metadata or notices: inspect the changed metadata and license text;
  pack only an affected project if its `.csproj` changed.
- CSS or fonts: inspect the relevant generated CSS and check one typography
  page, fallback and light/dark when applicable.
- Components: run the affected component's regression tests and build its
  package or consumer as needed.
- Shared infrastructure: expand checks according to the actual dependencies.

The complete browser suite is available through `npm run test:browser`; the
normal affected runner is `npm test`. Build all packages with
`tools/admiral/build-admiral-packages.sh --configuration Release` when a full
package check is warranted. `tools/admiral/verify-public-release.mjs` checks a
clean release checkout and assembled package contents.

A successful build or HTTP 200 is not proof of visual behavior. Use a rendered
browser for visual claims and state any scenarios that were not checked.
