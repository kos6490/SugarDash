# 공지

구조 담당 팀원이 팀원에게 전할 말을 적는 곳이다. 에이전트는 작업을 시작할 때 `git pull` 뒤에 이 문서를 읽고, 아직 전하지 않은 공지를 사람에게 알린다([team-rules.md](team-rules.md) 0절).

- 이 문서는 구조 담당 팀원만 고친다.
- 공지 번호는 1부터 하나씩 올린다. 새 공지는 맨 아래에 붙이고, 이미 쓴 번호는 바꾸거나 다시 쓰지 않는다.
- 대상: `전원` 또는 `팀원1(캐릭터)`, `팀원2(월드)`, `팀원3(화면)` 중 하나 이상.
- 더는 필요 없는 공지는 지워도 된다(번호는 다시 쓰지 않는다).

## 공지 1

- 날짜: 2026-11-05
- 대상: 전원
- 내용:
  - 작업 전에는 무조건 `git pull`한다. 에이전트가 작업을 시작할 때 Unity를 닫았는지 묻고 직접 pull한다(team-rules 0절, 8절).
  - 앞으로 전할 말은 이 문서(`docs/notices.md`)에 올린다. 에이전트가 pull한 뒤 새 공지를 알려 준다.

## 공지 2

- 날짜: 2026-11-05
- 대상: 전원
- 내용:
  - 의학 출처 판본을 정했다: [medical-sources.md](medical-sources.md). 출처 발췌, 문구, 그림에 의학 내용이 들어가면 이 문서의 판본과 장만 쓴다.
  - 기사나 요약은 권고 내용의 근거로 쓰지 않는다. 원문을 열고 장·쪽을 적는다(team-rules 4절).

## 공지 3

- 날짜: 2026-11-05
- 대상: 전원, 팀원3(화면)
- 내용:
  - 2구간 2-B(맵별 조건)가 들어왔다: [section2.md](section2.md) 7절. 1형은 운동장·집에 들어갈 때 측정기 위에 "혈당 재 볼까?" 말풍선이 잠깐 뜬다(혈당 재기 습관 순간). 2형은 놀이터에 쉬는 곳이 더 자주, 나들이에 음식 줄이 더 자주 나온다. 수치는 아직 개발용이다.
  - 팀원3(화면): 빈 틀 `Screen/HabitPromptView`를 채울 수 있다(roadmap 2.2절 "시작 가능"). 말풍선 프리팹은 `Assets/_Project/Art/UI/Screens/HabitPrompt.prefab`이다. 모양만 바꾸고, 문구는 `HabitPromptText`의 따옴표 안만 바꾼다(team-rules 2.3절).
  - 확인 방법은 [dev-scene.md](dev-scene.md) 9.14절이다.

## 공지 4

- 날짜: 2026-11-05
- 대상: 전원, 팀원1(캐릭터), 팀원3(화면)
- 내용:
  - 일시정지 메뉴와 탈락 장면이 들어왔다: [pause-elimination.md](pause-elimination.md). 주행 중 오른쪽 위 `II` 버튼으로 멈추고 `계속하기`·`타이틀로`를 고른다. 탈락하면 바로 결과 화면으로 가지 않고, 어른이 달려와 옆에 서서 말풍선 한 줄을 한 뒤 약 2.5초 뒤 결과 화면이 뜬다.
  - 팀원1(캐릭터): 빈 틀 `Character/GuardianArrivalView`를 채울 수 있다(roadmap 2.2절 "시작 가능"). 지금 나오는 주황색 캡슐을 어른 캐릭터 프리팹(발밑이 기준점)과 달려오기·서서 말하기 애니메이션으로 바꾼다. 위치·시간 계산은 그대로 둔다.
  - 팀원3(화면): 빈 틀 `Screen/PauseMenuView`, `Screen/EliminationSpeechView`를 채울 수 있다. 프리팹은 `Assets/_Project/Art/UI/Screens/PauseMenu.prefab`, `EliminationSpeech.prefab`이다. 말풍선 문구는 `EliminationText`의 따옴표 안만 바꾼다(team-rules 2.3절).
  - 작업 브랜치는 그대로 `feat/section2-frame`이다(roadmap 2.2절).
  - 확인 방법은 [dev-scene.md](dev-scene.md) 9.15절이다.
