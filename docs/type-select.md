# 유형 선택과 2형 1구간 설계

- 단위: 혈당 mg/dL, 거리 m(러너 월드 단위), 시간은 명시하지 않으면 게임 초
- 수치: 이 문서의 새 수치는 모두 PLACEHOLDER(`null`)다. 담당자가 정하기 전까지 임의의 값으로 채우지 않는다. 에디터 전용 개발 설정만 테스트 픽스처 값을 쓴다(8.1절).
- 의학 정보: 2형 신호, 음식별 혈당 영향, 2형 저혈당 여부·빈도는 세 출처(대한당뇨병학회 진료지침, ISPAD, ADA)로 확인한 뒤 값으로 넣는다. 이 문서는 틀만 정한다.
- 바탕: [type-tracks.md](type-tracks.md)(방향), [glucose-model.md](glucose-model.md), [hyper-event.md](hyper-event.md), [log-format.md](log-format.md), [result-screen.md](result-screen.md), [team-rules.md](team-rules.md)

## 1. 목적과 이유

지금은 실행하면 바로 1형 주행이 시작된다. 여기에 **시작할 때 유형(1형·2형)을 고르는 흐름**과 **2형 1구간 주행**을 더한다.

2형 1구간은 템플런 같은 러너에 더 가깝다. 아이가 달리면서 트랙의 음식을 먹거나 피하고, 쉬는 곳에서 쉬거나 계속 달리면서 혈당을 적정 범위 안에 두는 것이 목표다. 많이 먹으면 고혈당 쪽으로, 적게 먹고 계속 달리면 저혈당 쪽으로 간다. 혈당이 기준을 넘은 채로 이어지면 몸의 신호(notice)가 나오고, 아이는 어른에게 알린다(tell).

- 재미: 멈추지 않는 주행, 레인 선택이 곧 판단, 최고 점수.
- 학습: 음식 하나가 나쁜 것이 아니라 양과 균형이 중요하다. 몸이 힘들다는 신호를 알아채면 어른에게 알린다.

### 목표
- 처음 켤 때 유형을 고르고, 고른 유형을 기억한다. 설정 화면에서 바꿀 수 있다.
- 2형 대처 choose → move → notice → tell. 고혈당·저혈당 모두 notice → tell("어른에게 알리기" 한 번)로 다룬다.
- 2형 혈당은 1형 혈당 모델(GlucoseModel)을 다시 쓰고 값만 2형 설정에서 받는다.
- 2형 화면: 실제 혈당을 바로 보여 주는 혈당바. 측정기 없음.
- 2형 음식 아이템(간식 + 일반 음식). 음식마다 다른 흡수 곡선.
- 쉬는 곳(move): 혈당이 높으면 계속 달리기, 낮으면 쉬기가 맞는 선택.
- 지친 캐릭터 연출(연출만, 실제 속도 변화 없음).
- 영역 담당 팀원이 채울 빈 틀(11절).

### 범위 밖
- 1형 주행 변경. 1형은 지금 그대로 둔다. 1형 트랙에 일반 음식을 내보내는 것은 의학 정보를 확인한 뒤 따로 정한다.
- 2·3구간, 생활 환경 맵, 하루 흐름(낮·밤, 운동, 수면), 2형 측정 습관 장면. 다음 단계에서 설계한다.
- 유형 변경에 보호자 확인(ParentalGate). 2순위 보호자 리포트를 만들 때 함께 붙인다. 그전까지는 설정 화면에서 바로 바꾸고 끼워 넣을 자리만 둔다(6.3절).
- 2형 보호자 리포트 지표(2순위). 원시 기록만 남긴다(9절).
- 실제 그래픽·소리. 빈 틀과 기본 도형까지만 만든다.
- 튜토리얼, 퀴즈 관문.

### 하지 않을 것 (반목표)
- 음식을 "나쁜 음식"으로 부르거나, 하나 먹었다고 바로 캐릭터가 지치게 하지 않는다. 신호는 실제 혈당이 기준을 넘은 채로 일정 시간 이어질 때만 나온다.
- 먹는 행동 자체에 점수를 주지 않는다. choose·move에는 고르는 순간의 정답·오답 판정과 점수가 없다.
- 캐릭터 체형을 바꾸지 않는다. "아파한다"가 아니라 "지쳐 보인다"로 연출한다.
- 혈당은 정상인데 증상처럼 보이는 가짜 신호를 넣지 않는다.
- 높은 혈당이 게임상 이득이나 불이익이 되게 하지 않는다. 지친 연출은 실제 달리기 속도·충돌 판정·거리 점수를 바꾸지 않는다. 왜: 속도를 늦추면 장애물이 쉬워지는 보상이 되거나 거리 점수 벌칙이 된다.
- 의학적 대처가 1형과 같다고 주장하지 않는다. 같은 것은 아이의 행동("어른에게 알리기")뿐이다.
- 2형 세계를 tell 말고는 멈추지 않는다.

## 2. 화면 흐름

```
앱 시작 ─┬─ 저장된 유형 없음 → 유형 선택 → 타이틀
         └─ 저장된 유형 있음 → 타이틀

타이틀(지금 유형 표시, 시작, 설정)
  ├─ 시작 → 주행(고른 유형) → 탈락 → 결과(다시 하기, 타이틀로)
  └─ 설정 → 유형 다시 고르기(설정 화면에 그대로) / 돌아가기 → 타이틀
```

- 유형은 `TrackType { Type1, Type2 }`(Domain enum, 이름)로 다룬다.
- 고른 유형은 기기에 저장하고 다음 실행에도 쓴다(6절). 주행 중에는 바꿀 수 없다. 한 판 동안 유형은 바뀌지 않는다.
- 유형 선택 화면에서 고르면 바로 저장하고 타이틀로 간다. 설정에서 바꾸면 저장하고 설정 화면에 그대로 있다(지금 유형 표시가 바로 바뀐다). `돌아가기`로 타이틀에 가며, 다음 주행부터 새 유형으로 달린다. 앱을 다시 켤 필요는 없다.
- 결과 화면에 "타이틀로" 버튼을 더한다.
- 저장된 유형 파일이 깨졌거나 읽을 수 없으면 "저장된 유형 없음"으로 보고 유형 선택 화면을 띄운다.
- 일시정지 메뉴는 이 문서의 범위 밖이다([pause-elimination.md](pause-elimination.md)).

### 2.1 화면 문구(개발용)
화면 담당 팀원이 문구 초안을 만든다. 그전까지 개발용 문구를 쓴다. 치료·진단을 주장하지 않는다.
- 유형 선택: "어떤 당뇨가 있나요?" / 버튼 "1형", "2형". 보호자가 함께 고르도록 안내하는 한 줄을 둔다(문구는 화면 담당 초안).
- 타이틀: 게임 타이틀("슈가대시", 그림 자리), 그 아래 지금 유형("1형" 또는 "2형"만), 그 아래 "시작", 오른쪽 위 설정 버튼(톱니바퀴 그림 자리).

## 3. 2형 한 판의 흐름

```
시작(1구간) → 달리기 ─────────────────────────────────────────────┐
   ├─ choose: 음식 줄 → 레인으로 먹거나 피함 → 흡수 곡선으로 천천히 반영
   ├─ move: 쉬는 곳 → 들어가면 잠시 앉아 쉼(달리기 하강 멈춤)
   ├─ 실제 혈당 ≥ hyperThreshold 가 highHoldSec 동안 이어짐
   │     → notice(고혈당 신호) noticeSec → tell(고혈당) → 카운트다운 → 유예 → 달리기
   ├─ 실제 혈당 < hypoThreshold 가 lowHoldSec 동안 이어짐
   │     → notice(저혈당 신호) noticeSec → tell(저혈당) → 어른이 빠른 당 → 카운트다운 → 유예 → 달리기
   ├─ 장애물 충돌 → 탈락
   └─ 탈락 → 결과 화면
```

- 달리기 속도, 가속, 장애물, 장애물 탈락, 구간 기록, 점수 세 갈래 틀은 1형과 같은 코드를 쓴다.
- 2형 1구간 이벤트는 choose, move, notice → tell 뿐이다. 급하강, 표시 혈당 지연, 측정기, 게이지 경고, 저혈당 4단계는 2형에 없다.

## 4. 2형 혈당

### 4.1 모델
- GlucoseModel을 그대로 쓴다: `dBG/dt = − baseDecay − runDecay × activity + Σ 음식 흡수 곡선`. 급하강은 2형에서 발생하지 않는다.
- 달리는 동안 `activity = 1`, 쉬는 동안 `activity = 0`이다(5.2절). 그래서 쉬면 달리기 하강만큼 덜 내려간다.
- 표시 혈당은 쓰지 않는다. 2형 혈당바는 실제 혈당(`Actual`)을 그대로 보여 준다. 1형의 `display.delaySec` 불변식(1단계 제한 시간 > 지연)은 2형 설정에 적용하지 않는다.

### 4.2 음식
- 음식마다 `totalRise`(1회분이 올리는 양), `onsetRealMin`(먹고 오르기 시작할 때까지 실제 분), `absorbRealMin`(흡수되는 실제 분), `source`(출처)를 둔다.
- 실제 분은 로드할 때 `time.timeScale`로 게임 초로 바꾼다(`docs/glucose-model.md` 1.3절과 같은 방식). 흡수 곡선 모양은 1형과 같은 구간 내 균등 흡수다.
- 트랙 음식 하나를 먹으면 1회분이 반영된다.
- 음식 목록과 값은 2형 설정 파일에만 둔다. 1형 설정의 `snacks`는 그대로다.
- 처음 목록: `water`(물), `juice`(주스), `soda`(탄산음료), `apple`(사과), `cracker`(크래커), `chicken`(치킨), `pizza`(피자), `burger`(햄버거). 크래커는 1형 느린 당으로도 쓰려고 더했다(2026-11-09). 설정 파일에는 게임 수치를 정할 때 넣는다. 값은 모두 `null`, `source`도 `null`이다. 물도 값을 지어내지 않고 `null`로 둔다.
- 음식마다 등급(빠르게 올림, 천천히 올림, 늦게 오래 올림, 당 없음)을 정했다: [food-evidence.md](food-evidence.md). 세 출처로는 성질만 정하고, 등급별 게임 수치는 게임 표현값으로 따로 정한다. 등급을 설정에 어떻게 담을지는 게임 수치를 정할 때 함께 정한다.
- 화면 표시 이름은 Presentation의 문구 파일 `FoodText`가 id로 찾는다(두 유형 공통). 물, 주스, 탄산음료, 사과, 크래커, 치킨, 피자, 햄버거.

### 4.3 notice 판정 (EventDirector)
- 고혈당 쪽: 실제 혈당이 `glucose.hyperThreshold` 이상인 상태가 `type2.highHoldSec`(simDt) 동안 끊김 없이 이어지면 고혈당 notice를 시작한다. 중간에 기준 아래로 내려가면 누적을 0으로 되돌린다.
- 저혈당 쪽: 실제 혈당이 `glucose.hypoThreshold` 미만인 상태가 `type2.lowHoldSec` 동안 이어지면 저혈당 notice를 시작한다. 되돌림 규칙은 같다.
- notice가 시작되면 `type2.noticeSec`(simDt) 동안 신호만 보여 주며 주행은 계속된다. 이 시간이 끝나면 tell이 시작된다(4.4절).
  - notice가 한 번 시작되면 그사이 혈당이 기준 안으로 돌아와도 tell까지 간다. 왜: 신호를 실제로 보였으므로 "신호를 보면 알린다"를 끝까지 익히게 한다. 또 신호가 나왔다 사라지면 가짜 신호처럼 보일 수 있다.
  - `noticeSec = 0`이면 판정 즉시 tell이다.
- 재무장: 1형 고혈당과 같다. tell을 마친 뒤 고혈당 쪽은 `hyperThreshold − hyperReleaseMargin` 아래로, 저혈당 쪽은 `hypoThreshold + hypoReleaseMargin` 이상으로 돌아와야 다시 누적을 시작한다. cooldown과 재발 이벤트는 없다.
- 억제: notice·tell 진행 중, 재개 카운트다운, 유예 시간 끝까지는 두 쪽 모두 누적하지 않는다(누적은 0으로 둔다). 고혈당과 저혈당 notice는 동시에 진행되지 않는다.
- 1형 EventDirector 동작은 바꾸지 않는다. 2형 판정은 유형별 설정에 따라 켜진다. 1형 설정에는 hold 키가 없다.

### 4.4 tell (HypoEventFsm)
세계가 멈추고(worldDt = 0) 혈당 시간(simDt)은 흐른다. 1형 고혈당 알리기(`docs/hyper-event.md`)의 흐름을 그대로 쓴다: 정답이면 재개 카운트다운 → 유예, 오답·시간 초과면 오류 공통 처리(기록 → 위급 연출 → 알리기 대기 → 못 하면 탈락).

| | 고혈당 tell | 저혈당 tell |
| --- | --- | --- |
| 상태(Phase) | `HyperNotify` 재사용 | `LowNotify` 새로 추가 |
| 기록 단계(`step`) | `hyper_notify` 재사용 | `low_notify` |
| 제한 시간 | `type2.tellTimeLimitSec` | 같음 |
| 정답 | 어른에게 알리기 | 어른에게 알리기 |
| 오답 | 계속 달리기 `HY_KEEP_RUNNING`, 주스 마시기 `HY_JUICE` | 계속 달리기 `LO_KEEP_RUNNING` |
| 시간 초과 | `HY_TIMEOUT` | `LO_TIMEOUT` |
| 알린 뒤 혈당 | 어른이 알고 있으므로 따로 바꾸지 않는다(달리기 하강에 맡김) | 어른이 빠른 당(`type2.guardianSnackId`) 1회분을 건넨다 |

- "주스 마시기"를 고르면 주스(`type2.juiceChoiceFoodId`) 1회분을 혈당에 그대로 반영하고(`DoseApplied`, `chosenByChild: true`) 오류 공통 처리로 간다. 정책 A(`docs/glucose-model.md` 3절)와 같다.
- 저혈당 tell에서 어른이 빠른 당을 건네는 시점: 정답으로 알렸을 때, 또는 오류 뒤 알리기 대기에서 알렸을 때(`DoseApplied`, `chosenByChild: false`). 왜: 어른이 처치하지 않으면 혈당이 계속 낮아 재무장 조건을 넘지 못한다.
- 선택지 순서: 1형과 같이 섞고 "어른에게 알리기"는 맨 아래에 고정한다.
- 결과 문구 창(`answerFeedbackSec`)과 재개 카운트다운·유예는 1형과 같은 동작을 쓰고, 시간 값은 2형 설정에서 받는다.
- 2형 저혈당 tell의 오답을 더 둘지(예: 음식 관련)는 출처 확인 뒤 정한다. 지금은 "계속 달리기" 하나다.
- 2형 tell의 화면 문구: 고혈당·저혈당 같이 "몸이 신호를 보내요. 어떻게 할까요?". 신호의 종류(증상)를 문구로 설명하지 않는다(의학 내용이므로 출처 확인 뒤).

### 4.5 탈락 (2형)
1형 탈락 규칙을 그대로 쓴다.
1. 오류 뒤 알리기 대기에서 "다시 달리기"를 고르거나 시간 초과(`declined`, `prompt_timeout`).
2. 장애물에 부딪힘(`obstacle_hit`). 쉬는 곳과 음식은 장애물이 아니다.
3. tell 제한 시간을 넘긴 순간 실제 혈당이 안전 범위(`elimination.glucoseLow`~`glucoseHigh`) 밖이면 오류 처리 대신 바로 탈락(`glucose_low`·`glucose_high`). 평소 주행, notice, 쉬는 중, 카운트다운, 유예에는 혈당으로 탈락하지 않는다.

## 5. choose와 move

### 5.1 choose: 음식 줄
- 트랙에 음식 줄이 놓인다. 한 줄에는 서로 다른 음식 2개가 3레인 중 2레인에 하나씩 있고, 나머지 한 레인은 비어 있다. 왜: 빈 레인이 있어야 "안 먹고 지나가기"도 선택이 된다.
- 음식 종류와 레인은 `IRandomSource`가 아니라 지금 TrackSpawner처럼 시드 지정 난수로 무작위로 고른다(Presentation, 1형 트랙 간식과 같은 방식).
- 줄 간격은 `runner.foodRowIntervalMinSec`~`foodRowIntervalMaxSec`(worldDt) 사이 무작위다. 음식 줄과 장애물은 같은 위치에 겹치지 않는다.
- 캐릭터가 음식에 닿으면 먹는다(1형 트랙 간식과 같은 충돌 판정). 먹은 음식은 PlayerController → RunController 명령으로 전달되고, RunController가 GlucoseModel에 반영한다. PlayerController는 혈당을 직접 바꾸지 않는다.
- 고르는 순간 판정·점수는 없다. 기록만 남긴다(9절).
- 재개 카운트다운과 유예 중에도 음식 줄은 놓인다(1형 간식과 같다). 장애물만 치운다.

### 5.2 move: 쉬는 곳
- 쉬는 곳(벤치, 게임기 등)이 한 레인에 놓인다. 그 레인으로 들어가 닿으면 쉬기를 시작한다.
- 쉬는 동안
  - 캐릭터는 앉아 있고 앞으로 가지 않는다(달리기 속도 0, 거리 증가 없음, 가속도 멈춤).
  - `activity = 0`이라 달리기 하강이 멈춘다. 기본 하강과 음식 흡수는 계속된다.
  - 세계 시계(worldDt)는 흐른다. 이건 세계 정지가 아니다. notice 판정도 계속된다.
  - 입력(좌우, 점프, 슬라이드)은 무시한다.
- 쉬는 시간은 `runner.restDurationSec`(worldDt)로 고정이다. 중간에 일어나는 조작은 없다.
- 쉬는 곳 간격은 `runner.restIntervalMinSec`~`restIntervalMaxSec` 사이 무작위다. 쉬는 곳의 레인은 무작위, 쉬는 곳 주변(앞뒤 일정 거리)에는 장애물을 놓지 않는다.
- 쉬는 중 tell이 시작되면 쉬기를 끝내고 tell로 넘어간다. 재개 뒤에는 달린다.
- 고르는 순간 판정·점수는 없다. 쉰 사실과 그때 실제 혈당을 기록한다(9절).
- 거리 점수는 쉬는 동안 쌓이지 않는다. 혈당 유지 점수는 적정 범위에 있으면 계속 쌓인다. 이것이 쉬기의 대가와 보상이다.

## 6. 유형 저장

### 6.1 새 모듈
| 계층 | 이름 | 하는 일 | 이유 |
| --- | --- | --- | --- |
| Domain 포트 | `ISettingsStore` | 기기 설정(지금은 고른 유형) 읽기·쓰기. `TrackType? LoadTrackType()`, `void SaveTrackType(TrackType)` | 화면 흐름(App)이 저장 방식을 몰라야 한다. `IBestScoreStore`와 같은 모양 |
| Infrastructure | `SettingsRepository` | `ISettingsStore` 구현. `persistentDataPath/settings.json`, 임시 파일에 쓴 뒤 교체 | 덮어쓰는 파일은 atomic write(저장소 규칙) |

- 이름을 `ISettingsStore`로 한 이유: 앞으로 소리 크기 같은 기기 설정이 같은 파일에 들어올 수 있다. 유형만 다루는 포트를 따로 만들면 파일이 늘어난다.
- 형식: `{"schemaVersion":1,"trackType":"type1"}`. DTO는 Infrastructure에 두고 `[Preserve]`.
- 읽기 실패(파일 없음, 깨짐, 모르는 값)는 `null`을 돌려준다. 디스크 쓰기 오류는 호출자에게 올리지 않고 로그만 남긴다(`BestScoreRepository`와 같다).

### 6.2 최고 점수
- 유형별로 따로 둔다. 왜: 혈당 규칙과 대처가 달라 점수를 비교할 수 없다.
- `IBestScoreStore`는 바꾸지 않는다. Bootstrap이 유형마다 다른 파일 경로로 `BestScoreRepository`를 하나씩 만든다: 1형 `best-score.json`(지금 파일 그대로), 2형 `best-score.type2.json`.

### 6.3 보호자 확인 자리
- 설정 화면에서 유형을 바꾸는 명령은 GameFlowController의 메서드 하나로 받는다. 나중에 ParentalGate를 넣으면 이 메서드 앞에 끼운다. 지금은 확인 없이 바로 바꾼다.

## 7. 화면 (Presentation)

| 모듈 | 2형에서 하는 일 | 1형 |
| --- | --- | --- |
| HudPresenter | 혈당바가 실제 혈당을 보여 준다. 측정기 버튼·게이지 경고를 숨긴다 | 그대로 |
| HypoChoicePanel | 2형 tell 선택지(고혈당 3개, 저혈당 2개)를 보여 준다. 알리기 대기는 1형과 같다 | 그대로 |
| SymptomEffectPresenter | 2형에서는 화면 흔들림을 쓰지 않는다 | 그대로 |
| ResultScreenView | 2형 tell 요약을 보여 준다(9.2절). "타이틀로" 버튼 | "타이틀로" 버튼만 추가 |
| PlayerController | 음식·쉬는 곳 충돌, 쉬는 동안 앉기(속도 0, 입력 무시) | 그대로 |
| TrackSpawner | 2형: 음식 줄, 쉬는 곳 배치. 1형: 지금처럼 간식 하나 | 그대로 |

- 2형에서 실제 혈당을 보여 주는 혈당바는 "지금 혈당을 보며 먹는 양을 조절"하는 것이 목적이다. 2형도 실제로 측정하는 경우가 있으며, 측정 습관 장면은 2구간 설계에서 다룬다.
- 화면 흐름(타이틀, 유형 선택, 설정)과 2형 연출은 영역 빈 틀로 만든다(11절).

## 8. 설정

### 8.1 파일
- 유형마다 설정 파일을 하나씩 둔다.
  - 1형: `Assets/_Project/Config/game-config.json`(지금 파일, 내용 변경 없음, `trackType`만 추가)
  - 2형: `Assets/_Project/Config/game-config.type2.json`(새 파일, 값은 `null`)
  - 개발용: `Config/Dev/game-config.dev.json`(1형, `trackType`만 추가), `Config/Dev/game-config.type2.dev.json`(새 파일, `configVersion: "dev-not-real-type2"`, 테스트 픽스처 값)
- 두 설정 모두 앱 시작 때 불러와 검증한다. 하나라도 실패하면 지금처럼 오류를 남기고 Bootstrap을 끈다. 왜: 유형을 바꿨을 때에야 로드가 실패하는 일을 막는다.
- 파일마다 `"trackType": "type1" | "type2"`가 있고, 검증기는 그 유형에 필요한 키만 요구한다. 2형 파일에는 1형 전용 키(측정기, 표시 지연, 4단계 제한 시간·처치량, 급하강, 흔들림)를 두지 않는다. 오타 난 키는 지금처럼 `MissingMemberHandling.Error`로 잡는다.
- 빌드 설정 검증 EditMode 테스트(빌드에 실리는 설정을 불러와 검증)를 2형 파일에도 둔다. 값이 `null`인 동안 건너뛰는 방식은 1형 테스트와 같다.

### 8.2 2형 설정 키

```json
{
  "configVersion": "0.1.0-type2",
  "trackType": "type2",
  "unit": "mg/dL",
  "time": { "timeScale": null },
  "glucose": {
    "initial": null, "min": null, "max": null,
    "hypoThreshold": null, "hypoReleaseMargin": null,
    "hyperThreshold": null, "hyperReleaseMargin": null,
    "baseDecayPerSec": null, "runDecayPerSec": null,
    "normalRangeMin": null, "normalRangeMax": null
  },
  "type2": {
    "highHoldSec": null,
    "lowHoldSec": null,
    "noticeSec": null,
    "tellTimeLimitSec": null,
    "guardianPromptTimeLimitSec": null,
    "answerFeedbackSec": null,
    "resumeCountdownSec": null,
    "graceSec": null,
    "guardianSnackId": "juice",
    "juiceChoiceFoodId": "juice"
  },
  "foods": [
    { "id": "water",   "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null },
    { "id": "juice",   "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null },
    { "id": "soda",    "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null },
    { "id": "apple",   "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null },
    { "id": "chicken", "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null },
    { "id": "pizza",   "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null },
    { "id": "burger",  "totalRise": null, "onsetRealMin": null, "absorbRealMin": null, "source": null }
  ],
  "runner": {
    "foodRowIntervalMinSec": null, "foodRowIntervalMaxSec": null,
    "restIntervalMinSec": null, "restIntervalMaxSec": null,
    "restDurationSec": null
  },
  "elimination": { "glucoseLow": null, "glucoseHigh": null },
  "score": {
    "inRangePerSec": null,
    "perMeter": null,
    "correct": { "hyperNotify": null, "lowNotify": null },
    "errors": { "HY_KEEP_RUNNING": 0, "HY_JUICE": null, "HY_TIMEOUT": null,
                "LO_KEEP_RUNNING": 0, "LO_TIMEOUT": null }
  },
  "sections": [ { "id": "signal" } ]
}
```

- `source`를 포함해 `null`인 값이 하나라도 있으면 로드 실패다(1형 `snacks`와 같다). 개발 설정은 `source: "dev-not-real"`을 쓴다.
- 위험한 선택("계속 달리기")의 가중치는 1형처럼 0으로 고정한다.
- 거리 점수 가중치 `perMeter`도 2형 설정에 따로 둔다(9.3절 세 갈래 틀 유지).

### 8.3 2형 불변식 (로드 시 검증, 위반 시 실패)
1. `glucose.min ≤ glucoseLow < hypoThreshold`, `hyperThreshold < glucoseHigh ≤ glucose.max` (탈락 범위 안쪽에서 notice가 먼저 나올 수 있게)
2. `hypoThreshold + hypoReleaseMargin < hyperThreshold − hyperReleaseMargin` (1형과 같음)
3. `hypoThreshold ≤ normalRangeMin < normalRangeMax ≤ hyperThreshold`
4. `highHoldSec ≥ 0`, `lowHoldSec ≥ 0`, `noticeSec ≥ 0`, `tellTimeLimitSec > 0`, `guardianPromptTimeLimitSec > 0`, `restDurationSec > 0`
5. `foodRowIntervalMinSec ≤ foodRowIntervalMaxSec`, `restIntervalMinSec ≤ restIntervalMaxSec`, 둘 다 `> 0`
6. `guardianSnackId`, `juiceChoiceFoodId`가 `foods`에 있음. `foods` id 중복 없음, 2개 이상(음식 줄에 서로 다른 2개가 필요)
7. `totalRise ≥ 0`, `onsetRealMin ≥ 0`, `absorbRealMin > 0`, `timeScale > 0`
- 1형의 "1단계 제한 시간 > 표시 지연"은 2형에 없다.

## 9. 기록과 결과

### 9.1 기록 (`docs/log-format.md`)
- `schemaVersion`은 2로 둔다. 레코드 종류·필드·코드 값을 추가만 하고 기존 필드는 바꾸지 않는다. 왜: 조회가 버전이 다른 줄을 건너뛰므로 올리면 지난 기록을 읽지 못한다. `docs/type-tracks.md` 3절의 "스키마 버전을 올린다"는 이 이유로 고친다.
- `RunStarted`에 `trackType`(`type1`/`type2`)을 더한다. 없으면 `type1`로 읽는다(지난 기록).
- 새 레코드
  | `type` | 고유 필드 |
  | --- | --- |
  | `NoticeStarted` | `kind`(`high`/`low`), `actual`, `eventId` |
  | `LowEventTriggered` | `actual` (2형 저혈당 tell 시작) |
  | `LowResolved` | `allCorrect` |
  | `FoodEaten` | `foodId`, `actual`(먹은 순간 실제 혈당) |
  | `RestStarted`, `RestEnded` | `actual` |
- 2형 고혈당 tell은 `HyperEventTriggered`·`HyperResolved`를 그대로 쓴다. 유형은 `RunStarted.trackType`으로 구분한다.
- `eventId`: 2형도 고혈당·저혈당이 같은 번호 순서를 쓴다. `NoticeStarted`에서 번호를 정하고 이어지는 tell 레코드가 같은 번호를 쓴다.
- 새 `step`: `low_notify`. 새 `errorCode`: `HY_JUICE`, `LO_KEEP_RUNNING`, `LO_TIMEOUT`. `LogCodes`에 고정한다.
- 1형 트랙 간식은 지금처럼 `TrackSnackEaten`이다. 2형은 `FoodEaten`을 쓴다.
- 점수 재계산에 필요한 `GlucoseRangeEntered/Exited`, `StepAnswered`, `RunEliminated.distanceM`은 2형도 같게 남긴다.

### 9.2 결과 화면 (`docs/result-screen.md`)
- 틀은 같다: 제목, 요약, 점수, 최고 점수(유형별), 다시 하기, 타이틀로.
- 2형 요약: tell 이벤트가 있었을 때만 고혈당·저혈당 각각 한 줄("혈당이 높을 때 n번 중 m번 바로 알렸어요" 같은 개발용 문구). 문구는 화면 담당 초안을 받는다.
- MetricsCalculator의 `Summarize`가 유형에 따라 2형 요약(`RunSummary`에 저혈당 tell 결과 추가)을 만든다. 1형 지표에는 2형 이벤트를 넣지 않는다.

### 9.3 점수 (ScoringService)
- 세 갈래 틀(혈당 유지 + 대처 가중치 + 거리)은 그대로, 가중치는 2형 설정의 `score`에서 받는다. 2형 대처 가중치 대상은 `hyper_notify`, `low_notify` 단계뿐이다.
- 기록에서 다시 계산할 수 있어야 한다(쉬는 동안 거리 0은 `distanceM`에 이미 반영된다).

## 10. 모듈별 변경 요약

| 모듈 | 변경 |
| --- | --- |
| Domain `TrackType` | 새 enum |
| Domain 설정·검증 | `trackType`, 2형 설정 객체(`type2`, `foods`, 2형 `runner`·`score`), 유형별 필수 키·불변식 |
| GlucoseModel | 음식(게임 초로 바뀐 흡수 곡선) 먹기. 계산 방식은 그대로 |
| EventDirector | 2형: hold 누적, notice 시작, tell 발동, 재무장·억제. 1형은 그대로 |
| HypoEventFsm | `HyperNotify`에 "주스 마시기" 선택 추가(2형에서만 받음), `LowNotify` 단계, 알린 뒤 어른의 빠른 당 |
| ScoringService, MetricsCalculator | 2형 가중치·요약 |
| DomainEventBus 이벤트 | `NoticeStarted`, `LowEventTriggered`, `LowResolved`, `FoodEaten`, `RestStarted`, `RestEnded` |
| `ISettingsStore` (새) | 유형 저장 포트 |
| RunController | 음식 먹기·쉬기 명령, 쉬는 동안 activity 0·속도 0, notice 상태 노출, 유형별 세션 |
| RunSession | 유형을 가진다. Bootstrap 팩토리가 유형별 설정으로 만든다 |
| GameFlowController | 유형 선택·타이틀·설정·주행·결과 상태, 유형 저장, 유형별 최고 점수 |
| ConfigProvider | 2형 DTO·로드, 실제 분 → 게임 초 변환 |
| `SettingsRepository` (새) | `ISettingsStore` 구현 |
| LogRepository, ReportQueryService | 새 레코드·필드 쓰기·읽기 |
| GameBootstrapper | 두 설정 로드, 유형별 세션·최고 점수 저장소, 화면 빈 틀 연결 |
| Presentation | 7절 표와 11절 빈 틀 |

- App이 유형에 따라 갈라지는 곳은 GameFlowController(흐름)와 RunSession 생성(Bootstrap 팩토리)으로 한정한다. 나머지 코드는 설정·세션이 주는 값으로 동작한다.

## 11. 영역 빈 틀

빈 틀 규칙은 `docs/team-rules.md` 2.1절을 따른다. 영역 폴더(`Presentation/Character/`, `World/`, `Screen/`)를 이번에 만든다. 빈 틀마다 `Art/Sandbox/<이름>/`에서 혼자 시험할 수 있게 Inspector에서 상태를 바꿔 보는 값을 둔다.

| 영역 | 빈 틀 | 받는 것 | 채울 것 |
| --- | --- | --- | --- |
| 캐릭터 | `Character/CharacterStateView` | 상태: 보통 / 쉬는 중 / 지침(고혈당 notice·tell) / 지침(저혈당 notice·tell) | 애니메이터 연결, 지친 표정·느린 걸음·처진 자세(연출만), 앉아 쉬기 |
| 월드 | `World/TrackItemLook` | 아이템 종류(장애물, 음식 id, 쉬는 곳) | 종류별 프리팹 고르기. 없으면 지금 기본 도형 |
| 화면 | `Screen/TitleScreenView` | 지금 유형, 시작·설정 명령 | 화면 배치, 버튼 소리 |
| 화면 | `Screen/TypeSelectView` | 유형 고르기 명령 | 버튼 그림, 안내 문구 |
| 화면 | `Screen/SettingsView` | 지금 유형, 유형 바꾸기·돌아가기 명령 | 화면 배치 |
| 화면 | `Screen/NoticeSignalView` | notice 종류(high/low), 진행 여부 | 갈증 말풍선, 화장실 아이콘 깜빡임(high). low 신호 그림은 출처 확인 뒤 |

- 이 여섯 개는 Presentation의 새 모듈이다.
- 빈 틀은 받은 상태를 보여 주기만 한다. 상태 판정(notice인지, 쉬는 중인지)은 RunController가 노출하는 값으로 하고, 빈 틀 안에서 혈당을 보고 판단하지 않는다.
- 갈증·화장실 그림은 출처 확인 전까지 개발용 표시다. 출처 확인 결과에 따라 바뀔 수 있다고 PR과 할 일 목록에 적는다.
- 빈 틀을 작업 브랜치(처음에는 `feat/type-select`, 지금은 `feat/section2-frame`)에 push한 뒤 `docs/roadmap.md` 2.2절 할 일 목록의 빈 틀 채우기 줄을 "시작 가능"으로 바꾼다. 팀원은 이 브랜치를 받아 그 위에서 채우고 push한다(`docs/team-rules.md` 8절).


## 12. 남은 결정 (담당자)
| 정할 것 | 분야 |
| --- | --- |
| 2형 모든 수치(8.2절). 정함: [glucose-evidence.md](glucose-evidence.md), [game-values.md](game-values.md) | 결정, 의학 |
| 2형 저혈당을 얼마나 자주·세게 다룰지(값으로 조절) | 의학 |
| notice 신호(고혈당·저혈당)와 지친 연출의 출처, 저혈당 신호 그림. 정함: [notice-evidence.md](notice-evidence.md) | 의학 |
| 음식 목록과 음식별 값·출처. 정함: [food-evidence.md](food-evidence.md) | 결정, 의학 |
| 유형 변경에 보호자 확인을 둘지 | 결정 |
| 화면 문구(유형 선택, 타이틀, tell, 결과 요약). 정함(roadmap 4.2절) | 기획 |

## 13. 팀원별로 알아야 할 것

### 공통
- 작업 브랜치는 `feat/section2-frame`이다(`docs/roadmap.md` 2.2절 맨 위).
- 빈 틀은 받은 상태를 보여 주기만 한다. 빈 틀 안에서 혈당을 보고 판단하거나 세계·혈당을 바꾸지 않는다.
- 갈증·화장실 그림과 화면 문구(2.1절)는 개발용이다. 의학 출처 확인 결과에 따라 바뀔 수 있다.
- 화면·오브젝트의 모양(크기, 위치, 색, 그림, 애니메이션, 소리, 문구)은 바꿔도 되고 구조는 바꾸지 않는다([team-rules.md](team-rules.md) 2.3절).

### 팀원1(캐릭터)
- `Character/CharacterStateView`: 보통, 쉬는 중, 지침(높음), 지침(낮음) 네 상태를 애니메이터에 연결한다.
- 지친 연출은 연출만이다. 달리기 속도, 충돌, 거리를 바꾸지 않는다.
- 혈당 상태는 표정, 걸음, 자세로만 나타낸다. 체형은 바꾸지 않는다.

### 팀원2(월드)
- `World/TrackItemLook`: 장애물 3종, 음식 8종(id별), 쉬는 곳마다 프리팹을 고른다. 프리팹이 없으면 지금 기본 도형이 나온다.
- 장애물 종류(`LowObstacle` 점프, `LaneBlock` 옆으로, `HighObstacle` 슬라이드)는 그림이 바뀌어도 그대로 둔다. 게임 규칙이 종류로 판정한다.
- 음식이 "나쁜 음식"처럼 보이는 모양이나 연출을 넣지 않는다.

### 팀원3(화면)
- `Screen/TitleScreenView`, `TypeSelectView`, `SettingsView`: 화면 배치, 버튼 그림·소리. `root` 자식 오브젝트를 켜고 끄는 방식은 그대로 둔다.
- `Screen/NoticeSignalView`: 고혈당 신호(갈증 말풍선, 화장실 아이콘 깜빡임), 저혈당 신호(`몸이 떨려요`, 떨리는 손). 근거는 [notice-evidence.md](notice-evidence.md).
