# AdmiralRadzen

[English](README.en.md)

AdmiralRadzen — независимый проект. Он не является официальным продуктом
Radzen или AdmiralDS и не заявляет аффилированности с правообладателями
банковских и платёжных знаков. Проект не предоставляет дополнительных прав
на их товарные знаки.

Компоненты, темы и SVG-иконки дизайн-системы Admiral для приложений на
Radzen Blazor. Подключаются через NuGet и статические ресурсы Blazor.

Примеры доступны в локальной [smoke-галерее](samples/AdmiralConsumerSmoke/README.md)
и [WASM-приложении](samples/AdmiralDocs/README.md).

## Состав репозитория

| Каталог | Пакет | Назначение |
| --- | --- | --- |
| [Radzen.AdmiralComponents](Radzen.AdmiralComponents/README.md) | `AdmiralRadzen.Components` | Компоненты `Admiral*` с декларативным API поверх Radzen |
| [Radzen.AdmiralThemes](Radzen.AdmiralThemes/README.md) | `AdmiralRadzen.Themes` | Светлая и тёмная темы, шрифты, оформление прямых Radzen-компонентов |
| [Radzen.AdmiralIcons](Radzen.AdmiralIcons/README.md) | `AdmiralRadzen.Icons` | SVG-иконки и адаптер параметра `Icon` |
| [samples/AdmiralConsumerSmoke](samples/AdmiralConsumerSmoke/README.md) | — | Галерея и проверка потребления готовых пакетов |

## Совместимость

| Зависимость | Текущая версия |
| --- | --- |
| .NET SDK / целевая платформа | 10 / `net10.0` |
| Radzen.Blazor | 12.0.6 |
| AdmiralRadzen.Components | 1.2.2 |
| AdmiralRadzen.Themes | 1.1.2 |
| AdmiralRadzen.Icons | 1.0.3 |

Версии заданы в файлах `.csproj`. API, примеры и ограничения описаны в
[справочнике компонентов](docs/components.md).

## Сборка и запуск

Команды выполняются из корня репозитория. Для восстановления зависимостей
нужен доступ к NuGet.org либо настроенному зеркалу.
Источники задаются в `NuGet.config`; его `<clear />` отключает унаследованные
источники. Если среде нужны дополнительные источники, настройте их локально.

Smoke использует `PackageReference`, поэтому **сначала собираются локальные
пакеты**, затем решение. Источник `artifacts/packages/admiral` уже указан
в [NuGet.config](NuGet.config).

### macOS / Linux

Нужны .NET SDK 10, Bash, `ripgrep` (`rg`) и `unzip`.

```bash
tools/admiral/build-admiral-packages.sh --configuration Release
dotnet build Radzen.Admiral.slnx

ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5072 \
  dotnet run --project samples/AdmiralConsumerSmoke/AdmiralConsumerSmoke.csproj \
  --no-build --no-launch-profile
```

### Windows / PowerShell

Нужны .NET SDK 10 и PowerShell 5.1 или новее. Unix-утилиты не требуются.

```powershell
powershell -ExecutionPolicy Bypass -File tools/admiral/build-admiral-packages.ps1 -Configuration Release
dotnet build Radzen.Admiral.slnx

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5072"
dotnet run --project samples/AdmiralConsumerSmoke/AdmiralConsumerSmoke.csproj `
  --no-build --no-launch-profile
```

В PowerShell 7 первую команду можно заменить на
`pwsh -File tools/admiral/build-admiral-packages.ps1 -Configuration Release`.

Откройте [галерею](http://localhost:5072/). Остановка — Ctrl+C.
Готовые пакеты находятся в `artifacts/packages/admiral`.

## Подключение к существующему проекту

Установите пакет из NuGet.org. При разработке этого репозитория вместо
публичного источника можно использовать локальный каталог `artifacts/packages/admiral`:

```xml
<PackageReference Include="AdmiralRadzen.Components" Version="1.2.2" />
```

Он транзитивно подключает Radzen, тему и иконки. Имена NuGet-пакетов —
`AdmiralRadzen.*`; пространства имён C# и адреса статических ресурсов
`_content/Radzen.Admiral.*` сохраняются для совместимости. Дальнейшие шаги —
[регистрация сервисов, тема, скрипты и разметка](docs/radzen-integration.md).
Тему и иконки можно использовать отдельно, без Admiral-обёрток.

Минимальная регистрация в `Program.cs`:

```csharp
using Radzen.Admiral.Components;

builder.Services.AddAdmiralComponents();
```

Добавьте `@using Radzen.Admiral.Components` и
`@using Radzen.Admiral.Components.Models` в `_Imports.razor`.
Разместите `<AdmiralTheme Mode="AdmiralThemeMode.Light" />` рядом с
маршрутизатором, а `<AdmiralServices />` — в основном layout. Подключите
`_content/Radzen.Admiral.Icons/admiral-radzen-icons.css`,
`_content/Radzen.Admiral.Icons/admiral-radzen-icons.js` и
`_content/Radzen.Blazor/Radzen.Blazor.js` в host-странице. Для темы без
переключения используйте прямой CSS-файл
`_content/Radzen.Admiral.Themes/css/admiral-base.css` вместо `AdmiralTheme`.
Подробная инструкция и прямое использование Radzen — в
[руководстве по интеграции](docs/radzen-integration.md).

```razor
<AdmiralButton Text="Продолжить"
               Appearance="AdmiralButtonAppearance.Primary" />
```

## Проверки

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

Перед проверкой копируемых примеров и браузерными тестами запустите smoke
в отдельном терминале на `http://localhost:5072` по инструкции выше.
Для этих проверок нужен Chromium Playwright; подробности в
[инструкции smoke](samples/AdmiralConsumerSmoke/README.md).

## Типографика и лицензии

Тема использует системный шрифт для основного текста и Noto Sans SC/KR для
китайских и корейских символов. Собственный шрифт приложения подключается через
`@font-face` и `--admiral-font-family`; пример есть в
[руководстве по типографике](docs/fonts.md).

Собственный код проекта распространяется по [MIT](LICENSE). Лицензии и
уведомления для поставляемых сторонних ресурсов приведены в
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Использование изображённых
товарных знаков регулируется отдельно от лицензии на SVG.

## Взаимодействие с Radzen

- Приложения и smoke-галерея используют пакет `Radzen.Blazor` из NuGet.
- Admiral API и поведение реализуются в `Radzen.AdmiralComponents`;
  общие состояния и оформление Radzen — в SCSS пакета Themes.
- Готовый CSS хранится в репозитории. При изменении SCSS его нужно
  [пересобрать на исходниках Radzen](docs/radzen-regression.md).
- Для проверки темы на полных демо Radzen служит
  `tools/admiral/run-radzen-regression.sh`. Скрипт собирает временный снимок
  выбранной версии Radzen, сохраняя его исходный репозиторий без изменений.
- При переносе поведения ориентир — оригинальный React Admiral; при отсутствии
  аналога сохраняется поведение Radzen. Ограничения отмечаются в таблице покрытия.

## Документация

- [Подключение к Blazor / Radzen](docs/radzen-integration.md)
- [Справочник компонентов](docs/components.md)
- [CSS-классы для прямых Radzen-компонентов](docs/theme-classes.md)
- [Настройка шрифтов](docs/fonts.md)
- [WASM-приложение с примерами](samples/AdmiralDocs/README.md)
- [Галерея: запуск и проверки](samples/AdmiralConsumerSmoke/README.md)
- [Регрессия на Radzen и пересборка темы](docs/radzen-regression.md)
- [Стиль кода компонентов](docs/component-code-style.md)
- [Правила работы в репозитории](AGENTS.md)
