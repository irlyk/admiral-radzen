# CSS-классы Admiral для компонентов Radzen

Требуется подключённая [тема Admiral](../Radzen.AdmiralThemes/README.md).
Это оформление обычных Radzen-компонентов, не замена поведения Admiral-обёрток.
Примеры с иконками требуют также пакета Icons.

## Кнопки Admiral

Все варианты строятся на обычном `RadzenButton`. Класс выбирает внешний вид,
а стандартный параметр `Size` — размер Admiral:

| Admiral | Radzen |
| --- | --- |
| XL, 56 px | `ButtonSize.Large` |
| L, 48 px | `ButtonSize.Medium` |
| M, 40 px | `ButtonSize.Small` |
| S, 32 px | `ButtonSize.ExtraSmall` |

```razor
<RadzenButton class="admiral-button admiral-button--primary"
              Size="ButtonSize.Small" Text="Продолжить" />
<RadzenButton class="admiral-button admiral-button--secondary"
              Size="ButtonSize.Small" Variant="Variant.Outlined" Text="Назад" />
<RadzenButton class="admiral-icon-button admiral-icon-button--primary"
              Size="ButtonSize.Small" Variant="Variant.Text"
              Icon="admiral:service/plus-outline" aria-label="Добавить" />
<RadzenButton class="admiral-text-button"
              Size="ButtonSize.Small" Variant="Variant.Text" Text="Подробнее" />
```

Для обычной кнопки доступны модификаторы `--primary`, `--secondary`,
`--tertiary`, `--ghost`, `--danger`, `--success` и `--white`. У IconButton
вариант `admiral-icon-button--primary` переключает нейтральную иконку на
основной цвет. Для TextButton класс `admiral-text-button--secondary` включает
нейтральное начальное состояние. Hover, press, focus, disabled и busy получают
официальные токены Admiral обеих цветовых тем.

Группы создаются обычным контейнером с классом `admiral-button-group` и
модификатором `--primary`, `--secondary` или `--tertiary`. Для группы кнопок
только с иконками используйте `admiral-icon-button-group`. Вложенные
`RadzenButton` сохраняют свои стандартные параметры `Size`, `Disabled` и
`IsBusy`, а тема объединяет границы и скругления соседних кнопок.

```razor
<div class="admiral-button-group admiral-button-group--primary" role="group">
    <RadzenButton class="admiral-button admiral-button--primary"
                  Size="ButtonSize.Small" Text="Создать" />
    <RadzenButton class="admiral-button admiral-button--primary"
                  Size="ButtonSize.Small" Text="Копировать" />
</div>
```

## Альтернативные поверхности и скругления Admiral

Варианты можно применять к отдельному компоненту или к целой ветке интерфейса.
Класс `admiral-surface` добавляет фон и цвет текста, а модификатор выбирает
официальную поверхность Admiral:

- `admiral-surface--base` — основной фон темы;
- `admiral-surface--alternate` — контрастный нейтральный фон;
- `admiral-surface--elevated` — фон поднятого блока с тенью;
- `admiral-surface--dark-static` — неизменяемая тёмная поверхность в любой теме.

Для варианта `borderRadiusKind` доступны классы
`admiral-shape-radius-0`, `admiral-shape-radius-2`,
`admiral-shape-radius-4`, `admiral-shape-radius-6`,
`admiral-shape-radius-8` и `admiral-shape-radius-10`. Они меняют официальные
группы скруглений small, medium и large только внутри выбранного контейнера:

```html
<section class="admiral-surface admiral-surface--alternate admiral-shape-radius-8">
  ...компоненты Radzen...
</section>
```

## DataGrid в геометрии Admiral

Размер строки и заголовка выбирается классом
`admiral-table--s`, `admiral-table--m`, `admiral-table--l` или
`admiral-table--xl`. Для серого заголовка добавьте
`admiral-table--grey-header`. Классы `admiral-table--wrap-header` и
`admiral-table--wrap-cells` разрешают перенос длинного текста соответственно в
заголовках и ячейках. Без `admiral-table--grey-header` фон заголовка совпадает
с фоном строк, включая отсортированный столбец. Класс
`admiral-table--no-border` убирает внешнюю рамку; внутренние линии по-прежнему
задаются штатным `GridLines` Radzen:

```razor
<RadzenDataGrid Data="@items"
                TItem="Item"
                GridLines="DataGridGridLines.None"
                class="admiral-table--m admiral-table--no-border admiral-table--wrap-header admiral-table--wrap-cells">
    ...
</RadzenDataGrid>
```

Для `RowRender` пакет предоставляет классы
`admiral-table-row--success`, `admiral-table-row--warning` и
`admiral-table-row--danger`. Сортировка, фильтры, группировка, изменение ширины
и порядка колонок, frozen-колонки, виртуализация и раскрываемые строки остаются
стандартными возможностями `RadzenDataGrid` и получают оформление темы.

Встроенный pager `RadzenDataGrid` соответствует Admiral `PaginationTwo`.
По умолчанию используется размер M с кнопками 40 px, класс
`admiral-pagination-two--s` включает размер S с кнопками 32 px. Составные
`PaginationOne` и `PaginationSimple` предоставлены отдельными компонентами в
пакете `Radzen.Admiral.Components`.

## RadioButton, SegmentedControl и ContentSwitcher

`RadzenRadioButtonList` получает геометрию Admiral классом
`admiral-radio-list`. Для размера S добавьте `admiral-radio-list--s`, а класс
`admiral-radio--error` можно установить на отдельный
`RadzenRadioButtonListItem`.

`RadzenSelectBar` используется для двух разных паттернов:

```razor
<RadzenSelectBar class="admiral-segmented-control admiral-segmented-control--outlined admiral-segmented-control--m">
    ...
</RadzenSelectBar>

<RadzenSelectBar class="admiral-content-switcher admiral-content-switcher--primary admiral-content-switcher--fill">
    ...
</RadzenSelectBar>
```

Для SegmentedControl доступны appearance `--outlined` и `--filled`, а также
размеры `--xl`, `--l`, `--m`, `--s` (`xl` используется по умолчанию). Для
ContentSwitcher доступны обычный и `--primary` appearance, размеры `--l`,
`--m`, `--s` (`m` используется по умолчанию) и адаптивная ширина `--fill`.
Параметр `Multiple="true"` у `RadzenSelectBar` даёт checkbox-вариант
SegmentedControl.

## HorizontalTabs

Для актуального горизонтального TabMenu Admiral добавьте класс `admiral-tabs`
к `RadzenTabs`. По умолчанию используется размер L (48 px). Размер M включается
классом `admiral-tabs--m`, нижняя нейтральная линия — `admiral-tabs--underline`,
растягивание вкладок по ширине — `admiral-tabs--fill`, а нейтральный активный
индикатор — `admiral-tabs--secondary`.

## Link и Anchor

Обычный `RadzenLink` получает типографику, цвета и состояния Admiral через
класс `admiral-link`. Вторичный нейтральный вариант включается классом
`admiral-link--secondary`, размер S — `admiral-link--s`. Параметр `Icon`
размещает иконку слева; для иконки справа передайте текст и `RadzenIcon` через
`ChildContent`.

```razor
<RadzenLink class="admiral-link"
            Path="/payments"
            Icon="admiral:category/bank-outline"
            Text="Платежи" />

<RadzenLink class="admiral-link admiral-link--s admiral-link--secondary"
            Path="/details" Text="Подробнее" />
```

Для вертикальной якорной навигации используйте штатные `RadzenToc` и
`RadzenTocItem`. Класс `admiral-anchor` оформляет размеры M и уровни
вложенности; `admiral-anchor--s` включает размер S, а
`admiral-anchor--multiline` разрешает перенос длинных названий. Слежение за
прокруткой и выбор активного пункта остаются возможностями Radzen.

```razor
<RadzenToc class="admiral-anchor" Selector=".rz-body">
    <RadzenTocItem Text="Обзор" Selector="#overview" />
    <RadzenTocItem Text="Платежи" Selector="#payments">
        <RadzenTocItem Text="История" Selector="#history" />
    </RadzenTocItem>
</RadzenToc>
```

## Badge, BadgeDot, Chips и Tag

- Для `RadzenBadge` используйте `admiral-badge`; размер S включается классом
  `admiral-badge--s`. Семантические цвета берутся из `BadgeStyle`.
- Точечный индикатор создаётся элементом с классом `admiral-badge-dot` и
  модификаторами размера `--l`, `--s`, `--xs` и статуса `--info`, `--success`,
  `--warning`, `--error`, `--attention`.
- Для `RadzenChip` используйте `admiral-chip`, `Variant.Outlined` или
  `Variant.Filled`, параметры `Selected`, `Disabled`, `Size` и callback `Close`.
- Статусные метки предоставляет пакет компонентов через `AdmiralTag`.

## Размеры и статусы Modal

Для `DialogOptions.CssClass` используйте базовый класс `admiral-dialog` и класс
размера:

| Класс | Ширина Admiral |
| --- | ---: |
| `admiral-dialog--s` | 384 px |
| `admiral-dialog--m` | 488 px |
| `admiral-dialog--l` | 592 px |
| `admiral-dialog--xl` | 800 px |

Пример:

```csharp
new DialogOptions
{
    Width = "30.5rem",
    CssClass = "admiral-dialog admiral-dialog--m",
    ShowClose = true,
    CloseDialogOnOverlayClick = true
};
```

Для статусной иконки добавьте `admiral-dialog__status-icon` и один из классов:

- `admiral-dialog__status-icon--information`;
- `admiral-dialog__status-icon--success`;
- `admiral-dialog__status-icon--warning`;
- `admiral-dialog__status-icon--danger`.

Контейнер действий должен иметь класс `admiral-dialog__actions`. Ниже 576 px
кнопки автоматически становятся полноширинными и располагаются вертикально,
основное действие — первым.
