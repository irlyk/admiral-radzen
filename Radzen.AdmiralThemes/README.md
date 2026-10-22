# Radzen.Admiral.Themes

Светлая и тёмная темы Admiral Design System для Radzen Blazor. Пакет содержит
готовые CSS-файлы, SCSS-снимки и шрифты для иконок и CJK-текста.

## Подключение

Добавьте пакет:

```xml
<PackageReference Include="Radzen.Admiral.Themes" Version="1.1.2" />
```

Рекомендуемый вариант для Radzen Blazor 10.3.1 и новее:

```razor
<RadzenTheme Theme="admiral"
             CssPath="_content/Radzen.Admiral.Themes/css"
             PreloadIconFont="false" />
```

`PreloadIconFont="false"` обязателен: тема использует шрифты из
`Radzen.Admiral.Themes`, а не из стандартного каталога `Radzen.Blazor`.

## Переключение светлой и тёмной темы

Стандартный переключатель Radzen работает с именами `admiral` и
`admiral-dark`:

```razor
<RadzenAppearanceToggle LightTheme="admiral"
                        DarkTheme="admiral-dark"
                        aria-label="Переключить цветовую тему" />
```

Для сохранения выбора зарегистрируйте cookie-сервис в `Program.cs`:

```csharp
using Radzen;

builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "ApplicationTheme";
    options.Duration = TimeSpan.FromDays(365);
});
```

## Подключение без переключателя

Если тема всегда одна, CSS можно подключить напрямую в `<head>`:

```html
<link href="_content/Radzen.Admiral.Themes/css/admiral-base.css"
      rel="stylesheet" />
```

Тёмная версия:

```html
<link href="_content/Radzen.Admiral.Themes/css/admiral-dark-base.css"
      rel="stylesheet" />
```

Одновременно использовать прямой `<link>` и `RadzenTheme` не следует.

## Состав пакета

Доступные CSS-файлы:

- `css/admiral-base.css`;
- `css/admiral.css`;
- `css/admiral-dark-base.css`;
- `css/admiral-dark.css`;
- `css/admiral-wcag.css`;
- `css/admiral-dark-wcag.css`.

Пакет включает шрифты иконок, Source Sans 3 и Admiral CJK через
`_content/Radzen.Admiral.Themes/fonts/`. Основной текст использует системный
шрифт; собственное семейство приложения подключается по
[инструкции](../docs/fonts.md).

Для китайских и корейских символов тема включает локальные Noto Sans SC/KR
по лицензии SIL Open Font License 1.1 (`fonts/cjk/OFL.txt`). Эти WOFF2-файлы
загружаются браузером только при наличии соответствующих символов на странице,
поэтому отображение CJK-текста не зависит от системных шрифтов машины.
Латиница и кириллица по умолчанию используют системный fallback.
Исходники шрифтов — [Noto Sans SC](https://github.com/google/fonts/tree/main/ofl/notosanssc)
и [Noto Sans KR](https://github.com/google/fonts/tree/main/ofl/notosanskr)
из Google Fonts; упакованные файлы перекодированы в WOFF2 без удаления глифов.

SCSS-снимки находятся в каталоге `themes/` внутри пакета и репозитория. Для
обычного использования они не нужны: приложение подключает готовый CSS.

## Оформление прямых компонентов Radzen

Кнопки, таблицы, вкладки, меню и поля можно использовать без Admiral-обёрток.
Размеры, поверхности и состояния описаны в
[каталоге CSS-классов](../docs/theme-classes.md).
Рабочие примеры есть во вкладке Radzen каждой страницы smoke-галереи.

Пакет не имеет зависимости от сборки `Radzen.Blazor`, но его CSS зависит от
разметки и токенов Radzen. Текущая база — 12.0.6; отсутствие NuGet-зависимости
не означает совместимость с любой версией.

## Разработка

Готовые CSS хранятся в `wwwroot/css/`, исходники Admiral — в `themes/`.
Обычная сборка пакета не компилирует SCSS. После изменения исходников используйте
[пересборку темы и регрессионные проверки](../docs/radzen-regression.md),
затем [сборку пакетов](../README.md#сборка-и-запуск).
Не исправляйте только сгенерированный CSS: изменение должно воспроизводиться
из SCSS. Репозиторий Radzen при этом не изменяется.
