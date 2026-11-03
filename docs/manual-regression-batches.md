# Manual regression batches

Work takes place on `codex/manual-regression-batch`. Merge to `main` and choose new package versions only after review. The pinned AdmiralDS React reference is `4721c1b326ac17fb7b08e915adfce5b8dced0664`.

## Batch 1 — documentation shell and navigation

- The existing menu search, RU/EN theme preservation, individual Checkbox and Input pages, table action visibility, PaginationOne simple/complex examples, and PhoneNumberInput popup fixes were already present on `main`.
- Add a global radius control in smoke, improve dark documentation links, hide the scrollbar in collapsed icon navigation, give IconButtonGroup a separate page, and expose Skeleton in the IconButton playground.
- Keep the existing `/components/button-groups` URL and all regression examples.

## Batch 2 — actions and Checkbox

- Keep Primary and Danger loading buttons visually distinct and prevent loading buttons from submitting or resetting forms.
- Make ButtonGroup and IconButtonGroup appearance and dimension changes update their children.
- Restore the unchecked Checkbox error border in both themes.
- Keep the existing Checkbox check transition: the pinned React component uses a 0.1-second background transition and does not draw the checkmark with a separate animation.

## Batch 3 — inputs and file cards

- Match pinned AdmiralDS error/success input borders in default, hover, and focus states; reverse only AdmiralNumberInput stepper placement to minus/plus.
- Match FileInput text colours in both themes and use XL/M file-card layouts with extension-based file-type icons. Preserve direct RadzenUpload as an independent themed example.
- NumberInput prefix/suffix colours remain a separate design task. The current RadzenNumeric format puts prefix, value, and suffix into one input text node, so CSS cannot style them independently. A markup change needs a focused interaction and accessibility review.

## Follow-up — direct upload and phone composition

- The direct RadzenUpload example now switches between XL and M. Radzen's optional image preview occupies the same slot as the generic file icon in XL and is hidden in M. RadzenUpload has no per-file row template, so file-type SVGs remain an AdmiralUploadStatus feature; the direct Radzen tab states this limit.
- PhoneNumberInput reserves room for the flag and chevron after the shared TextInput padding reset. The former rule was overwritten by a later `!important` selector, allowing the flag to cover the number. Browser regression checks text start against the country selector for each rendered size.

The batches share one final browser and local-package check. Source presence or CSS selectors alone do not establish visual parity.

## Follow-up — TextArea

- TextAreaField associates its label with the textarea even when the caller omits `Id`. The browser test checks the accessible name and focus on label click.
- Native resize writes a height onto the textarea. In browsers that support `field-sizing`, AutoHeight now takes precedence over that height so the field can grow and shrink after switching modes.
- In Chromium, the S/M/XL typography and base padding agree with the pinned React source. The light and dark checks covered error/success borders, disabled and read-only, copying, manual resize and bounded auto-height. The direct Radzen example retained multiline binding, focus, row changes and disabled behavior.
- The upstream Radzen `/textarea` demo also passed multiline change, rows/columns, focus and disabled checks. The demo’s auto-resize example relies on an inline event handler blocked by the host CSP with both Admiral themes and the stock Material theme.
- React gives the clear action priority when both clear and copy are enabled. The Blazor wrapper exposes both actions in that case. It uses CSS for auto-height, while React measures content.

## Follow-up — Select and DropDown

- Escape restores focus to the combobox in both Admiral selection modes. The previous focus handler only recognized the single-select panel.
- When narrowing the window clips an open Admiral popup horizontally, Admiral closes it through Radzen so it can reopen at the new position. Direct RadzenDropDown retains its original resize and focus behavior.
- The browser suite checks grouped single selection, adding and removing multiple values, and independent direct Radzen selection in both themes and languages at desktop and mobile widths. Grouping and async data are still limited to the single-select path.

## Follow-up — PhoneNumberInput

- Keyboard selection keeps the existing country combobox mounted so focus can return to it after the country list closes. The first Tab after keyboard selection can still be consumed by the dropdown; a subsequent Tab reaches the phone input.
- The browser test covers retaining Kazakhstan when editing a shared +7 number, replacing the dial code without losing national digits, and updating the country label when a typed international prefix changes the country in Digits mode.

## Follow-up — NumberInput

- NumberInputField uses the native Radzen input ID for label focus and references the visible label for the accessible name. An explicit Name still supplies the native input name and ID.
- Text decorations preserve literal apostrophes when building a numeric format. Chromium checks cover editing a decorated value, stepping, clearing a nullable value, and enforcing Min/Max. Direct RadzenNumeric keeps its own binding, stepping and bounds.
- Both themes passed the S/M/XL height and minus/plus order checks, and the fractional keyboard step used the expected separator in RU and EN. `Status=Error` sets `aria-invalid` on the spinbutton. Leaving Error restores the native Radzen validation state.

## Follow-up — DateRangePicker

- Confirming the existing range with Enter leaves both callbacks quiet. A changed boundary emits only its own callback, and clearing a complete range emits one callback per boundary.
- Both themes passed browser checks for reversed typed ranges, Escape recovery, keyboard calendar selection, interior shading and the direct Radzen range fixture.

## Follow-up — TextInput and TextField

- TextField supplies a stable input ID when the caller omits one, so the visible label names and focuses the input. An explicit ID still takes precedence.
- Leaving password mode resets visibility. Re-entering password mode masks the value again, and toggling visibility does not emit a value change.
- Chromium checks cover label focus, password visibility, clear, read-only, disabled and loading. The direct RadzenTextBox example retains typed input after blur and after switching to disabled. In read-only mode Admiral keeps the password toggle disabled, while the pinned React TextInput hides it.

## Open differences

| Area | Current behavior | Reason / next check |
| --- | --- | --- |
| NumberInput prefix and suffix | They share the value's text colour. | RadzenNumeric renders one input text node; separate colours require a markup and accessibility design. |
| Direct RadzenUpload file icons | Generic preview rather than extension-specific icons. | The direct control has no per-file row template; the Admiral wrapper supplies file-type icons. |
