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

The batches share one final browser and local-package check. Source presence or CSS selectors alone do not establish visual parity.
