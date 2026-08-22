# Translating Reclatch

Every string the user sees lives in this folder. You never need to open the source code.

## How to add a language

Copy `en.json` to `<code>.json` — use the two-letter ISO 639-1 code, so `de.json` for
German. Translate the values on the right. **Never translate or rename the keys** on the
left; they are what the code looks up, and changing one breaks every language.

## Rules

Placeholders are named, like `{message}`. Keep them exactly as they are, but move them
wherever your language needs them in the sentence.

Keep every key present. A missing key falls back to Turkish rather than showing an empty
label, which makes the gap hard to spot.

Turkish is the source language and is typically 20-30% longer than English. If your
translation is longer still, say so in your pull request so the layout can be checked.
