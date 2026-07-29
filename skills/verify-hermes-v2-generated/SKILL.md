---
name: verify-hermes-v2-generated
description: Validate Hermes v2 generated-manifest automation without changing production. Use for fcs-v2-publish workflow reviews, generator provenance, deterministic output, schema and canonical-byte checks, runtime resource diffs, compatibility-floor validation, and proof that a proposed workflow change leaves the public pointer unchanged.
---

# Verify Hermes v2 Generated Publication

Review static evidence without dispatching a workflow or changing R2.

## Establish scope

1. Read repository `AGENTS.md`.
2. Use `--repo sappho192/ffxiv-hermes` on every `gh` command.
3. Inspect `git status --short --branch`, exact base/head SHAs, and unrelated changes.
4. Treat `generated` as production-eligible static evidence, not live-game evidence.

## Review the contract

Require:

- `validation.status` is `generated`;
- validation contains no game version, executable hash, or verifier commit;
- `compatibility.minimumSharlayanVersion` is 9.2.1 or newer;
- `chatLog`, `talk`, `currentTalk`, and optional `battleTalk` form one atomic revision;
- BattleTalk requires Sharlayan 9.2.0 or newer;
- canonical JSON is UTF-8 without BOM, LF-only, and has one trailing newline.

Run:

```powershell
dotnet test tests/Hermes.V2.Generator.Tests/Hermes.V2.Generator.Tests.csproj -c Release
```

Follow `.github/workflows/hermes-v2-ci.yml` for Git blob versus working-byte checks.

## Review the workflow

Inspect `.github/workflows/fcs-v2-publish.yml` and require:

1. schedule and manual runs resolve one exact FCS SHA;
2. the external FCS build job has read-only contents permission and no environment or secrets;
3. current `main` is checked before generation and again before Git push;
4. FCS builds and generator tests pass;
5. generation runs twice and canonical bytes match;
6. a separate credentialed job revalidates revision, provenance, status, and resource diff;
7. only `{roots,resources}` changes create a new revision;
8. immutable R2 bytes are uploaded or accepted only when read-back is identical;
9. Git records manifest, latest, and FCS head before R2 latest changes;
10. reruns reconcile R2 latest to the Git-recorded target;
11. public latest and immutable bytes are verified with transient-error retries;
12. unchanged FCS resources preserve the canonical generated manifest under `v2/generated/`,
    advance `v2/source/fcs-head.json`, and leave production latest unchanged.

Confirm the job uses the `main` environment for secret scope, not manual approval.

## Review resource changes

Compare generated `{roots,resources}` with the manifest selected by `v2/latest.json`.
Separate runtime layout changes from provenance and validation metadata. Flag resource removal,
semantic-constant changes, compatibility regressions, or mismatched shared layouts.

Never infer live Talk or BattleTalk behavior from FCS extraction or unit tests.

## Report

Lead with pass, fail, or incomplete. Include exact SHAs, deterministic-generation result, changed
runtime fields, tests, canonical-byte result, workflow ordering, and compatibility behavior. State
explicitly that no live-game evidence was collected.
