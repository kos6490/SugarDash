# 고혈당 "어른에게 알리기" 설계 결정 기록

- 브랜치: `feat/hyper-event`
- 상태: 설계 확정, 수치는 PLACEHOLDER (담당자 결정 전까지 `null`)
- 범위: 1형 주행 기준이다. 2형 주행은 `docs/type-tracks.md`를 본다.
- 배경: 아동이 고른 양을 그대로 혈당에 반영하므로 과잉 처치는 고혈당으로 이어진다(`docs/glucose-model.md` 3절). 고혈당은 "어른에게 알리기" 한 단계만 다룬다(CLAUDE.md 2절).

## 1. 흐름

```
실제 혈당 ≥ hyperThreshold → HyperEventTriggered
  → [HyperNotify] 세계 정지. 선택지: "어른에게 알리기" / "계속 달리기" (제한 시간 hyper.notifyTimeLimitSec)
     ├ 어른에게 알리기          → 정답 → HyperResolved(allCorrect = true)
     └ 계속 달리기 / 시간 초과   → 오류 공통 처리: 기록 → 위급 연출 → 알리기 대기(GuardianPrompt)
          ├ 어른에게 알리기 → HyperResolved(allCorrect = false)
          └ 다시 달리기 / 알리기 제한 시간 초과 → 탈락(RunEliminated)
  → 재개 카운트다운 → 유예 시간 → 일반 주행   (저혈당과 같은 hypo.resumeCountdownSec, hypo.graceSec)
```

- 오류 코드: 계속 달리기 `HY_KEEP_RUNNING`, 시간 초과 `HY_TIMEOUT`. 기록 단계 이름은 `hyper_notify`.
- 측정기 버튼은 HyperNotify 동안 막는다(저혈당 2~4단계와 같다).
- 선택지를 더 넣는 것(예: 간식 먹기)은 의학 내용이므로 담당자가 정한다. 지금은 두 개만 둔다.

## 2. 발동과 재무장 (EventDirector)

- 판정은 실제 혈당으로 한다. 저혈당 발동과 같은 기준이다.
- 상태: `Armed → Active → WaitingRelease → Armed`
  - Active는 대처를 마칠 때(`NotifyHyperEventEnded`)까지다.
  - WaitingRelease에서 실제 혈당이 `hyperThreshold − hyperReleaseMargin` 아래로 내려오면 다시 무장한다.
  - cooldown과 재발 이벤트는 두지 않는다. 왜: 어른이 이미 알고 있으니 높은 상태로 반복 발동할 필요가 없고, 기본 하강으로 언젠가는 내려온다.
- 억제: 저혈당 대처 중(1~4단계, 알리기 대기), 고혈당 대처 중, 대처를 마친 뒤 유예 시간 끝까지는 발동하지 않는다. 과잉 처치 직후에는 저혈당 대처의 카운트다운과 유예가 끝난 뒤 발동한다.
- 고혈당 대처 중에는 저혈당 판정과 급하강 판정도 하지 않는다.
- 고혈당 대처를 마친 뒤의 유예도 저혈당 유예처럼 저혈당 발동·급하강·장애물을 억제한다. 저혈당 cooldown에는 영향을 주지 않는다.
- 한 판이 탈락으로만 끝나게 되면서 주행 길이로 끝나는 경우는 없어졌다. 그전에는 주행 길이가 다 차도 유예 중이거나 고혈당이 발동할 차례(`IsHyperTriggerPending`)면 주행을 끝내지 않았다.
- 고혈당 알리기를 제한 시간 안에 못 했을 때 실제 혈당이 `elimination.glucoseHigh`를 넘었으면 오류 처리 대신 바로 탈락한다(`glucose_high`).
- `eventId`는 저혈당과 같은 번호 순서를 쓴다. 이벤트 종류는 발동 레코드의 type으로 구분한다.

## 3. 혈당과 시계

저혈당 2~4단계와 같다(`docs/glucose-model.md` 2.2절). HyperNotify와 알리기 대기에서는 worldDt = 0이고 simDt와 decisionDt는 흐른다. 그래서 기본 하강과 간식 흡수가 이어진다.

## 4. 구현 위치

- HypoEventFsm에 `HyperNotify` 단계를 추가했다(`BeginHyper`). 오류 공통 처리, 탈락, 재개, 유예, `IsWorldFrozen`을 저혈당과 함께 쓰고, 두 이벤트가 동시에 진행되지 않음을 상태 하나로 보장한다. 새 모듈은 만들지 않았다.
- `ChooseKeepRunning`은 2단계에서는 `RS_KEEP_RUNNING`, HyperNotify에서는 `HY_KEEP_RUNNING` 오류다.
- GameBootstrapper가 `HyperEventTriggered`를 `HypoEventFsm.BeginHyper`에 구독시킨다. RunSession이 구독 여러 개를 받아 함께 해제한다.

## 5. 설정

| 키 | 의미 | 값 |
| --- | --- | --- |
| `glucose.hyperReleaseMargin` | 재무장 해제 여유분(mg/dL) | PLACEHOLDER |
| `hyper.notifyTimeLimitSec` | 알리기 선택 제한 시간(decisionDt 초) | PLACEHOLDER |

검증(`docs/glucose-model.md` 8절):
- `hyperReleaseMargin > 0`
- `hypoThreshold + hypoReleaseMargin < hyperThreshold − hyperReleaseMargin` (해제 구간이 겹치지 않게)
- `hyper.notifyTimeLimitSec > 0`

개발 설정(`game-config.dev.json`)은 에디터에서 과잉 처치로 고혈당을 볼 수 있게 `hyperThreshold`를 135로 낮췄다. 실제 수치가 아니다.

## 6. 기록과 지표

- 레코드: `HyperEventTriggered`(`actual`), `HyperResolved`(`allCorrect`). 이 밖에 `StepAnswered`, `GuardianPromptStarted`, `GuardianNotified`, `RunEliminated`에 `step: "hyper_notify"`가 들어간다(`docs/log-format.md`).
- `schemaVersion`은 1로 유지한다. 레코드 종류와 코드 값을 추가만 했고 기존 필드는 바뀌지 않았다. 조회는 버전이 다른 줄을 건너뛰므로 올리면 지난 기록을 읽지 못한다.
- 저혈당 지표(정확 대응률, 단계별 정답률, 측정 선행률)에는 고혈당 이벤트를 넣지 않는다. 오류 유형 분포에는 고혈당 코드도 센다. 판단 시간 중앙값은 `hyper_notify`를 따로 구한다.
- 결과 요약(`RunSummary.HyperEvents`)에 고혈당 이벤트별 결과를 담는다. 결과 화면에는 고혈당이 있었을 때만 한 줄을 더 보여 준다(`docs/result-screen.md`).

## 7. 남은 결정 사항

- 수치(`hyperThreshold`, `hyperReleaseMargin`, `hyper.notifyTimeLimitSec`)
- HyperNotify 화면 문구(개발용: "혈당이 높아요. 어떻게 할까요?")와 선택지 추가 여부
- 고혈당 증상 연출 여부(SymptomEffectPresenter)
