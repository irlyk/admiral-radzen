# Подключение Admiral к приложению Radzen

Инструкция для приложения на .NET 10 с интерактивным Blazor. Исходники Radzen
для подключения пакетов не нужны. Версии и сборка локального источника описаны
в [README](../README.md).

## Подключение к существующему проекту

Пакеты можно получать из NuGet-источника организации или собирать локально.
Ниже показано подключение локальной сборки.

### 1. Собрать пакеты

В корне `admiral-radzen` выполните на macOS или Linux:

```bash
tools/admiral/build-admiral-packages.sh --configuration Release
```

На Windows запустите PowerShell и выполните:

```powershell
powershell -ExecutionPolicy Bypass -File tools/admiral/build-admiral-packages.ps1 `
  -Configuration Release
```

Если используется PowerShell 7 (`pwsh`):

```powershell
pwsh -File tools/admiral/build-admiral-packages.ps1 -Configuration Release
```

PowerShell-скрипт не требует `ripgrep`, `unzip` или Unix-утилит. Он собирает
те же три пакета и проверяет их содержимое средствами .NET.

Готовые `.nupkg` появятся в `artifacts/packages/admiral`.

### 2. Подключить локальный NuGet-источник

Добавьте в `NuGet.config` существующего приложения путь к каталогу с пакетами.
Путь можно сделать абсолютным или относительным относительно `NuGet.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="admiral-radzen-local"
         value="../admiral-radzen/artifacts/packages/admiral" />
    <add key="nuget.org"
         value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Не добавляйте `<clear />`, если проект уже использует корпоративные или другие
частные источники NuGet.

### 3. Добавить PackageReference

Для всей дизайн-системы добавьте в `.csproj` приложения:

```xml
<ItemGroup>
  <PackageReference Include="Radzen.Admiral.Components" Version="1.2.2" />
</ItemGroup>
```

Пакет компонентов транзитивно подключает Radzen Blazor, тему и набор иконок.
Если нужны только тема или иконки, соответствующие пакеты можно подключить
отдельно.

Затем выполните `dotnet restore`.

### 4. Зарегистрировать Admiral

В `Program.cs`:

```csharp
using Radzen.Admiral.Components;

builder.Services.AddAdmiralComponents(options =>
{
    options.ThemeCookieName = "ApplicationTheme";
    options.ThemeCookieDuration = TimeSpan.FromDays(365);
});
```

### 5. Добавить пространства имён

В `_Imports.razor`:

```razor
@using Radzen.Admiral.Components
@using Radzen.Admiral.Components.Models
```

### 6. Подключить тему

Разместите `AdmiralTheme` рядом с маршрутизатором приложения:

```razor
<AdmiralTheme Mode="AdmiralThemeMode.Light" />
```

Для начального запуска в тёмной теме используйте
`Mode="AdmiralThemeMode.Dark"`. Для переключения во время работы:

```razor
<RadzenAppearanceToggle LightTheme="admiral"
                        DarkTheme="admiral-dark"
                        aria-label="Переключить цветовую тему" />
```

Если переключение во время работы не требуется, вместо `AdmiralTheme` можно
подключить один файл напрямую в `<head>`:

```html
<link href="_content/Radzen.Admiral.Themes/css/admiral-base.css"
      rel="stylesheet" />
```

Для тёмной темы используйте `admiral-dark-base.css`. Не подключайте одновременно
прямой CSS-файл и `AdmiralTheme`: две темы будут конкурировать в каскаде.

### 7. Подключить иконки и JavaScript Radzen

Добавьте в `<head>` host-страницы:

```html
<link href="_content/Radzen.Admiral.Icons/admiral-radzen-icons.css"
      rel="stylesheet" />
<script src="_content/Radzen.Admiral.Icons/admiral-radzen-icons.js"></script>
```

Перед закрывающим `</body>`:

```html
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

Адаптер иконок в `<head>` подключайте без `defer` и `async`. Тогда текстовое
имя Admiral SVG будет скрыто ещё при разборе server-prerendered страницы и не
успеет появиться в первом кадре.

Где находится host-разметка:

- Blazor Server: `Pages/_Host.cshtml`;
- Blazor WebAssembly: `wwwroot/index.html`;
- современный Blazor Web App: корневой `Components/App.razor`.

Если `Radzen.Blazor.js` уже подключён, второй раз добавлять его не нужно.

### 8. Добавить сервисный хост Admiral

В основной layout добавьте один экземпляр:

```razor
<AdmiralServices />
```

Он размещает внутренние хосты диалогов, уведомлений, Tooltip и других
overlay-компонентов.

### 9. Использовать Admiral

Компоненты Admiral используют префикс `Admiral`:

```razor
<AdmiralIcon Icon="admiral:service/plus-outline"
             Size="AdmiralIconSize.Large" />

<AdmiralButton Text="Создать платёж"
               Appearance="AdmiralButtonAppearance.Primary"
               Dimension="AdmiralButtonDimension.M"
               IconStart="admiral:service/plus-outline" />
```

Прямые `Radzen*` компоненты доступны для сценариев, которым нужен расширенный
Radzen API. Подключённая Admiral-тема оформляет их через общие SCSS-переменные
и селекторы.

Составные компоненты также подключаются с префиксом `Admiral`:

```razor
<AdmiralDateRangePicker @bind-Start="startDate"
                        @bind-End="endDate"
                        Placeholder="Выберите период" />

<AdmiralDrawer @bind-Open="drawerOpen" Title="Параметры платежа">
    Содержимое панели
</AdmiralDrawer>
```

Полный набор примеров находится в
[`samples/AdmiralConsumerSmoke/Pages`](../samples/AdmiralConsumerSmoke/Pages).

## Быстрая проверка подключения

После запуска приложения следующие адреса должны возвращать HTTP 200:

```text
/_content/Radzen.Admiral.Themes/css/admiral-base.css
/_content/Radzen.Admiral.Themes/fonts/SourceSans3VF-Upright.ttf.woff2
/_content/Radzen.Admiral.Icons/admiral-radzen-icons.css
/_content/Radzen.Admiral.Icons/admiral-icons/icons.txt
/_content/Radzen.Admiral.Icons/admiral-icons/system/search-outline.svg
```

Если тема не применяется, проверьте `CssPath` и убедитесь, что другая тема
Radzen не подключается позже в документе.

Если вместо иконки отображается её текстовое имя, проверьте наличие обоих файлов
`admiral-radzen-icons.css` и `admiral-radzen-icons.js`.
## Особенности Blazor

В Blazor Web App компоненты с событиями должны находиться в интерактивном
дереве (`InteractiveServer` или `InteractiveWebAssembly`). Сервисный хост
размещайте в том же интерактивном дереве, что и вызывающие его компоненты.
`AddAdmiralComponents` не включает интерактивность приложения самостоятельно.

Для динамической темы нужен штатный `HeadOutlet`. Подключите также
`YourApp.styles.css`, заменив `YourApp` именем сборки приложения: через него
Blazor подключает изолированные стили библиотек. Сохраните стандартный
скрипт запуска Blazor, соответствующий типу приложения.

Cookie-сервис сохраняет выбор темы, но начальное значение нужно прочитать на
стороне приложения. Пример для Blazor Server:
[App.razor](../samples/AdmiralConsumerSmoke/App.razor).
Не переносите чтение `HttpContext` напрямую в WebAssembly.

## Только Radzen с темой Admiral

Если обёртки не нужны, подключите `Radzen.Blazor` 12.0.6,
`Radzen.Admiral.Themes` 1.1.2 и, при необходимости,
`Radzen.Admiral.Icons` 1.0.3. Используйте стандартную регистрацию сервисов
Radzen и его сервисные хосты, а не `AddAdmiralComponents`/`AdmiralServices`.

Тема подключается одним статическим `<link>` из примера выше либо через
`RadzenTheme` с `CssPath="_content/Radzen.Admiral.Themes/css"` и
`PreloadIconFont="false"`. Уберите ранее подключённую тему Radzen.

[Каталог CSS-классов](theme-classes.md) описывает геометрию Admiral для
обычных `Radzen*` компонентов. CSS меняет оформление, но не добавляет
поведение обёрток: например, их API действий, локализацию или управление фокусом.

## Обновление и диагностика

- Версии зависимостей сверяйте с [таблицей в README](../README.md#совместимость).
  Более новый master Radzen не считается автоматически совместимым.
- Если новая сборка того же номера пакета не видна, причина может быть в
  глобальном NuGet-кэше. Предпочитайте новый номер версии и обновление
  `PackageReference`; `restore --force` сам по себе не заменяет содержимое
  уже распакованного пакета.
- После обновления пакетов пересоберите и перезапустите приложение.
  Проверяйте не только HTTP 200, но и содержимое загруженных CSS/JS в Network.
- При размещении приложения под префиксом URL проверьте `<base href>` и
  фактические запросы шрифтов: сгенерированная тема использует абсолютные
  пути `/_content/...`. Не считайте развёртывание в подкаталоге проверенным
  только по загрузке главной страницы.
