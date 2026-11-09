# 장애물 패턴 만들기 (팀원2·월드)

장애물은 미리 만든 **패턴**(묶음) 단위로 트랙에 놓인다. 패턴 목록은 에셋 하나(`Assets/_Project/Art/World/TrackPatterns.asset`)에 있고, 월드 담당 팀원이 Unity Inspector에서 만들고 고친다.

## 1. 왜 패턴인가

예전에는 18m마다 장애물 하나를 무작위로 놓았다. 그래서 속도가 붙어도 하나씩 피하면 되어 쉬웠다. 패턴은 "어느 레인으로 가서 무엇을 할지" 고를 거리를 만든다.

- 고르기만 무작위다. 고른 패턴은 만든 모양 그대로 나온다.
- 저혈당 1단계(측정기를 눌러야 하는 동안, 1형)에는 **쉬운 패턴**만 새로 나온다. 아이가 측정을 놓쳤을 때 조작이 바빠서가 아니라 판단 때문이어야 하기 때문이다.
- 두 유형, 모든 구간이 같은 패턴 묶음을 쓴다.

## 2. 패턴 칸

Project 창에서 `Assets > _Project > Art > World > TrackPatterns`를 클릭하면 Inspector에 `Patterns` 목록이 보인다. 목록 오른쪽 아래 `+`로 패턴을 더하고, `-`로 뺀다. `+`는 빈 패턴이 아니라 **맨 아래 패턴의 복사본**을 만든다. 새 패턴은 `Id`부터 바꾸고 나머지 칸도 모두 다시 정한다(그대로 두면 `id가 다른 패턴과 겹친다` 경고가 뜬다).

| 칸 | 뜻 |
| --- | --- |
| `Id` | 패턴 이름. 영어 소문자와 `_`(예: `two_block`). 다른 패턴과 겹치면 안 된다 |
| `Rows` | 줄 목록. 위에서부터 차례로 트랙에 놓인다 |
| `Rows` 안의 `Left`, `Center`, `Right` | 화면 기준 왼쪽, 가운데, 오른쪽 레인에 놓을 것. `Empty`(비어 있음), `Low`(낮은 장애물, 점프), `High`(높은 장애물, 슬라이드), `Block`(레인 막이, 옆으로 피함) |
| `Rows` 안의 `Gap Before M` | 앞 줄과의 거리(m). 첫 줄은 무시한다 |
| `Easy` | 쉬운 패턴. 줄이 하나여야 한다. 시작 직후와 저혈당 1단계에 쓴다 |
| `Start Distance M` | 한 판에서 이만큼(m) 달린 뒤부터 나온다. 0이면 처음부터 |
| `Weight` | 뽑힐 비율. 2면 1인 패턴보다 두 배 자주 나온다. 0이면 나오지 않는다 |
| `Mirror` | 켜면 좌우를 뒤집은 모양도 같은 비율로 나온다. 왼쪽용·오른쪽용을 따로 만들지 않아도 된다 |

- 패턴과 패턴 사이는 18m(`Track`의 `Obstacle Spacing`)이다.
- 아래쪽 `Timing`(최고 속도, 점프·레인 이동 시간, 반응 시간)은 **바꾸지 않는다**. 지나갈 수 있는지 계산하는 기준이고 구조 담당 팀원이 정한다.

## 3. 꼭 지킬 규칙 (자동으로 확인함)

1. 줄마다 지나갈 레인이 하나는 있어야 한다. 세 레인 모두 `Low`(점프)나 모두 `High`(슬라이드)는 된다. 세 레인 모두 `Block`은 안 된다.
2. 줄 간격은 최고 속도에서도 다음 동작을 할 수 있을 만큼 넓어야 한다. 기준(최고 속도 20 기준):
   - 같은 레인에서 `Low` 다음 `Low`(착지한 뒤 다시 점프): 18m 이상
   - 레인 두 칸 옮기기(왼쪽 → 오른쪽): 10m 이상
   - 레인 한 칸 옮기기: 7m 이상
   - 그 밖(예: `Low` 다음 `High`, 슬라이드가 점프를 바로 끊는다): 4m 이상
   - 지나갈 수 있는 레인 중 **하나라도** 이어지면 된다.
3. `Easy` 패턴은 줄이 하나다.
4. `Easy`이고 `Start Distance M`이 0이며 `Weight`가 0보다 큰 패턴이 하나는 있어야 한다.
5. `Id`는 비어 있거나 겹치면 안 된다.

규칙을 어기면 Inspector에서 값을 바꾸는 즉시 Console(`Window > General > Console`)에 노란 경고가 뜬다. 경고에 패턴 `Id`와 몇 번째 줄이 문제인지 나온다. 경고가 남은 채로 Play하면 패턴을 쓰지 않고 예전처럼 하나씩 놓는다(Console에 빨간 오류).

## 4. 처음 들어 있는 패턴

| Id | 모양 | 비고 |
| --- | --- | --- |
| `single_low`, `single_high`, `single_block` | 왼쪽 레인에 하나, `Mirror` 켬, `Weight` 2 | 예전 동작과 같은 비율이 되게 했다 |
| `single_low_center`, `single_high_center`, `single_block_center` | 가운데 레인에 하나, `Weight` 1 | |

모두 `Easy`, `Start Distance M` 0이다. 이 패턴은 지우지 말고 `Weight`만 조절한다(규칙 4).

## 5. 만들어 볼 패턴 예시

| 예시 | 줄 | `Easy` |
| --- | --- | --- |
| 모두 점프 | `Low Low Low` | 예 |
| 모두 슬라이드 | `High High High` | 예 |
| 두 레인 막이 | `Block Block Empty`, `Mirror` | 아니오 |
| 골라 지나가기 | `Block Low High`, `Mirror` | 아니오 |
| 점프 뒤 슬라이드 | `Block Low Block`, 다음 줄 `Block High Block`(4m 이상) | 아니오 |
| 지그재그 | `Empty Block Block`, 다음 줄 `Block Block Empty`(10m 이상) | 아니오 |

- 어려운 패턴일수록 `Start Distance M`을 크게 해 판 뒤쪽에 나오게 한다. 예: 두 레인 막이 200, 지그재그 500.
- 초등학생이 하는 게임이다. 한 패턴에 동작 두 번 정도가 어려움의 상한이다.

## 6. 확인하고 push하기

1. Inspector에서 고친 뒤 Cmd+S로 저장한다(에셋도 저장해야 파일에 남는다).
2. Console에 노란 경고가 없는지 본다.
3. `DevRun`을 열고 Play해서 직접 지나가 본다. 시작 직후에는 쉬운 패턴만, 달린 거리가 늘면 새 패턴이 섞여 나온다.
4. 테스트 창(`Window > General > Test Runner`)에서 `EditMode` 탭 → `TrackPatternTests`를 펼쳐 `ProjectAsset_PassesRules_AndSpacing`을 선택하고 `Run Selected`. 초록색이면 된다.
5. `TrackPatterns.asset` 파일만 커밋해 push한다. `DevRun` 씬은 고치지 않는다(team-rules 2절).
