# Hermes v2 구현 및 첫 production 배포 기록

## 문서 목적

이 문서는 `V2_IMPLEMENTATION_PLAN.md`를 출발점으로 이번 대화 세션에서 수행한 Hermes v2
구현, 실제 게임 진단, GitHub/R2 설정 및 첫 production 배포 결과를 정리한다. 세부 설계는
기존 계획 문서에 유지하고, 여기에는 실제로 결정되거나 검증된 사실과 다음 배포에서 재사용할
운영 정보를 기록한다.

## 최종 결과

- Hermes v2 generator, schema, fixture, CI, candidate 및 publish workflow를 구현했다.
- public base URL은 기존 도메인의 `https://hermes.sapphosound.com/v2/`로 확정했다.
- FCS `8ff04195c4e77ef0b85d15c6fd1c67785378f0fb` 기반 candidate를 실제 게임에서 검증했다.
- 화면에 현재 표시 중인 표준 NPC Talk를 나타내는 `currentTalk` 리소스를 v2 계약에 추가했다.
- live-verified production revision
  `sha256:419248bf2ef93aa64e72723ea9e97d5503163178dab63e90a8155b359ebcf96d`를
  R2와 저장소에 배포했다.
- Sharlayan.Lite embedded manifest를 같은 canonical byte로 동기화했다.
- Sharlayan.Lite `9.1.2`가 NuGet.org에 공개되어 Hermes v2의
  `minimumSharlayanVersion`과 일치한다.
- IronworksTranslator source는 이 세션에서 수정하지 않았고, 별도 작업용 handoff 문서만
  남기는 범위를 유지했다.

## 확정된 운영 결정

| 항목 | 결정 |
| --- | --- |
| minimum Sharlayan version | `9.1.2` |
| v2 public base | `https://hermes.sapphosound.com/v2/` |
| legacy endpoint | `/latest/address.json`을 무기한 유지 |
| live smoke | interactive game session과 GPU-capable Windows 환경이 필요하므로 수동 유지 |
| candidate PR 인증 | workflow가 참조하는 전용 repository credential 사용 |
| production environment | `main` |
| production protection | 현재 reviewer와 deployment branch 설정은 GitHub API로 확인 |
| manifest signing | 도입 시점 미정 |

## 구현한 v2 파이프라인

### Canonical manifest와 generator

- `tools/Hermes.V2.Generator`가 정확한 FCS commit과 generator commit을 입력으로 받는다.
- schema와 generator는 `chatLog`, 마지막 확정값인 `talk`, 현재 표시값인 `currentTalk`를
  하나의 manifest revision으로 생성한다.
- canonical JSON은 UTF-8, BOM 없음, LF, 정확히 하나의 trailing newline 및 고정 property
  순서를 사용한다.
- `resourceRevision`은 immutable manifest의 정확한 byte에 대한 SHA-256이며 manifest
  내부에는 포함하지 않는다.
- Windows working copy가 CRLF로 변환될 수 있으므로 production revision은 Git blob 또는
  canonical generator output으로만 계산한다.
- `minimumSharlayanVersion`은 `9.1.2`, `pointerResolverVersion`은 `1`이다.

### Candidate 자동화

`.github/workflows/fcs-v2-candidate.yml`은 다음 순서를 따른다.

1. FCS `main` HEAD를 한 번만 resolve한다.
2. detached commit에서 generator를 두 번 실행해 deterministic byte를 검증한다.
3. schema, generator test 및 diff를 검사한다.
4. `v2/candidates/<fcs-sha>.json`과 review summary PR을 만든다.
5. candidate PR에는 production storage credential을 제공하지 않는다.

Candidate PR 생성에는 workflow가 참조하는 전용 repository credential을 사용한다. 현재
credential 식별자와 등록 여부는 값을 출력하지 않는 아래 명령으로 확인한다.

```powershell
rg -n '\$\{\{\s*secrets\.' .github/workflows/fcs-v2-candidate.yml
gh secret list --repo sappho192/ffxiv-hermes
```

이 구성으로 candidate PR의 일반 `pull_request` CI가 정상적으로 실행된다.

### Production 배포

`.github/workflows/publish-v2.yml`은 `main` protected environment의 수동
`workflow_dispatch`만 사용한다.

1. candidate를 입력한 live verification metadata로 다시 생성한다.
2. final manifest와 revision을 검증한다.
3. immutable object를 새 R2 key에 올리고 read-back checksum을 검증한다.
4. mutable `v2/latest.json`을 마지막에 교체한다.
5. public endpoint가 같은 revision을 반환하는지 확인한다.
6. production manifest와 latest pointer를 Git에 기록한다.

Candidate JSON 자체를 production object로 승격하지 않으며, production generator가
`validation.status=live-verified`로 다시 생성한 byte만 배포한다.

## 현재 Talk와 LastTalk의 실제 의미

초기 live smoke에서 표준 NPC Talk를 여러 번 표시했지만 `LastTalkName`과 `LastTalkText`가
현재 화면의 다음 대사로 즉시 바뀌지 않았다. raw `Utf8String` header를 확인한 결과:

- `StringLength(+0x18)`은 실제 값이 있어도 `0`일 수 있다.
- 유효 byte 길이는 FCS 구현과 동일하게 `BufUsed - 1`이다.
- `LastTalkName`과 `LastTalkText`는 현재 창이 아니라 직전에 확정된 표준 Talk를 보존한다.

현재 표시 중인 Talk는 `RaptureAtkUnitManager.AllLoadedUnitsList`에서 활성 `Talk` addon을
찾아 읽을 수 있었다.

- `AtkValues[0]`: 본문
- `AtkValues[1]`: 화자
- 관측된 type: `ManagedString(0x28)`
- addon은 ready 및 visible 상태이고 최소 두 개의 값을 가져야 한다.

이 결과를 바탕으로 manifest에 `currentStandardTalk` 리소스를 추가했다. Sharlayan의 기본
정책은 `Current` 우선, 현재 addon이 없거나 일시적으로 읽을 수 없을 때 `Last` fallback이다.
실제 문자열은 일시적인 로컬 에이전트 진단에서만 화면과 대조하고 public 문서나 공유 로그에
보존하지 않는다. Public 증거에는 source, visibility, 길이 및 화면 일치 여부만 남긴다.

## 실제 게임 검증

검증에는 Sharlayan commit
`3e27261f82851e1e88c413a25461e6ca0ad551e8`을 사용했다.

- Framework/CHATLOG signature match: 정확히 1개
- module scan failed read: 0개
- current Talk의 name/text가 화면과 일치
- 다음 대사로 진행했을 때 current Talk가 새 값으로 전환
- Talk 종료 후 이전 값이 `Source=Last`, `IsVisible=False`로 보존
- 60초 CHATLOG polling 중 신규 entry와 cursor 진행 확인
- 종료 결과: `LIVE SMOKE PASS`

이 검증은 candidate를 production으로 승격하기 위한 수동 live smoke다. Interactive game
session과 GPU-capable Windows 환경이 필요하므로 GitHub-hosted runner에서 자동화하지 않는다.

## 첫 production 식별자

| 항목 | 값 |
| --- | --- |
| FCS commit | `8ff04195c4e77ef0b85d15c6fd1c67785378f0fb` |
| generator commit | `746414d55919677c79f6c3709f839ace556551aa` |
| verifier commit | `3e27261f82851e1e88c413a25461e6ca0ad551e8` |
| game version | `2026.06.18.0000.0000` |
| resource revision | `sha256:419248bf2ef93aa64e72723ea9e97d5503163178dab63e90a8155b359ebcf96d` |
| published at | `2026-07-25T13:34:54Z` |
| production record commit | `cd57f81` |

Public 확인 결과:

- `https://hermes.sapphosound.com/v2/latest.json`: HTTP 200
- immutable manifest: HTTP 200
- public manifest SHA-256:
  `419248bf2ef93aa64e72723ea9e97d5503163178dab63e90a8155b359ebcf96d`

## R2와 Cloudflare cache 정책

Origin이 설정한 `Cache-Control`을 Cloudflare가 그대로 존중하도록 Cache Rule을 설정했다.

- Edge TTL: **Use cache-control header if present, bypass cache if not**
- Browser TTL: **Respect origin TTL**

Object별 origin header:

- immutable manifest:
  `public,max-age=31536000,immutable`
- mutable latest pointer:
  `public,max-age=0,s-maxage=60,must-revalidate`

2026-07-25 public 응답에서도 위 의미와 같은 header를 확인했다. Cloudflare purge는 브라우저
cache를 제거하지 않으므로 latest에 긴 browser TTL을 설정하지 않는다.

`main` environment의 credential 식별자, reviewer 및 branch policy는 public 문서에 복사하지
않는다. 현재 구성을 값 노출 없이 확인하려면 다음 읽기 전용 명령을 사용한다.

```powershell
gh secret list --repo sappho192/ffxiv-hermes --env main
gh api repos/sappho192/ffxiv-hermes/environments/main
gh api repos/sappho192/ffxiv-hermes/environments/main/deployment-branch-policies
rg -n '\$\{\{\s*secrets\.' .github/workflows/publish-v2.yml
```

## 첫 publish에서 확인한 장애와 보완

GitHub Actions run
`https://github.com/sappho192/ffxiv-hermes/actions/runs/30159936235`에서 immutable
manifest upload/read-back과 latest pointer 교체는 성공했지만, 바로 이어진 첫 public endpoint
요청이 일시적인 HTTP 403을 반환했다. 당시 script의 `set -e` 때문에 기존 convergence loop에
도달하기 전에 job이 실패했고 Git production record 단계가 실행되지 않았다.

후속 조치:

- public endpoint 요청에 `curl --retry 12 --retry-all-errors --retry-delay 10`을 적용했다.
- 이미 배포된 public byte와 revision을 확인한 뒤 `cd57f81`로 production state를 기록했다.
- retry 보완 commit은 `2601b37`이다.

따라서 향후 publish 실패 시 job conclusion만 보고 object를 다시 덮어쓰지 않는다. R2
read-back, public latest, immutable object 및 Git production state를 각각 확인해 실제 실패
지점을 판별한다.

## Sharlayan.Lite 연계 결과

- Sharlayan.Lite embedded manifest는 위 production manifest의 canonical LF byte와 같다.
- package version `9.1.2`가 NuGet.org에 공개됐다.
- runtime provider는 remote → verified cache → embedded 순서로 동작한다.
- current Talk와 LastTalk fallback을 구분하는 API가 구현됐다.
- 자세한 release 및 NuGet Trusted Publishing 기록은 `sappho192/Sharlayan.Lite` 저장소의
  `docs/2026-07-25/2026-07-25-hermes-v2-and-nuget-release.md`를 참조한다.

## 남은 후속 작업

- manifest signing 도입 시점과 key 관리 정책 결정
- Sharlayan.Lite package에 README 포함
- 미충족 장시간 CHATLOG/wrap/명시적 channel fixture 검증을 추가 지인 테스트로 보강
- IronworksTranslator 별도 세션에서 Hermes/Sharlayan handoff를 바탕으로 consumer integration
  수행

Sharlayan.Lite `9.1.2` 공개는 지인 테스트를 시작하기 위한 사용자 승인 release waiver를
포함한다. 이 결정은 미충족 장시간 gate가 PASS였다는 의미가 아니며, 다음 릴리스에 자동으로
승계하지 않는다.

## 관련 문서

- `docs/V2_IMPLEMENTATION_PLAN.md`
- `docs/V2_GITHUB_AND_CACHE_SETUP.md`
- `docs/V2_LIVE_FINDINGS_AND_REQUIRED_CHANGES.md`
- `v2/README.md`
- `sappho192/Sharlayan.Lite`의
  `docs/2026-07-22/2026-07-22-07-hermes-v2-handoff.md`
