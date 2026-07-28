---
name: verify-hermes-v2-candidate
description: Validate an ffxiv-hermes v2 candidate PR or merged candidate without publishing it. Use for candidate generation provenance, PR and merge inspection, GitHub Actions checks, generator tests, canonical JSON byte validation, resource diffs, and confirmation that public production stayed unchanged.
---

# Verify Hermes v2 Candidate

Validate repository, CI, and public-state evidence while preserving the boundary between a candidate and live-verified production.

## Establish scope

1. Read the repository `AGENTS.md`.
2. Resolve the repository as `sappho192/ffxiv-hermes`, the PR number, candidate FCS commit, PR head SHA, and merge SHA.
3. Inspect `git status --short`, the current branch, `origin/main`, and unrelated user changes before doing anything else.
4. Stay read-only. Fetching remote refs is allowed; do not switch or fast-forward the user's branch merely to inspect a merged candidate.
5. Use `--repo sappho192/ffxiv-hermes` on every `gh` command.

## Inspect the candidate and merge

Fetch current refs and inspect the PR:

```powershell
git fetch origin main
gh pr view <pr> --repo sappho192/ffxiv-hermes `
  --json number,title,state,baseRefName,headRefName,headRefOid,mergeCommit,mergedAt,url,commits,files,statusCheckRollup
```

Require all of the following:

- base branch is `main`;
- PR is merged when validating a merged candidate;
- merge commit is contained in current `origin/main`;
- candidate JSON and summary use the same 40-character FCS commit;
- `v2/source/fcs-head.json` records that commit;
- candidate `validation.status` is `candidate`;
- candidate compatibility and resource semantics are compared with `AGENTS.md`, the current
  production manifest, and the authoritative workflows; report any discrepancy instead of silently
  accepting or rewriting it;
- no production manifest or `v2/latest.json` changed as part of a candidate-only PR.

Inspect the exact merge diff. Do not assume that every candidate PR changes exactly three files; explain any schema, generator, workflow, or fixture changes separately.

## Verify automation

Inspect three distinct layers:

1. Candidate generation workflow that resolved and checked out the exact FCS commit, built FCS, ran generator tests, generated twice, compared bytes, compared runtime resources, and created the PR.
2. PR checks for the candidate head SHA.
3. Push CI and CodeQL for the merge SHA on `main`.

Use `gh run list` and `gh run view` to inspect job steps, not only the aggregate conclusion. Treat skipped “already processed” paths according to their workflow conditions.

## Reproduce static validation

Validate the exact merged tree, preferably in a clean temporary snapshot or detached worktree. Do not use an older local `main`.

Run:

```powershell
dotnet test tests/Hermes.V2.Generator.Tests/Hermes.V2.Generator.Tests.csproj -c Release

dotnet run --project tools/Hermes.V2.Generator/Hermes.V2.Generator.csproj `
  -c Release --no-build -- validate `
  --manifest "v2/candidates/<fcs-commit>.json" `
  --schema schemas/hermes-v2.schema.json
```

Require:

- all generator tests pass;
- validator output equals the revision in the candidate summary;
- SHA-256 is computed from the exact canonical candidate bytes;
- every tracked JSON under `v2/` and `schemas/` matches its Git blob;
- no UTF-8 BOM or CR byte is present;
- each canonical JSON file has exactly one trailing LF.

Follow `.github/workflows/hermes-v2-ci.yml` as the authoritative canonical-byte check.

## Review resource changes

Compare candidate `{roots,resources}` against the manifest selected by `v2/latest.json`.

- Separate runtime layout changes from provenance and validation metadata.
- Confirm `chatLog`, `talk`, `currentTalk`, and `battleTalk` remain one atomic manifest revision.
- Treat offsets derived from FCS as static evidence only.
- Flag unexpected semantic constants, resource removal, compatibility-floor changes, or mismatched shared layouts.
- Never infer live Talk or BattleTalk support solely from static extraction or passing unit tests.

## Check public isolation

Read the public endpoint with retries:

```powershell
curl.exe --fail --silent --show-error --retry 5 --retry-all-errors `
  https://hermes.sapphosound.com/v2/latest.json
```

Require:

- public latest still selects the existing live-verified production revision;
- public latest bytes and immutable production bytes match the corresponding Git blobs;
- the candidate revision is not selected by public latest;
- before promotion, a candidate immutable URL normally returns 404 because candidate merge is not
  publication. If it returns 200, determine whether that revision was already promoted rather than
  assuming isolation failed.

Do not treat GitHub raw content as a production Hermes endpoint.

## Report

Lead with pass, fail, or incomplete. Include:

- PR, head SHA, merge SHA, and current `origin/main`;
- candidate FCS commit and candidate revision;
- changed runtime fields;
- candidate workflow, PR checks, post-merge CI, local test, validator, and canonical-byte results;
- public latest and candidate-publication isolation;
- the explicit statement that live game verification remains required.

Do not publish, promote, or modify `v2/latest.json` under this skill.
