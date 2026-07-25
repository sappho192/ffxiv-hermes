# Hermes v2 live 확인 결과 및 필수 변경사항

## 지금까지 확인된 사실

- CHATLOG signature와 읽기는 정상이며 60초 smoke를 통과했습니다.
- `UIModule.LastTalkName/Text` 주소도 정상입니다.
- 기존 Talk 실패는 `StringLength(+0x18)=0`을 빈 문자열로 오판한 Sharlayan reader 문제입니다.
- 실제 길이는 FCS 구현처럼 `BufUsed - 1`로 계산해야 합니다.
- 현재 표시 중인 Talk는 `Talk` addon에서 취득할 수 있습니다.
  - `AtkValues[0]`: 현재 본문
  - `AtkValues[1]`: 현재 화자
- `LastTalk`는 현재 화면보다 한 단계 뒤처질 수 있으므로 현재 대화 UX의 주 데이터로는 부적합합니다.
- `AtkUnitBase`는 `AtkValues`, 개수, readiness와 visibility 상태를 제공합니다.
  [FCS AtkUnitBase](https://raw.githubusercontent.com/aers/FFXIVClientStructs/d25004c582d2c5d78118830d79ffd1479fe650ee/FFXIVClientStructs/FFXIV/Component/GUI/AtkUnitBase.cs)

## Hermes에서 해야 할 변경

### 1. Utf8String 길이 계약 수정

현재 `stringLengthOffset`을 authoritative length처럼 표현하는 계약을 수정해야 합니다.

권장안:

- `stringPointerOffset`
- `bufferUsedOffset`
- `lengthSource: "bufferUsedMinusNull"`

`stringLengthOffset`은 첫 production 전 제거하는 편이 좋습니다. 호환성을 위해 남긴다면
`informational`로 명확히 정의하고 소비자가 유효성 판단에 사용하지 못하게 해야 합니다.

### 2. `currentTalk` resource 추가

기존 `resources.talk`은 `semantics: lastStandardTalk`로 유지하고, 별도 resource를 추가하는 것이
좋습니다.

```text
resources.talk         → 직전 확정값, fallback
resources.currentTalk  → 현재 표시 중인 Talk addon
```

`currentTalk`에는 다음 메모리 계약이 필요합니다.

- UIModule → RaptureAtkModule
- RaptureAtkUnitManager
- AllLoadedUnitsList
- AtkUnitList entries/count
- AtkUnitBase addon name
- readiness/visibility field와 mask
- AtkValues pointer/count
- AtkValue size/type/value offsets
- addon name: `Talk`
- text index: `0`
- name index: `1`
- 허용 string type: 현재 관측된 `ManagedString(0x28)`

구조 offset은 FCS assembly metadata에서 추출하고, `Talk`, index 0/1 같은 의미 상수는 manifest에
명시해야 합니다.

### 3. Schema·generator·fixture 동시 갱신

다음을 함께 변경해야 합니다.

- `schemas/hermes-v2.schema.json`
- generator DTO
- FCS metadata extractor
- semantic validator
- generator unit tests
- canonical fixture
- 구현 계획과 manifest semantics 문서

`currentTalk`을 새 candidate에서 필수 resource로 만드는 것을 권장합니다.

### 4. Canonical JSON EOL 고정

Windows checkout에서 candidate가 CRLF로 변해 canonical revision과 local revision이 달라졌습니다.

```gitattributes
v2/**/*.json text eol=lf
schemas/*.json text eol=lf
```

추가 후 generator 출력, Git blob, working-copy byte의 SHA-256이 모두 같은지 Windows CI에서도
확인해야 합니다.

### 5. 새 candidate 생성

현재 `d25004c...` candidate는 production으로 승격하지 않고 새 generator commit으로 다시
생성해야 합니다.

새 candidate에는 다음이 포함되어야 합니다.

- 수정된 Utf8String 계약
- `currentTalk`
- 동일한 FCS commit 또는 새로 고정한 FCS HEAD
- `minimumSharlayanVersion: 9.1.2`
- 새 generator commit
- `validation.status: candidate`

## Sharlayan에서 해야 할 변경

### 1. 기존 `LastTalk` reader 수정

길이는 다음처럼 계산해야 합니다.

```text
effectiveLength = BufUsed - 1
```

검증 조건:

- `StringPtr != 0`
- `1 <= BufUsed <= MaximumStringBytes + 1`
- 마지막 byte가 null
- null을 제외한 byte만 strict UTF-8 decode
- before/after에서 `StringPtr`와 `BufUsed`가 동일
- race가 발생하면 한 번 재시도
- `StringLength(+0x18)=0`은 실패 조건으로 사용하지 않음

### 2. `currentTalk` manifest parser와 layout 추가

Hermes의 새 resource를 strict DTO와 semantic validator에서 해석하고, 동적 addon 탐색에 필요한
layout으로 매핑해야 합니다.

이 값은 기존 Scanner의 고정 `MemoryLocation`이 아니라 매 poll마다 addon list를 안전하게
순회하는 dynamic resource가 됩니다.

### 3. 현재 Talk reader 구현

권장 읽기 순서는 다음과 같습니다.

1. AllLoadedUnitsList의 count를 capacity 이내로 검증
2. addon name이 정확히 `Talk`인지 확인
3. `IsReady=true`, `IsVisible=true` 확인
4. `AtkValuesCount >= 2` 확인
5. `[0]`과 `[1]`이 허용된 string type인지 확인
6. `[0]`을 text, `[1]`을 name으로 bounded read
7. addon 상태와 두 포인터를 다시 읽어 snapshot 일관성 확인

### 4. API 의미 분리

기존 `GetLastTalk()`의 의미는 바꾸지 않는 편이 안전합니다. 다음 API를 추가하는 구성이 좋습니다.

```text
GetCurrentTalk()   → 활성 Talk만 반환
GetLastTalk()      → 직전 확정 Talk
GetTalk()          → Current 우선, 필요하면 Last fallback
```

결과에는 최소한 다음 상태가 필요합니다.

```text
Source: Current | Last
IsVisible
IsAvailable
Name
Text
```

초기 attach 시 남아 있던 `Last`를 새 대사 이벤트로 오인하지 않도록 baseline 처리도 필요합니다.

### 5. 테스트 보강

단위 테스트에는 다음을 포함해야 합니다.

- `StringLength=0`, 유효한 `BufUsed`
- inline/heap Utf8String
- null 누락, oversize, invalid UTF-8
- header race
- visible/current Talk
- hidden/unready Talk
- addon list count/pointer 손상
- AtkValue type 불일치
- name/text가 서로 다른 프레임에서 바뀌는 race
- Current 부재 시 Last fallback
- Current가 있으면 stale Last보다 우선

### 6. LiveSmoke 구분

기존 의미가 섞이지 않도록 다음처럼 구분하는 것을 권장합니다.

- `--require-last-talk`
- `--require-current-talk`
- 필요하면 `--print-talk`으로 수동 화면 대조

필수 live 시나리오:

1. 현재 화면과 `Current` 이름/본문 일치
2. 다음 대사로 넘기면 `Current`가 갱신
3. `Last`는 직전 값을 보존
4. 창을 닫으면 `Current`가 unavailable
5. CHATLOG와 Talk를 함께 60초 검증
6. 다국어와 SeString payload 포함 대사 검증

## 최종 진행 순서

1. Hermes schema/generator/currentTalk 구현
2. 새 candidate PR 생성·merge
3. Sharlayan 9.1.2에서 새 candidate 지원
4. 전체 multi-target build/test/package verification
5. Sharlayan verifier commit 생성
6. merged candidate로 최종 live smoke
7. Hermes `publish-v2` 수동 promote 및 `main` environment 승인
8. live-verified immutable manifest를 Sharlayan embedded resource로 동기화
9. 다시 package verification 후 Sharlayan.Lite 9.1.2 공개
