---
name: verify-hermes-v2-production
description: Promote a live-verified Hermes v2 candidate through the protected publish workflow and verify production end to end. Use after explicit authorization to dispatch publish-v2, monitor the workflow, verify immutable R2 and public bytes, confirm latest and Git production state, diagnose partial failures, or perform post-publication IronworksTranslator smoke checks.
---

# Verify Hermes v2 Production

Promote only a merged candidate that has passed the dedicated live-game skill. Treat workflow, storage, CDN, Git, and application checks as separate evidence layers.

## Require explicit authority

Publishing changes production state. Before dispatch:

1. Require explicit user authorization to promote the exact FCS commit.
2. Confirm the candidate is merged into current remote `main`.
3. Confirm live smoke passed for CHATLOG, CurrentTalk, LastTalk, Talk policy, and BattleTalk as applicable.
4. Confirm the user-approved `game_version`, `executable_sha256`, and `verifier_commit`.
5. Do not infer authorization from a request to inspect, validate, prepare, or explain.

Use `--repo sappho192/ffxiv-hermes` on every `gh` command.

## Validate promotion inputs

Require:

- `fcs_commit`: lowercase 40-character SHA with `v2/candidates/<sha>.json` on remote `main`;
- `game_version`: value from `ffxivgame.ver`, not `ffxiv_dx11.exe` PE version;
- `executable_sha256`: lowercase 64-character SHA-256 of the tested executable;
- `verifier_commit`: exact 40-character Sharlayan commit used for live smoke, present on a remote ref;
- candidate revision independently validated from exact canonical bytes.

Fetch and inspect remote state without switching the user's branch:

```powershell
git fetch origin main
git show origin/main:v2/candidates/<fcs-commit>.json
git show origin/main:.github/workflows/publish-v2.yml
```

The selected workflow revision must be current `main`.

## Dispatch the protected workflow

After explicit authorization, run:

```powershell
gh workflow run publish-v2.yml `
  --repo sappho192/ffxiv-hermes `
  --ref main `
  -f mode=promote `
  -f fcs_commit=<fcs-commit> `
  -f game_version=<game-version> `
  -f executable_sha256=<executable-sha256> `
  -f verifier_commit=<verifier-commit>
```

Resolve the newly created run by workflow, event, creation time, and head SHA. Do not accidentally monitor an older run.

## Monitor every publish stage

Inspect job steps and logs. Require:

1. dispatch inputs validated;
2. exact FCS commit fetched and built;
3. generator tests passed;
4. live-verified manifest rebuilt from current `main`;
5. candidate and production `{roots,resources}` are structurally identical after normalized
   comparison;
6. final manifest schema and revision validated;
7. immutable object uploaded with `If-None-Match` protection or accepted only when existing bytes are identical;
8. R2 read-back bytes match the generated manifest;
9. `v2/latest.json` replaced only after immutable read-back;
10. public latest and immutable endpoint convergence checks passed with retries;
11. Git production manifest, latest pointer, and source state recorded on `main`.

Do not call a workflow success alone complete production proof.

## Verify public and Git state independently

Fetch the post-workflow `main` and resolve:

- production record commit;
- new resource revision;
- `v2/latest.json`;
- `v2/manifests/<revision-without-sha256-prefix>.json`;
- `v2/source/fcs-head.json`.

Fetch public objects with transient-error retries:

```powershell
curl.exe --fail --silent --show-error `
  --dump-header - `
  --retry 12 --retry-all-errors --retry-delay 10 `
  https://hermes.sapphosound.com/v2/latest.json
```

Require:

- public latest selects the new revision and exact FCS commit;
- public immutable URL uses the `sha256:` object key;
- public immutable SHA-256 equals the revision hex;
- public latest bytes equal the Git latest blob;
- public immutable bytes equal the Git manifest blob;
- manifest `validation.status` is `live-verified`;
- game version, executable hash, and verifier commit equal approved inputs;
- manifest runtime resources equal the candidate runtime resources;
- immutable cache control is `public,max-age=31536000,immutable`;
- latest cache control is `public,max-age=0,s-maxage=60,must-revalidate`.

Report timestamps in UTC and the user's local timezone when useful.

## Diagnose a failed publish safely

A failed final verification does not prove upload failure. Before retrying, inspect separately:

1. workflow-generated revision and manifest;
2. R2 immutable object existence and read-back bytes;
3. public immutable endpoint;
4. public latest pointer;
5. Git production manifest and latest record.

Never overwrite an existing immutable revision with different bytes. Never dispatch a blind retry until the actual partial state is known.

Do not print secret values, credential identifiers, reviewer identities, account IDs, or protected-environment internals. Metadata-only security inspection must follow repository `AGENTS.md`.

## Run post-publication application smoke

Restart IronworksTranslator because resource selection occurs at handler initialization. Confirm:

- `RemotePreferred` selects `ResourceSource.Remote`;
- selected revision and FCS commit equal production;
- cache latest and manifest are written;
- cached manifest SHA-256 equals the selected revision;
- standard Talk and BattleTalk translation reach the overlay in `Speaker: Text` form;
- CHATLOG still polls normally;
- no legacy `address.json` or manual hot reload is used.

Distinguish:

- workflow and byte verification;
- host application startup;
- automatic remote selection and cache behavior;
- actual live translation and overlay behavior.

Do not claim the last three from CI evidence.

## Report completion

Include:

- workflow run URL and conclusion;
- production revision and record commit;
- FCS, generator, verifier, game version, and executable hash;
- R2 read-back, public latest, immutable bytes, cache headers, and Git blob comparisons;
- IronworksTranslator restart and live-smoke status;
- any unverified or manually deferred layer.
