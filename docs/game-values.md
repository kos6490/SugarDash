# 게임 표현값

출처로 정하지 않고 게임의 재미와 난이도로 정하는 값이다. 실제 수치가 아니며 아동 플레이 테스트에서 조정한다. 출처로 정한 혈당 기준값은 [glucose-evidence.md](glucose-evidence.md), 음식 등급은 [food-evidence.md](food-evidence.md)에 있다.

- 정한 값은 빌드 설정(`Assets/_Project/Config/game-config.json`, `game-config.type2.json`)에 넣는다. 에디터 개발 설정(`Config/Dev/`)은 그대로 둔다.
- 출발점은 개발 설정 값이다. 저학년용으로 제한 시간은 넉넉하게 시작한다.
- 묶음을 모두 정하면 빌드 설정이 불러와져 설정 검증 테스트(불변식)가 돌기 시작한다. 그때 어긋나는 값을 함께 고친다.

## 1. 시간 (2026-11-09)

| 설정 | 1형 | 2형 | 뜻 |
| --- | --- | --- | --- |
| `time.timeScale` | 150 | 150 | 실제 1분을 게임 0.4초로 줄인다. 다시 재는 15분은 게임 6초 |
| `display.delaySec` | 3 | 없음 | 혈당바가 실제 혈당을 늦게 따라오는 시간 |
| `hypo.step1TimeLimitSec` | 10 | 없음 | 측정기 누르기 |
| `hypo.step2~4TimeLimitSec` | 10 | 없음 | 빠른 당·양·재확인 고르기 |
| `type2.tellTimeLimitSec` | 없음 | 10 | 2형 tell 고르기 |
| `hyper.notifyTimeLimitSec` | 10 | 없음 | 1형 고혈당 알리기 |
| `guardianPromptTimeLimitSec` | 8 | 8 | 오류 뒤 "어른에게 알리기" |
| `resumeCountdownSec` | 3 | 3 | 다시 달리기 전 3-2-1 |
| `graceSec` | 3 | 3 | 다시 달린 뒤 장애물·발동 없는 시간 |
| `answerFeedbackSec` | 1.5 | 1.5 | 고른 결과 문구를 읽을 시간 |
| `type1.habitCheckWindowSec` | 5 | 없음 | 혈당 재기 습관 말풍선 |
| `time.fastForwardTickSimSec` | 0.5 | 없음 | 재확인 빨리감기 단위(보이지 않는 값) |

- 2형 값은 1형의 같은 뜻 값과 맞췄다.
- 불변식 "1단계 제한 시간 > 표시 지연"(10 > 3)은 지켜진다.
