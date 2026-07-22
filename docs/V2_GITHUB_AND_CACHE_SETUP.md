# Hermes v2 GitHub 및 cache 설정

작성일: 2026-07-23

## 확인된 현재 상태

GitHub API로 `sappho192/ffxiv-hermes`를 확인한 결과 `main` environment에 다음
environment secret 네 개가 존재한다. 값은 GitHub에서 조회할 수 없으며 확인하지 않았다.

- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_DEFAULT_REGION`
- `AWS_DEFAULT_OUTPUT`

R2 endpoint, bucket 이름 및 public v2 base URL은 workflow에 공개 설정값으로 고정하며
secret으로 취급하지 않는다.

Repository Actions secret `HERMES_CANDIDATE_TOKEN`은 2026-07-23에 설정된 것을 GitHub
API에서 이름으로 확인했다. Secret 값은 조회하지 않았다.

같은 날 `main` environment의 required reviewer가 `sappho192`로 설정되고
`prevent_self_review=false`인 것과, deployment branch policy에 정확한 `main` branch가
등록된 것을 GitHub API로 확인했다. Administrator bypass는 현재 허용 상태다.

## Production environment

Repository의 **Settings → Environments → main**에서 다음을 설정한다.

1. Deployment branches and tags에서 selected branch로 정확히 `main`을 추가한다.
2. Required reviewers에 `sappho192`를 추가한다.
3. 유일한 승인자가 workflow 실행자와 같으므로 **Prevent self-review**는 끈다.
4. 승인 절차를 실제 gate로 강제하려면 동작 확인 후 administrator bypass를 끈다.

`publish-v2.yml`은 `main` environment를 사용하며, workflow 자체도 `main`의 현재 HEAD에서
dispatch된 실행만 허용한다. Environment approval 전에는 R2 secret이 job에 제공되지 않는다.
최초 production 승격 전에 manifest를 받아들일 수 있는 Sharlayan.Lite 9.1.2를 먼저 배포하고,
그 버전의 embedded manifest도 동일 계약으로 갱신해야 한다.

## Candidate PR token

Candidate job에는 production environment를 연결하지 않는다. Candidate branch push와 PR 생성용
fine-grained personal access token을 다음 최소 범위로 만든다.

- Resource owner: `sappho192`
- Repository access: **Only select repositories** → `ffxiv-hermes`
- Repository permissions:
  - **Contents: Read and write**
  - **Pull requests: Read and write**
  - Metadata는 GitHub가 요구하는 read 권한만 사용
- Expiration: 운영 가능한 짧은 주기(권장 90일)로 설정하고 만료 전에 교체

Repository의 **Settings → Secrets and variables → Actions → New repository secret**에서
토큰을 `HERMES_CANDIDATE_TOKEN`이라는 repository secret으로 저장한다. `main` environment
secret으로 저장하면 environment를 사용하지 않는 candidate job에서 읽을 수 없으므로 안 된다.

CLI로 설정할 때는 토큰을 명령행 인자로 남기지 않고 다음 명령이 표시하는 비공개 입력에 붙여
넣는다.

```powershell
gh secret set HERMES_CANDIDATE_TOKEN --repo sappho192/ffxiv-hermes
```

Workflow의 checkout, branch push 및 `gh pr create`가 모두 이 PAT를 사용한다. PAT를 사용하면
자동 생성 PR의 `pull_request` workflow가 `GITHUB_TOKEN` 생성 PR처럼 별도 승인 대기 상태가
되는 것을 피할 수 있다.

## Cache policy

Immutable manifest는 content-addressed이고 절대 덮어쓰지 않으므로 다음 값을 사용한다.

```text
Cache-Control: public,max-age=31536000,immutable
```

Mutable latest pointer는 client cache에서는 즉시 stale로 만들고 Cloudflare shared cache에서만
60초 동안 유지한다. 만료 후에는 stale 응답을 제공하지 않고 재검증한다.

```text
Cache-Control: public,max-age=0,s-maxage=60,must-revalidate
```

Sharlayan은 latest ETag를 사용하며 원격 장애 시 검증된 local cache와 embedded manifest로
fallback하므로 stale latest를 CDN에서 별도로 제공하지 않는다. Publish workflow는 CDN의 기존
latest가 최대 60초 남아 있을 수 있음을 고려해 public 검증을 최대 120초 동안 재시도한다.

Cloudflare는 JSON을 기본 cache 대상에 포함하지 않을 수 있으므로 custom domain의 Cache Rule도
필요하다.

1. `hermes.sapphosound.com` zone의 **Caching → Cache Rules**에서 rule을 만든다.
2. Host가 `hermes.sapphosound.com`이고 URI path가 `/v2/`로 시작할 때만 적용한다.
3. Cache eligibility를 **Eligible for cache**로 설정한다.
4. Edge TTL과 Browser TTL은 origin의 `Cache-Control`을 존중하도록 설정한다.
5. 다른 Edge TTL override가 이 rule보다 뒤에서 적용되지 않는지 확인한다.

Legacy `/latest/address.json`에는 이 v2 rule을 적용하지 않는다. Legacy endpoint는 별도의 종료
일정 없이 무기한 유지한다.

위 `/v2/` Cache Rule은 2026-07-23에 설정 완료되었다. Edge TTL은 origin에
`Cache-Control`이 있을 때 이를 사용하고 없으면 bypass하며, Browser TTL은 origin TTL을
존중한다.

## Manifest signing

서명 도입 시점은 정하지 않는다. R2/CDN 계정 침해까지 방어해야 하거나 배포 주체를 client에서
암호학적으로 확인해야 하는 요구가 생기면 key rotation과 신뢰 root 배포를 포함한 별도 v3
설계로 재검토한다.
