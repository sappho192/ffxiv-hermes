# Hermes v2 GitHub 및 cache 설정

작성일: 2026-07-23

## 확인된 현재 상태

R2 endpoint, bucket 이름 및 public v2 base URL은 workflow에 공개 설정값으로 고정하며
secret으로 취급하지 않는다.

Public 문서에는 credential 식별자, reviewer identity, self-review 및 administrator bypass
상태를 고정해 기록하지 않는다. 현재 설정은 값을 노출하지 않는 다음 읽기 전용 명령으로
확인한다.

```powershell
rg -n '\$\{\{\s*secrets\.' .github/workflows
gh secret list --repo sappho192/ffxiv-hermes
gh secret list --repo sappho192/ffxiv-hermes --env main
gh api repos/sappho192/ffxiv-hermes/environments/main
gh api repos/sappho192/ffxiv-hermes/environments/main/deployment-branch-policies
```

`gh secret list`는 등록된 식별자와 metadata만 표시하며 secret 값은 반환하지 않는다.

## Publication environment

Repository의 **Settings → Environments → main**에서 다음을 확인한다.

1. Deployment branches and tags에서 selected branch로 정확히 `main`을 추가한다.
2. Required reviewer가 설정되어 있지 않은지 확인한다.
3. Environment는 credential scope에만 사용하고 수동 승인 gate로 사용하지 않는다.

`fcs-v2-publish.yml`의 publish job과 `publish-v2.yml`은 `main` environment를 사용하며,
workflow 자체도 `main`의 현재 HEAD에서 실행된 경우만 허용한다. 외부 FCS를 build하고
manifest를 생성하는 job에는 environment나 production secret을 제공하지 않는다. 자동
publication 전에 `generated` 상태를 받아들일 수 있는 Sharlayan.Lite 9.2.1을 먼저 배포해야
한다.

## Repository write access

Repository의 **Settings → Actions → General → Workflow permissions**에서
`Read and write permissions`를 사용한다. 두 운영 workflow는 job 단위로
`contents: write`만 요청하며, 후보 branch나 PR 생성용 별도 token은 사용하지 않는다.

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

Sharlayan은 latest ETag를 사용하며 원격 장애 시 유효한 local cache와 embedded manifest로
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
