# ffxiv-hermes

Hermes publishes FFXIV runtime metadata used by Sharlayan.Lite. The legacy
`latest/address.json` endpoint remains available, while v2 adds deterministic
FFXIVClientStructs-based manifests for CHATLOG and the last standard NPC Talk.

## v2 status

- `schemas/hermes-v2.schema.json` defines the manifest contract.
- `tools/Hermes.V2.Generator` extracts metadata from an exact FCS checkout.
- `v2/fixtures/manifest.valid.json` is the candidate contract fixture used by
  Hermes and Sharlayan.Lite tests.
- `fcs-v2-candidate.yml` checks FCS `main` every six hours and opens a candidate
  pull request only when runtime resources change.
- `publish-v2.yml` rebuilds a live-verified manifest in the protected `main`
  environment, uploads the immutable object, and updates
  `v2/latest.json` last. It also supports rollback to an existing revision.

Candidate metadata is never production metadata. Promotion requires game version,
executable SHA-256, and verifier commit values obtained from a Sharlayan live smoke
run.

The initial v2 contract requires Sharlayan.Lite 9.1.2. Live smoke remains a manual
Windows/GPU procedure because the game login requires two-factor authentication.

The v2 public base is `https://hermes.sapphosound.com/v2/`. Public objects are
therefore served as `latest.json` and `manifests/sha256:<hex>.json` below that base;
the legacy endpoint remains outside it at `/latest/address.json`.

## Local verification

```powershell
dotnet test Hermes.V2.slnx -c Release
dotnet run --project tools/Hermes.V2.Generator -c Release -- validate `
  --manifest v2/fixtures/manifest.valid.json `
  --schema schemas/hermes-v2.schema.json
```

See [`v2/README.md`](v2/README.md) for canonical byte and object-key rules, and
[`V2_IMPLEMENTATION_PLAN.md`](docs/V2_IMPLEMENTATION_PLAN.md) for the cross-repository
architecture and rollout procedure. Repository and Cloudflare setup is documented in
[`V2_GITHUB_AND_CACHE_SETUP.md`](docs/V2_GITHUB_AND_CACHE_SETUP.md).

To contribute, open an issue.
