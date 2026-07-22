# ffxiv-hermes v2 구현 계획

## 1. 목적

`ffxiv-hermes`를 수동으로 찾은 단일 NPC 대화 주소의 배포 저장소에서 FFXIVClientStructs(FCS) 기반 런타임 메타데이터의 생성, 검증, 버전 관리 및 배포 저장소로 확장한다.

v2의 우선 목표는 다음과 같다.

- FCS에서 `CHATLOG` 시그니처와 구조 오프셋을 생성한다.
- FCS에서 표준 NPC `Talk`의 이름과 대사 위치를 생성한다.
- 게임 주소 데이터만 바뀐 경우 Sharlayan.Lite와 IronworksTranslator를 다시 배포하지 않아도 대응할 수 있게 한다.
- 사용한 FCS 커밋과 생성기 커밋을 모든 산출물에 기록하여 재현 가능하게 한다.
- 자동 생성 결과와 실제 게임에서 검증된 배포 결과를 구분한다.
- 기존 `latest/address.json`과 기존 소비자를 당분간 유지한다.

## 2. 범위

### 포함

- `ffxiv-hermes`의 `v2/` 배포 규격
- FCS HEAD 주기 확인과 변경 감지
- FCS 메타데이터 추출기
- immutable manifest 및 mutable latest pointer 배포
- Sharlayan.Lite의 원격 manifest, 로컬 캐시 및 embedded fallback 지원
- Sharlayan.Lite의 표준 NPC 대화 읽기 API
- IronworksTranslator의 고수준 Sharlayan API 사용 전환
- 정적 검증, 실제 게임 smoke test, 승인 및 rollback 절차

### 초기 범위에서 제외

- 모든 종류의 NPC 발화를 하나의 API로 통합
- BattleTalk의 현재 표시 문자열
- `TalkSubtitle`, `NpcYell`, 말풍선 및 `_MiniTalk`
- FCS의 모든 구조를 범용 직렬화하는 기능
- 원격 manifest만을 유일한 fallback으로 사용하는 구조
- FCS의 새 커밋을 검증 없이 즉시 production으로 승격하는 기능

초기 v2는 지속 채팅 로그와 표준 `Talk`의 마지막 이름 및 대사만 지원한다. 다른 발화 유형은 v2의 선택적 리소스로 점진적으로 추가한다.

## 저장소별 실행 계획

이 문서는 세 저장소의 상위 아키텍처와 Hermes 계약을 정의한다. 저장소별 구현 작업은 다음 문서를 따른다.

```text
D:\REPO\sharlayan\docs\2026-07-22\2026-07-22-06-hermes-v2-runtime-plan.md
D:\REPO\IronworksTranslator\HERMES_V2_MIGRATION_PLAN.md
```

저장소별 문서는 이 문서의 schema를 복제해 독립적으로 변경하지 않는다. 계약 변경은 Hermes schema, fixture, Sharlayan consumer, IronworksTranslator integration 순서로 반영한다.

## 3. 저장소별 책임

### FFXIVClientStructs

- 게임 구조, 정적 시그니처 및 필드 오프셋의 원본이다.
- Hermes는 정확한 FCS 커밋 SHA를 입력으로 사용한다.
- Hermes 또는 Sharlayan이 FCS의 이동 중인 브랜치를 런타임에 직접 참조하지 않는다.

### ffxiv-hermes

- FCS 새 커밋을 감지한다.
- FCS 메타데이터를 Hermes v2 manifest로 정규화한다.
- 생성 결과를 정적으로 검증하고 candidate를 만든다.
- 승인된 immutable manifest와 `v2/latest.json`을 R2에 배포한다.
- 배포 이력과 rollback 가능한 immutable 산출물을 보존한다.
- 게임 프로세스 메모리를 직접 읽는 런타임 코드는 포함하지 않는다.

### Sharlayan.Lite

- Hermes v2 스키마를 검증하고 해석한다.
- 원격, 로컬 캐시, embedded manifest 순으로 리소스를 선택한다.
- 시그니처 검색과 포인터 해석을 담당한다.
- CHATLOG 구조를 설정하고 기존 채팅 API를 제공한다.
- `LastTalkName`과 `LastTalkText`를 안전하게 읽는 고수준 API를 제공한다.
- Hermes 스키마와 현재 라이브러리의 호환성을 검사한다.

### IronworksTranslator

- Hermes JSON이나 Sharlayan `Signature`를 직접 구성하지 않는다.
- Sharlayan의 CHATLOG 및 NPC 대화 API만 사용한다.
- 번역 큐, 중복 억제 및 화면 표시와 같은 애플리케이션 동작만 담당한다.
- 원격 manifest 장애를 애플리케이션 시작 실패로 취급하지 않는다.

## 4. 디렉터리 및 배포 구조

기존 파일과 배포 경로는 그대로 유지한다. 새 배포 산출물은 `v2/` 아래에 추가하고 generator, schema 및 workflow처럼 배포되지 않는 지원 파일만 기존 저장소 관례에 맞는 디렉터리에 추가한다.

```text
ffxiv-hermes/
  latest/
    address.json                 # 기존 소비자용, 당분간 유지
  v2/
    latest.json                  # 현재 production manifest를 가리키는 포인터
    manifests/
      <sha256-hex>.json          # immutable 검증 완료 manifest (Windows-safe filename)
    fixtures/
      manifest.valid.json        # 스키마 및 소비자 테스트 fixture
    source/
      fcs-head.json              # workflow가 마지막으로 처리한 FCS HEAD
  tools/
    Hermes.V2.Generator/
  schemas/
    hermes-v2.schema.json
  .github/
    workflows/
      main.yml                   # 기존 legacy 배포
      fcs-v2-candidate.yml       # FCS 확인 및 candidate 생성
      publish-v2.yml             # 승인된 v2 배포 및 rollback
```

R2에는 다음 public object만 배포한다.

```text
/latest/address.json
/v2/latest.json
/v2/manifests/<resource-revision>.json
```

v2 public base URL은 `https://hermes.sapphosound.com/v2/`이다. 따라서 consumer와
배포 후 검증 단계는 이 base를 기준으로 `latest.json` 및
`manifests/<resource-revision>.json`을 해석한다. R2 object key에는 계속 선행
`v2/` prefix를 포함한다.

R2 object key에는 전체 `sha256:<hex>` revision을 사용한다. Git working tree와 Windows
로컬 cache에서는 콜론이 파일명에 허용되지 않으므로 `<hex>.json`만 사용한다.

`v2/fixtures`, `v2/source`, generator 및 schema는 저장소 관리용이며 R2에 배포하지 않는다.

## 5. v2 배포 모델

### Immutable manifest

검증된 리소스 전체를 Git에서는 `v2/manifests/<sha256-hex>.json`, R2에서는 `v2/manifests/<resource-revision>.json`에 저장한다. 한 번 배포된 revision의 내용은 수정하거나 덮어쓰지 않는다.

`resourceRevision`은 generator가 만든 immutable manifest의 정확한 UTF-8 byte에 대한 SHA-256으로 계산한다. immutable manifest 안에는 자신의 `resourceRevision`을 넣지 않는다. 생성 시간처럼 매 실행마다 달라지는 값도 immutable manifest에 넣지 않아 동일 입력이 항상 동일한 byte와 revision을 만들게 한다.

### Latest pointer

`v2/latest.json`은 현재 production revision만 가리키는 작은 mutable 문서이다.

```json
{
  "schemaVersion": 2,
  "resourceRevision": "sha256:<hex>",
  "manifest": "manifests/<resource-revision>.json",
  "fcsCommit": "<40-character-sha>",
  "publishedAt": "<utc-timestamp>"
}
```

클라이언트는 `latest.json`을 받은 뒤 immutable manifest를 요청한다. 캐시된 revision과 같으면 기존 manifest를 그대로 사용한다.

### 배포 순서

1. immutable manifest를 새 key로 업로드한다.
2. 업로드한 object를 다시 읽고 SHA-256을 검증한다.
3. `v2/latest.json`을 마지막에 교체한다.
4. 배포 후 public endpoint에서 latest와 manifest를 다시 검증한다.

이 순서로 latest가 아직 존재하지 않는 manifest를 가리키는 상태를 방지한다.

## 6. Manifest 스키마 초안

v2 manifest는 FCS의 C# 타입을 그대로 노출하지 않고 Sharlayan이 필요한 의미 있는 리소스로 정규화한다.

```json
{
  "schemaVersion": 2,
  "compatibility": {
    "minimumSharlayanVersion": "<semver>",
    "pointerResolverVersion": 1
  },
  "source": {
    "fcsRepository": "https://github.com/aers/FFXIVClientStructs.git",
    "fcsCommit": "<40-character-sha>",
    "generatorRepository": "https://github.com/sappho192/ffxiv-hermes.git",
    "generatorCommit": "<40-character-sha>"
  },
  "platform": {
    "process": "ffxiv_dx11.exe",
    "architecture": "x64"
  },
  "roots": {
    "framework": {
      "pattern": "488B1D????????8B7C24",
      "relativeFollowOffset": 3,
      "isPointer": true
    }
  },
  "resources": {
    "chatLog": {
      "root": "framework",
      "uiModuleOffset": 11112,
      "raptureLogModuleOffset": 6848,
      "indexVectorOffset": 72,
      "dataVectorOffset": 96
    },
    "talk": {
      "root": "framework",
      "semantics": "lastStandardTalk",
      "uiModuleOffset": 11112,
      "nameOffset": 1044224,
      "textOffset": 1044328,
      "utf8String": {
        "stringPointerOffset": 0,
        "bufferUsedOffset": 16,
        "stringLengthOffset": 24
      }
    }
  },
  "validation": {
    "status": "live-verified",
    "gameVersion": "<game-version>",
    "executableSha256": "<sha256>",
    "verifierCommit": "<40-character-sha>"
  }
}
```

위 숫자는 현재 FCS에서 생성되는 값의 예시이며 스키마 상수로 취급하지 않는다.

### 스키마 규칙

- `schemaVersion`이 다른 문서는 부분적으로 해석하지 않는다.
- `pointerResolverVersion`이 지원되지 않으면 embedded fallback을 사용한다.
- SHA는 40자리 소문자 16진수만 허용한다.
- 패턴은 공백 없는 대문자 16진수와 `??` wildcard만 허용한다.
- 모든 오프셋은 허용 범위를 검사하고 unchecked 정수 변환을 금지한다.
- 필수 리소스는 `chatLog`와 `talk`이다.
- 이후 발화 유형은 optional property로만 추가한다.
- 필드 의미나 포인터 해석 규칙이 바뀌면 v3를 만든다.
- `minimumSharlayanVersion`보다 낮은 클라이언트는 해당 manifest를 적용하지 않는다.

## 7. FCS 메타데이터 매핑

### Framework root

- `Framework.Instance`의 `StaticAddressAttribute`
- pattern
- relative follow offset
- `isPointer`

### CHATLOG

- `Framework.UIModule`
- `UIModule.RaptureLogModule`
- `LogModule.LogMessageIndex`
- `LogModule.LogMessageData`
- `StdVector`의 first, last 및 end layout

### 표준 NPC Talk

- `Framework.UIModule`
- `UIModule.LastTalkName`
- `UIModule.LastTalkText`
- `Utf8String.StringPtr`
- `Utf8String.BufUsed`
- `Utf8String.StringLength`

`LastTalkName`과 `LastTalkText`는 마지막 표준 Talk 값을 나타내며 현재 대화창 활성 상태를 보장하지 않는다. 이 의미를 숨기지 않고 manifest의 `semantics`에 기록한다.

## 8. Generator 계획

`tools/Hermes.V2.Generator`를 deterministic .NET console tool로 추가한다.

### 입력

- repository root
- 별도로 checkout된 FCS 디렉터리
- 정확한 FCS commit SHA
- 정확한 generator commit SHA
- candidate 또는 production validation metadata

### 출력

- canonical v2 manifest
- resource revision
- 사람이 검토할 요약
- 이전 production manifest와의 필드 단위 diff

### 동작

1. reflection으로 FCS attribute와 `FieldOffsetAttribute`를 읽는다.
2. 필수 타입, 메서드, 필드 및 attribute가 존재하는지 검사한다.
3. FCS 메타데이터를 v2 DTO로 정규화한다.
4. JSON property 순서, UTF-8 encoding, newline 및 숫자 형식을 고정한다.
5. FCS, generator 및 validation 입력이 같으면 byte-identical 출력을 만드는지 확인한다.
6. 생성 결과를 JSON Schema로 검증한다.
7. 생성기를 두 번 실행하여 결과가 동일한지 CI에서 검사한다.

생성기는 FCS source를 실행 가능한 production 코드로 배포하지 않는다. FCS build와 source generator 실행은 secret이 없는 일회성 GitHub-hosted runner에서만 수행한다.

## 9. GitHub Actions 계획

### `fcs-v2-candidate.yml`

트리거:

```yaml
on:
  schedule:
    - cron: "0 */6 * * *"
  workflow_dispatch:
```

동시 실행은 하나만 허용한다.

```yaml
concurrency:
  group: fcs-v2-candidate
  cancel-in-progress: false
```

처리 순서:

1. `git ls-remote`로 FCS `refs/heads/main` SHA를 한 번만 확인한다.
2. SHA 형식을 검증하고 해당 실행 전체에서 같은 SHA를 사용한다.
3. `v2/source/fcs-head.json`과 같으면 종료한다.
4. FCS를 detached HEAD로 checkout한다.
5. generator를 두 번 실행하고 byte-identical 결과를 확인한다.
6. JSON Schema, generator test 및 Sharlayan contract test를 실행한다.
7. production manifest와 비교하여 리소스 변경 여부를 판단한다.
8. production이 아직 없으면 fixture와 동일해도 최초 candidate PR을 만든다.
9. production이 있고 리소스가 바뀌지 않았으면 처리한 FCS SHA 상태만 갱신한다.
10. 리소스가 바뀌면 candidate manifest와 diff를 포함한 PR을 만든다.
11. 자동 생성 PR에는 R2 credential을 제공하지 않는다.

FCS HEAD가 workflow 실행 중 다시 바뀌더라도 현재 실행은 처음 확인한 SHA만 처리한다. 다음 예약 실행이 새 HEAD를 처리한다.

### `publish-v2.yml`

트리거:

- 보호된 environment를 사용하는 `workflow_dispatch`
- 또는 승인된 candidate PR merge 후 명시적 승격
- rollback할 기존 revision을 선택하는 수동 입력

처리 순서:

1. candidate manifest와 resource revision을 다시 계산한다.
2. schema, generator identity 및 live verification metadata를 검증한다.
3. immutable manifest를 조건부 업로드한다.
4. 업로드 object의 checksum을 검증한다.
5. `v2/latest.json`을 마지막에 갱신한다.
6. public URL에서 배포 결과를 검증한다.

R2 credential은 이 workflow의 protected environment에만 둔다.

### 기존 `main.yml`

- `latest/address.json`의 legacy 업로드 역할을 유지한다.
- v2 내부 상태만 바뀐 commit에서 legacy object를 불필요하게 재업로드하지 않도록 path filter를 추가한다.
- 기존 `actions/checkout@master`는 검토된 commit SHA로 pin한다.

## 10. 생성과 승격의 분리

FCS commit이 새로 생겼다는 사실은 해당 데이터가 현재 production 게임에서 정상 동작한다는 증거가 아니다.

### 자동 candidate 검증

- 필수 FCS attribute와 field 존재
- pattern 문법
- 포인터 및 필드 오프셋 범위
- JSON Schema
- deterministic generation
- 이전 manifest와의 diff
- Sharlayan DTO 역직렬화
- synthetic memory를 사용한 포인터 해석 테스트
- CHATLOG 및 Talk reader 단위 테스트

### 실제 게임 검증

- Framework signature가 main module에서 정확히 한 번 일치
- CHATLOG 주소와 vector 포인터가 읽기 가능한 메모리
- 신규 채팅 메시지가 정상적으로 polling됨
- 표준 Talk에서 NPC 이름과 대사가 화면 내용과 일치
- 대화창 종료 후 `LastTalk` 값의 잔존 동작 기록
- 동일 대사 반복 및 닫기/재열기 동작
- 빈 문자열, 긴 문자열 및 UTF-8 다국어 문자열
- 지원 대상 글로벌 및 한국 클라이언트 확인

초기에는 로컬 Windows 검증 명령과 결과 파일을 candidate PR에 첨부한다. 충분히 안정화한 뒤 보호된 self-hosted Windows runner로 자동화할 수 있다.

## 11. Sharlayan.Lite 변경 계획

### Resource provider

리소스 획득 정책을 Sharlayan 내부로 이동한다.

우선순위:

1. 검증된 원격 `v2/latest.json`과 immutable manifest
2. 마지막으로 검증된 로컬 manifest cache
3. NuGet package에 포함된 embedded manifest

원격 요청 실패는 handler 생성 실패로 이어지지 않아야 한다. 각 단계에서 스키마, revision hash 및 호환 버전을 검증한 뒤 다음 fallback으로 진행한다.

### Cache

- latest pointer에는 ETag conditional request를 사용한다.
- immutable manifest는 resource revision을 파일명으로 저장한다.
- 다운로드 완료 전 임시 파일을 사용하고 검증 후 atomic rename한다.
- 손상된 cache는 격리하고 embedded fallback을 사용한다.
- timeout과 최대 응답 크기를 제한한다.

### Embedded fallback

- release 시점의 마지막 검증 manifest를 package에 포함한다.
- embedded manifest의 revision과 FCS commit을 assembly metadata 또는 진단 API로 노출한다.
- embedded 리소스와 원격 리소스는 같은 parser와 validator를 사용한다.

### CHATLOG 초기화

- manifest를 선택한 뒤 CHATLOG structure와 signature를 함께 설정한다.
- signature만 원격으로 바꾸고 structure는 package 값으로 남기는 혼합 상태를 금지한다.
- 초기화에 사용한 manifest revision을 handler 생명주기 동안 고정한다.
- polling 도중 latest가 바뀌어도 실행 중 handler를 자동 교체하지 않는다.
- 새 handler 또는 명시적 reload에서만 새 revision을 적용한다.

### Talk API

IronworksTranslator가 메모리 구조를 알 필요가 없도록 고수준 API를 추가한다.

예시:

```csharp
public sealed class TalkResult {
    public string Name { get; init; }
    public string Text { get; init; }
}

public TalkResult GetLastTalk();
```

구현 규칙:

- `Utf8String.StringPtr`와 `StringLength`를 읽는다.
- 고정 2048바이트 전체를 무조건 읽지 않는다.
- 최대 허용 길이를 적용한다.
- header를 읽은 뒤 pointer 또는 length가 바뀌면 한 번 재시도한다.
- UTF-8 경계와 null terminator를 안전하게 처리한다.
- 읽기 실패 시 빈 결과 또는 명시적인 unavailable 상태를 반환한다.
- 이름과 대사는 가능한 한 같은 snapshot에서 읽는다.

`GetLastTalk`은 현재 표시 여부를 의미하지 않는다. 필요하면 이후 `TalkResult`에 revision 또는 관측 시각이 아닌 raw state 정보를 추가하되, FCS에서 검증할 수 없는 활성 상태를 추측해서 제공하지 않는다.

### 진단 정보

다음 정보를 로그 또는 진단 API로 확인할 수 있게 한다.

- 선택된 source: remote, cache 또는 embedded
- resource revision
- FCS commit
- manifest validation 결과
- signature match 개수
- fallback이 발생한 이유

## 12. IronworksTranslator 변경 계획

- `HermesAddress.GetLatestAddress()` 직접 호출을 제거한다.
- `ALLMESSAGES`라는 사용자 정의 `Signature` 등록을 제거한다.
- `GetString(..., 2048)` 기반 NPC 대화 polling을 제거한다.
- Sharlayan의 `GetLastTalk()`을 사용한다.
- 이름과 대사를 함께 번역 파이프라인에 전달한다.
- 프로세스 attach 직후 남아 있는 `LastTalk` 값을 baseline으로 저장하고 신규 대사로 처리하지 않는다.
- 값 변경만이 아니라 대화 session 종료와 재시작을 표현할 방법을 후속 검토한다.
- Sharlayan이 remote manifest를 사용하지 못해 embedded fallback으로 전환해도 정상 동작하게 한다.
- 선택된 resource revision과 FCS commit을 진단 로그에 기록한다.

초기 migration release에서는 기존 Hermes address 경로를 비상 fallback으로 남길 수 있다. 새 Talk API가 실제 게임에서 충분히 검증되면 IronworksTranslator의 legacy fallback을 제거한다.

## 13. 기존 형식 호환 및 폐기

현재 `latest/address.json`과 R2 URL은 수정하지 않는다.

호환 정책:

1. v2를 지원하는 Sharlayan.Lite와 IronworksTranslator를 먼저 배포한다.
2. 일정 기간 기존 `latest/address.json`을 계속 제공한다.
3. legacy endpoint 사용량을 확인할 수 있으면 관찰한다.
4. 지원 중인 IronworksTranslator가 모두 v2 기반으로 전환된 뒤 legacy 수동 갱신을 중단한다.
5. endpoint 삭제가 필요하면 별도 공지와 sunset 기간을 둔다.

기존 endpoint 유지가 저비용이면 파일 자체는 장기 보존하고 더 이상 갱신하지 않는 선택도 가능하다.

## 14. 보안 및 안정성

- 모든 GitHub Action은 tag가 아니라 검토된 commit SHA로 pin한다.
- FCS build job에는 repository write 권한과 R2 secret을 제공하지 않는다.
- candidate 생성과 production 배포를 서로 다른 job 및 trust boundary로 분리한다.
- R2 credential은 bucket과 `v2/` prefix에 필요한 최소 권한만 부여한다.
- mutable latest와 immutable manifest 모두 HTTPS로 제공한다.
- 클라이언트는 manifest 내부 hash만 신뢰하지 않고 다운로드한 byte의 SHA-256을 직접 계산한다.
- bucket 또는 CDN 침해까지 방어해야 한다면 manifest 서명을 후속 도입한다.
- manifest 응답 크기, timeout, redirect 및 content type을 제한한다.
- 동일 SHA의 재처리와 동일 revision의 재배포는 안전한 idempotent 동작이어야 한다.
- rollback은 이전 immutable revision으로 latest pointer를 되돌리는 방식으로 수행한다.

## 15. 테스트 계획

### Hermes generator

- FCS attribute 추출 테스트
- 필드 누락 시 명확한 실패 테스트
- pattern 정규화 테스트
- canonical JSON 및 deterministic output 테스트
- resource revision 계산 테스트
- schema validation 테스트
- 현재 승인된 FCS commit fixture와 예상 값 비교

### Sharlayan.Lite

- latest pointer 및 manifest 역직렬화
- schema version 거부
- minimum version 거부
- revision hash 불일치 거부
- remote, cache 및 embedded fallback 순서
- 손상된 cache 복구
- ETag 304 처리
- CHATLOG signature와 structure의 atomic 적용
- synthetic memory 기반 Framework, CHATLOG 및 Talk 포인터 해석
- `Utf8String` inline 및 heap buffer 읽기
- 길이 변경 race와 최대 길이 처리
- 기존 CHATLOG parser 및 ring cursor 회귀 테스트

### IronworksTranslator

- attach 시 기존 `LastTalk` baseline 무시
- 이름과 대사 변경 감지
- 동일 문자열 중복 억제
- Sharlayan fallback 상태에서 정상 동작
- 게임 프로세스 종료 및 재연결
- 원격 endpoint 장애가 애플리케이션 전체 장애로 전파되지 않음

### 실제 게임 smoke

- CHATLOG signature unique match
- 신규 CHATLOG entry polling
- 일반 NPC Talk 이름과 대사
- 연속 대사 전환
- 같은 대사 반복
- 대화창 닫기와 다시 열기
- 한국어, 일본어 및 영문 UTF-8
- 긴 대사와 제어 payload 포함 대사

## 16. 구현 단계

### Phase 1: Hermes v2 규격과 generator

- [x] `schemas/hermes-v2.schema.json` 추가
- [x] `v2/fixtures/manifest.valid.json` 추가
- [x] `tools/Hermes.V2.Generator` 추가
- [x] 현재 pinned FCS commit으로 deterministic manifest 생성
- [x] generator unit test 추가
- [x] `resourceRevision` 계산 규칙 문서화

완료 조건:

- 같은 FCS 및 generator commit으로 두 번 실행한 결과가 byte-identical이다.
- 현재 CHATLOG와 `LastTalkName` 및 `LastTalkText` 값이 생성된다.
- 생성 결과가 JSON Schema를 통과한다.

### Phase 2: Sharlayan v2 consumer

- [x] v2 DTO와 validator 추가
- [x] remote, cache 및 embedded provider 구현
- [x] CHATLOG 초기화를 선택된 manifest로 전환
- [x] embedded manifest 추가
- [x] `GetLastTalk()` 구현
- [x] provider, pointer resolver 및 UTF-8 reader 테스트 추가
- [x] resource revision 진단 정보 추가

완료 조건:

- 네트워크가 없어도 embedded fallback으로 CHATLOG가 동작한다.
- 원격 manifest를 바꾸면 library 재빌드 없이 CHATLOG와 Talk 오프셋이 바뀐다.
- 호환되지 않거나 손상된 manifest는 적용되지 않는다.

### Phase 3: IronworksTranslator 전환

현재 작업 범위에서는 애플리케이션 코드를 변경하지 않는다. Hermes와
Sharlayan.Lite의 구현 상태 및 향후 연동 계약만
`D:\REPO\IronworksTranslator\docs\2026-07-22-hermes-v2-upstream-status.md`에
기록했다.

- [ ] Hermes JSON 직접 다운로드 제거
- [ ] `ALLMESSAGES` custom signature 제거
- [ ] Sharlayan `GetLastTalk()` 사용
- [ ] NPC 이름과 대사를 함께 처리
- [ ] attach baseline과 중복 처리 보완
- [ ] fallback 및 재연결 테스트 추가

완료 조건:

- IronworksTranslator가 Hermes 스키마와 포인터 체인을 직접 알지 않는다.
- CHATLOG와 표준 Talk가 Sharlayan API만으로 동작한다.
- Hermes 또는 네트워크 장애 시 embedded fallback으로 실행된다.

### Phase 4: Candidate 자동화

- [x] `fcs-v2-candidate.yml` 추가
- [x] 6시간 schedule 및 `workflow_dispatch` 추가
- [x] FCS SHA 고정 checkout 구현
- [x] deterministic generation 및 diff 구현
- [x] 변경 시 candidate PR 생성
- [x] 변경 없음 상태 처리 구현
- [x] Action dependency commit pin 적용
- [x] repository secret `HERMES_CANDIDATE_TOKEN` 설정

완료 조건:

- 새 FCS HEAD를 한 번만 처리한다.
- 최초 production 전에는 bootstrap fixture와 동일해도 candidate PR을 만든다.
- 관련 메타데이터가 달라질 때만 manifest 변경 PR이 생긴다.
- candidate workflow에는 production secret이 없다.

### Phase 5: 검증과 production 배포

- [x] Sharlayan live smoke에 v2 manifest 입력 지원 추가
- [x] CHATLOG 및 Talk 실제 게임 검증 절차 구현
- [x] `publish-v2.yml` 추가
- [x] GitHub repository R2 secret 설정
- [x] 기존 `main` protected environment 사용 결정
- [x] `main` required reviewer와 deployment branch policy 설정
- [ ] Sharlayan.Lite 9.1.2 배포 및 embedded manifest 동기화
- [x] immutable upload, checksum read-back 및 latest-last 적용
- [x] rollback dispatch 구현
- [x] R2 cache-control 및 content-type 설정
- [x] Cloudflare `/v2/` Cache Rule 설정

완료 조건:

- 검증되지 않은 candidate는 `v2/latest.json`에 반영될 수 없다.
- 이전 revision으로 rollback할 수 있다.
- latest와 immutable manifest가 CDN cache 환경에서도 일관되게 동작한다.

### Phase 6: Legacy 정리와 리소스 확장

- [x] legacy endpoint를 무기한 유지하기로 결정
- [ ] `latest/address.json` 수동 갱신 중단
- [ ] Talk 활성 상태 판별 가능성 조사
- [ ] `TalkSubtitle` 리소스 검토
- [ ] `NpcYell` 및 말풍선 리소스 검토
- [ ] BattleTalk의 누락된 FCS semantic metadata 추적

## 17. 운영 절차

### 정상 FCS 갱신

1. schedule이 새 FCS HEAD를 발견한다.
2. workflow가 candidate와 diff를 생성한다.
3. 정적 테스트 결과를 검토한다.
4. 현재 게임 executable에서 live smoke를 실행한다.
5. game version, executable hash 및 verifier commit을 기록한다.
6. candidate PR을 승인하고 merge한다.
7. protected publish workflow로 revision을 승격한다.
8. Sharlayan 및 IronworksTranslator 진단 로그에서 새 revision 사용을 확인한다.

### 긴급 rollback

1. 마지막 정상 immutable revision을 선택한다.
2. `publish-v2.yml` rollback mode를 실행한다.
3. `v2/latest.json`만 이전 revision으로 되돌린다.
4. public endpoint 및 클라이언트 ETag 갱신을 확인한다.
5. 문제 revision은 삭제하지 않고 rejected 상태를 운영 기록에 남긴다.

### FCS가 아직 갱신되지 않은 게임 패치

- embedded 또는 마지막 검증 cache를 계속 사용한다.
- signature 또는 구조가 깨졌다면 해당 기능을 unavailable로 표시한다.
- 검증되지 않은 수동 추정값을 production latest에 바로 반영하지 않는다.
- 긴급 수동 candidate가 필요하면 동일한 schema, smoke test 및 승인 절차를 거친다.

## 18. 완료 기준

v2 전환은 다음 조건을 모두 만족할 때 완료된 것으로 본다.

- Hermes가 FCS commit에서 CHATLOG와 표준 Talk manifest를 재현 가능하게 생성한다.
- production 리소스는 immutable revision과 latest pointer로 배포된다.
- Sharlayan이 remote, cache 및 embedded fallback을 검증하여 선택한다.
- Sharlayan의 CHATLOG signature와 structure가 같은 manifest revision에서 설정된다.
- IronworksTranslator가 Hermes 주소 및 `Signature`를 직접 다루지 않는다.
- 새 FCS commit 감지와 candidate 생성이 GitHub Actions로 자동화된다.
- 실제 게임 검증 없이 candidate가 production으로 승격되지 않는다.
- 이전 production revision으로 안전하게 rollback할 수 있다.
- 기존 `latest/address.json` 소비자는 migration 기간 동안 영향을 받지 않는다.

## 19. 후속 결정 사항

다음 운영 값으로 확정한다. GitHub 및 Cloudflare의 구체적인 설정 절차는
`docs/V2_GITHUB_AND_CACHE_SETUP.md`를 따른다.

- [확정] v2 public base URL: `https://hermes.sapphosound.com/v2/`
- [확정] 최초 `minimumSharlayanVersion`: `9.1.2`
- [확정] live smoke: 수동 유지. 게임 2FA와 GPU instance가 필요하므로 self-hosted
  automation은 도입하지 않는다.
- [확정] candidate PR: repository secret `HERMES_CANDIDATE_TOKEN`에 저장한
  fine-grained PAT 사용
- [확정] production 승인자: `sappho192`, protected environment: `main`
- [확정] immutable cache-control: `public,max-age=31536000,immutable`
- [확정] latest cache-control: `public,max-age=0,s-maxage=60,must-revalidate`
- [확정] legacy endpoint: 무기한 유지
- [미정] manifest 서명 도입 시점. 운영 이슈 또는 위협 모델 변경 시 재검토한다.
