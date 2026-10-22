# Проверка Admiral-темы на полном наборе Radzen demos

## Требования и запуск

Это проверка **прямых Radzen-компонентов с темой Admiral**, не замена
smoke-галереи обёрток. Нужны .NET SDK 10, Bash, Git, `tar`, `lsof`
и отдельный checkout исходников Radzen с проектом `RadzenBlazorDemos.Host`.
На Windows эти shell-сценарии требуют совместимой Unix-среды, например WSL;
обычная сборка пакетов поддерживает PowerShell без неё.

По умолчанию checkout ищется в `../radzen/radzen-blazor` относительно
корня этого репозитория. Любое другое расположение задаётся через
`RADZEN_SOURCE_ROOT`. Скрипт не клонирует и не обновляет Radzen самостоятельно.

Из корня `admiral-radzen` запустите:

```bash
tools/admiral/run-radzen-regression.sh
```

Скрипт напечатает HTTPS-ссылки на светлую и тёмную темы. По умолчанию он выбирает первый свободный порт в диапазоне 5084–5099; при необходимости можно задать `ADMIRAL_REGRESSION_PORT=5090`. Параметр `theme` сохраняется при переходе к конкретной странице компонента Radzen. Остановить сервер — `Ctrl+C`.

Нужен локальный ASP.NET development certificate (`dotnet dev-certs https`).
Если браузер не доверяет ему, доверие настраивается пользователем обычным
`dotnet dev-certs https --trust`; скрипт самостоятельно сертификаты не создаёт
и настройки доверия не меняет. HTTP можно включить через
`ADMIRAL_REGRESSION_SCHEME=http`, но он **не подходит** для визуальной проверки
актуальных demos: upstream CSP `img-src data: https:` блокирует локальные SVG-маски.
HTTPS исправляет это без ослабления CSP и без патча исходников Radzen.

Скрипт через `git archive` создаёт временный снимок **закоммиченного** исходного
кода Radzen. Сборка и генерация файлов выполняются только в этом снимке.
Четыре CSS-файла (светлая/тёмная база и WCAG-палитры) копируются в его `wwwroot/css`
под именами `admiral-regression-*.css`. Внешний MSBuild targets-файл подключает
проекты со шрифтами и SVG из `_content/`. Рабочее дерево Radzen не меняется,
не сбрасывается, не переключается и не коммитится. Локальные изменения не включаются.
После остановки временная копия удаляется.

Можно явно выбрать checkout и уже имеющийся в нём commit/tag:

```bash
RADZEN_SOURCE_ROOT=/path/to/radzen-blazor RADZEN_SOURCE_REF=HEAD \
  tools/admiral/run-radzen-regression.sh
```

Скрипт не делает `fetch` самостоятельно. Для воспроизводимого запуска
выбирайте исходники Radzen версии, указанной в проектах пакетов.

## Пересборка темы

При обновлении Radzen может измениться набор SCSS-переменных и селекторов. Пересобрать CSS пакета из наших SCSS и текущих исходников Radzen, не меняя его рабочее дерево, можно командой:

```bash
tools/admiral/rebuild-admiral-theme.sh
```

После пересборки проверьте изменения в четырёх CSS-файлах пакета и повторите регрессионный просмотр в обеих темах.

`RADZEN_SOURCE_ROOT` поддерживается и здесь. Исходный Admiral SCSS компилируется
с актуальными общими partials Radzen во временном каталоге; CSS не исправляется
подстановками селекторов поверх старой версии. Пересборка читает рабочие SCSS выбранного checkout; `RADZEN_SOURCE_REF`
здесь не применяется. Для воспроизводимости используйте исходники нужной версии
без локальных изменений. Нужны также `rg` и `perl`.
Скрипт обновляет четыре основных CSS; два WCAG-файла остаются отдельными
палитрами и этой командой не пересоздаются.

После проверки diff выполните [сборку пакетов и решения](../README.md#сборка-и-запуск).
Обновите версии пакетов при выпуске, зависимость `Radzen.Blazor` в Components
и ссылки smoke согласованно. Не меняйте только NuGet-версию Radzen без проверки
темы, popup-компонентов, таблиц и клавиатурного поведения.

## Браузерные проверки

### Локальные проверки smoke-приложения

Нужны Node.js 20 или новее и Chromium. В `.nvmrc` закреплён Node.js
24.21.0 LTS. Из корня репозитория один раз
установите зависимости и браузер:

```bash
npm install
npx playwright install chromium
```

`package-lock.json` фиксирует версии. На чистом checkout для воспроизводимой
установки используйте `npm ci`, затем `npx playwright install chromium`.
`node_modules/` не хранится в Git.

Сначала соберите пакеты и решение, затем запустите smoke-приложение в отдельном
терминале:

```bash
tools/admiral/build-admiral-packages.sh --configuration Release
dotnet build Radzen.Admiral.slnx
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5072 \
  dotnet run --project samples/AdmiralConsumerSmoke/AdmiralConsumerSmoke.csproj \
  --no-build --no-launch-profile
```

Когда приложение доступно по `http://localhost:5072`, из второго терминала
запустите все локальные браузерные проверки:

```bash
npm run test:browser
```

Можно запускать отдельные наборы: `test:browser:audit`,
`test:browser:interactions`, `test:browser:combo`, `test:browser:drawer`,
`test:browser:hint`, `test:browser:slider`, `test:browser:file-input`,
`test:browser:image-viewer`, `test:browser:action-bar`, `test:browser:tree`,
`test:browser:native-date-range` и `test:browser:layout`.
Сценарий `test:radzen-demos` проверяет отдельный Radzen demos сервер и в общий
`test:browser` не включён. По умолчанию он обращается к
`https://localhost:5084`; внешний `RADZEN_URL` используйте только после отдельного
согласования.

Переменные окружения:

- `SMOKE_URL` — адрес локального smoke-приложения; по умолчанию
  `http://localhost:5072`.
- `PLAYWRIGHT_MODULE` — путь к существующему модулю `playwright` или
  `playwright-core`; если не задан, используется установленный npm-пакет
  `playwright`.
- `CHROME_PATH` — путь к системному Chromium/Chrome, если нужно использовать его
  вместо браузера, установленного Playwright.
- `ADMIRAL_FONT_FIXTURE_DIR` — внешний каталог с файлами Regular, Medium и
  SemiBold для необязательной проверки собственного шрифта. Для
  `font-distribution-tests.mjs` вместе с ним укажите
  `ADMIRAL_FONT_FIXTURE_EXPECTED_FAMILY` — внутреннее имя семейства этих файлов;
  тест сравнит его с фактически использованным браузером шрифтом.
- `AUDIT_OUTPUT` — каталог для JSON и скриншотов; значение по умолчанию зависит
  от сценария. Для layout-аудита это `artifacts/smoke-layout-audit`.
- `AUDIT_WIDTHS` и `AUDIT_ROUTES` — ширины и маршруты для `test:browser:layout`.
- `RADZEN_URL` — адрес Radzen demos для отдельного сценария
  `npm run test:radzen-demos`; не задавайте внешний URL без согласования.

### Аудит состояний и отдельные команды

Браузерный инвентаризационный проход smoke:

```bash
npm run test:browser:audit
```

Отчёт — `artifacts/browser-audit/gallery-states.json`: computed styles default,
hover, focus, hover+focus для разных классов контролов в обеих темах.
Это **не** автоматическое утверждение визуального паритета или проверки всех
клавиш. Поведенческие проверки и оставшиеся ограничения ведутся отдельно.

Проверки клавиатуры в smoke:

```bash
npm run test:browser:interactions
npm run test:browser:combo
npm run test:browser:drawer
npm run test:browser:hint
npm run test:browser:slider
npm run test:browser:file-input
npm run test:browser:image-viewer
npm run test:browser:action-bar
npm run test:browser:tree
npm run test:browser:native-date-range
```

Для локальных smoke-сценариев по умолчанию используется
`SMOKE_URL=http://localhost:5072`. JSON и скриншоты записываются в каталоги
`artifacts/`; точное имя каталога задаёт каждый сценарий либо `AUDIT_OUTPUT`.
Проверяйте итоговый JSON и код завершения процесса: отсутствие исключений
в консоли само по себе не подтверждает прохождение сценариев. Текущий объём проверки
и непокрытые сценарии фиксируйте в отчёте о конкретном изменении.

Это регрессионный просмотр **прямых Radzen-компонентов с Admiral-темой**, а не проверка Admiral-обёрток. Если сломался компонент Radzen, отметьте страницу, тему, размер окна и состояние (обычное, hover, focus, active, disabled), затем сравните с smoke-галереей и официальным Admiral.
