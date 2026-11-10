# 3구간(복합 상황)

- 범위: 3-A 겹친 조건(3절), 3-B 1형 "빈 간식 가방"(5절). 둘 다 구현함.
- 수치: 3구간 덮어쓰기 값은 게임 표현값이다([game-values.md](game-values.md) 6절). 방향은 2-B 근거([section2b-evidence.md](section2b-evidence.md))를 그대로 쓴다.
- 바탕: [section2.md](section2.md), [type-tracks.md](type-tracks.md) 8절

## 1. 목적

1구간(신호 인식) → 2구간(조건별 판단) → 3구간(복합 상황). 3구간에서는 2구간에서 맵마다 하나씩 겪은 조건이 겹쳐 나온다. 한 판 안에서 자연스럽게 어려워진다.

### 하지 않을 것
- 새 맵 그래픽. 2구간의 맵 4개(운동장, 놀이터, 나들이, 집)를 다시 쓴다.
- 3-B 말고는 새 판정 규칙이 없다. 점수, 탈락 규칙은 그대로다.
- 2형 수면 습관 장면. 근거가 ADA 5.57뿐이고 소아 전용 권고가 아니다.
- 운동 구간을 맵 일부(체육 시간)로 나누기. 2구간 운동장 조건과 많이 겹친다.

## 2. 흐름

```
1구간 signal ──(lengthM)──→ 2구간 condition: 하루 1번 ──→ 3구간 complex: 하루가 끝없이 되풀이
                            아침·운동장 → 오후·놀이터 → 저녁·나들이 → 밤·집
```

- 2구간은 하루를 `days`번(정한 값 1) 돌고 끝난다. 이어서 다음 날 아침부터 3구간이다.
- 날짜(`DayIndex`)는 구간이 바뀌어도 이어진다. 그래서 2구간 집 장면이 끝나면 수면 전환(암전)을 지나 3구간 아침이 된다.
- 구간 진입은 지금처럼 `SectionEntered`(`complex`)로, 장면 진입은 `SceneEntered`로 기록한다.

### 설정 규칙
- 장면이 있는 구간에 `days`(1 이상의 정수)가 있으면 그 하루를 그만큼 돌고 다음 구간으로 간다. 길이는 장면 길이 합 × `days`다.
- 마지막 구간에는 `days`를 두지 않는다(끝없이 되풀이). 마지막이 아닌 장면 구간에는 `days`가 있어야 한다.
- 장면이 있는 구간에는 지금처럼 `lengthM`을 두지 않는다.

## 3. 겹친 조건 (3-A)

| 맵 | 1형 | 2형 |
| --- | --- | --- |
| 운동장 | 운동 하강(2구간과 같음) + 급하강 확률이 2배. 혈당이 빨리, 갑자기 떨어진다. 운동 전 혈당 재기 순간은 그대로 | 음식 줄이 자주 나온다. 달리기 하강과 먹기 사이에서 고른다 |
| 놀이터 | 트랙 간식이 드물고 느린 당(크래커·사과)만 나온다 | 쉬는 곳과 음식 줄이 둘 다 자주 나온다. choose와 move를 동시에 판단한다 |
| 나들이 | 트랙 간식이 자주 나온다. 많이 먹으면 고혈당 알리기로 이어진다 | 외식 음식(치킨·피자·햄버거)과 물 위주로 자주 나온다. 높은 쪽 신호가 잦다 |
| 집 | 2구간과 같다(자기 전 혈당 재기 순간) | 2구간과 같다 |

- 모두 이미 있는 장면 덮어쓰기 칸(`runDecayPerSec`, `suddenDropChancePerSec`, `trackSnackIds`, `trackSnackInterval*`, `foodRowInterval*`, `foodRowIds`, `restInterval*`)으로 정한다. 새 칸은 구간의 `days` 하나다.
- 어떤 조건도 음식을 "나쁜" 것으로 보이게 하지 않는다. 나들이에서 많이 먹었다고 바로 지치는 연출은 없다(혈당이 기준을 일정 시간 넘겨야 신호가 나온다).
- 불변식 18(최악의 경우 회복)은 모든 구간의 장면 덮어쓰기 중 가장 큰 달리기 하강으로 본다(지금 그대로).

## 4. 모듈

| 모듈 | 바뀌는 것 |
| --- | --- |
| `SectionConfig`(Domain) | `Days`(장면 구간이 하루를 도는 횟수, 마지막 구간이면 null) |
| `GameConfigValidator`(Domain) | 2절 설정 규칙 |
| `ConfigProvider`·DTO(Infrastructure) | `sections[].days` |
| `RunController`(App) | 장면 구간이 `days`를 다 돌면 다음 구간으로. 날짜는 이어진다 |
| 설정 4개 | `condition`에 `days: 1`, `complex` 구간(장면 4개, 3절 덮어쓰기) |

화면은 바꾸지 않는다. 수면 전환이 하루가 넘어갈 때 이미 어두워졌다 밝아진다.

## 5. 3-B 1형 "빈 간식 가방" (구현함)

- 언제: 1형 3구간에서 저혈당이 발동할 때 장면별 확률(`overrides.emptyBagChance`)로 정한다. 운동장 0.5, 다른 장면은 없음(0). 2형 설정에는 둘 수 없다.
- 2단계 화면: 제목 `가방에 빠른 당이 없어요. 어떻게 할까요?`. 선택지는 느린 당 하나(크래커·사과 중 무작위), `계속 달리기`, 맨 아래 `어른에게 알리기`. 빠른 당과 `먹을 게 없어요`는 뺀다(`어른에게 알리기`와 뜻이 겹치지 않게).
- 판정: `어른에게 알리기`가 정답이다. 어른이 빠른 당(`hypo.guardianSnackId`)을 건네고 3단계(양 고르기)로 간다. 느린 당은 `RS_SLOW`, 계속 달리기는 `RS_KEEP_RUNNING`, 시간 초과는 `RS_TIMEOUT`(지금과 같다). 1·3·4단계는 그대로다.
- 사건 뒤: 다시 달리기 전 3-2-1 동안 `다음엔 빠른 당을 챙겨요`가 보인다.
- 기록: `HypoEventTriggered`에 `emptyBag: true`(빈 간식 가방일 때만, [log-format.md](log-format.md)).

### 5.1 근거 (원문 기계 대조함)

ISPAD 2022 운동 장(PMC10107219, 2 Executive Summary and Recommendations, Grade E):
> During all forms of physical exercise high glycemic index carbohydrates should be available to prevent and treat hypoglycemia.

ISPAD 2022 저혈당 장(PMC10107518, 2 Executive Summary and Recommendations, Grade E):
> Particular attention should be given to training children, parents, school teachers, and other caregivers to recognize the early warning signs of hypoglycemia and treat low blood glucose immediately and appropriately.

ISPAD 2022 저혈당 장(4 Definition):
> However, as young children require assistance to correct even mild hypoglycemia,

- 해석: "빠른 당이 없으면 어른에게 알린다"를 그대로 적은 문장은 없다. 빠른 당은 늘 있어야 하고(운동 장), 아이 주변 어른이 처치하도록 교육받으며, 어린 아이는 가벼운 저혈당에도 도움이 필요하다(저혈당 장)는 내용을 합친 해석이다. 운동장에서만 나오게 한 것은 운동 장 근거와 맞추기 위해서다.
