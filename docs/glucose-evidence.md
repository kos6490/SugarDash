# 혈당 기준값과 출처 발췌 (담당자 확인 전 초안)

- 상태: 값은 정했다(2026-11-09). 아래 발췌는 프로젝트 담당자가 원문과 대조하기 전이다. 대조가 끝나기 전에는 빌드 설정(`game-config.json`, `game-config.type2.json`)에 넣지 않는다.
- 범위: 출처에 근거가 있는 혈당 기준값만. 시간 압축, 지연, 제한 시간, 점수 가중치, 등급별 상승량 같은 게임 표현값은 따로 정한다.
- 출처는 [medical-sources.md](medical-sources.md)에 있는 판본만 썼다. 인용은 원문 그대로 옮겼다. 단위는 mg/dL.

## 확인할 것

1. **쪽 번호**: ISPAD 2022 저혈당(PMC10107518)과 ISPAD 2024 혈당 목표(PMC11854972)는 PMC 웹 원문에서 옮겨 쪽 번호가 없다. 절 제목으로만 적었다.
2. **인용 대조**: 웹 요약 도구로 읽었다. 원문과 글자 단위로 대조한다.
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
