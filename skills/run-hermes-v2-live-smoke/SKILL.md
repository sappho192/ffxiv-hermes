---
name: run-hermes-v2-live-smoke
description: Validate a merged Hermes v2 candidate against a running FFXIV client with Sharlayan.LiveSmoke before production promotion. Use for candidate manifest injection, CHATLOG attach and polling, CurrentTalk and LastTalk checks, BattleTalk visibility and sequencing, and collection of game-version, executable-hash, and verifier-commit evidence.
---

# Run Hermes v2 Live Smoke

Use the Sharlayan source verifier to load a candidate directly. Do not route pre-production validation through IronworksTranslator or the public latest endpoint.

## Preserve the trust boundary

Normal Sharlayan `RemotePreferred` reads `v2/latest.json` and rejects `validation.status=candidate`. IronworksTranslator uses that normal path. `tools/Sharlayan.LiveSmoke` is the intended exception: `--manifest` supplies local bytes through an internal override and permits a candidate only for live verification.

Do not:

- publish a candidate to make it testable;
- rewrite candidate validation metadata;
- point IronworksTranslator at a candidate;
- expose the candidate override as a normal user setting;
- claim that an attach or static scan proves Talk or BattleTalk semantics.

## Prepare exact inputs

1. Read `ffxiv-hermes/AGENTS.md` and the Sharlayan repository instructions.
2. Use the merged candidate bytes from current `ffxiv-hermes` `main`.
3. Verify the candidate with `Hermes.V2.Generator validate`.
4. Use a clean, committed Sharlayan checkout that contains the required parser, mapper, reader, and LiveSmoke support.
5. Require the verifier commit to exist on a remote ref so the run is reproducible.
6. Record the exact candidate revision, FCS commit, and full verifier SHA.
7. Require one running `ffxiv_dx11` process or pass `--process-id` explicitly when multiple clients exist.

Never preserve local install paths or process IDs in public evidence.

## Run attach validation

```powershell
$candidate = '<absolute-path-to-v2-candidate.json>'

dotnet run `
  --project D:\REPO\sharlayan\tools\Sharlayan.LiveSmoke\Sharlayan.LiveSmoke.csproj `
  --configuration Release -- `
  --manifest $candidate `
  --attach-only
```

Require:

- `source=Local`;
- expected candidate revision and FCS commit;
- `validation=candidate`;
- exactly one CHATLOG signature match;
- zero failed module reads;
- successful initialization and readable CHATLOG address;
- first poll emits no historical entries;
- `LIVE ATTACH PASS`.

## Validate standard Talk

Open a standard NPC Talk before running:

```powershell
dotnet run `
  --project D:\REPO\sharlayan\tools\Sharlayan.LiveSmoke\Sharlayan.LiveSmoke.csproj `
  --configuration Release -- `
  --manifest $candidate `
  --attach-only `
  --require-current-talk `
  --require-talk `
  --print-talk
```

Transiently compare speaker and text with the visible game UI. Advance to another line and confirm the current result changes immediately.

Close the Talk window, then run:

```powershell
dotnet run `
  --project D:\REPO\sharlayan\tools\Sharlayan.LiveSmoke\Sharlayan.LiveSmoke.csproj `
  --configuration Release -- `
  --manifest $candidate `
  --attach-only `
  --require-last-talk
```

Require the last committed Talk to remain available with `Source=Last` and `IsVisible=False`.

Use `--print-talk` only in a transient local diagnostic session. Never retain player names, NPC names, or raw dialogue in committed artifacts, shared logs, or public documentation. Retain match status, source, visibility, and lengths.

## Validate BattleTalk

When a BattleTalk is visible, run:

```powershell
dotnet run `
  --project D:\REPO\sharlayan\tools\Sharlayan.LiveSmoke\Sharlayan.LiveSmoke.csproj `
  --configuration Release -- `
  --manifest $candidate `
  --attach-only `
  --require-battle-talk `
  --print-talk
```

For a timing window, run:

```powershell
dotnet run `
  --project D:\REPO\sharlayan\tools\Sharlayan.LiveSmoke\Sharlayan.LiveSmoke.csproj `
  --configuration Release -- `
  --manifest $candidate `
  --attach-only `
  --battle-talk-sequence-seconds 120 `
  --battle-talk-sequence-interval 100
```

Trigger BattleTalk during the observation window. Require:

- visible appearance and disappearance;
- nondecreasing sequence;
- sequence or content fingerprint change when the displayed BattleTalk changes;
- name and text lengths or hashes corresponding to the UI transition;
- no reader exception or process exit.

The sequence probe reports observations but does not itself require a visible event. Do not call it a BattleTalk PASS unless a relevant transition was actually observed.

## Validate CHATLOG

Run without `--attach-only`:

```powershell
dotnet run `
  --project D:\REPO\sharlayan\tools\Sharlayan.LiveSmoke\Sharlayan.LiveSmoke.csproj `
  --configuration Release -- `
  --manifest $candidate `
  --poll-seconds 60 `
  --minimum-entries 1
```

Generate at least one new game chat entry during the interval. Require cursor progress, no historical first-poll output, no exception, and `LIVE SMOKE PASS`.

Even if CHATLOG offsets did not change, run this check because all resources are selected and applied as one revision.

## Collect promotion evidence

Read the game version from `ffxivgame.ver`, not the PE file version:

```powershell
$game = Get-Process ffxiv_dx11 | Select-Object -First 1
$exe = $game.MainModule.FileName
$versionFile = Join-Path (Split-Path -Parent $exe) 'ffxivgame.ver'

(Get-Content -LiteralPath $versionFile -Raw).Trim()
(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
git -C D:\REPO\sharlayan rev-parse HEAD
```

Require:

- game version format `yyyy.MM.dd.nnnn.nnnn`;
- lowercase 64-character executable SHA-256;
- full 40-character verifier commit present on a remote ref.

Record only:

- candidate revision and FCS commit;
- verifier commit;
- game version and executable SHA-256;
- each scenario's pass or fail;
- source, visibility, lengths, counts, and sequence behavior.

## Finish

Report each scenario independently. A partial run is not an overall PASS.

Stop before production workflow dispatch unless the user explicitly authorizes promotion. State that IronworksTranslator translation and overlay validation remains a post-publication check.
