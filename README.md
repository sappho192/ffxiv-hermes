# Hermes v2 audit records

This orphan branch stores repository-only Hermes v2 generation records. It is
not a production source and none of its files are published to R2.

`v2/generated/<sha256-hex>.json` is an immutable canonical manifest. The
filename is the SHA-256 revision of the exact UTF-8 bytes. Existing files must
never be overwritten with different bytes.

`v2/source/fcs-state.json` records the latest successfully handled FCS commit,
its generated revision, and whether it changed production. The scheduled
workflow advances this state only after the audit record or production publish
has completed successfully.
