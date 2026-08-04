---
name: verify-hermes-v2-production
description: Verify Hermes v2 production after scheduled or manual generated publication, diagnose partial failures, or roll latest back to a recorded immutable revision. Use for workflow-run inspection, R2 and public byte checks, Git production-state comparison, rollback-previous or rollback-revision operations, and optional post-publication IronworksTranslator smoke checks.
---

# Verify Hermes v2 Production

Treat workflow, R2, CDN, Git, and application behavior as separate evidence layers.

## Inspect publication

Use `--repo sappho192/ffxiv-hermes` on every `gh` command. Resolve the exact workflow run, head SHA,
FCS SHA, target revision, and Git production-record commit.

For `.github/workflows/fcs-v2-publish.yml`, require:

1. exact FCS checkout and successful build;
2. generator tests and byte-identical double generation;
3. `generated` validation with minimum Sharlayan 9.2.1 and no live metadata;
4. immutable upload/read-back before any latest update;
5. Git target recorded before R2 latest replacement;
6. public convergence checks pass with retries.

Workflow success alone is not complete production proof.

## Verify Git, R2, and public state

Fetch current `origin/main` and `origin/hermes-v2/audit`. Resolve `v2/latest.json`, its Git
manifest, and audit `v2/source/fcs-state.json`. Fetch public latest and immutable objects with
retries.

Require:

- public latest bytes equal the Git latest blob;
- public immutable bytes equal the Git manifest blob;
- immutable SHA-256 equals the revision hex;
- latest and manifest FCS commits agree;
- validation is `generated` or historic `live-verified`;
- generated manifests contain no live metadata and require Sharlayan 9.2.1 or newer;
- immutable cache control is `public,max-age=31536000,immutable`;
- latest cache control is `public,max-age=0,s-maxage=60,must-revalidate`.

## Diagnose partial failures

Inspect separately:

1. generated target and revision from workflow artifacts or the audit branch;
2. immutable R2 object and read-back bytes;
3. Git latest and manifest;
4. direct R2 latest;
5. public CDN latest and immutable bytes.

Never overwrite an immutable revision with different bytes. Audit state advances only after
successful archival or publication. A rerun regenerates work that failed before the Git record or
reconciles a Git-recorded target that failed afterward. Do not assume a failed CDN check means
upload failed.

## Roll back

Rollback changes production state and requires explicit user authorization for the operation.

Dispatch the current `main` workflow:

```powershell
gh workflow run publish-v2.yml `
  --repo sappho192/ffxiv-hermes `
  --ref main `
  -f mode=rollback-previous
```

Or select a recorded immutable revision:

```powershell
gh workflow run publish-v2.yml `
  --repo sappho192/ffxiv-hermes `
  --ref main `
  -f mode=rollback-revision `
  -f rollback_revision=sha256:<hex>
```

Require the target manifest to exist in Git and R2 with identical bytes. Confirm the workflow
records the rollback latest in Git before replacing R2 latest. Audit processed state intentionally
remains unchanged so the next schedule does not immediately republish the rolled-back FCS.

## Optional application smoke

Restart IronworksTranslator because resource selection occurs at handler initialization. Confirm
`RemotePreferred` source, revision, FCS commit, cache write, CHATLOG polling, and relevant Talk
translation. Do not claim application or live-game behavior from workflow evidence.

## Report

Include run URL, target revision, record commit, FCS and generator SHAs, validation status, R2
read-back, public/Git byte comparison, cache headers, and any application checks actually performed.
