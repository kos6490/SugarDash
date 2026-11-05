# 일시정지 메뉴와 탈락 장면

- 범위: 주행 중 일시정지 버튼·메뉴, 앱에서 나갔다 돌아올 때의 동작, 탈락 뒤 결과 화면 전의 짧은 장면. 두 유형이 같이 쓴다.
- 바탕: [roadmap.md](roadmap.md) 5.2절, [glucose-model.md](glucose-model.md) 2절(세 시계), [result-screen.md](result-screen.md), [dev-scene.md](dev-scene.md) 6.14~6.16·9.15절

## 1. 목적

- 일시정지: 아이가 주행 중에 잠깐 멈추거나 그만둘 수 있게 한다. 앱에서 나갔다 돌아왔을 때 갑자기 달리기가 다시 시작되어 장애물에 부딪히지 않게 한다.
- 탈락 장면: 탈락을 "끝"이 아니라 "어른이 도와주러 온다"로 보여 준다. 탈락하자마자 결과 화면으로 넘어가지 않고, 어른이 달려와 옆에 서서 한마디 하는 장면을 2~3초 보여 준다.

하지 않는 것:
- 대처·알리기 패널이 떠 있는 동안 일시정지 버튼을 보여 주지 않는다. 판단 중에 멈춰 생각할 시간을 버는 수단이 되지 않게 한다(앱에서 나갔을 때는 예외, 2.2절).
- 탈락 장면을 건너뛰게 하지 않는다. 짧고, 어른이 도와주러 오는 장면을 끝까지 보여 주는 것이 의도다.
- 탈락 문구에 아이를 탓하는 말, 의학 설명, "죽음"·"생존" 표현을 쓰지 않는다.

## 2. 일시정지

### 2.1 버튼과 메뉴

- 주행 화면 구석에 일시정지 버튼이 있다. 누르면 메뉴가 뜨고 세 시계(세계, 혈당, 판단)가 모두 멈춘다.
- 버튼은 세계가 달리고 있을 때만 보인다(`GameFlowController.CanPause`).

| 때 | 버튼 |
| --- | --- |
| 일반 주행, 1단계(측정기 누르기), 유예, 2형 쉬는 중 | 보임 |
| 2~4단계·고혈당 알리기·2형 tell·알리기 대기 패널 | 숨김 |
| 재개 카운트다운(3-2-1), 튜토리얼 설명 창·"다시 해봐요"·3-2-1 | 숨김 |
| 탈락 장면, 결과·타이틀 등 주행 밖 화면 | 숨김 |
| 메뉴가 떠 있을 때 | 숨김 |

- 메뉴는 `계속하기`와 `타이틀로` 두 버튼이다. 화면 전체를 덮는 배경이 뒤의 버튼(측정기 등) 터치를 막는다.

### 2.2 앱에서 나갔다 돌아올 때

- 주행 중에 앱이 백그라운드로 가면 메뉴를 연다. 패널이 떠 있던 중이어도 연다(`OpenPauseMenu(force: true)`).
- 돌아와도 바로 이어 가지 않는다. 메뉴가 떠 있고, 아이가 `계속하기`를 눌러야 이어 간다.
- 탈락 장면, 결과·타이틀 화면에서 나갔다 오면 아무 일도 일어나지 않는다.

### 2.3 계속하기

- 메뉴를 열 때 세계가 달리고 있었으면 3-2-1 뒤에 이어 간다. 3-2-1 동안 세 시계가 멈춰 있고 캐릭터 조작을 받지 않는다. HUD의 카운트다운 글자를 같이 쓴다.
- 패널이 떠 있던 중(앱에서 나갔다 온 경우)이면 바로 이어 간다. 패널이 다시 보이고 제한 시간은 멈춘 곳부터 흐른다.
- 3-2-1 길이는 `Bootstrapper`의 `Pause Countdown Sec`(기본 3초)이다. 게임 규칙 수치가 아니라 화면 연출 값이라 설정 JSON에 두지 않는다.

### 2.4 타이틀로

- 판을 그만두고 타이틀로 간다. 기록 파일은 닫지만 결과 화면, 점수, 최고 점수는 계산하지 않는다.
- 새 기록 종류를 만들지 않는다. 탈락 기록(`RunEliminated`)이 없는 판으로 남고, 앱이 강제 종료된 판과 같다.
- 튜토리얼 판에서 나가면 튜토리얼을 끝낸 것으로 치지 않는다. 다음 `시작하기`도 튜토리얼 판이다.

## 3. 탈락 장면

### 3.1 흐름

```
탈락(RunState.Eliminated) → 탈락 장면(FlowScreen.Elimination, 기본 2.5초) → 기록 닫기 → 결과 화면
```

- 세계는 탈락한 순간에 멈춘다. 장면 동안 주행은 열어 두어 HUD(혈당바, 점수)가 마지막 값을 그대로 보여 준다.
- 장면이 끝나면 지금까지처럼 기록을 닫고 다시 읽어 결과를 계산한다.
- 장면 길이는 `Bootstrapper`의 `Elimination Scene Sec`(기본 2.5초)이다. 화면 연출 값이다.
- 튜토리얼 판에서도 같은 장면을 보여 준 뒤 튜토리얼 결과 화면으로 간다.
- 장면 동안 일시정지 메뉴, `다시 하기`, `타이틀로`는 받지 않는다.

### 3.2 어른과 말풍선

- 어른 캐릭터가 옆 레인 앞쪽에서 나타나 주인공 바로 옆까지 달려와 선다(`GuardianArrivalView`, 캐릭터 담당). 오는 시간은 Inspector `Arrive Sec`(기본 1초)이며 장면 길이보다 짧게 둔다. 모델이 없는 동안은 주황색 캡슐이 나온다.
- 어른 옆에 말풍선 한 줄이 뜬다(`EliminationSpeechView`, 화면 담당). 문구는 탈락 이유 세 갈래마다 다르다(`EliminationText`, 자리 채움 초안).

| 갈래 | 탈락 이유(`EliminationCause`) | 초안 문구 |
| --- | --- | --- |
| 장애물 | `ObstacleHit` | 괜찮아? 어른이 왔어 |
| 알리지 않음 | `Declined`(알리기 대신 다시 달리기), `PromptTimeout`(알리기 제한 시간 초과) | 힘들 땐 어른에게 알려 줘 |
| 혈당 범위 | `GlucoseLow`, `GlucoseHigh` | 어른이 도와줄게 |

### 3.3 화면 흔들림

- 1형 저혈당 쪽 카메라 흔들림은 탈락 장면에서 끄지 않고 살짝만 이어 간다. 세기는 실제 혈당으로 정한 세기에 `SymptomEffectPresenter`의 `Eliminated Scale`(기본 0.3)을 곱한다.
- 일시정지 메뉴가 떠 있는 동안에는 흔들지 않는다.

## 4. 코드 구조

| 계층 | 바뀐 것 |
| --- | --- |
| Domain | `HypoEventFsm.EliminatedBy`: 탈락 방식(`EliminationCause?`). `RunEliminated`의 `Cause`와 같다 |
| App | `RunController.EliminatedBy`, `Resume(countdownSec)`(일시정지 뒤 3-2-1). `GameFlowController`: `CanPause`, `IsPauseMenuOpen`, `OpenPauseMenu(force)`, `ContinueFromPause()`, `QuitToTitle()`, `IsShowingElimination`, `EliminatedBy`, `Tick(dt)`. `FlowScreen.Elimination` |
| Presentation | 빈 틀 `PauseMenuView`(화면), `EliminationSpeechView`(화면), `GuardianArrivalView`(캐릭터), 문구 파일 `EliminationText`, `SymptomShake.StateScale` |
| Bootstrap | 세 빈 틀 연결, `Pause Countdown Sec`·`Elimination Scene Sec` 칸, 앱 백그라운드 시 메뉴 열기(바로 이어 가지 않음) |

- 기록 형식은 바뀌지 않는다([log-format.md](log-format.md)).

## 5. 빈 틀 채우기

| 담당 | 파일 | 채울 것 |
| --- | --- | --- |
| 팀원1(캐릭터) | `Presentation/Character/GuardianArrivalView.cs` | 어른 캐릭터 프리팹(`Guardian Prefab` 칸, 발밑이 기준점), 달려오기·서서 말하기 애니메이션. 위치·시간 계산은 그대로 둔다 |
| 팀원3(화면) | `Presentation/Screen/PauseMenuView.cs` | 일시정지 버튼 아이콘, 메뉴 배치·그림, 버튼 소리 |
| 팀원3(화면) | `Presentation/Screen/EliminationSpeechView.cs` | 말풍선 그림, 어른 쪽 꼬리, 나타나는 연출 |
| 기획 | `Presentation/EliminationText.cs` | 세 갈래 문구 확정(1절 하지 않는 것 지킴) |

- 확인은 [dev-scene.md](dev-scene.md) 9.15절 순서로 한다.
