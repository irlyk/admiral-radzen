# AdmiralRadzen

[Русский](README.md)

AdmiralRadzen is an independent project. It is not an official product of Radzen or AdmiralDS, claims no affiliation with the owners of bank or payment marks, and grants no additional trademark rights.

Admiral Design System components, light and dark themes, and SVG icons for Radzen Blazor applications. The three packages are independently versioned and consumed through normal `PackageReference` and Blazor static web assets.

Explore the local [smoke gallery](samples/AdmiralConsumerSmoke/README.md) or the [WASM example app](samples/AdmiralDocs/README.md).

| Package | Version | Purpose |
| --- | --- | --- |
| [AdmiralRadzen.Components](https://www.nuget.org/packages/AdmiralRadzen.Components/1.2.2) | 1.2.2 | `Admiral*` components built on Radzen |
| [AdmiralRadzen.Themes](https://www.nuget.org/packages/AdmiralRadzen.Themes/1.1.2) | 1.1.2 | Packaged CSS, SCSS snapshots, fonts, and styling for direct Radzen controls |
| [AdmiralRadzen.Icons](https://www.nuget.org/packages/AdmiralRadzen.Icons/1.0.3) | 1.0.3 | Admiral SVG assets and icon adapter |

The projects target .NET 10 and Components currently depends on Radzen.Blazor 12.0.6. See the [component guide](docs/components.md) for examples and limitations.

## Build and run locally

From the repository root, with .NET SDK 10, Node.js 20 or newer, Bash, `rg`, and `unzip`:

```bash
tools/admiral/build-admiral-packages.sh --configuration Release
dotnet build Radzen.Admiral.slnx --configuration Release
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5072 \
  dotnet run --project samples/AdmiralConsumerSmoke/AdmiralConsumerSmoke.csproj \
  --no-build --no-launch-profile
```

The local packages are written to `artifacts/packages/admiral`, which is already configured in the repository's `NuGet.config`. The smoke app is a server application and the separate `AdmiralConsumerValidation` project checks consumption through NuGet packages. See the [smoke instructions](samples/AdmiralConsumerSmoke/README.md). Windows package-building instructions are in the [Russian README](README.md).

## Use in a Blazor application

Install the components package from NuGet.org. When working on this repository,
you can instead use `artifacts/packages/admiral` as a local NuGet source:

```xml
<PackageReference Include="AdmiralRadzen.Components" Version="1.2.2" />
```

The NuGet IDs are `AdmiralRadzen.*`; C# namespaces and static asset URLs
under `_content/Radzen.Admiral.*` remain unchanged for compatibility.

In `Program.cs`, register the services:

```csharp
using Radzen.Admiral.Components;
builder.Services.AddAdmiralComponents();
```

Add `@using Radzen.Admiral.Components` and `@using Radzen.Admiral.Components.Models` to `_Imports.razor`. Render `<AdmiralTheme Mode="AdmiralThemeMode.Light" />` near the router and `<AdmiralServices />` in the main layout. The host page needs `_content/Radzen.Admiral.Icons/admiral-radzen-icons.css`, `_content/Radzen.Admiral.Icons/admiral-radzen-icons.js`, and `_content/Radzen.Blazor/Radzen.Blazor.js`. For a fixed light theme, directly link `_content/Radzen.Admiral.Themes/css/admiral-base.css` instead of rendering `AdmiralTheme`. Do not load both methods together. The [integration guide](docs/radzen-integration.md) covers theme switching and direct Radzen usage.

```razor
<AdmiralButton Text="Continue"
               Appearance="AdmiralButtonAppearance.Primary" />
```

## Checks

```bash
tools/admiral/build-admiral-packages.sh --configuration Release
dotnet restore Radzen.Admiral.slnx
dotnet build Radzen.Admiral.slnx --configuration Release
npm ci
npx playwright install chromium
node tools/admiral/validate-consumer-integration.mjs
node tools/admiral/validate-copyable-razor.mjs
npm test
npm run test:browser
npm run test:wasm
```

Before validating copyable examples and running browser tests, start the smoke server at `http://localhost:5072` in a separate terminal as shown above.

## Typography, licensing, and documentation

The theme uses a system font for primary text and Noto Sans SC/KR for Chinese and Korean characters. You can add an application font through `@font-face` and `--admiral-font-family`; see the [font guide](docs/fonts.md).

The project's own code is covered by the root [MIT license](LICENSE). Bundled third-party materials and their notices are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Use of depicted trademarks is separate from the SVG license.

- [WASM example app](samples/AdmiralDocs/README.md)
- [Integration](docs/radzen-integration.md)
- [Components](docs/components.md)
- [Theme classes for direct Radzen](docs/theme-classes.md)
- [Contribution guide](CONTRIBUTING.md)
