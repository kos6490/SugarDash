# GlucoseModel 설계 결정 기록

- 브랜치: `feat/glucose-model`
- 상태: 설계 확정, 수치는 PLACEHOLDER (담당자 결정 전까지 `null`)
- 단위: 혈당 mg/dL, 시간은 명시하지 않으면 게임 초(game seconds)

## 1. 혈당 변화 방식

변화율(rate)을 합산한다. 도메인은 UnityEngine의 `Time`을 읽지 않고 경과 시간을 인자로 받는다.

```
dBG/dt = − baseDecay                      (시간 경과)
         − runDecay × activity            (달리기, 주행 중에만)
         + Σ snackAbsorption_i(t)         (활성 간식별 흡수 곡선)
         − suddenDropContribution         (급하강 이벤트 진행 중)
```

### 1.1 간식: 흡수 곡선
- 즉시 더하지 않는다. 간식마다 `totalRise`, `onsetSec`, `absorbSec`를 두고 활성 간식 목록을 관리한다.
- 빠른 당과 느린 당의 차이는 `onsetSec`와 `absorbSec`로만 표현한다. 이는 2단계 "빠른 당 vs 느린 당" 선택의 교육 의도를 혈당 곡선에 드러내기 위함이다.
- 흡수 곡선의 모양(구간 내 균등, 삼각형 등)은 구현 단계에서 정한다. 1순위는 구간 내 균등 흡수(onset 이후 absorbSec 동안 일정 속도)로 시작한다.

### 1.2 급하강
- 확률 이벤트다. 발생 판정은 EventDirector가 하고, 난수는 `IRandomSource` 포트로 주입받아 단위 테스트가 재현되도록 한다. GlucoseModel은 판정된 급하강을 혈당에 적용만 한다.
- 발생 확률은 `p = 1 − (1 − chancePerSec)^simDt`로 계산해 프레임 길이와 무관하게 한다.
- 급하강은 simTime이 흐를 때만 판정한다(정지 구간에서는 발생하지 않음).

### 1.3 시간 압축
- 설정에 `timeScale`(실제 시간 ÷ 게임 시간)을 하나 둔다.
- 의학적 근거가 있는 값은 **실제 분 단위 그대로 적고 출처를 붙인 뒤** 로드 시 변환한다. 예: 재측정 간격 15분(ISPAD 2022).
- 목표: 재측정 간격 15분 → 게임 5~7초. 이때 `timeScale` ≈ 130~180 (15×60 ÷ 7 ≈ 129, 15×60 ÷ 5 = 180). 확정 수치는 PLACEHOLDER.
- 근거가 없는 게임 수치(달리기 하강률 등)는 게임 초 단위로 둔다.

## 2. 저혈당 이벤트 중 혈당과 시계

### 2.1 시계 두 개
| 시계 | 용도 | 동작 |
|---|---|---|
| `simTime` | 혈당 모델 | 정지하거나 빨리 감김 |
| `realElapsed` | 단계 제한 시간, 판단 시간(지표) | 항상 흐름 |

`Tick(dtReal, activity)` 하나만 받는다. simTime으로 넘길지는 호출자(App, HypoEventFsm)가 지정한 시계 모드(`Running`, `ActualFrozen`, `AllFrozen`)로 정한다. 모델은 FSM 단계를 모른다. 기록(LogRepository)에는 두 시각을 모두 남긴다.

### 2.2 구간별 동작
| 구간 | 실제 혈당 | 표시 혈당 시계 | 주행 |
|---|---|---|---|
| 발동 ~ 1단계(위험 인지) | 정지 (발동 시점 값 고정) | 계속 진행 | 계속 |
| 2단계(대응 선택) ~ 3단계(처치량) | 정지 | 정지 | 멈춤 |
| 4단계(재확인) 진입 | 재측정 간격만큼 빨리 감기(fast-forward) | 함께 진행 | 멈춤 |

- 1단계에서 표시 시계가 흘러야 게이지 경고가 발동 후 늦어도 `delaySec` 뒤에 뜬다(`warningThreshold = hypoThreshold`이면 정확히 `delaySec` 뒤). 그래야 "경고 후 눌러도 정답, 단 측정 선행은 아님" 판정이 성립한다.
- 4단계 빨리 감기에서는 달리기 항을 0으로 두고 간식 흡수와 기본 하강만 적용한다. 실시간 대기는 없다. 재측정 결과는 결정적(deterministic)이다.

## 3. 처치 반영 정책: 아동이 고른 양을 그대로 반영

- 혈당에는 아동이 **실제로 고른** 간식과 양을 반영한다. "어른에게 알리기 → 정답 상태로 보정"은 서사와 채점(단계 진행)에만 적용하고, 혈당 값에는 적용하지 않는다.
- 결과:
  - 과소 처치 또는 느린 당 선택 → 회복이 부족 → 재발생 조건(4장)에 따라 재발 이벤트가 발동할 수 있다.
  - 과잉 처치 → 고혈당 쪽으로 간다. 고혈당 이벤트("어른에게 알리기" 한 단계)가 **1순위 범위로 들어온다.**
- 영향: HypoEventFsm, 고혈당 처리, ScoringService, MetricsCalculator. 고혈당 임계값(`hyperThreshold`)을 설정에 추가한다.

## 4. 재발생 조건

발동 임계값과 해제 여유분을 따로 두고, 시간 기반 금지 기간(cooldown)을 함께 쓴다.

```
이벤트 종료 후 cooldown 동안: 발동하지 않음
재무장(re-arm):
  BG ≥ hypoThreshold + hypoReleaseMargin           → 정상 재무장 (다음 하강 시 일반 이벤트)
  cooldown 경과 && BG < hypoThreshold              → "재발" 이벤트로 발동, 기록에 isRecurrence = true
```

- 시간 금지만 두면: cooldown 종료 즉시 재발동한다.
- 값 여유분만 두면: 처치 부족으로 해제 임계값을 넘지 못하면 영원히 재발동하지 않는다.
- 둘을 함께 써서 두 문제를 모두 막는다. 정책 A에서는 두 번째 줄이 "처치 부족으로 인한 재발" 경로가 된다.

## 5. 표시 혈당 지연

- **순수 시간 지연(pure time delay)**: `display(t) = actual(t − delaySec)`.
- 1차 저역통과 필터(low-pass filter)는 쓰지 않는다. 급하강 시 경고 시점이 `delaySec`를 넘을 수 있어 "1단계 제한 시간 > 지연" 불변식을 보장하지 못한다.
- 구현: `(simTime, value)` 샘플을 링 버퍼(ring buffer)에 쌓고 `t − delaySec` 시점 값을 선형 보간(linear interpolation)으로 구한다. 시작 시 버퍼를 초기값으로 채운다. 단, 표시 시계는 2.2의 표를 따른다.
- 1순위 범위에서는 노이즈를 넣지 않는다(경고 깜빡임과 판정 시점 흔들림 방지).

## 6. 지표 정의 변경

- **불필요 측정**: "저혈당 상황이 발동하지 않았을 때" → "**실제 혈당이 `hypoThreshold` 이상일 때** 누른 측정"으로 바꾼다.
- 이유: cooldown 중에도 혈당이 낮으면 증상이 보이고, 이때 측정은 올바른 행동이다. 기존 정의로는 불필요 측정으로 잘못 집계된다.
- 영향: MetricsCalculator(지표 계산은 ScoringService가 아니라 여기서 한다), CLAUDE.md 2절.

## 7. 설정 구조

계층 배치:
- 인프라: JSON DTO와 Newtonsoft 역직렬화. `MissingMemberHandling.Error`로 오타 난 키를 잡는다.
- 도메인: 순수 C# `GlucoseConfig`와 불변식 검증. Newtonsoft를 참조하지 않는다.

PLACEHOLDER는 `null`로 둔다. 로더는 `null` 필드의 경로를 모두 모아 한 번에 실패시킨다. 테스트는 테스트용 설정 픽스처(fixture)를 쓴다.

```json
{
  "configVersion": "0.1.0",
  "unit": "mg/dL",
  "time": {
    "timeScale": null,
    "recheckIntervalRealMin": 15,
    "recheckIntervalSource": "ISPAD 2022 Ch.12, doi:10.1111/pedi.13443"
  },
  "glucose": {
    "initial": null,
    "min": null,
    "max": null,
    "hypoThreshold": null,
    "hypoReleaseMargin": null,
    "hyperThreshold": null,
    "baseDecayPerSec": null,
    "runDecayPerSec": null
  },
  "recurrence": { "cooldownSec": null },
  "display": { "delaySec": null, "warningThreshold": null },
  "hypo": { "step1TimeLimitSec": null },
  "suddenDrop": { "chancePerSec": null, "amount": null, "durationSec": null },
  "snacks": [
    { "id": "juice", "kind": "fast", "totalRise": null, "onsetSec": null, "absorbSec": null, "source": null },
    { "id": "cracker", "kind": "slow", "totalRise": null, "onsetSec": null, "absorbSec": null, "source": null }
  ]
}
```

간식 id와 종류는 예시다. 실제 목록은 2·3단계 선택지와 함께 정한다.

## 8. 불변식

### 8.1 GlucoseConfig 단독
1. `min < hypoThreshold < initial < hyperThreshold ≤ max`
2. `hypoReleaseMargin > 0`, `hypoThreshold + hypoReleaseMargin < hyperThreshold`
3. 하강률, 상승량, `onsetSec`, `absorbSec`는 모두 `≥ 0`, `absorbSec > 0`
4. `display.delaySec ≥ 0`
5. `display.warningThreshold ≥ hypoThreshold` (경고가 발동 후 늦어도 `delaySec` 뒤에 뜨기 위한 조건)
6. `0 ≤ suddenDrop.chancePerSec`, `suddenDrop.amount > 0`, `suddenDrop.durationSec > 0`
7. `recurrence.cooldownSec > 0`
8. 간식 `id` 중복 금지, 모든 `fast` 간식의 `onsetSec` < 모든 `slow` 간식의 `onsetSec`
9. `timeScale > 0`
10. 모든 `fast` 간식의 `onsetSec` < 재측정 간격(게임 초). 그래야 4단계 재측정에 빠른 당의 효과가 보인다.
11. `configVersion`은 비어 있으면 안 된다(기록마다 저장).

### 8.2 교차 검증 (모든 설정 로드 후)
12. `hypo.step1TimeLimitSec > display.delaySec` ← 핵심. 1단계 제한 시간은 HypoEventFsm 설정에 속한다.

## 9. 영향받는 모듈

| 모듈 | 영향 |
|---|---|
| HypoEventFsm | 단계별 혈당 정지·빨리 감기 호출, 재발 이벤트 구분, 정책 A |
| 고혈당 처리 | 정책 A로 1순위 범위 편입, `hyperThreshold` 사용 |
| MetricsCalculator | 불필요 측정 정의 변경, 재발 이벤트의 정확 대응률 포함 여부 |
| EventDirector | 급하강 판정(`IRandomSource`), 저혈당 발동·cooldown·재무장·재발 판정 |
| RunController | 1단계 중 주행 유지, 2~4단계 주행 정지 |
| LogRepository | simTime·realElapsed 모두 기록, `isRecurrence`, 실제 선택한 간식과 양 |
| 설정 로더 | null 수집 실패, 교차 검증 단계 |

## 10. 남은 결정 사항

- 재발 이벤트를 정확 대응률 분모에 넣을지
- 고혈당 이벤트 중 혈당 정지 여부(저혈당과 같은 규칙을 쓸지)
- 고혈당 이벤트 발동 후 재무장 규칙(해제 여유분, cooldown). 정해지기 전까지 `hyperThreshold`는 설정과 불변식에만 쓰고 발동 판정은 구현하지 않는다.
- 흡수 곡선 모양(1순위는 균등 흡수로 시작)
- 모든 수치(PLACEHOLDER)
