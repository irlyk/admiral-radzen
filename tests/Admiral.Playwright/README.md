# C# Playwright regressions

The tests use NUnit 4 and Microsoft.Playwright on .NET 10. Test orchestration,
assertions, process management and file/package checks run in C#. JavaScript
expressions passed to Playwright only inspect or exercise browser DOM APIs.
There is no JavaScript test runner, npm dependency, or global PowerShell requirement.

## Structure and conventions

| Folder | Responsibility |
| --- | --- |
| `Components/<component>/` | Named behavioral scenarios for one component |
| `Interactions/` | Keyboard and focus scenarios, grouped by component |
| `Visual/` | Geometry, colors, focus rings and rendered pixel checks |
| `Documentation/` | Documentation navigation, examples and API checks |
| `Documentation/Snippets/` | Separate scanner, compiler and reports for rendered Razor |
| Typography/, Audits/ | Font inspection and gallery state/layout inventories |
| `Integration/` | Isolated NuGet consumer and static WASM hosting |
| Packaging/, Upstream/ | Package/release checks and external Radzen demos |
| `Pages/` | Page objects: locators, navigation and reusable user actions |
| `Models/` | Immutable geometry, style and rendering snapshots |
| `Infrastructure/` | Browser sessions, diagnostics, hosts, processes and fixtures |

Each test method names a scenario; theme, language and viewport variations use
NUnit test cases. Add scenarios to the owning component folder. Keep assertions
in the test and use page objects for repeated interactions. Shared assertions
should describe a concrete rule, such as color contrast, rather than wrap NUnit.

Use native Playwright locator assertions for eventual UI state. DOM evaluation
belongs to measurements or browser APIs that locators cannot expose. Return a
primitive or a typed snapshot through `ReadSnapshotAsync<T>`; snapshot JSON names
are explicit and C# properties use PascalCase. Avoid dynamic, truthiness helpers,
scenario-number switches and shared mutable test state.

BrowserTest owns browser sessions and failure artifacts. Register temporary hosts
with `Own(...)` so contexts close before their server stops. `GalleryTest` adds the
gallery page/context; `GalleryPage` waits for Blazor interactivity during navigation.
Keep host publishing and package restoration in infrastructure, outside component tests.

The test projects share `tests/.editorconfig`; the runner has a matching file: four spaces,
file-scoped namespaces, braces, PascalCase members, `_camelCase` private fields
and `Async` suffixes for asynchronous methods. Check formatting with:

~~~bash
dotnet format tests/Admiral.Playwright --verify-no-changes --no-restore
dotnet format tests/Admiral.TestRunner.Tests --verify-no-changes --no-restore
dotnet format tools/Admiral.TestRunner --verify-no-changes --no-restore
~~~

The runner is organized separately into `Commands/`, `Selection/` and `Execution/`.
Affected selection recursively scans test folders and follows page-object/helper
dependencies before mapping gallery routes through Razor component composition.
Its selection and command parsing have a separate fast unit suite that does not
start browsers or build packages:

```bash
dotnet test tests/Admiral.TestRunner.Tests
```

From the repository root:

```bash
dotnet run --project tests/Admiral.Playwright -- install chromium
dotnet test tests/Admiral.Playwright --logger trx --results-directory artifacts/test-results
```

On Linux use `install --with-deps chromium` to install browser system dependencies.
The browser version is pinned by the Playwright PackageReference. Browser installation
is required once after changing that version.

Package preparation uses the repository build scripts (Bash on Unix, Windows
PowerShell on Windows, and the tools required by those scripts).

The default run builds local packages, publishes the smoke app using an isolated
NuGet cache, starts it on a free port and stops it afterwards. Set `SMOKE_URL`
to test an existing smoke server. Consumer validation publishes its own copied
PackageReference project, verifies restored package hashes, and starts a separate
server. Snippet compilation also uses a copied project. Source samples are never
modified. WASM tests publish and serve static files under `/admiral-radzen/`,
including genuine HTTP 404 responses for deep links.

## Selecting tests

```bash
# Select by NUnit category using the standard .NET test runner.
SMOKE_URL=http://localhost:5072 dotnet test tests/Admiral.Playwright --filter TestCategory=Slider

# Choose suites affected by changed files (Razor composition, SCSS selectors, routes).
dotnet run --project tools/Admiral.TestRunner -- affected

dotnet run --project tools/Admiral.TestRunner -- affected --list Radzen.AdmiralComponents/AdmiralTree.razor

dotnet run --project tools/Admiral.TestRunner -- browser
dotnet run --project tools/Admiral.TestRunner -- consumer
dotnet run --project tools/Admiral.TestRunner -- snippets
dotnet run --project tools/Admiral.TestRunner -- wasm
```

Categories: `Audit`, `Layout`, `Button`, `Checkbox`, `Interactions`, `Combo`, `TreeSelect`, `GlobalSearch`, `Drawer`,
`Hint`, `Slider`, `PhoneNumber`, `Select`, `Visual`, `FileInput`, `ImageViewer`,
`ActionBar`, `Tree`, `NativeDateRange`, `Fonts`, `DocsUx`, `DocsIteration`,
`Documentation`, `Consumer`, `Snippets`, `Wasm`, `Release`, `CleanRelease`, `Radzen`.
`browser` includes all gallery categories; `all` also includes infrastructure.
Unknown affected dependencies fall back to the complete suite. `--list` does not
build packages or start browsers. Existing npm test scripts are thin aliases.

Optional checks:

- `RADZEN_URL=https://localhost:5084` enables the external Radzen demo tests.
  Start the isolated upstream host with `tools/admiral/run-radzen-regression.sh`.
  These tests are skipped without the URL.
- `REQUIRE_CLEAN_RELEASE=1` enables the clean-checkout release gate. Package
  contents and licenses are checked even in a working checkout.
- `ADMIRAL_FONT_FIXTURE_DIR` and `ADMIRAL_FONT_FIXTURE_EXPECTED_FAMILY` enable
  consumer font checks using external Regular, Medium and SemiBold files.
  The default run checks system fallback and packaged Radzen icon fonts.
- `SNIPPET_LIMIT` restricts compilation for a quick local diagnosis; leave it unset
  for a complete gate. Host/setup fragments and snippets needing component context
  are reported separately, matching the previous validation rules.

## Diagnostics and coverage

`AUDIT_OUTPUT` defaults to `artifacts/playwright`; each fixture has its own subdirectory
to prevent screenshot and report collisions. Failing browser tests attach
screenshots and traces to NUnit/TRX. State and layout inventories write JSON
there; representative layout and component screenshots are also retained.
`HEADED=1` opens a visible browser; `CHROME_PATH` selects a custom executable.
`AUDIT_WIDTHS` and `AUDIT_ROUTES` restrict the layout inventory.

The migration preserves keyboard/focus, callbacks, disabled/read-only/loading,
geometry and color, light/dark, RU/EN and narrow viewport scenarios from the old
scripts. State inventories do not establish full visual parity. OS drag/drop and
physical-device touch remain manual checks; synthetic events and CDP touch
emulation only establish browser behavior.
