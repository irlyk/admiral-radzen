# Статическая документация AdmiralDocs

`AdmiralDocs` — Blazor WebAssembly-приложение с шестью страницами компонентов:
`Button`, `Checkbox`, `Select`, `PhoneNumberInput`, `Tables`, `Tooltip`.
Страницы и общие Razor-блоки подключены из `AdmiralConsumerSmoke` ссылками на
исходные файлы, поэтому примеры остаются одинаковыми в обоих приложениях.

## Локальная сборка

Сначала соберите три локальных NuGet-пакета:

```bash
tools/admiral/build-admiral-packages.sh --configuration Release
dotnet build samples/AdmiralDocs/AdmiralDocs.csproj -c Release
dotnet publish samples/AdmiralDocs/AdmiralDocs.csproj -c Release -o /tmp/admiral-docs-publish
```

Для просмотра во время разработки:

```bash
ASPNETCORE_URLS=http://localhost:5073 dotnet run --project samples/AdmiralDocs/AdmiralDocs.csproj --no-launch-profile
```

Откройте `http://localhost:5073/`. Не открывайте `wwwroot/index.html` через `file://`: Blazor WebAssembly и пакетные static web assets требуют HTTP-сервера. Общий `site.css` синхронизируется из smoke при сборке, поэтому страницы используют тот же стиль без отдельной ручной копии.

Публикуйте содержимое `wwwroot` **под** `/admiral-radzen/`. Host использует `<base href="/admiral-radzen/">`. В publish-каталоге находятся `.nojekyll` и копия `index.html` как `404.html` для прямого открытия вложенных маршрутов на GitHub Pages. Локальный статический сервер тоже должен возвращать этот файл при неизвестном маршруте; обычный `python -m http.server` этого не делает.

Локальный `dotnet run` запускает стандартный WASM DevServer в корне; на
`localhost` приложение меняет base URI для этого режима. Для проверки
опубликованных файлов под `/admiral-radzen/` используйте `npm run test:wasm`.

Тема и язык сохраняются в `localStorage`. По умолчанию выбраны русский язык и
светлая тема. Если целевая страница не входит в эти шесть, приложение сообщает
об этом явно. Серверные маршруты `/coverage`, `/icons`, `/upload/smoke/...` и
регрессионные страницы доступны только в `AdmiralConsumerSmoke`.

Приложение использует те же пакеты и версию Radzen, что и smoke.
