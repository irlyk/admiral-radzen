# Radzen.Admiral.Icons

Полный набор SVG-иконок Admiral Design System и адаптер для стандартных
компонентов Radzen Blazor. Пакет не встраивает все SVG в начальный bundle:
браузер загружает только реально использованные иконки.

Пакет предназначен для .NET 10. Порядок сборки локального NuGet-источника —
в [README репозитория](../README.md). Для стандартных `Radzen*` компонентов
сам `Radzen.Blazor` подключается приложением; Icons его не устанавливает.

## Подключение

Добавьте пакет:

```xml
<PackageReference Include="Radzen.Admiral.Icons" Version="1.0.3" />
```

В `<head>` host-страницы подключите стили и сразу после них адаптер:

```html
<link href="_content/Radzen.Admiral.Icons/admiral-radzen-icons.css"
      rel="stylesheet" />
<script src="_content/Radzen.Admiral.Icons/admiral-radzen-icons.js"></script>
```

Не добавляйте адаптеру `defer` или `async`: раннее блокирующее подключение
устанавливает наблюдатель до разбора server-prerendered разметки и не даёт
текстовому имени SVG попасть в первый кадр.

После этого иконки можно передавать в обычные параметры `Icon`:

```razor
<RadzenButton Icon="admiral:system/search-outline" Text="Найти" />
<RadzenIcon Icon="admiral:system/settings-outline" />
<RadzenIcon Icon="admiral:flags/russian-federation" />
```

Формат значения:

```text
admiral:<категория>/<имя-файла-без-svg>
```

Например, файл `system/search-outline.svg` используется как
`admiral:system/search-outline`.

## Предзагрузка критичных иконок

Адаптер отслеживает как новые элементы, так и отдельно добавленные Blazor
текстовые узлы. Поэтому при асинхронном появлении Accordion, Drawer или таблицы
строка `admiral:system/edit-outline` очищается до отрисовки, даже если Blazor
вставил элемент и его содержимое двумя разными DOM-операциями.

Предзагрузка не нужна для скрытия текстового имени. Она остаётся необязательной
оптимизацией, если сама SVG должна появиться без ожидания первого сетевого
запроса:

```html
<script>
  window.addEventListener("load", () => {
    window.AdmiralRadzenIcons.preload([
      "admiral:system/edit-outline",
      "admiral:system/delete-outline"
    ]).catch(error => console.warn(error));
  });
</script>
```

Официальный пакет Admiral также использует SVG-файлы и SVG React-компоненты,
а не иконочный WOFF-шрифт. Текущий адаптер сохраняет этот формат, в том числе
исходные цвета логотипов, флагов и платёжных систем.

## Как найти нужную иконку

Полный манифест доступен во время работы приложения:

```text
_content/Radzen.Admiral.Icons/admiral-icons/icons.txt
```

В consumer smoke также есть страница `/icons` с поиском, фильтром по категориям
и копированием готового значения `admiral:...`.

## Цветные и монохромные SVG

Монохромные иконки автоматически наследуют `currentColor`, поэтому реагируют на
цвет кнопки, темы и состояния. Многоцветные логотипы, платёжные системы и флаги
сохраняют исходные цвета и не превращаются в одноцветный прямоугольник.

## Собственное расположение assets

Если SVG публикуются по другому адресу, задайте `basePath` до подключения
адаптера:

```html
<script>
  window.AdmiralRadzenIcons = {
    basePath: "/assets/admiral-icons/"
  };
</script>
<script src="_content/Radzen.Admiral.Icons/admiral-radzen-icons.js"></script>
```

## Обновление исходного набора

Это операция разработчика пакета, а не шаг подключения приложения.
Нужны Git и Node.js. Команды выполняются из корня этого репозитория;
каталог для клонирования должен быть новым. Для воспроизводимого обновления
выберите нужный tag или commit в клонированном репозитории перед импортом.

```bash
git clone https://github.com/AdmiralDS/icons.git ../admiral-icons-source
node tools/admiral/import-admiral-icons.mjs ../admiral-icons-source
tools/admiral/build-admiral-packages.sh --configuration Release
```

На Windows весь процесс выглядит так:

```powershell
git clone https://github.com/AdmiralDS/icons.git ../admiral-icons-source
node tools/admiral/import-admiral-icons.mjs ../admiral-icons-source
powershell -ExecutionPolicy Bypass -File tools/admiral/build-admiral-packages.ps1 `
  -Configuration Release
```

Импортёр читает `public/icons`, сохраняет категории и приводит имена файлов к
строчным URL-safe идентификаторам.

Перед фиксацией проверьте diff набора и манифеста происхождения
`icons/admiral-icons-source.json`, затем страницу `/icons` в smoke.
