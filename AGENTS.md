# Repository guidance

This file applies to the whole `ffxiv-hermes` repository.

## Repository and branches

- GitHub repository: `sappho192/ffxiv-hermes`.
- Default and production branch: `main`.
- When using `gh`, always pass `--repo sappho192/ffxiv-hermes`.
- Keep commits scoped. Do not stage generated, temporary, or unrelated user files.

## Repository skills

- For generated-manifest CI, canonical-byte, and publication checks, read and follow
  `skills/verify-hermes-v2-generated/SKILL.md`.
- For optional diagnostics against a running FFXIV client, read and follow
  `skills/run-hermes-v2-live-smoke/SKILL.md`.
- For production-state verification, partial-failure diagnosis, and rollback
  verification, read and follow `skills/verify-hermes-v2-production/SKILL.md`.

## Hermes v2 invariants

- The public v2 base is `https://hermes.sapphosound.com/v2/`.
- Public object keys are `v2/latest.json` and
  `v2/manifests/sha256:<lowercase-hex>.json`.
- `/latest/address.json` is a legacy endpoint with indefinite support. Do not remove or repurpose it.
- Scheduled publication creates `validation.status=generated` manifests with no live-verification
  metadata. Historic `live-verified` manifests remain valid immutable revisions.
- Generated audit records and processed-FCS state live only on the orphan
  `hermes-v2/audit` branch. When `{roots,resources}` are unchanged, advance that branch and leave
  `main`, the production environment, R2, and public verification untouched.
- External FCS build and generation must run in a job without production environments or secrets.
  A separate publication job revalidates the artifact before receiving R2 credentials.
- Upload and read back the immutable manifest before replacing `v2/latest.json`.
- Never overwrite an existing immutable revision with different bytes.
- `resourceRevision` is SHA-256 of the exact canonical manifest bytes. It is not stored inside the
  immutable manifest.
- Canonical JSON must be UTF-8 without BOM, LF-only, exactly one trailing newline, and use the
  generator's fixed property order. Never use a CRLF working-copy hash as production evidence.
- Git/local filenames omit the `sha256:` prefix because `:` is invalid in Windows filenames. R2
  object keys retain the full revision.
- The compatibility floor for generated manifests is Sharlayan.Lite `9.2.1`.

## Runtime resource semantics

- `chatLog`, `talk`, and `currentTalk` must come from one manifest revision and be applied atomically.
- `talk` means the last committed standard Talk value; it does not prove that the Talk addon is
  currently visible.
- LastTalk UTF-8 length is `BufUsed - 1`. `StringLength(+0x18)` may be zero for valid live values.
- `currentTalk` means the visible `Talk` addon. The observed contract is text at `AtkValues[0]`,
  speaker at `AtkValues[1]`, with allowed `ManagedString(0x28)` values.
- FCS-derived layouts and semantic constants must be regenerated twice and statically validated
  before automatic publication. Live-game verification is optional diagnostic evidence.

## Validation

Run the generator tests:

```powershell
dotnet test tests/Hermes.V2.Generator.Tests/Hermes.V2.Generator.Tests.csproj -c Release
```

For changes under `v2/` or `schemas/`, also verify that canonical JSON working bytes equal the Git
blob bytes. `.github/workflows/hermes-v2-ci.yml` contains the authoritative check.

Live smoke remains manual because it needs an interactive game session and a GPU-capable Windows
instance. A static build or generated publication must not be described as live evidence.

## GitHub and release operations

- Scheduled publication workflow: `.github/workflows/fcs-v2-publish.yml`.
- Rollback workflow: `.github/workflows/publish-v2.yml`.
- Secret-scoping environment: `main`; it must not require manual approval.
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
- On a failed publish job, inspect R2 read-back, public latest, immutable object, Git production
  state, and the audit branch separately before retrying. Audit state advances only after success;
  the next schedule regenerates or reconciles the Git-recorded target as appropriate. A failed
  final verification does not prove the upload failed.

## Current production baseline

- FCS commit: `81b94d8729306e5e43635811d42bac18f58993bb`.
- Verifier commit: `226f6c6e9c71dac6f6814d3348291f0200244afb`.
- Resource revision:
  `sha256:3867f77f8deefb72998c1eb1ba03459df67817bc9f65d1370f90235787cb18b5`.
- This baseline remains live-verified; the first generated revision is published only after
  Sharlayan.Lite 9.2.1 is available to consumers.
- Manifest signing is undecided. Do not introduce a signing format without an explicit design
  decision and coordinated Sharlayan parser change.
