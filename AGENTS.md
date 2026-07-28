# Repository guidance

This file applies to the whole `ffxiv-hermes` repository.

## Repository and branches

- GitHub repository: `sappho192/ffxiv-hermes`.
- Default and production branch: `main`.
- When using `gh`, always pass `--repo sappho192/ffxiv-hermes`.
- Keep commits scoped. Do not stage generated, temporary, or unrelated user files.

## Repository skills

- For candidate PR, CI, canonical-byte, and public-isolation checks, read and follow
  `skills/verify-hermes-v2-candidate/SKILL.md`.
- For pre-production testing against a running FFXIV client, read and follow
  `skills/run-hermes-v2-live-smoke/SKILL.md`.
- For an explicitly authorized production promotion, partial-failure diagnosis, and public
  verification, read and follow `skills/verify-hermes-v2-production/SKILL.md`.

## Hermes v2 invariants

- The public v2 base is `https://hermes.sapphosound.com/v2/`.
- Public object keys are `v2/latest.json` and
  `v2/manifests/sha256:<lowercase-hex>.json`.
- `/latest/address.json` is a legacy endpoint with indefinite support. Do not remove or repurpose it.
- A candidate is not production. Only the protected publish workflow may create a
  `validation.status=live-verified` production manifest.
- Upload and read back the immutable manifest before replacing `v2/latest.json`.
- Never overwrite an existing immutable revision with different bytes.
- `resourceRevision` is SHA-256 of the exact canonical manifest bytes. It is not stored inside the
  immutable manifest.
- Canonical JSON must be UTF-8 without BOM, LF-only, exactly one trailing newline, and use the
  generator's fixed property order. Never use a CRLF working-copy hash as production evidence.
- Git/local filenames omit the `sha256:` prefix because `:` is invalid in Windows filenames. R2
  object keys retain the full revision.
- The current compatibility floor is Sharlayan.Lite `9.1.2`.

## Runtime resource semantics

- `chatLog`, `talk`, and `currentTalk` must come from one manifest revision and be applied atomically.
- `talk` means the last committed standard Talk value; it does not prove that the Talk addon is
  currently visible.
- LastTalk UTF-8 length is `BufUsed - 1`. `StringLength(+0x18)` may be zero for valid live values.
- `currentTalk` means the visible `Talk` addon. The observed contract is text at `AtkValues[0]`,
  speaker at `AtkValues[1]`, with allowed `ManagedString(0x28)` values.
- FCS-derived layouts and semantic constants must be regenerated, statically validated, and then
  verified against the live game before production promotion.

## Validation

Run the generator tests:

```powershell
dotnet test tests/Hermes.V2.Generator.Tests/Hermes.V2.Generator.Tests.csproj -c Release
```

For changes under `v2/` or `schemas/`, also verify that canonical JSON working bytes equal the Git
blob bytes. `.github/workflows/hermes-v2-ci.yml` contains the authoritative check.

Live smoke remains manual because it needs an interactive game session and a GPU-capable Windows
instance. A static build or candidate CI pass is not live evidence.

## GitHub and release operations

- Candidate workflow: `.github/workflows/fcs-v2-candidate.yml`.
- Production workflow: `.github/workflows/publish-v2.yml`.
- Protected environment: `main`.
- Do not copy credential identifiers, reviewer identities, account IDs, local user paths, process
  IDs, player names, user-generated chat, NPC names, or raw NPC dialogue into public documentation.
  Exact Talk strings may be viewed transiently in a local agent diagnostic session, but retain only
  match status, source, visibility, and lengths in committed artifacts.
- Inspect current security-sensitive configuration with read-only commands instead:

```powershell
rg -n '\$\{\{\s*secrets\.' .github/workflows
gh secret list --repo sappho192/ffxiv-hermes
gh secret list --repo sappho192/ffxiv-hermes --env main
gh api repos/sappho192/ffxiv-hermes/environments/main
gh api repos/sappho192/ffxiv-hermes/environments/main/deployment-branch-policies
```

- These commands show configuration metadata, not secret values. Never attempt to print secret
  values.
- Immutable cache control: `public,max-age=31536000,immutable`.
- Latest cache control: `public,max-age=0,s-maxage=60,must-revalidate`.
- Cloudflare Edge TTL uses origin Cache-Control and bypasses when absent; Browser TTL respects
  origin TTL.
- Public endpoint checks must tolerate transient CDN errors. Do not remove the retry behavior added
  after the first production request returned a transient 403.
- On a failed publish job, inspect R2 read-back, public latest, immutable object, and Git production
  state separately before retrying. A failed final verification does not prove the upload failed.

## Current production baseline

- FCS commit: `8ff04195c4e77ef0b85d15c6fd1c67785378f0fb`.
- Verifier commit: `3e27261f82851e1e88c413a25461e6ca0ad551e8`.
- Resource revision:
  `sha256:419248bf2ef93aa64e72723ea9e97d5503163178dab63e90a8155b359ebcf96d`.
- See `docs/2026-07-25/2026-07-25-v2-release-session.md` before the next promotion.
- Manifest signing is undecided. Do not introduce a signing format without an explicit design
  decision and coordinated Sharlayan parser change.
