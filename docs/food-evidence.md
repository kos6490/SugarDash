# 음식 등급과 출처 발췌

- 상태: 확정(2026-11-09). 등급 방식과 음식별 등급을 정했고, 발췌는 원문과 기계 대조했다. 프로젝트 담당자가 기계 대조로 충분하다고 정했다. 빌드 설정의 `source` 칸에 근거를 적었다.
- 방식: 세 출처로는 음식의 **성질(등급)**만 정한다. 등급마다 게임 수치는 담당자가 게임 표현값(실제 수치 아님)으로 정한다. 음식별 탄수화물 양은 세 출처 밖이라 쓰지 않는다.
- 출처는 [medical-sources.md](medical-sources.md)에 있는 판본만 썼다. 인용은 원문 그대로 옮겼다.

## 확인할 것

1. **쪽 번호**
   - ISPAD 2022 저혈당: PMC 웹 원문(PMC10107518)에서 옮겨 쪽 번호가 없다. 절 제목으로만 적었다.
   - ISPAD 2022 영양: ISPAD 공식 PDF는 조기 공개판("Pediatr Diabetes. 2022;1–25")이다. 아래 "PDF n쪽"은 그 쪽수다. 게재본 1297–1321쪽과의 대응은 확인하지 않았다([section2b-evidence.md](section2b-evidence.md) "확인할 것" 1과 같다).
2. **인용 대조(2026-11-09, 기계 대조)**: 발췌 14개 모두 원문(저혈당 장 PMC HTML, 영양 장 PDF 텍스트)에 글자 그대로 있음을 문자열 대조로 확인했다. 줄바꿈 하이픈·공백 차이만 무시했다. 문장 앞뒤를 자른 곳은 … 또는 문장 일부로 옮겼다. 근거 등급(Grade) 글자와 문맥이 맞는지는 대조하지 않았다.
3. **추론이 들어간 등급**: 치킨·피자·햄버거를 "지방·단백질이 많은 식사"로 본 것, 크래커를 원문의 biscuits로 본 것은 원문에 음식 이름이 직접 나오지 않는다. 담당자가 이 해석을 받아들였다(2026-11-09).

## 1. 등급

| 등급 | 뜻(게임 안에서) | 음식 |
| --- | --- | --- |
| 빠르게 올림 | 먹고 곧 오른다 | 주스(`juice`), 탄산음료(`soda`, 설탕 든 것) |
| 천천히 올림 | 빠르게 올림보다 늦게, 완만하게 오른다 | 사과(`apple`), 크래커(`cracker`, 1형 느린 당) |
| 늦게 오래 올림 | 처음에는 덜 오르고 뒤에 오래 오른다 | 치킨(`chicken`), 피자(`pizza`), 햄버거(`burger`) |
| 당 없음 | 혈당 반영 없음 | 물(`water`) |

- 1형 2단계 "빠른 당" 선택지: 주스, 탄산음료. "느린 당"(`RS_SLOW`) 선택지: 크래커, 사과.
- 두 유형이 같이 쓰는 음식 목록은 8개다(2형 처음 목록 7개 + 크래커). 크래커는 2형 트랙에도 나온다.
- 1형 트랙에는 빠르게 올림·천천히 올림 네 가지만 나온다. 늦게 오래 올림 음식과 물은 2형 트랙에만 나온다([type-tracks.md](type-tracks.md) 6절).
- 초콜릿(지방 때문에 흡수가 늦다, A-4)은 지금 음식 목록에 없어 넣지 않았다. 넣으려면 음식 목록에 더해야 한다.
- 등급 이름은 설계·설정용이다. 화면에 "빠른 당" 같은 말을 쓸지는 화면 문구 검토(roadmap 5.5) 때 정한다.
- 어떤 등급도 "나쁜 음식"으로 부르지 않는다. 늦게 오래 올림 음식을 먹었다고 바로 지친 연출을 넣지 않는다.

## 2. 발췌

### A. ISPAD 2022 저혈당 장 (9 Hypoglycemia Treatment)

A-1. 빠른 당:
> 0.3 g/kg of rapidly acting carbohydrate containing preparations (glucose tablets, orange juice)

A-2. 탄산음료:
> If a soft drink is used for treatment, ensure regular and not sugar‐free/diet forms is used for treatment.

A-3. 주스와 포도당:
> 40 g of carbohydrate in the form of juice results in approximately the same rise as 20 g in the form of glucose tablets.

A-4. 첫 처치로 피할 것:
> complex carbohydrates, or foods containing fat (chocolate) which delay intestinal absorption … should be avoided as the initial treatment of hypoglycemia.

A-5. 우유:
> Whole milk containing 20 g of carbohydrate (435 ml) causes a minimal response with rise of ~1 mmol/L (18 mg/dl).

A-6. 처치 뒤 느린 당:
> a snack (15 g) of slower‐acting carbohydrate, such as bread, milk, biscuits, or fruit should be consumed.

A-7. 다시 재기(2 Executive Summary and Recommendations, Grade E):
> Following initial hypoglycemia treatment, blood glucose should be retested in 15 min.

### B. ISPAD 2022 영양 장

B-1. 혈당지수 구분(9 GLYCEMIC INDEX AND GLYCEMIC LOAD, PDF 10쪽):
> Suggested cut points for classification are high (GI ≥70), medium (GI 56–69), and low (GI ≤55) GI values.

B-2. 낮은 혈당지수 음식(같은 절, PDF 10쪽):
> Low GI food sources include whole grain breads, pasta, temperate fruits and dairy products.

B-3. 지방·단백질(10.1 Fat and protein, PDF 11쪽):
> meals high in either protein or fat increase delayed hyperglycemia (up to 3–6 h after the meal) and reduce the early (1–2 h) postprandial rise.

B-4. 단 음료(PDF 5쪽):
> Large quantities of sugary beverages cause high postprandial glucose peaks that are difficult to adequately cover with insulin.

B-5. 물(PDF 7쪽, 음주 부분):
> non-alcoholic sugar-free drinks (including water)

B-6. 물(13.4 Fluid, PDF 15쪽, 운동 부분):
> Water is suitable for most activities up to 60 min duration

B-7. 2형(TYPE 2 DIABETES IN YOUNG PEOPLE, PDF 15쪽). 근거 기록으로만 두고 게임 문구·규칙에는 쓰지 않는다(음식 죄책감):
> Simply eliminating sugary beverages such as soft drinks and fruit juices can accomplish improvement in blood glucose and weight.

## 3. 등급별 근거

| 음식 | 등급 | 근거 |
| --- | --- | --- |
| 주스 | 빠르게 올림 | A-1, A-3 |
| 탄산음료 | 빠르게 올림 | A-2, B-4 |
| 사과 | 천천히 올림 | A-6(fruit), B-2(temperate fruits) |
| 크래커 | 천천히 올림 | A-6(biscuits, 해석) |
| 치킨, 피자, 햄버거 | 늦게 오래 올림 | B-3(해석) |
| 물 | 당 없음 | B-5, B-6. "물은 혈당을 올리지 않는다"는 문장은 없다 |
