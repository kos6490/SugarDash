# 혈당 기준값과 출처 발췌

- 상태: 확정(2026-11-09). 발췌는 원문과 기계 대조했고, 프로젝트 담당자가 기계 대조로 충분하다고 정했다. 정한 값은 빌드 설정(`game-config.json`, `game-config.type2.json`)에 넣었다.
- 범위: 출처에 근거가 있는 혈당 기준값만. 시간 압축, 지연, 제한 시간, 점수 가중치, 등급별 상승량 같은 게임 표현값은 따로 정한다.
- 출처는 [medical-sources.md](medical-sources.md)에 있는 판본만 썼다. 인용은 원문 그대로 옮겼다. 단위는 mg/dL.

## 확인할 것

1. **쪽 번호**: ISPAD 2022 저혈당(PMC10107518)과 ISPAD 2024 혈당 목표(PMC11854972)는 PMC 웹 원문에서 옮겨 쪽 번호가 없다. 절 제목으로만 적었다.
2. **인용 대조(2026-11-09, 기계 대조)**: 발췌 11개 모두 PMC 원문 HTML에 글자 그대로 있음을 문자열 대조로 확인했다. A-1처럼 문장 일부만 옮긴 것이 있다(원문은 "…initiating treatment for hypoglycemia"로 이어진다). 근거 등급(Grade) 글자가 그 문장에 붙은 것인지는 대조하지 않았다.
3. **2형 적용**: ISPAD 2024 혈당 목표는 1형·2형을 나누지 않고 "children and adolescents with diabetes"에 적용한다. 근거 연구는 주로 1형이다. 2형에도 같은 값을 쓰기로 했다. ISPAD 2024 2형 장에 아동 2형 목표가 따로 있는지는 아직 보지 않았다.
4. **해석이 들어간 값**: 탈락 기준(54, 250)은 출처에 "탈락"에 해당하는 개념이 없어 짝지은 해석이다.

## 1. 정한 값

| 설정 | 값 | 근거 | 성격 |
| --- | --- | --- | --- |
| `glucose.hypoThreshold` | 70 | A-1 | 출처 값 그대로 |
| `glucose.normalRangeMin` | 70 | B-1 | 출처 값 그대로 |
| `glucose.normalRangeMax` | 180 | B-1 | 출처 값 그대로. 혈당 유지 점수의 위쪽 경계 |
| `glucose.hyperThreshold` | 250 | B-2 | 출처 값. 180은 식사 뒤에도 흔히 넘어 알리기 단계가 너무 잦으므로 더 높은 단계(250)를 알리기 기준으로 썼다 |
| `elimination.glucoseLow` | 54 | A-2 | 해석: 탈락은 어른이 도와주러 오는 장면이라 "심한 저혈당" 기준과 짝지었다. 중증 저혈당은 숫자 기준 없이 "다른 사람의 도움이 필요한 상황"으로 정의된다(A-3) |
| `elimination.glucoseHigh` | 250보다 큰 값(게임 표현값) | B-2 | 출처에 250보다 높은 단계가 없다. 설정 규칙 `hyperThreshold < glucoseHigh`에 맞춰, 고혈당 알리기를 놓친 뒤 탈락까지 여유를 주는 게임 표현값으로 정한다(2026-11-09). 정확한 값은 게임 표현값 묶음에서 정한다 |
| 적정량 처치의 총 상승 | 63 | A-4(54~72) | 범위 가운데. 조금은 54 미만, 많이는 72 초과로 정한다. 1회분 상승과 양 배수는 게임 표현값에서 정한다 |
| `time.recheckIntervalRealMin` | 15 | A-5 | 이미 설정에 있다 |

- 두 유형이 같은 값을 쓴다(확인할 것 3).

## 2. 발췌

### A. ISPAD 2022 저혈당 장

A-1. 처치 시작 기준(2 Executive Summary and Recommendations, Grade E):
> Glucose value <3.9 mmol/L (70 mg/dl) is used as the clinical alert or threshold value for initiating treatment

A-2. 심한 저혈당(같은 곳, Grade E):
> Glucose value <3.0 mmol/L (54 mg/dl) is defined as clinically important or serious hypoglycemia

A-3. 중증 저혈당(4.1 Definition, Table 1):
> No specific glucose threshold

> Requires third-party assistance to administer carbohydrates, glucagon, or intravenous dextrose

A-4. 처치로 올릴 양(2 Executive Summary and Recommendations, Grade C):
> Treatment of hypoglycemia should increase blood glucose level by ~3 to 4 mmol/L (54 to 72 mg/dl).

A-5. 다시 재기(같은 곳, Grade E):
> Following initial hypoglycemia treatment, blood glucose should be retested in 15 min.

### B. ISPAD 2024 혈당 목표

B-1. 목표 범위(Grade B):
> >70% between 3.9 and 10 mmol/L (70–180 mg/dL)

B-2. 높은 쪽(Grade B):
> <25% >10 mmol/L (180 mg/dL)

> <5% >13.9 mmol/L (250 mg/dL)

B-3. 낮은 쪽(Grade B):
> <4% <3.9 mmol/L (70 mg/dL)

> <1% <3.0 mmol/L (54 mg/dL)

## 3. 2형 달리기 하강 (2026-11-09)

- 정함: 2형도 달리는 동안 혈당이 더 내려간다(`glucose.runDecayPerSec` 0.7, [game-values.md](game-values.md) 2절). 2형 move(쉬는 곳)는 이 규칙 위에 서 있다(쉬면 달리기 하강이 멈춘다).
- 방향의 근거는 꾸준한 운동의 장기 효과(C-1)다. 운동하는 순간의 혈당 변화는 2형 아동 근거가 적다(C-2). 그래서 달리는 순간 내려가는 것은 시간을 줄인 게임 표현이다.
- 튜토리얼과 화면 문구에는 "쉬면 혈당이 덜 내려간다"처럼 효과를 사실로 말하지 않는다. 쉬는 곳 문구는 `여기로 가면 잠깐 쉴 수 있어요`.

C-1. ISPAD 2024 2형 아동 장(PMC11854986, Increasing Physical Activity and Limiting Sedentary Time):
> Regular exercise improves blood glucose levels, reduces cardiovascular risk factors, contributes to weight loss, and improved sense of well-being

C-2. ISPAD 2022 운동 장(5.4 Reasons for dysglycemia during exercise in youth with T1D, [section2b-evidence.md](section2b-evidence.md) A-5):
> For youth with T2D, there is little evidence for the influence of duration, type, or intensity of exercise on acute glycemic excursions or glucose time‐in‐range.
