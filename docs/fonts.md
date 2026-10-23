# Шрифты в AdmiralRadzen

Темы `admiral` и `admiral-dark` используют системный шрифт для основного текста
и поставляемые Noto Sans SC/KR для китайских и корейских символов. Переменная
`--admiral-font-family` задаёт семейство для компонентов `Admiral*` и прямых
компонентов `Radzen*` с темой Admiral, включая всплывающие списки.

## Подключение шрифта приложения

Если приложение использует собственный шрифт, разместите его файлы, например,
в `wwwroot/css/fonts/` и создайте `wwwroot/css/admiral-fonts.css`:

```css
@font-face {
  font-family: 'MyApp Font';
  src: url('./fonts/MyAppFont-Regular.woff2') format('woff2');
  font-style: normal;
  font-weight: 400;
  font-display: swap;
}

@font-face {
  font-family: 'MyApp Font';
  src: url('./fonts/MyAppFont-Medium.woff2') format('woff2');
  font-style: normal;
  font-weight: 500;
  font-display: swap;
}

:root {
  --admiral-font-family: 'MyApp Font', 'Admiral CJK', system-ui, sans-serif;
}
```

Имена и файлы в примере замените на лицензированный шрифт вашего приложения.
Объявляйте только те начертания и веса, для которых у вас есть файлы.

Подключите CSS после темы в `App.razor`, host page или `wwwroot/index.html`:

```html
<link href="_content/Radzen.Admiral.Themes/css/admiral-base.css" rel="stylesheet" />
<link href="css/admiral-fonts.css" rel="stylesheet" />
```

При переключении темы через `RadzenTheme` подключайте только
`css/admiral-fonts.css` в host page и оставляйте его подключённым для обеих тем.
Относительные URL в этом файле разрешаются относительно самого CSS-файла.
Если приложение размещено в подпути, используйте относительный `href`, как в
примере, вместе с корректным `<base href>`.

Для локальной проверки в smoke можно разместить собственные файлы и `preview.css`
в `samples/AdmiralConsumerSmoke/wwwroot/local-typography/` и запустить приложение
с `ADMIRAL_LOCAL_TYPOGRAPHY=1`. Папка игнорируется Git; без этого флага smoke
проверяет публичную конфигурацию без пользовательского основного шрифта.

## Настройка fallback

Если собственный шрифт не нужен, менять переменную не требуется. Тема задаёт
`--rz-text-font-family` через `--admiral-font-family`; `Admiral CJK` применяется
только к соответствующим символам благодаря CSS `unicode-range`. Остальной
текст использует системный шрифт. Если собственный шрифт не содержит китайских
или корейских глифов, они также переходят к `Admiral CJK`.

Не переопределяйте `--rz-text-font-family` отдельно после темы: иначе прямые
компоненты Radzen могут получить другую типографику. Правило действует и для
всплывающих элементов, которые Radzen размещает у `body`.
