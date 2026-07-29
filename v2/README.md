# Hermes v2 artifacts

`manifests/<sha256-hex>.json` contains immutable production manifests in git. New
automatic publications use `validation.status=generated`; historic
`live-verified` revisions remain valid. The
revision is `sha256:<lowercase hex>` of the exact UTF-8 manifest bytes; a manifest
does not contain its own revision. Canonical files use the generator's fixed property
order, two-space indentation, LF newlines, no UTF-8 BOM, and exactly one trailing
newline.

`latest.json` is mutable and is published only after its immutable manifest has been
uploaded and read back successfully. When generation finds no `{roots,resources}`
change, the canonical result is retained under `generated/<sha256-hex>.json` as a
repository-only audit record. The filename is its generated resource revision, while
the exact FCS and generator commits remain in the manifest's `source` object. Historic
candidate files are repository records and are never referenced by `latest.json`.

The public R2 key retains the full revision (`v2/manifests/sha256:<hex>.json`). Git and
local cache filenames use only `<hex>.json` because `:` is not a valid Windows filename
character.

The public base URL is `https://hermes.sapphosound.com/v2/`. Relative to this base,
the mutable pointer is `latest.json` and immutable objects are
`manifests/sha256:<hex>.json`. The R2 object keys retain the leading `v2/` prefix.

Repository-only generated records, fixtures, and FCS processing state are stored
under `generated/`, `fixtures/`, and `source/`; they are not public R2 objects.

The scheduled workflow generates twice, compares canonical bytes, runs schema and
semantic validation, and publishes only when `{roots,resources}` changes. A
`generated` manifest requires Sharlayan.Lite 9.2.1 or newer and contains no
live-verification metadata.

The first production manifest requires three runtime resources:

- `chatLog`: persistent chat log vectors.
- `talk`: the last committed standard Talk value, used only as a fallback.
- `currentTalk`: the visible standard `Talk` addon's current text and speaker.

`talk.utf8String.lengthSource` is `bufferUsedMinusNull`; consumers must not use
FCS `Utf8String.StringLength` as the authoritative byte length. `currentTalk`
describes the FCS-derived addon-list, addon-state, and `AtkValue` layouts together
with the semantic constants `Talk`, text index `0`, name index `1`, and allowed
`ManagedString` type `0x28`.
