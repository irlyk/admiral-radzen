using Microsoft.AspNetCore.Components;
using Radzen.Admiral.Components;

namespace AdmiralConsumerSmoke.Shared;

public static class ComponentDocsCatalog
{
    public static IReadOnlyList<Type> AdmiralTypes(string component) => component switch
    {
        "Badge" => [typeof(AdmiralBadge), typeof(AdmiralBadgeDot)],
        "ButtonGroups" => [typeof(AdmiralButtonGroup), typeof(AdmiralIconButtonGroup)],
        "Carousel" => [typeof(AdmiralCarousel), typeof(AdmiralCarouselItem)],
        "CheckboxGroup" => [typeof(AdmiralCheckboxCompositeGroup), typeof(AdmiralCheckboxGroup), typeof(AdmiralCheckboxField)],
        "FloatingButtonMenu" => [typeof(AdmiralFloatingButtonMenu), typeof(AdmiralFloatingButtonMenuItem)],
        "FileInput" => [typeof(AdmiralFileInput), typeof(AdmiralUploadStatus)],
        "ImageViewer" => [typeof(AdmiralImageViewer)],
        "GroupActionsPane" => [typeof(AdmiralGroupActionsPane)],
        "Toast" => [typeof(AdmiralToastHost)],
        "Chips" => [typeof(AdmiralChip)],
        "Divider" => [typeof(AdmiralDivider)],
        "IconButton" => [typeof(AdmiralIconButton)],
        "Link" => [typeof(AdmiralLink)],
        "Lists" => [typeof(AdmiralOrderedList), typeof(AdmiralUnorderedList), typeof(AdmiralListItem)],
        "Menu" => [typeof(AdmiralMenu), typeof(AdmiralMenuItem), typeof(AdmiralMenuButton), typeof(AdmiralOverflowMenu)],
        "NumberInput" => [typeof(AdmiralNumberInput<decimal>), typeof(AdmiralNumberInputField<decimal>)],
        "PhoneNumberInput" => [typeof(AdmiralPhoneNumberInput), typeof(AdmiralPhoneNumberInputField)],
        "Pills" => [typeof(AdmiralPill), typeof(AdmiralPills)],
        "Tag" => [typeof(AdmiralTag)],
        "ScrollContainer" => [typeof(AdmiralScrollContainer)],
        "Select" => [typeof(AdmiralSelect<string, string>), typeof(AdmiralSelectField<string, string>)],
        "Slider" => [typeof(AdmiralNumericSlider), typeof(AdmiralSlider)],
        "TreeSelect" => [typeof(AdmiralTreeSelect)],
        "Tree" => [typeof(AdmiralTree)],
        "Skeleton" => [typeof(AdmiralPseudoText), typeof(AdmiralPseudoIcon)],
        "Stepper" => [typeof(AdmiralStepper), typeof(AdmiralStep)],
        "TextButton" => [typeof(AdmiralTextButton)],
        "TextInput" => [typeof(AdmiralTextInput), typeof(AdmiralTextField)],
        "TextArea" => [typeof(AdmiralTextArea), typeof(AdmiralTextAreaField)],
        "Tooltip" => [typeof(AdmiralTooltip)],
        "Typography" => [typeof(AdmiralText)],
        _ => []
    };

    public static string Title(string component) => component switch
    {
        "ButtonGroups" => "ButtonGroup / IconButtonGroup",
        "CheckboxGroup" => "CheckboxGroup / CheckboxCompositeGroup",
        "Lists" => "List",
        "NumberInput" => "NumberInput / NumberInputField",
        "PhoneNumberInput" => "PhoneNumberInput / PhoneInputField",
        "ScrollContainer" => "ScrollContainer",
        "Select" => "Select / SelectField",
        "Skeleton" => "PseudoText / PseudoIcon",
        "TextInput" => "TextInput / TextField",
        "Typography" => "T / Typography",
        _ => component
    };

    public static string RadzenEquivalent(string component) => component switch
    {
        "Badge" or "Pills" or "Tag" => "RadzenBadge",
        "ButtonGroups" => "RadzenStack + RadzenButton",
        "Carousel" => "RadzenCarousel / RadzenCarouselItem",
        "CheckboxGroup" => "RadzenCheckBox + RadzenStack",
        "FloatingButtonMenu" => "RadzenFabMenu / RadzenFabMenuItem",
        "FileInput" => "RadzenUpload",
        "ImageViewer" => "No direct Radzen equivalent",
        "GroupActionsPane" => "RadzenButton / RadzenTextBox / RadzenCheckBox / RadzenPopup",
        "Toast" => "NotificationService / RadzenNotification",
        "Chips" => "RadzenChip",
        "Divider" => "HTML hr + Admiral theme classes (нет прямого Radzen-компонента)",
        "IconButton" or "TextButton" => "RadzenButton",
        "Link" => "RadzenLink",
        "Lists" => "HTML list + Admiral typography tokens",
        "Menu" => "RadzenMenu / RadzenSplitButton",
        "NumberInput" => "RadzenNumeric",
        "PhoneNumberInput" => "RadzenTextBox + RadzenDropDown",
        "ScrollContainer" => "HTML container + theme scrollbar",
        "Select" => "RadzenDropDown",
        "Slider" => "RadzenSlider",
        "TreeSelect" => "RadzenTree (без выпадающего поля)",
        "Tree" => "RadzenTree / RadzenTreeItem",
        "Skeleton" => "RadzenSkeleton",
        "Stepper" => "RadzenSteps",
        "TextInput" => "RadzenTextBox / RadzenFormField",
        "TextArea" => "RadzenTextArea",
        "Tooltip" => "RadzenTooltip",
        "Typography" => "RadzenText",
        _ => "Radzen component"
    };

    public static string Description(string name) => name switch
    {
        "Value" => "The current value; supports two-way binding with ValueChanged.",
        "ValueChanged" or "SelectedChanged" or "ActiveStepChanged" => "Reports a controlled value change; use with binding or as a standalone callback.",
        "ChildContent" or "Content" => "Arbitrary Razor content inside the component.",
        "Text" or "Label" => "The visible component label.",
        "Appearance" => "The Admiral visual or semantic appearance.",
        "Indeterminate" => "Shows partial selection; the application computes the parent value from its children.",
        "IsOpen" => "Controls menu visibility; supports @bind-IsOpen with IsOpenChanged.",
        "OpenChanged" => "Reports visibility changes, including clicks outside the menu.",
        "CloseIcon" => "The main button icon while the menu is open.",
        "Fixed" => "Pins the action group to a viewport corner; false keeps it in the page flow.",
        "Mobile" => "Uses a 16 px mobile inset instead of the 28 px desktop inset.",
        "ExtraText" => "Additional helper text below the component.",
        "FilesSelected" => "Returns validated IBrowserFile selection. A rejected selection clears the previous selection and returns an empty list. The application performs the upload.",
        "FileRemoved" => "Reports which IBrowserFile was removed from the built-in list.",
        "Accept" => "Comma-separated file extensions or MIME types; used by the file dialog and validated again by the component.",
        "MaxFiles" => "Maximum files per selection; defaults to 10.",
        "MaxFileSize" => "Maximum size of one file in bytes; null disables the limit.",
        "TooManyFilesText" or "FileTooLargeText" or "FileTypeErrorText" => "Localizable text for the corresponding selection error.",
        "RemoveFileAriaLabel" => "Accessible name of the remove button in the built-in file list.",
        "Error" => "Applies the checkbox error appearance.",
        "InputAttributes" => "HTML and aria attributes of the inner RadzenCheckBox input.",
        "SelectedIndex" => "Zero-based active slide index; supports @bind-SelectedIndex with SelectedIndexChanged.",
        "Change" => "Reports the index after selecting a different slide.",
        "Auto" => "Enables automatic slide switching.",
        "Interval" => "Automatic slide interval in milliseconds.",
        "AnimationDuration" => "Transition duration in milliseconds.",
        "AllowNavigation" => "Shows previous and next slide buttons.",
        "AllowPaging" => "Shows clickable slide indicators.",
        "AllowScroll" => "Enables swipe navigation on touch screens.",
        "ItemsPerPage" => "Slides per page; values greater than one change the navigation step.",
        "Height" => "Carousel viewport height in CSS units.",
        "PreviousText" or "NextText" => "Accessible arrow label, available to screen readers but visually hidden.",
        "PagerButtonAriaLabelFormat" => "Accessible indicator label format; {0} is the one-based slide number.",
        "Dimension" or "Size" => "Component size and geometry.",
        "Disabled" => "Disables interaction and applies the disabled appearance.",
        "ReadOnly" => "Prevents value changes while retaining reading and focus.",
        "Loading" => "Shows loading state and temporarily blocks the action.",
        "Skeleton" => "Shows a skeleton during initial loading.",
        "Class" => "Additional CSS class on the outer element.",
        "AdditionalAttributes" => "Unmatched HTML, aria and data attributes on the outer element.",
        "Click" => "Handles the component's primary action.",
        "Icon" or "IconStart" or "IconEnd" => "Icon name, for example admiral:system/star-solid.",
        "Placeholder" => "Hint displayed in an empty input.",
        "Status" => "Input semantic state: default, success or error.",
        "Kind" => "Tag semantic status: Neutral, Success, Primary, Danger or Warning.",
        "Orientation" => "Horizontal or vertical divider axis.",
        "Length" => "Divider length in CSS units on the chosen axis; defaults to 100%.",
        "CustomColor" => "Custom CSS color instead of Appearance.",
        "StatusViaBackground" => "Shows status using a background and border instead of a colored dot.",
        "IsBorderHidden" => "Hides the background Tag border.",
        "Width" => "Tag width in CSS units; long text is truncated with an ellipsis.",
        "CustomBackground" => "Custom dot or background color, depending on StatusViaBackground.",
        "CustomBorder" => "Custom border color for a background Tag.",
        "CustomHoverBackground" => "Custom hover background for an interactive Tag.",
        "Tooltip" => "Full text in the browser's native tooltip.",
        "Items" or "Data" or "Countries" => "The component's item source.",
        "CheckedValues" => "Checked tree IDs; cascading mode includes normalized parent IDs.",
        "CheckedValuesChanged" => "Reports checked IDs; supports @bind-CheckedValues.",
        "ExpandedValues" => "Expanded branches; local initial state without binding or controlled state with a callback.",
        "ExpandedValuesChanged" => "Reports expanded IDs after a user action; supports @bind-ExpandedValues.",
        "AllowCheckBoxes" => "Shows checkboxes; defaults to true in AdmiralTree.",
        "CheckStrictly" => "Independent checkboxes without cascading; defaults to false.",
        "IconSize" => "Leading menu icon size; defaults to Large (24 px). Small ActionBar uses Medium (20 px).",
        "Actions" => "Toolbar actions: Id, Text, Icon, Disabled and DividerBefore.",
        "ActionClicked" => "Returns the selected AdmiralActionItem; disabled actions do not invoke the callback.",
        "Columns" => "Column menu entries: Id, Text, Visible and Disabled.",
        "ColumnsChanged" => "Reports the updated AdmiralPaneColumn list; supports @bind-Columns.",
        "SearchValue" => "Search text; the application applies it to its data.",
        "SearchValueChanged" => "Reports search text changes; closing search clears it.",
        "ShowSettings" => "Shows the settings button; defaults to true.",
        "SettingsClicked" => "Requests settings; the application provides the settings UI.",
        "Template" or "ItemTemplate" or "ValueTemplate" => "Rendering template with the current item as its context.",
        "Style" => "Inline CSS on the outer element.",
        _ when name.EndsWith("Changed", StringComparison.Ordinal) => "Reports changes to the corresponding parameter.",
        _ when name.StartsWith("Aria", StringComparison.Ordinal) || name.EndsWith("AriaLabel", StringComparison.Ordinal) => "Accessible name or text for screen readers.",
        _ => "Configures the corresponding component behavior. Type and default are shown alongside."
    };

    public static string Example(string component) => component switch
    {
        "Badge" => "<AdmiralBadge Text=\"7\" Appearance=\"AdmiralBadgeAppearance.Info\" Dimension=\"AdmiralBadgeDimension.M\" />",
        "ButtonGroups" => "<AdmiralButtonGroup Dimension=\"AdmiralButtonDimension.M\"><AdmiralButton Text=\"Да\" /><AdmiralButton Text=\"Нет\" /></AdmiralButtonGroup>",
        "Carousel" => "<AdmiralCarousel Appearance=\"AdmiralCarouselAppearance.Outer\" Height=\"16rem\" @bind-SelectedIndex=\"slide\"><Items><AdmiralCarouselItem>Первый слайд</AdmiralCarouselItem><AdmiralCarouselItem>Второй слайд</AdmiralCarouselItem></Items></AdmiralCarousel>",
        "CheckboxGroup" => "<AdmiralCheckboxCompositeGroup Dimension=\"AdmiralCheckboxDimension.M\"><AdmiralCheckboxField Text=\"Все\" Value=\"@anyChecked\" Indeterminate=\"@partiallyChecked\" Change=\"SelectAll\" /><AdmiralCheckboxGroup><AdmiralCheckboxField Text=\"Москва\" @bind-Value=\"moscow\" /></AdmiralCheckboxGroup></AdmiralCheckboxCompositeGroup>",
        "FloatingButtonMenu" => "<AdmiralFloatingButtonMenu Icon=\"admiral:system/email-outline\" AriaLabel=\"Действия\" @bind-IsOpen=\"menuOpen\"><AdmiralFloatingButtonMenuItem Icon=\"admiral:system/print-outline\" AriaLabel=\"Печать\" Click=\"Print\" /></AdmiralFloatingButtonMenu>",
        "FileInput" => "<AdmiralFileInput Dimension=\"AdmiralFileInputDimension.Xl\" Title=\"Добавьте файлы\" Accept=\".pdf,.png\" FilesSelected=\"OnFilesSelected\" />",
        "ImageViewer" => "<AdmiralImageViewer Items=\"@images\" Multiple=\"true\" @bind-ActiveIndex=\"activeIndex\" @bind-Visible=\"visible\" />",
        "GroupActionsPane" => "<AdmiralGroupActionsPane Actions=\"@actions\" @bind-SearchValue=\"search\" @bind-Columns=\"columns\" ActionClicked=\"HandleAction\" SettingsClicked=\"OpenSettings\" />",
        "Toast" => "<AdmiralToastHost Position=\"AdmiralToastPosition.TopRight\" />",
        "Chips" => "<AdmiralChip Text=\"Фильтр\" @bind-Selected=\"selected\" IconStart=\"admiral:system/filter-outline\" />",
        "Divider" => "<AdmiralDivider Appearance=\"AdmiralDividerAppearance.Primary\" Size=\"AdmiralDividerSize.Small\" Length=\"70%\" />",
        "IconButton" => "<AdmiralIconButton Icon=\"admiral:system/edit-outline\" AriaLabel=\"Редактировать\" Click=\"Edit\" />",
        "Link" => "<AdmiralLink Path=\"/profile\" Text=\"Профиль\" IconEnd=\"admiral:system/arrow-right-outline\" />",
        "Lists" => "<AdmiralOrderedList><AdmiralListItem>Первый пункт</AdmiralListItem><AdmiralListItem>Второй пункт</AdmiralListItem></AdmiralOrderedList>",
        "Menu" => "<AdmiralMenuButton Text=\"Действия\"><AdmiralMenuItem Text=\"Редактировать\" Click=\"Edit\" /></AdmiralMenuButton>",
        "NumberInput" => "<AdmiralNumberInputField TValue=\"decimal\" Label=\"Сумма\" TextAfter=\"₽\" @bind-Value=\"amount\" />",
        "PhoneNumberInput" => "<AdmiralPhoneNumberInputField Label=\"Телефон\" @bind-Value=\"phone\" @bind-Country=\"country\" />",
        "Pills" => "<AdmiralPill Text=\"Активен\" Appearance=\"AdmiralPillAppearance.Success\" />",
        "Tag" => "<AdmiralTag Text=\"Выполнено\" Kind=\"AdmiralTagKind.Success\" StatusViaBackground=\"true\" />",
        "ScrollContainer" => "<AdmiralScrollContainer Height=\"16rem\">@content</AdmiralScrollContainer>",
        "Select" => "<AdmiralSelectField TValue=\"string\" Label=\"Город\" Data=\"cities\" @bind-Value=\"city\" />",
        "Slider" => "<AdmiralSlider @bind-Value=\"value\" Min=\"0\" Max=\"100\" ShowTooltip=\"true\" />",
        "TreeSelect" => "<AdmiralTreeSelect Items=\"sections\" Multiple=\"true\" @bind-Values=\"selectedIds\" />",
        "Tree" => "<AdmiralTree Items=\"items\" @bind-Value=\"selected\" @bind-CheckedValues=\"checkedIds\" @bind-ExpandedValues=\"expandedIds\" />",
        "Skeleton" => "<AdmiralPseudoText Appearance=\"AdmiralPseudoTextAppearance.Primary\" />",
        "Stepper" => "<AdmiralStepper @bind-ActiveStep=\"step\"><AdmiralStep>Данные</AdmiralStep><AdmiralStep>Проверка</AdmiralStep></AdmiralStepper>",
        "TextButton" => "<AdmiralTextButton Text=\"Подробнее\" IconEnd=\"admiral:system/arrow-right-outline\" Click=\"Open\" />",
        "TextInput" => "<AdmiralTextField Label=\"Название\" Placeholder=\"Введите название\" @bind-Value=\"name\" />",
        "TextArea" => "<AdmiralTextAreaField Label=\"Комментарий\" @bind-Value=\"comment\" Rows=\"3\" />",
        "Tooltip" => "<AdmiralTooltip Text=\"Подсказка\" Position=\"AdmiralTooltipPosition.Top\"><AdmiralButton Text=\"Наведи\" /></AdmiralTooltip>",
        "Typography" => "<AdmiralText Typography=\"AdmiralTypography.HeaderH3\" Tag=\"AdmiralTextTag.H2\">Заголовок</AdmiralText>",
        _ => string.Empty
    };

    public static string ThemeExample(string component) => component switch
    {
        "Badge" or "Pills" or "Tag" => "<RadzenBadge Text=\"Активен\" BadgeStyle=\"BadgeStyle.Success\" />",
        "ButtonGroups" => "<RadzenStack Orientation=\"Orientation.Horizontal\" Gap=\"0\"><RadzenButton Text=\"Первое\" /><RadzenButton Text=\"Второе\" /></RadzenStack>",
        "Carousel" => "<RadzenCarousel class=\"admiral-carousel admiral-carousel--outer\" Auto=\"false\" PagerOverlay=\"false\" Style=\"height:16rem\"><Items><RadzenCarouselItem>Первый слайд</RadzenCarouselItem></Items></RadzenCarousel>",
        "CheckboxGroup" => "<RadzenStack Gap=\"1rem\"><label><RadzenCheckBox TValue=\"bool?\" @bind-Value=\"anyChecked\" /> Все</label><RadzenStack Gap=\"1rem\" Style=\"padding-inline-start:32px\"><label><RadzenCheckBox TValue=\"bool?\" @bind-Value=\"moscow\" /> Москва</label></RadzenStack></RadzenStack>",
        "FloatingButtonMenu" => "<RadzenFabMenu Icon=\"admiral:system/email-outline\" ToggleIcon=\"admiral:service/close-outline\" @bind-IsOpen=\"menuOpen\"><RadzenFabMenuItem Icon=\"admiral:system/print-outline\" Text=\"Печать\" Click=\"Print\" /></RadzenFabMenu>",
        "Chips" => "<RadzenButton Text=\"Фильтр\" Icon=\"admiral:system/close-outline\" Variant=\"Variant.Outlined\" Size=\"ButtonSize.Small\" />",
        "Divider" => "<hr class=\"admiral-divider admiral-divider--horizontal admiral-divider--s admiral-divider--primary\" role=\"separator\" aria-orientation=\"horizontal\" />",
        "IconButton" => "<RadzenButton Icon=\"admiral:system/edit-outline\" aria-label=\"Редактировать\" Variant=\"Variant.Text\" Size=\"ButtonSize.Small\" />",
        "TextButton" => "<RadzenButton Text=\"Подробнее\" Icon=\"admiral:system/arrow-right-outline\" Variant=\"Variant.Text\" Size=\"ButtonSize.Small\" />",
        "Link" => "<RadzenLink Path=\"/profile\" Text=\"Профиль\" Icon=\"admiral:system/arrow-right-outline\" />",
        "Lists" => "<ol class=\"admiral-text admiral-text--body1long\"><li>Первый пункт</li><li>Второй пункт</li></ol>",
        "Menu" => "<RadzenSplitButton Text=\"Действия\" Icon=\"admiral:system/more-horizontal-outline\" Click=\"OnAction\"><RadzenSplitButtonItem Text=\"Редактировать\" Value=\"edit\" /></RadzenSplitButton>",
        "NumberInput" => """
            <RadzenNumeric TValue="decimal?" @bind-Value="amount" Min="0" Max="1000"
                           ShowUpDown="true" class="admiral-input--m" />
            <output aria-live="polite">Amount: @amount</output>
            @code { private decimal? amount = 100; }
            """,
        "PhoneNumberInput" => """
            <RadzenStack Orientation="Orientation.Horizontal">
                <RadzenDropDown TValue="string" Data="@countries" @bind-Value="country" />
                <RadzenTextBox @bind-Value="phone" class="admiral-input--m" />
            </RadzenStack>
            <output aria-live="polite">@country · @phone</output>
            @code {
                private string[] countries = ["RUS", "GBR", "USA"];
                private string country = "RUS";
                private string phone = "+7 999 123-45-67";
            }
            """,
        "ScrollContainer" => "<div class=\"admiral-scroll-container\" style=\"height:16rem;overflow:auto\">@content</div>",
        "Select" => """
            <RadzenDropDown TValue="string" Data="@cities" @bind-Value="city"
                            AllowFiltering="true" AllowClear="true" class="admiral-input--m" />
            <output aria-live="polite">City: @city</output>
            @code {
                private string[] cities = ["Moscow", "Kazan"];
                private string? city;
            }
            """,
        "Skeleton" => "<RadzenSkeleton Width=\"12rem\" Height=\"1.5rem\" />",
        "Stepper" => "<RadzenSteps @bind-SelectedIndex=\"step\"><Steps><RadzenStepsItem Text=\"Данные\" /><RadzenStepsItem Text=\"Готово\" /></Steps></RadzenSteps>",
        "TextInput" => """
            <RadzenFormField Text="Name" class="admiral-input--m">
                <ChildContent><RadzenTextBox @bind-Value="name" /></ChildContent>
            </RadzenFormField>
            <output aria-live="polite">Name: @name</output>
            @code { private string? name; }
            """,
        "TextArea" => "<RadzenTextArea @bind-Value=\"comment\" Rows=\"3\" class=\"admiral-text-area-control admiral-text-area-control--m\" />",
        "Tooltip" => "@inject TooltipService Tooltips\n<RadzenButton @ref=\"target\" Text=\"Наведи\" @onmouseenter=\"() => Tooltips.Open(target, \"Подсказка\")\" />",
        "Typography" => "<RadzenText TextStyle=\"TextStyle.H3\" TagName=\"TagName.H2\">Заголовок</RadzenText>",
        _ => string.Empty
    };
}
