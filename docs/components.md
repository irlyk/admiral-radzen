# Справочник компонентов Admiral

Готовые компоненты Admiral Design System для приложений на Radzen Blazor.
Компоненты имеют префикс `Admiral`, используют тему и набор иконок из соседних
пакетов и не требуют изменений в исходниках `Radzen.Blazor`.

## Подключение

```xml
<PackageReference Include="Radzen.Admiral.Components" Version="1.2.2" />
```

Пакет уже зависит от `Radzen.Blazor`, `Radzen.Admiral.Icons` и
`Radzen.Admiral.Themes`. Подключение темы, скриптов и сервисов описано в
[корневом README](radzen-integration.md).

Ветка 1.2 использует Radzen 12.0.6 и тему 1.1.x. Минимальная версия Radzen
повышена: в 12.x изменился внутренний Popup API. Публичная разметка Admiral
не требует замены типов или параметров.

Меню поддерживает ArrowUp/ArrowDown, Home/End, Enter/Space, Escape и выход
по Tab. Escape и выбор действия возвращают фокус на кнопку открытия;
клик снаружи и Tab не перехватывают фокус у следующего элемента.
Drawer и ImageViewer удерживают Tab внутри модальной поверхности и
возвращают фокус после закрытия. Это не означает полного соответствия
всех компонентов: ограничения отмечены в таблице покрытия.

Добавьте пространства имён в `_Imports.razor`:

```razor
@using Radzen.Admiral.Components
@using Radzen.Admiral.Components.Models
```

Зарегистрируйте Admiral в `Program.cs`:

```csharp
using Radzen.Admiral.Components;

builder.Services.AddAdmiralComponents();
```

Перед маршрутизатором разместите тему, а в layout — сервисный хост:

```razor
<AdmiralTheme Mode="AdmiralThemeMode.Light" />
<AdmiralServices />
```

Если приложение использует компоненты с изолированными стилями, подключите
сгенерированный Blazor-файл стилей приложения (`YourApp.styles.css`) в host-странице.
Он включает стили компонентов из NuGet-пакета, в том числе доступные, но
визуально скрытые подписи кнопок карусели.

Карусель с внешней навигацией:

```razor
<AdmiralCarousel Appearance="AdmiralCarouselAppearance.Outer"
                 Height="20rem" @bind-SelectedIndex="slideIndex">
    <Items>
        <AdmiralCarouselItem>Первый слайд</AdmiralCarouselItem>
        <AdmiralCarouselItem>Второй слайд</AdmiralCarouselItem>
    </Items>
</AdmiralCarousel>
```

`Appearance` выбирает внутренние или внешние стрелки и индикаторы;
`SelectedIndex` поддерживает двустороннюю привязку. `PreviousText`, `NextText`
и `PagerButtonAriaLabelFormat` позволяют локализовать доступные подписи.

## Компоненты

| Компонент | Назначение |
| --- | --- |
| `AdmiralTheme` | Подключение светлой или тёмной темы без Radzen-разметки |
| `AdmiralServices` | Хост для overlay-компонентов и сервисов |
| `AdmiralIcon` | Иконка Admiral с типизированными размерами 16/20/24/32 px |
| `AdmiralButton` | Основная кнопка с appearance, размерами XL/L/M/S, иконками, числовым badge и loading-состоянием |
| `AdmiralIconButton` | Квадратная кнопка-иконка XL/L/M/S |
| `AdmiralTextButton` | Текстовая кнопка M/S с иконками в начале или конце |
| `AdmiralActionBar` | Панель основных и дополнительных действий |
| `AdmiralAvatar` | Аватар с фото, инициалами или иконкой, статусом и кольцом активности |
| `AdmiralAvatarGroup` | Компактная группа аватаров с индикатором скрытых участников |
| `AdmiralCarousel` / `AdmiralCarouselItem` | Карусель с внутренним или внешним оформлением Admiral, перелистыванием и индикаторами |
| `AdmiralCheckboxCompositeGroup` / `AdmiralCheckboxGroup` / `AdmiralCheckboxField` | Вложенная группа флажков M/S с общим disabled, частичным выбором и пояснениями |
| `AdmiralDateRangePicker` | Выбор начала и конца диапазона дат |
| `AdmiralDivider` | Горизонтальный или вертикальный разделитель S/M в цветах Admiral |
| `AdmiralDrawer` | Боковая панель с затемнением фона и управлением фокусом |
| `AdmiralDropDown` | Типизированный список с группами, поиском и виртуализацией |
| `AdmiralDataGrid` | Таблица с общей панелью, поиском, выбором столбцов, фильтрами, пагинацией, локализацией и действиями строк |
| `AdmiralFloatingButton` | Плавающая кнопка M/XL с бейджем или точечным статусом |
| `AdmiralFloatingButtonMenu` / `AdmiralFloatingButtonMenuItem` | Раскрывающееся меню вторичных плавающих действий с управляемым состоянием |
| `AdmiralFileInput` | Выбор файлов XL/M с ограничениями, доступной зоной выбора и списком состояний через `AdmiralUploadStatus` |
| `AdmiralImageViewer` | Миниатюры и просмотр изображений с навигацией, поворотом, отражением и масштабом |
| `AdmiralToastHost` / `AdmiralToastService` | Стек всплывающих уведомлений с позиционированием, автозакрытием и полоской прогресса |
| `AdmiralGlobalSearch` | Строка глобального поиска с подсказками |
| `AdmiralGroupActionsPane` | Действия над группой, поиск и настройка столбцов |
| `AdmiralHint` | Информационная подсказка на базе Radzen Tooltip |
| `AdmiralInputField` | Поле с основной и дополнительной подписью над Radzen-контролом, подсказкой, размером и статусом |
| `AdmiralTextInput` | Текстовое поле S/M/XL с иконками, очисткой, паролем, loading и семантическими статусами |
| `AdmiralTextField` | Готовая композиция `AdmiralInputField` и `AdmiralTextInput` без Radzen-разметки в потребителе |
| `AdmiralNumberInput` / `AdmiralNumberInputField` | Числовой ввод S/M/XL с шагом, ограничениями, форматированием и кнопками плюс/минус |
| `AdmiralSelect` / `AdmiralSelectField` | Типизированный Select с шаблонами, группами, поиском и состояниями Admiral |
| `AdmiralTreeSelect` | Иерархический выбор в поле: одиночный/множественный режим, поиск, каскад и чипы; замена устаревшего SelectTree |
| `AdmiralTree` | Самостоятельное дерево M/S: выбор, каскадные/независимые checkbox-ы, disabled-ветки, раскрытие и клавиатура |
| `AdmiralNumericSlider` / `AdmiralSlider` | Ползунок XL/M: decimal, дробный/свободный шаг, пересечение и совпадение маркеров, DragEnded, локализованные отметки и шаблон. `AdmiralSlider` сохраняет совместимый int API |
| `AdmiralPhoneNumberInput` / `AdmiralPhoneNumberInputField` | Телефонное поле с флагом, поиском страны и заменой телефонного кода |
| `AdmiralGrid` / `AdmiralGridItem` | Адаптивная модульная сетка Admiral на 4, 8 и 12 колонок |
| `AdmiralLayout` | Основа страницы с декларативными слотами header/menu/content и адаптивным боковым меню 240/288/336 px |
| `AdmiralLayoutHeader` | Шапка layout со встроенным управлением мобильным и desktop-меню |
| `AdmiralSideMenu` / `AdmiralSideMenuItem` | Рекурсивное меню с вложенностью, активной веткой и icons-only режимом |
| `AdmiralMenu` / `AdmiralMenuItem` | Декларативное popup-меню с иконками, описанием, checkbox, shortcut и состояниями |
| `AdmiralMenuButton` / `AdmiralOverflowMenu` | Меню с обычной кнопкой или компактной кнопкой-иконкой |
| `AdmiralMultiButton` / `AdmiralTextButtonMenu` / `AdmiralTagMenu` | Специализированные Admiral-триггеры поверх общего меню |
| `AdmiralNotificationItem` | Статусное уведомление с заголовком, действиями и закрытием |
| `AdmiralPaginationOne` | Полная или компактная пагинация с диапазоном, размером страницы и выбором страницы |
| `AdmiralPaginationSimple` | Компактный полосовой индикатор страниц или слайдов |
| `AdmiralProgressPage` | Линейный прогресс операции или страницы |
| `AdmiralProgressHeader` | Фиксированный прогресс высотой 4 px у верхнего края окна |
| `AdmiralProgressStepper` | Пошаговый индикатор с навигацией |
| `AdmiralSpinner` | Индикатор загрузки S/MS/M/L/XL с оригинальной SVG-геометрией |
| `AdmiralStatusIndicator` | Текстовый статус M/S с иконкой слева или справа |
| `AdmiralTag` | Статическая или интерактивная статусная метка M/S |
| `AdmiralUploadStatus` | Состояние загрузки отдельного файла |

## Базовые примеры

Layout не требует `@ref`, ручного переноса событий из header в sidebar или
отдельного состояния для мобильного меню:

```razor
<AdmiralLayout DesktopCollapseTarget="AdmiralSideMenuState.IconsOnly"
               SideMenuShadow="true">
    <Header>
        <AdmiralLayoutHeader>
            <Brand>Мой продукт</Brand>
            <Actions>
                <AdmiralIconButton Icon="admiral:system/settings-outline"
                                   AriaLabel="Настройки" />
            </Actions>
        </AdmiralLayoutHeader>
    </Header>
    <SideMenu>
        <AdmiralSideMenu>
            <AdmiralSideMenuItem Text="Главная"
                                 Icon="admiral:system/home-outline"
                                 Href="/" Match="NavLinkMatch.All" />
            <AdmiralSideMenuItem Text="Документы"
                                 Icon="admiral:documents/folder-outline"
                                 Expanded="true">
                <AdmiralSideMenuItem Text="Архив" Href="/archive" />
                <AdmiralSideMenuItem Text="Шаблоны" Href="/templates" />
            </AdmiralSideMenuItem>
        </AdmiralSideMenu>
    </SideMenu>
    <Content>@Body</Content>
</AdmiralLayout>
```

На ширине 1024–1279 px меню автоматически работает как раскрывающаяся по
наведению панель иконок; ниже 1024 px открывается поверх контента. Если активен
вложенный пункт, в icons-only режиме подсвечивается и его родитель.

### Состояния бокового меню

| Ширина окна | Обычное состояние | Hover / фокус | Кнопка в header |
| --- | --- | --- | --- |
| Меньше 1024 px | Меню скрыто | Не раскрывает меню | Открывает полноразмерное меню поверх body с затемнением |
| 1024–1279 px | Полоса иконок 56 px, body занимает остальное место | Временно раскрывает меню поверх body | Открывает полноразмерное меню поверх body; оно остаётся открытым после ухода мыши до повторного нажатия или закрытия |
| От 1280 px, `Expanded` | Полное меню выбранной ширины, body занимает остальное место | Не меняет ширину | Переключает в `DesktopCollapseTarget` (`IconsOnly` или `Hidden`) |
| От 1280 px, `IconsOnly` | Полоса иконок 56 px | Временно раскрывает меню поверх body | Переключает в постоянный `Expanded` |
| От 1280 px, `Hidden` | Меню полностью скрыто | Нет области для hover | Переключает в постоянный `Expanded` |

`SideMenuState` управляет постоянным состоянием на широком экране, а `MenuOpen` —
временным открытием поверх body на узком экране. При полном раскрытии подписи и
вложенные пункты видны без hover; скрывать их разрешено только в свернутой полосе
иконок. Поэтому правила компактной строки не должны перебивать `Expanded` или
`MenuOpen`.

```razor
<AdmiralButton Text="Создать"
               Appearance="AdmiralButtonAppearance.Primary"
               Dimension="AdmiralButtonDimension.M"
               IconStart="admiral:service/plus-outline"
               BadgeCount="5"
               Click="Create" />

<AdmiralIconButton Icon="admiral:system/settings-outline"
                   AriaLabel="Настройки"
                   Dimension="AdmiralButtonDimension.M" />

<AdmiralTextButton Text="Подробнее"
                   IconEnd="admiral:system/arrow-right-outline" />

<AdmiralProgressPage Label="Загрузка документов…" Value="64" />

<AdmiralProgressHeader Value="64"
                       AriaLabel="Загрузка страницы" />

<AdmiralNotificationItem Status="AdmiralNotificationStatus.Info"
                         Title="Платёж готов"
                         DisplayStatusIcon="true"
                         IsClosable="true"
                         Close="CloseNotification">
    <ChildContent>Проверьте реквизиты перед отправкой.</ChildContent>
    <Actions>
        <RadzenButton Text="Проверить" Variant="Variant.Text" />
    </Actions>
</AdmiralNotificationItem>

<AdmiralTag Text="Выполнено" Kind="AdmiralTagKind.Success" />
<AdmiralTag Text="Ошибка" Kind="AdmiralTagKind.Danger"
            StatusViaBackground="true" />

<AdmiralAvatar Name="Елена Петрова"
               ImageUrl="images/elena.jpg"
               Status="AdmiralAvatarStatus.Success" />

<AdmiralAvatarGroup Items="@participants"
                    Size="AdmiralAvatarSize.Medium"
                    MaxVisible="4" />

<AdmiralSpinner Size="AdmiralSpinnerSize.Medium"
                AriaLabel="Загрузка" />

<AdmiralStatusIndicator Text="Выполнено"
                        Icon="admiral:service/check-outline"
                        Appearance="AdmiralStatusIndicatorAppearance.Success" />

<AdmiralDivider Appearance="AdmiralDividerAppearance.Primary"
                Size="AdmiralDividerSize.Small"
                Length="70%" />

<AdmiralFloatingButton Icon="admiral:system/email-outline"
                       AriaLabel="Открыть сообщения"
                       BadgeCount="5" />

<AdmiralProgressStepper Steps="@steps"
                        @bind-SelectedIndex="step"
                        ShowActions="true"
                        PreviousText="Назад"
                        NextText="Далее" />

<AdmiralActionBar Items="@actions" ActionClicked="OnAction" />
<AdmiralGroupActionsPane Actions="@actions" Columns="@columns"
                         Size="AdmiralGroupActionsPaneSize.Large" />
<AdmiralGlobalSearch Items="@searchItems"
                     @bind-Value="query"
                     Placeholder="Поиск" />

<AdmiralDataGrid TItem="Payment"
                 Data="@payments"
                 Columns="@columns"
                 RowActions="@rowActions"
                 AllowFiltering="true"
                 Pagination="AdmiralDataGridPagination.PaginationTwo"
                 PageSize="20" />

<AdmiralDrawer @bind-Open="drawerOpen" Title="Параметры платежа"
               Position="AdmiralDrawerPosition.Left" Width="30rem"
               Appearance="AdmiralDrawerAppearance.Primary">
    <ChildContent>
        Содержимое боковой панели
    </ChildContent>
    <Actions>
        <AdmiralButton Text="Сохранить" Click="@(() => drawerOpen = false)" />
    </Actions>
</AdmiralDrawer>

<AdmiralHint TriggerText="Подробнее" Title="Обработка платежа"
             Placement="AdmiralHintPlacement.Auto"
             TriggerAppearance="AdmiralButtonAppearance.Secondary">
    Платёж проходит проверку.
</AdmiralHint>

<AdmiralInputField Label="Номер счёта"
                   AdditionalLabel="Необязательно"
                   ExtraText="Укажите 20 цифр"
                   Size="AdmiralInputSize.Medium"
                   For="account-number">
    <RadzenTextBox id="account-number" @bind-Value="accountNumber" />
</AdmiralInputField>

<AdmiralTextField Label="Поиск"
                  ExtraText="Введите имя клиента"
                  IconStart="admiral:system/search-outline"
                  DisplayClearIcon="true"
                  @bind-Value="searchValue" />

<AdmiralNumberInputField TValue="decimal?"
                         Label="Сумма"
                         Prefix="₽ "
                         Step="100"
                         Min="0"
                         @bind-Value="amount" />

<AdmiralPhoneNumberInputField Label="Телефон"
                              @bind-Value="phone"
                              @bind-Country="country" />

<AdmiralLayout SideMenuWidth="AdmiralSideMenuWidth.Medium"
               DesktopCollapseMode="AdmiralSideMenuCollapseMode.Button"
               @bind-SideMenuCollapsed="navigationCollapsed">
    <Header>Заголовок приложения</Header>
    <SideMenu>Навигация</SideMenu>
    <ChildContent>
        <AdmiralGridItem MobileSpan="4" TabletSpan="4" DesktopSpan="6">
            Левая область
        </AdmiralGridItem>
        <AdmiralGridItem MobileSpan="4" TabletSpan="4" DesktopSpan="6">
            Правая область
        </AdmiralGridItem>
    </ChildContent>
</AdmiralLayout>

<AdmiralUploadStatus FileName="payment.pdf"
                     Meta="2,4 МБ"
                     State="AdmiralUploadState.Loading"
                     Progress="64" />
```

`AdmiralLayout` использует flex-вариант сетки: на 320 px доступны 4 колонки,
на 768 px — 8, с 1024 px — 12. На 1024–1279 px боковое меню сворачивается
до 56 px и раскрывается поверх контента; ниже 1024 px оно скрывается и
открывается с модальным затемнением. На широком экране меню занимает выбранные
240, 288 или 336 px. `DesktopCollapseMode.IconsOnly` постоянно оставляет полосу
56 px и раскрывает полную ширину меню поверх контента по hover или при переходе
фокуса внутрь меню. `DesktopCollapseMode.Button` оставляет полноразмерное меню и
полностью скрывает его при `SideMenuCollapsed="true"`; старое имя `Hidden`
сохранено как совместимый алиас `Button`. Пункт меню получает класс
`admiral-layout__side-menu-item`, а его подпись —
`admiral-layout__side-menu-label`: в rail это даёт иконку 24 px с отступами 8 px
внутри области 40 px. Внешние кнопки открытия и сворачивания можно привести к
той же геометрии классом `admiral-layout__menu-control`. Для независимой сетки
без shell используйте
`AdmiralGrid`, а для компактных внешних отступов —
`Variant="AdmiralGridVariant.Alternative"`.

Header занимает всю ширину shell, а sidebar и body располагаются следующей
строкой под ним. Стандартная высота header — `4rem`; если у приложения другая
геометрия, передайте её через `HeaderHeight` — это значение также задаёт верхнюю
границу мобильного меню и затемнения.

`AdmiralDataGrid` автоматически выбирает русские или английские подписи фильтров,
пейджера, поиска и настройки столбцов по `CurrentUICulture`. Тексты можно полностью
заменить одним объектом `Localization`. Колонки и действия описываются один раз:

```csharp
private readonly IReadOnlyList<AdmiralDataGridColumnDefinition<Payment>> columns =
[
    new("number", nameof(Payment.Number), "Номер") { Width = "10rem" },
    new("amount", nameof(Payment.Amount), "Сумма")
    {
        FormatString = "{0:N2}",
        TextAlign = TextAlign.Right
    }
];

private IReadOnlyList<AdmiralDataGridAction<Payment>> rowActions =>
[
    new("edit", "admiral:system/edit-outline")
    {
        AriaLabelFor = payment => $"Редактировать {payment.Number}",
        Execute = payment => EditPayment(payment)
    },
    new("send", "admiral:system/arrow-right-outline")
    {
        AriaLabelFor = payment => $"Отправить {payment.Number}",
        ExecuteAsync = payment => SendPaymentAsync(payment)
    }
];
```

Синхронная lambda передаётся через `Execute`, асинхронная — через
`ExecuteAsync`. Для действий также доступен единый `RowActionClicked`, если
удобнее обрабатывать все идентификаторы в одном месте. `PaginationOne`, её
компактный вариант и стандартная `PaginationTwo` выбираются параметром
`Pagination`.

Действия строк можно объявить прямо в разметке. У каждого действия собственный
типизированный `Click`, поэтому общий callback и сопоставление по строковому
идентификатору не требуются:

```razor
<AdmiralDataGrid TItem="Payment" Data="@payments" Columns="@columns">
    <Actions>
        <AdmiralDataGridRowAction TItem="Payment"
                                  Icon="admiral:documents/copy-outline"
                                  AriaLabelFor="@(payment => $"Копировать {payment.Number}")"
                                  Click="CopyPayment" />
        <AdmiralDataGridRowAction TItem="Payment"
                                  Icon="admiral:system/edit-outline"
                                  AriaLabelFor="@(payment => $"Редактировать {payment.Number}")"
                                  Click="EditPayment" />
    </Actions>
</AdmiralDataGrid>
```

Методы получают строку напрямую: `void EditPayment(Payment payment)`. У
`AdmiralDataGridRowAction` также есть `Text`, `Disabled`, `DisabledFor`,
`DividerBefore`, `ButtonStyle` и необязательный `Size`. Пользовательский шаблон
действия намеренно не предусмотрен — компонент сохраняет единую геометрию и
состояния Admiral.

Для полного контроля над колонками доступна Radzen-подобная разметка. Она
поддерживает `Template` и `HeaderTemplate`, а верхнеуровневый `Template`
задаёт раскрываемое содержимое строки:

```razor
<AdmiralDataGrid TItem="Payment" Data="@payments"
                 ShowToolbar="false" ShowExpandColumn="true">
    <ColumnContent>
        <AdmiralDataGridColumn TItem="Payment" Property="Number" Title="Номер" />
        <AdmiralDataGridColumn TItem="Payment" Property="Status" Title="Статус">
            <Template Context="payment">
                <RadzenBadge Text="@payment.Status" />
            </Template>
        </AdmiralDataGridColumn>
    </ColumnContent>
    <Template Context="payment">
        Подробности платежа @payment.Number
    </Template>
</AdmiralDataGrid>
```

`Columns` и `ColumnContent` можно комбинировать. Автоматический поиск и выбор
видимости работают для колонок из `Columns`; для полностью разметочного варианта
задайте `SearchPredicate` либо отключите общую панель через `ShowToolbar="false"`.
`AdmiralDataGridColumn<TItem>` наследует полный публичный API
`RadzenDataGridColumn<TItem>`, поэтому имена `Property`, `Template`,
`HeaderTemplate`, `FilterTemplate`, `EditTemplate`, `Frozen`, `Pickable` и прочие
остаются привычными.

Серверная загрузка использует те же имена, что и `RadzenDataGrid`:

```razor
<AdmiralDataGrid TItem="Payment"
                 Data="@payments"
                 Count="@count"
                 LoadData="LoadPayments"
                 IsLoading="@isLoading">
    <ColumnContent>
        <AdmiralDataGridColumn TItem="Payment" Property="Number" Title="Номер" />
    </ColumnContent>
</AdmiralDataGrid>
```

Основные параметры Radzen типизированы напрямую. Остальные параметры его грида
автоматически перенаправляются во внутренний `RadzenDataGrid`; для программного
формирования также доступен словарь `GridAttributes`. Внешний контейнер и сам
грид независимо настраиваются через `Class`/`Style` и
`GridClass`/`GridStyle`.

Перестановка строк включается через `AllowRowReorder`. `RowKey` должен возвращать
стабильный уникальный ключ, а `RowReordered` вызывается один раз после drop.
Как и оригинальная Admiral Table, компонент использует управляемую модель:
обработчик переставляет элемент в исходном списке по `OldIndex` и `NewIndex`.
В аргументах также доступны `RowKey` и `NextRowKey`.
После перестановки передайте компоненту новую ссылку на коллекцию, чтобы Blazor
и внутреннее представление Radzen гарантированно обновили порядок строк.
Маркер поддерживает клавиши <kbd>↑</kbd> и <kbd>↓</kbd> как доступную
альтернативу перетаскиванию мышью.

## Пагинация

В терминологии Admiral встроенный pager `RadzenPager` и `RadzenDataGrid`
соответствует `PaginationTwo`: это ряд круглых кнопок с номерами страниц.
Размер S включается на гриде классом `admiral-pagination-two--s`, размер M
используется по умолчанию.

`AdmiralPaginationOne` реализует отдельный составной вариант с выбором размера
страницы, номера страницы, диапазоном элементов и стрелками. Через `Simple`
доступен его компактный режим без выпадающих списков.

```razor
<AdmiralPaginationOne @bind-Page="page"
                      @bind-PageSize="pageSize"
                      TotalItems="128"
                      PageSizeOptions="@(new[] { 10, 20, 50 })" />

<AdmiralPaginationOne @bind-Page="page"
                      PageSize="10"
                      TotalItems="128"
                      Simple="true"
                      Size="AdmiralPaginationSize.Small" />

<AdmiralPaginationSimple Count="6" @bind-CurrentIndex="section" />
```

`Page` у `AdmiralPaginationOne` начинается с единицы, а `CurrentIndex` у
`AdmiralPaginationSimple` — с нуля.

## Диапазон дат

`AdmiralDateRangePicker` хранит две даты: `Start` и `End`. Первый выбор в
календаре задаёт начало диапазона, второй — конец. Компонент также поддерживает
ручной ввод, `Min`, `Max`, формат и локализацию.

```razor
<AdmiralDateRangePicker @bind-Start="rangeStart"
                        @bind-End="rangeEnd"
                        DateFormat="dd.MM.yyyy"
                        Separator=" — "
                        Placeholder="ДД.ММ.ГГГГ — ДД.ММ.ГГГГ"
                        AriaLabel="Диапазон дат"
                        ToggleAriaLabel="Открыть календарь"
                        PopupAriaLabel="Календарь диапазона дат" />

@code {
    private DateTime? rangeStart;
    private DateTime? rangeEnd;
}
```

При ошибочном вводе или обратном порядке дат последние корректные значения не
перезаписываются. Для выбора только через календарь задайте `AllowInput="false"`.

## Dropdown с локальными данными

```razor
<AdmiralDropDown TValue="string" TItem="Currency"
                 @bind-Value="currencyCode"
                 Data="@currencies"
                 TextSelector="@(item => item.Name)"
                 ValueSelector="@(item => item.Code)"
                 GroupSelector="@(item => item.Region)"
                 Placeholder="Выберите валюту"
                 EmptyText="Ничего не найдено" />
```

Для множественного выбора укажите подходящий тип `TValue`, а также
`Multiple="true"` и `Chips="true"`.

## Dropdown с асинхронной загрузкой

`DataProvider` получает текст поиска, смещение страницы и её размер. При новом
запросе предыдущий автоматически отменяется через `CancellationToken`.

```razor
<AdmiralDropDown TValue="int?" TItem="Company"
                 @bind-Value="companyId"
                 DataProvider="LoadCompaniesAsync"
                 TextSelector="@(item => item.Name)"
                 ValueSelector="@(item => item.Id)"
                 AllowVirtualization="true"
                 PageSize="20"
                 LoadingText="Загрузка…"
                 ErrorText="Не удалось загрузить компании"
                 RetryText="Повторить" />

@code {
    private int? companyId;

    private async Task<AdmiralDropDownResult<Company>> LoadCompaniesAsync(
        AdmiralDropDownQuery query,
        CancellationToken cancellationToken)
    {
        var page = await companyService.FindAsync(
            query.SearchText, query.Skip, query.Take ?? 20, cancellationToken);

        return new AdmiralDropDownResult<Company>(page.Items, page.TotalCount);
    }
}
```

Дополнительно доступны отключение группы через `GroupDisabledSelector`,
отключение отдельного пункта через `DisabledSelector`, а также собственные
`ItemTemplate` и `SelectedTemplate`.

## Локализация

Текстовые параметры и ARIA-подписи компонентов можно передавать из ресурсов
обычным способом Blazor. Smoke-приложение содержит пример переключения русского
и английского языков.
