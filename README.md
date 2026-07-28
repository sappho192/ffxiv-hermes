# ffxiv-hermes

Hermes publishes FFXIV runtime metadata used by Sharlayan.Lite. The legacy
`latest/address.json` endpoint remains available, while v2 adds deterministic
FFXIVClientStructs-based manifests for CHATLOG and the last standard NPC Talk.

## v2 status

- `schemas/hermes-v2.schema.json` defines the manifest contract.
- `tools/Hermes.V2.Generator` extracts metadata from an exact FCS checkout.
- `v2/fixtures/manifest.valid.json` is the contract fixture used by
  Hermes and Sharlayan.Lite tests.
- `fcs-v2-publish.yml` checks FCS `main` every six hours, runs deterministic
  generation and static validation, and publishes changed runtime resources with
  `validation.status=generated`. The external FCS build job has no production
  credentials; a separate job revalidates its artifact before publication.
- `publish-v2.yml` rolls `v2/latest.json` back to the preceding or an explicitly
  selected immutable revision.

Generated metadata is transparent about its evidence: it carries no game version,
executable hash, or verifier commit. Live smoke remains available as an optional
diagnostic procedure, but it is not a publication gate.

Generated manifests require Sharlayan.Lite 9.2.1 or newer. Older clients reject the
new status and use their last valid cache or embedded manifest.

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
