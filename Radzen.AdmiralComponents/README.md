# AdmiralRadzen.Components

Компоненты Admiral Design System для Blazor на .NET 10. Обёртки используют
Radzen.Blazor 12.0.6, тему Admiral 1.1.2 и иконки 1.0.3.
Изменений исходников Radzen не требуется.

## Подключение

```xml
<PackageReference Include="AdmiralRadzen.Components" Version="1.2.2" />
```

Зависимости подключаются транзитивно. Для разработки пакеты также можно собрать
в локальный NuGet-каталог; порядок сборки указан в [README](../README.md).

В `Program.cs`:

```csharp
using Radzen.Admiral.Components;

builder.Services.AddAdmiralComponents();
```

В `_Imports.razor`:

```razor
@using Radzen.Admiral.Components
@using Radzen.Admiral.Components.Models
```

Подключите тему через `AdmiralTheme` либо статический CSS, адаптер иконок,
`Radzen.Blazor.js` и `YourApp.styles.css`. В интерактивном layout разместите
один `AdmiralServices`. Полная последовательность и особенности разных
типов Blazor: [руководство подключения](../docs/radzen-integration.md).

## Пример

```razor
<AdmiralButton Text="Продолжить"
               Appearance="AdmiralButtonAppearance.Primary"
               Dimension="AdmiralButtonDimension.M" />
```

## API и проверка

[Справочник](../docs/components.md) содержит список компонентов, примеры
таблиц, пагинации, календарей, меню и асинхронных списков.
Интерактивные примеры, параметры и ограничения находятся также в
[smoke-галерее](../samples/AdmiralConsumerSmoke/README.md).

Предпочтительная разметка использует `Admiral*`. Прямые `Radzen*` компоненты
остаются поддерживаемым вариантом. Некоторые старые контракты, в том числе
части DataGrid API, содержат типы Radzen. Подробности — в справочнике
и документации соответствующего компонента.

Компоненты 1.2.x используют тему 1.1.x и Radzen.Blazor 12.0.6. Поддерживаемые
сценарии и ограничения описаны в справочнике и на страницах примеров.

При разработке следуйте [стилю кода](../docs/component-code-style.md).
