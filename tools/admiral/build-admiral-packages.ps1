#Requires -Version 5.1

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Output = "artifacts/packages/admiral"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory "../.."))
$outputDirectory = if ([System.IO.Path]::IsPathRooted($Output)) {
    [System.IO.Path]::GetFullPath($Output)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Output))
}

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$CommandArguments)

    & dotnet @CommandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($CommandArguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-LatestPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$PackageId
    )

    $packages = @(Get-ChildItem -LiteralPath $Directory -Filter "$PackageId.*.nupkg" -File |
        Sort-Object LastWriteTimeUtc -Descending)

    if ($packages.Count -eq 0) {
        throw "$PackageId package was not created."
    }

    return $packages[0]
}

function Get-PackageEntryCount {
    param(
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        return @($archive.Entries | Where-Object { $_.FullName -match $Pattern }).Count
    }
    finally {
        $archive.Dispose()
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK (dotnet) is required."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

Push-Location $repositoryRoot
try {
    Write-Host "Building Admiral packages..."
    Invoke-DotNet @("build", "Radzen.AdmiralIcons/Radzen.AdmiralIcons.csproj", "-f", "net10.0", "-c", $Configuration)
    Invoke-DotNet @("build", "Radzen.AdmiralThemes/Radzen.AdmiralThemes.csproj", "-f", "net10.0", "-c", $Configuration)
    Invoke-DotNet @("build", "Radzen.AdmiralComponents/Radzen.AdmiralComponents.csproj", "-f", "net10.0", "-c", $Configuration)

    Write-Host "Packing Admiral packages into $outputDirectory..."
    Invoke-DotNet @("pack", "Radzen.AdmiralIcons/Radzen.AdmiralIcons.csproj", "-c", $Configuration, "-p:TargetFramework=net10.0", "-o", $outputDirectory)
    Invoke-DotNet @("pack", "Radzen.AdmiralThemes/Radzen.AdmiralThemes.csproj", "-c", $Configuration, "-p:TargetFramework=net10.0", "-o", $outputDirectory)
    Invoke-DotNet @("pack", "Radzen.AdmiralComponents/Radzen.AdmiralComponents.csproj", "-c", $Configuration, "-p:TargetFramework=net10.0", "-o", $outputDirectory)

    $iconsPackage = Get-LatestPackage -Directory $outputDirectory -PackageId "Radzen.Admiral.Icons"
    $themesPackage = Get-LatestPackage -Directory $outputDirectory -PackageId "Radzen.Admiral.Themes"
    $componentsPackage = Get-LatestPackage -Directory $outputDirectory -PackageId "Radzen.Admiral.Components"

    $sourceIconCount = @(Get-ChildItem -LiteralPath "Radzen.AdmiralIcons/icons" -Filter "*.svg" -File -Recurse).Count
    $manifestIconCount = @(Get-Content -LiteralPath "Radzen.AdmiralIcons/wwwroot/admiral-icons/icons.txt").Count
    $packageIconCount = Get-PackageEntryCount $iconsPackage.FullName '^staticwebassets/admiral-icons/.+\.svg$'
    $packageThemeCssCount = Get-PackageEntryCount $themesPackage.FullName '^staticwebassets/css/admiral.*\.css$'
    $packageThemeScssCount = Get-PackageEntryCount $themesPackage.FullName '^themes/admiral.*\.scss$'
    $packageThemeFontCount = Get-PackageEntryCount $themesPackage.FullName '^staticwebassets/fonts/(MaterialSymbolsOutlined\.woff2|SourceSans3VF-(Italic|Upright)\.ttf\.woff2|cjk/NotoSans(SC|KR)-variable\.woff2)$'
    $packageComponentsDllCount = Get-PackageEntryCount $componentsPackage.FullName '^lib/net10\.0/Radzen\.Admiral\.Components\.dll$'

    if ($sourceIconCount -ne $manifestIconCount) {
        throw "Icon manifest count mismatch: source=$sourceIconCount manifest=$manifestIconCount"
    }
    if ($sourceIconCount -ne $packageIconCount) {
        throw "Icon package count mismatch: source=$sourceIconCount package=$packageIconCount"
    }
    if ($packageThemeCssCount -ne 6) {
        throw "Expected 6 Admiral CSS files in themes package, got $packageThemeCssCount"
    }
    if ($packageThemeScssCount -ne 6) {
        throw "Expected 6 Admiral SCSS files in themes package, got $packageThemeScssCount"
    }
    if ($packageThemeFontCount -ne 5) {
        throw "Expected 5 font files in themes package, got $packageThemeFontCount"
    }
    if ($packageComponentsDllCount -ne 1) {
        throw "Expected the Admiral components assembly in the components package, got $packageComponentsDllCount"
    }

    $invalidFontReferences = @(Get-ChildItem -LiteralPath "Radzen.AdmiralThemes/wwwroot/css" -Filter "*.css" -File |
        Select-String -Pattern '/_content/Radzen\.Blazor/fonts/')
    if ($invalidFontReferences.Count -gt 0) {
        throw "Admiral CSS must not reference fonts owned by Radzen.Blazor."
    }

    Write-Host "Admiral package pipeline completed."
    Write-Host "Icons package:  $($iconsPackage.FullName)"
    Write-Host "Themes package: $($themesPackage.FullName)"
    Write-Host "Components:     $($componentsPackage.FullName)"
    Write-Host "Icon count:     $sourceIconCount"
    Write-Host "Theme fonts:    $packageThemeFontCount"
}
finally {
    Pop-Location
}
