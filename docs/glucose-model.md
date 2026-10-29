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
- 급하강은 simTime이 흐를 때만 판정한다. 저혈당 이벤트 발동부터 재개 후 유예 시간 끝까지는 판정하지 않는다(2.3절).

### 1.3 시간 압축
- 설정에 `timeScale`(실제 시간 ÷ 게임 시간)을 하나 둔다.
- 의학적 근거가 있는 값은 **실제 분 단위 그대로 적고 출처를 붙인 뒤** 로드 시 변환한다. 예: 재측정 간격 15분(ISPAD 2022).
- 목표: 재측정 간격 15분 → 게임 5~7초. 이때 `timeScale` ≈ 130~180 (15×60 ÷ 7 ≈ 129, 15×60 ÷ 5 = 180). 확정 수치는 PLACEHOLDER.
- 근거가 없는 게임 수치(달리기 하강률 등)는 게임 초 단위로 둔다.

## 2. 저혈당 이벤트 중 혈당과 시계

### 2.1 시계 세 개
RunController가 프레임마다 경과 시간을 셋으로 나눠 Domain에 넘긴다. 어떤 시계를 멈출지는 HypoEventFsm의 현재 단계가 정하고(`IsWorldFrozen`), RunController는 그 값을 읽어 worldDt를 0으로 만든다.

| 시계 | 누적값(기록 필드) | 쓰는 곳 |
|---|---|---|
| `worldDt` | - | 러너 이동, 트랙 생성, 달리기로 인한 혈당 감소 |
| `simDt` | `SimTime` (`SimT`) | 기본 하강, 간식 흡수, 급하강, 표시 혈당 지연 버퍼, cooldown, 유예 시간 |
| `decisionDt` | `DecisionTime` (`T`) | HypoEventFsm 제한 시간, 판단 시간(지표) |

- `GlucoseModel.Tick(simDt, worldDt, decisionDt, activity)`. `worldDt`는 0 또는 `simDt`와 같아야 한다. 모델은 FSM 단계를 모른다.
- 기본 하강 = `baseDecayPerSec × simDt`, 달리기 감소 = `runDecayPerSec × activity × worldDt`.
- 메뉴 일시정지와 앱 백그라운드 전환에서는 셋 다 0이다.
- 모든 도메인 이벤트와 기록에 `T`와 `SimT`를 남긴다. 판단 시간은 `T`의 차이로 계산한다.

### 2.2 구간별 동작
| 구간 | worldDt | simDt | decisionDt |
|---|---|---|---|
| 일반 주행, 1단계(위험 인지) | 흐름 | 흐름 | 흐름 |
| 2~4단계, 오류 후 알리기 대기, 재개 카운트다운 | 0 (세계 정지) | 흐름 | 흐름 |
| 4단계 재측정 빨리 감기 | 0 | 재측정 간격만큼 진행 | 0 |
| 재개 후 유예 시간 | 흐름 | 흐름 | 흐름 |

- 1단계에서는 실제 혈당도 계속 내려간다(달리기 감소 포함). 표시 시계도 흐르므로 게이지 경고는 발동 후 늦어도 `delaySec` 뒤에 뜬다(`warningThreshold = hypoThreshold`이면 정확히 `delaySec` 뒤). 그래서 "경고 후 눌러도 정답, 단 측정 선행은 아님" 판정이 성립한다.
- 2~4단계에도 기본 하강과 흡수는 simDt로 계속된다. 증상 연출 강도는 이 값을 따른다. 그래서 2~4단계에도 제한 시간을 둔다(혈당이 끝없이 내려가지 않게).
- 재측정 빨리 감기는 재측정 간격(`RecheckIntervalGameSec`)을 `time.fastForwardTickSimSec` 간격으로 나눠 여러 번 Tick한다. 큰 값 하나로 넘기지 않는 것은 표시 혈당 지연 버퍼를 채워 재개 시점에 혈당바가 오른 값을 보여 주기 위해서다. 실시간 대기는 없고 결과는 결정적이다.
- 화면 문구 "15분 뒤"와 재측정 간격(게임 초)은 다른 숫자다.

### 2.3 재개와 유예 시간
- 대처를 마치면 `hypo.resumeCountdownSec` 동안 카운트다운(세계 정지), 이어서 `hypo.graceSec`(simDt) 동안 유예 시간이다.
- 유예 시간에는 저혈당 발동과 급하강 판정을 하지 않고 장애물을 만들지 않는다. 측정기를 누르면 `inGrace: true`로 기록한다.
- cooldown은 대처를 마친 시점에 시작하며 유예 시간과 별개다. 유예 중에도 해제 수준 도달은 기록하고, 유예가 끝난 뒤 4절 규칙으로 판정한다.

### 2.4 게이지 경고
- 표시 혈당 < `display.warningThreshold`이면 경고가 켜져 있고, 경고선 이상으로 올라오면 꺼진다. 히스테리시스는 두지 않는다. 판정은 EventDirector가 한다.
- 측정 선행은 누른 순간 경고가 꺼져 있었는지로 판정한다. 재발 이벤트처럼 발동 시점에 이미 경고가 켜져 있으면 측정 선행이 아니다.

### 2.5 프레임 순서와 주행 종료 (RunController)
- 화면 명령(측정기, 선택지, 알리기, 트랙 간식)은 큐에 쌓았다가 다음 프레임 맨 앞에서 처리한 뒤 Tick → Evaluate → Update 순서로 진행한다.
- 명령은 지난 프레임의 Update가 끝난 시각에 처리되므로 마감이 지난 답은 정상 답으로 받지 않는다. 대신 판단 시간은 최대 한 프레임 짧게 잡힌다.
- 달리기 강도(`activity`)는 달리는 동안 항상 1이다. 점프·슬라이드에 따른 차등은 두지 않는다.
- 트랙 간식을 주우면 `runner.trackSnackServings`배만큼 혈당에 반영한다. 1단계(위험 인지) 중에도 반영하며 대처 단계 판정과는 무관하다. 세계가 멈춘 동안에는 주울 수 없다.
- 트랙 간식은 `runner.trackSnackIntervalSec`(worldDt 기준 초)마다 하나씩 놓인다. 간식 종류는 `snacks` 목록에서 무작위로 고른다. 이 간격이 주행 중 혈당 균형을 직접 좌우하므로 담당자가 정할 값이다.
- 장애물 충돌은 혈당과 무관하다. 부딪히면 잠깐 느려질 뿐이고(PlayerController) 탈락하지 않는다.
- 러너 화면(캐릭터, 트랙)은 `RunController.LastWorldDt`로만 움직인다. 그래서 2~4단계·알리기 대기·재개 카운트다운·일시정지 중에는 멈추고, 1단계와 유예 시간에는 달린다.
- 재개 카운트다운과 유예 시간에는 트랙의 장애물을 모두 치우고 새로 놓지 않는다. 간식은 계속 놓는다.
- 주행은 worldDt 누적이 `runner.runDurationSec`에 닿으면 끝난다(`RunCompleted`). 대처 중(1~4단계, 알리기 대기, 재개 카운트다운)에는 끝내지 않고 대처를 마친 뒤 끝낸다.
- 탈락하면 주행이 멈추고 더 진행하지 않는다.

## 3. 처치 반영 정책: 아동이 고른 양을 그대로 반영

- 혈당에는 아동이 **실제로 고른** 간식과 양을 반영한다. "어른에게 알리기 → 정답 상태로 보정"은 서사와 채점(단계 진행)에만 적용하고, 혈당 값에는 적용하지 않는다.
- 아동이 고른 것이 없을 때만 어른이 정답을 대신한다. 2단계에서 계속 달리기·당 없음·시간 초과·선택 대신 알리기면 어른이 빠른 당(`hypo.guardianSnackId`)을 건네고, 3단계에서 시간 초과·선택 대신 알리기면 어른이 적정량(`hypo.doses.proper`)을 넣는다.
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
- 구현: `(simTime, value)` 샘플을 쌓고 `t − delaySec` 시점 값을 선형 보간(linear interpolation)으로 구한다. 시작 시 버퍼를 초기값으로 채운다. 표시 시계는 simTime이다(2.2절).
- 1순위 범위에서는 노이즈를 넣지 않는다(경고 깜빡임과 판정 시점 흔들림 방지).

## 6. 지표 정의 변경

- **불필요 측정**: "저혈당 상황이 발동하지 않았을 때" → "**실제 혈당이 `hypoThreshold` 이상일 때** 누른 측정"으로 바꾼다.
- 이유: cooldown 중에도 혈당이 낮으면 증상이 보이고, 이때 측정은 올바른 행동이다. 기존 정의로는 불필요 측정으로 잘못 집계된다.
- 영향: MetricsCalculator(지표 계산은 ScoringService가 아니라 여기서 한다), CLAUDE.md 2절.

### 6.1 지표 계산 규칙 (MetricsCalculator)
원시 기록(주행별 .jsonl)을 조회할 때마다 계산하고 저장하지 않는다. 저혈당 이벤트는 `eventId`로 묶는다.

| 지표 | 계산 |
| --- | --- |
| 정확 대응률 | 4단계를 모두 보정 없이 맞힌 이벤트(`HypoResolved.allCorrect`) ÷ 끝난 이벤트. 끝난 이벤트는 대처를 마쳤거나 탈락한 이벤트다. 탈락은 실패로 센다. 기록이 중간에 끊긴 이벤트(앱 종료 등)는 뺀다. **재발 이벤트도 넣는다** |
| 단계별 정답률 | 단계마다 맞힌 수 ÷ 그 단계까지 간 수. 탈락해 가지 못한 단계는 분모에서 뺀다. 4단계는 재측정과 알리기를 묶어 하나로 세고, 둘 다 기록되고 둘 다 맞아야 정답이다 |
| 오류 유형 분포 | 오류 코드별 횟수 |
| 판단 시간 중앙값 | 단계(재측정·알리기는 따로)별 `decisionSec` 중앙값. 시간 초과(`choiceId` 없음)는 판단이 아니므로 뺀다. 시간 초과 횟수는 오류 유형 분포에 잡힌다 |
| 측정 선행률 | 1단계 답에서 `measuredBeforeWarning`인 것 ÷ 1단계 답이 기록된 이벤트. 불필요 측정 횟수와 함께 보여 준다 |
| 불필요 측정 횟수 | `MeterPressed.unnecessary` 수 |

결과 화면(아동용)은 완주·탈락, 저혈당 횟수와 그중 모두 맞힌 횟수, 이벤트별 4단계 O/X만 보여 준다. 나머지 지표는 보호자 리포트(2순위)에서 보여 준다.

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
    "recheckIntervalSource": "ISPAD 2022 Ch.12, doi:10.1111/pedi.13443",
    "fastForwardTickSimSec": null
  },
  "glucose": {
    "initial": null,
    "min": null,
    "max": null,
    "hypoThreshold": null,
    "hypoReleaseMargin": null,
    "hyperThreshold": null,
    "baseDecayPerSec": null,
    "runDecayPerSec": null,
    "normalRangeMin": null
  },
  "recurrence": { "cooldownSec": null },
  "display": { "delaySec": null, "warningThreshold": null },
  "hypo": {
    "step1TimeLimitSec": null,
    "step2TimeLimitSec": null,
    "step3TimeLimitSec": null,
    "step4TimeLimitSec": null,
    "guardianPromptTimeLimitSec": null,
    "resumeCountdownSec": null,
    "graceSec": null,
    "guardianSnackId": "juice",
    "doses": { "under": null, "proper": null, "over": null }
  },
  "suddenDrop": { "chancePerSec": null, "amount": null, "durationSec": null },
  "snacks": [
    { "id": "juice", "kind": "fast", "totalRise": null, "onsetSec": null, "absorbSec": null, "source": null },
    { "id": "cracker", "kind": "slow", "totalRise": null, "onsetSec": null, "absorbSec": null, "source": null }
  ],
  "runner": { "trackSnackServings": null, "runDurationSec": null, "trackSnackIntervalSec": null }
}
```

간식 id와 종류는 예시다. 실제 목록은 2·3단계 선택지와 함께 정한다.

- `hypo.step1~4TimeLimitSec`, `guardianPromptTimeLimitSec`, `resumeCountdownSec`는 decisionDt 기준 초, `graceSec`는 simDt 기준 게임 초다. 4단계 제한 시간은 선택 하나당 적용한다.
- `hypo.doses`는 간식 1회분에 곱하는 배수(과소·적정·과잉)다.
- `hypo.guardianSnackId`는 아동이 간식을 고르지 못했을 때(계속 달리기, 당 없음, 시간 초과, 선택 대신 알리기) 어른이 건네는 빠른 당이다. 수치가 아니라 간식 id 참조라서 `"juice"`로 둔다.
- `glucose.normalRangeMin`은 적정 처치 후 재측정에서 도달해야 하는 하한이다(불변식 18).
- `runner.trackSnackServings`는 트랙 간식 하나에 곱하는 배수, `runner.runDurationSec`는 worldDt 기준 주행 길이, `runner.trackSnackIntervalSec`는 트랙 간식 간격(worldDt 기준 초)이다(2.5절). 달리기 속도, 레인 폭, 점프·슬라이드 시간, 장애물 간격, 충돌 감속 같은 러너 감각 수치는 혈당과 무관하므로 JSON이 아니라 각 컴포넌트의 Inspector 값으로 둔다.

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
13. `hypoThreshold ≤ normalRangeMin < hyperThreshold`
14. 재측정 간격(게임 초) `> display.delaySec`, `0 < fastForwardTickSimSec < display.delaySec`
15. `step2~4TimeLimitSec`, `guardianPromptTimeLimitSec`, `resumeCountdownSec`, `graceSec` 모두 `> 0`
16. `0 < doses.under < doses.proper < doses.over`
17. `guardianSnackId`는 `snacks`에 있는 `fast` 간식이어야 한다.
18. **적정 처치 회복 보장**. 최악의 경우에서 적정량을 먹고 재측정 빨리 감기를 마친 혈당이 정상 범위여야 한다.
    - 최악의 투입 시점 혈당 `G0 = hypoThreshold − suddenDrop.amount − (baseDecay + runDecay) × step1Limit − baseDecay × (3 × guardianLimit + step2Limit + step3Limit)` (발동 직후 급하강 전량, 1단계는 주행 중, 1~3단계 모두 오류 후 알리기 대기를 최대로 쓴 경우)
    - `G0 < min`이면 위반이다.
    - 모든 `fast` 간식 각각에 대해 `G0 + A(w) − D ≥ normalRangeMin`이어야 한다.
      - `A(w) = totalRise × doses.proper × clamp((w − onsetSec) / absorbSec, 0, 1)`
      - `D = baseDecay × (step4Limit + guardianLimit + W)`, `W` = 재측정 간격(게임 초)
      - 실제 혈당은 `w = W`, 표시 혈당은 `w = W − delaySec`로 두 번 확인한다.
    - `W`가 양의 유한수가 아니면 14·18번은 건너뛴다(9번이 이미 보고).

### 8.3 주행 설정
19. `0 < runner.trackSnackServings < hypo.doses.under`, `runner.runDurationSec > 0`, `runner.trackSnackIntervalSec > 0`

구간별 1단계 제한 시간 배율이 생기면(2순위) "모든 구간에서 `step1TimeLimitSec × 배율 > delaySec`"를 추가한다.

## 9. 영향받는 모듈

| 모듈 | 영향 |
|---|---|
| HypoEventFsm | 4단계 판정, 오류 공통 처리, 탈락, 재측정 빨리 감기, 재개·유예, `IsWorldFrozen`, 정책 A |
| 고혈당 처리 | 정책 A로 1순위 범위 편입, `hyperThreshold` 사용 |
| MetricsCalculator | 불필요 측정 정의 변경, 재발 이벤트의 정확 대응률 포함 여부, 4단계는 재측정·알리기 두 기록을 묶어 계산 |
| EventDirector | 급하강 판정(`IRandomSource`), 저혈당 발동·cooldown·재무장·재발 판정, EventId, 게이지 경고, 유예 중 억제 |
| RunController | 세 시계 계산(`IsWorldFrozen`이면 worldDt = 0, 일시정지면 모두 0), 프레임마다 쌓인 명령 처리 → Tick → Evaluate → Update 순서 호출(2.5절) |
| LogRepository | `SimT`·`T` 모두 기록, `isRecurrence`, `inGrace`, `helpInstead`, 실제 선택한 간식과 양(`DoseApplied`), 빨리 감기 전후 혈당 |
| 설정 로더 | null 수집 실패, 교차 검증 단계 |

## 10. 남은 결정 사항

- 고혈당 이벤트 중 혈당 정지 여부(저혈당과 같은 규칙을 쓸지)
- 고혈당 이벤트 발동 후 재무장 규칙(해제 여유분, cooldown). 정해지기 전까지 `hyperThreshold`는 설정과 불변식에만 쓰고 발동 판정은 구현하지 않는다.
- 흡수 곡선 모양(1순위는 균등 흡수로 시작)
- 모든 수치(PLACEHOLDER)
