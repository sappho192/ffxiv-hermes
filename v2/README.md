# Hermes v2 artifacts

`manifests/<sha256-hex>.json` contains immutable, live-verified manifests in git. The
revision is `sha256:<lowercase hex>` of the exact UTF-8 manifest bytes; a manifest
does not contain its own revision. Canonical files use the generator's fixed property
order, two-space indentation, LF newlines, no UTF-8 BOM, and exactly one trailing
newline.

`latest.json` is mutable and is published only after its immutable manifest has been
uploaded and read back successfully. Candidate manifests belong in pull requests or
workflow artifacts and must not be referenced by `latest.json`.

The public R2 key retains the full revision (`v2/manifests/sha256:<hex>.json`). Git and
local cache filenames use only `<hex>.json` because `:` is not a valid Windows filename
character.

The public base URL is `https://hermes.sapphosound.com/v2/`. Relative to this base,
the mutable pointer is `latest.json` and immutable objects are
`manifests/sha256:<hex>.json`. The R2 object keys retain the leading `v2/` prefix.

Repository-only fixtures and FCS processing state are stored under `fixtures/` and
`source/`; they are not public R2 objects.

Generated pull requests keep unverified manifests and review summaries in
`candidates/`. A candidate becomes deployable only when the protected publish
workflow rebuilds it with `live-verified` metadata; candidate files are never
uploaded to R2.
