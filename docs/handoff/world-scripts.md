# 월드 빈 틀 채우기: 연결 안내 (팀원2)

- 작성: 팀원2(월드), 2026-11-09 (개발용 배경·기본 조명 추가 2026-11-13)
- 브랜치: `feat/section2-frame`
- 대상: 구조 담당 팀원(씬 연결, 스크립트 확인)

## 1. 채운 빈 틀

| 빈 틀 | 한 일 |
| --- | --- |
| `World/TrackItemLook` | 물체 종류·음식 id별 프리팹으로 만든다. 장면별 묶음 → 기본 묶음 → 기본 도형(PrimitiveViews) 순서로 찾는다. 프리팹이 하나도 없으면 지금과 똑같이 기본 도형이 나온다 |
| `World/SceneLook` | 장면 id별 맵 배경을 켜고 끄고, 때별 조명·하늘을 바꾼다. 다음 날 첫 장면은 암전 중이므로 섞지 않고 바로 바꾼다. `Backdrops`·`Lighting`을 연결하지 않아도 실행 중에 개발용 배경(기본 도형)과 기본 때별 조명을 스스로 만든다(6절) |
| `World/TutorialLaneHighlightView` | 띠가 보이는 동안 투명도가 흐림 ↔ 진함으로 오간다(반짝임). 프리팹 띠도 같은 색으로 반짝인다 |

빈 틀의 `public` 이름·인자, 구독 이벤트, 보내는 명령은 바꾸지 않았다. 추가한 것은 `private` 필드·메서드와 `[SerializeField]` 필드뿐이다.

## 2. 새로 만든 영역 파일 (team-rules 2.2절)

모두 `Presentation/World/` 안, 연출만, Domain·App 타입 참조 없음.

| 파일 | 하는 일 | 왜 따로 만들었나 |
| --- | --- | --- |
| `WorldKeyedObject` | Inspector 목록 한 줄(문자열 키 → 오브젝트)과 찾기 | 음식 프리팹 표, 장면별 묶음, 맵 배경 표가 같이 쓴다 |
| `TrackItemPrefabSet` (+ `SceneTrackItemSet`) | 물체 종류별 프리팹 묶음, 장면별 묶음 | Inspector에 표로 보이려면 직렬화 클래스가 필요하다 |
| `TrackItemViewFactory` | 빈 부모 아래 프리팹을 바닥 높이로 내려 만들고 콜라이더를 지운다 | TrackSpawner가 모습을 기본 도형 높이(`PrimitiveViews.BaseHeight`)에 두므로, 발밑 기준점 프리팹이 공중에 뜨지 않게 보정한다 |
| `TrackItemIdle` | 음식·쉬는 곳 제자리 회전·흔들림 | 프리팹에 붙이는 연출 컴포넌트 |
| `WorldMotion` | 흔들림·회전·깜빡임 계산(순수 함수) | `TrackItemIdle`과 `TutorialLaneHighlightView`가 같이 쓴다 |
| `LightingPreset` | 때 하나의 조명·하늘 값과 섞기 | Inspector 목록용 값 묶음 |
| `TimeOfDayLighting` | 해·주변광·하늘·카메라 배경·안개를 때별로 섞어 바꾼다 | 조명 담당 오브젝트에 붙이는 컴포넌트. 바꾼 RenderSettings는 없어질 때 되돌린다 |
| `SceneBackdropSwitcher` | 장면 id별 배경 루트 하나만 켠다 | 배경 오브젝트를 묶는 루트에 붙이는 컴포넌트 |
| `PlaceholderShape` | 기본 도형 하나(종류, 위치, 크기, 회전, 색) | 개발용 배경 목록 한 줄. Inspector에서 고칠 수 있다 |
| `PlaceholderBackdropRecipes` (+ `PlaceholderScene`) | 장면 4개의 기본 도형 목록(학교·골대·울타리, 미끄럼틀·그네·모래밭, 언덕·개울·돗자리, 집·가로등) | 배경 내용(데이터)과 만드는 코드를 나눈다 |
| `PlaceholderShapeFactory` | 기본 도형을 만들고 콜라이더를 지운다. 그림자는 드리우지 않는다 | 만드는 방법을 한곳에 둔다 |
| `ColorMaterialCache` | 색마다 머티리얼 하나만 만들고 없어질 때 지운다 | 같은 색 도형이 머티리얼을 나눠 써서 드로우콜·메모리를 줄인다 |
| `PlaceholderBackdrops` | 장면 id별 개발용 배경을 처음 볼 때 만들고 하나만 켠다. 장면 없는 구간은 `street` 배경 | 모델이 나오기 전까지 맵 구분을 Play에서 보이게 한다 |
| `BackdropTileStrip` | 배경 한 칸을 앞뒤로 이어 붙여 달린 거리만큼 뒤로 흘려보낸다 | 트랙은 물체가 다가오는 방식이라 배경도 같은 거리만큼 움직여야 달리는 느낌이 난다 |

## 3. 씬에 연결할 것 (구조 담당 팀원)

프리팹·배경이 준비되면 `DevRun`(또는 정식 씬)에서 아래를 연결한다. 연결하지 않아도 오류 없이 지금 모습 그대로 돈다.

| 오브젝트 | 칸 | 넣을 것 |
| --- | --- | --- |
| `TrackSpawner` | `Look` | `TrackItemLook`이 붙은 오브젝트 |
| `TrackItemLook` | `Default Prefabs` | 낮은 장애물, 높은 장애물, 레인 막이, 쉬는 곳, 바닥 표시, 음식 id별(`water`, `juice`, `soda`, `apple`, `chicken`, `pizza`, `burger`, 1형 `cracker`) 프리팹 |
| `TrackItemLook` | `Scene Prefabs` | 장면 id(`schoolyard`, `playground`, `outing`, `home`)별로 바꿀 장애물 프리팹 |
| `SceneLook` | `Backdrops` | `SceneBackdropSwitcher`가 붙은 배경 루트 |
| `SceneLook` | `Lighting` | `TimeOfDayLighting`이 붙은 조명 오브젝트(Directional Light, Main Camera 연결) |
| `SceneLook` | `Track` | `Track` 오브젝트(`TrackSpawner`). 연결해야 개발용 배경이 달리는 만큼 뒤로 흘러간다 |

- 프리팹은 발밑(바닥)을 기준점으로 만든다. 크기는 레인 폭(2.5)에 맞춘다.
- 높은 장애물 프리팹은 바닥에서 1.6 높이 언저리를 막는 모양이어야 슬라이드로 지나가는 판정과 맞는다(판정은 계산이라 모양과 별개다. 모양이 판정과 어긋나 보이지 않게만 맞춘다).
- `TimeOfDayLighting`은 안개를 처음부터 켜 두고 색·농도만 바꾼다(낮은 옅게). 중간에 안개를 켜면 에디터에서 셰이더를 새로 준비하는 동안 물체가 하늘색으로 번쩍일 수 있다.
- `TimeOfDayLighting`의 `Presets`에는 아침·오후·저녁·밤 기본값이 들어 있다. `Sun`이 비어 있고 씬에 Sun Source도 없으면(지금 `DevRun`) 해는 건드리지 않고 주변광·안개만 바뀐다. Directional Light를 `Sun`에 넣으면 해 각도·밝기·색도 바뀐다.

## 4. 요청 (구조 담당 팀원)

- `SceneLook.Bind`의 `SectionEntered` 구독이 `SetGround(null)`만 부른다. 새 판을 시작해 1구간(장면 없음)으로 돌아가도 맵 배경과 조명은 마지막 장면(예: 집·밤) 그대로 남는다.
  - 고칠 방법 제안: 그 구독에서 `backdrops.Show(null)`, `lighting.Apply(null)`도 부르게 하거나, 그 자리에 `TODO(월드)` 메서드를 두기.
  - `Bind`는 빈 틀에서 바꿀 수 없는 곳이라 직접 고치지 않았다.
  - 반영함(구조): 구간에 들어갈 때 바닥 색과 함께 배경(`backdrops.Show(null)`)과 조명(`lighting.ApplyImmediate(null)`)도 장면 없는 모습으로 되돌린다. 새 판(거리 0)이면 하루 번호도 처음으로 되돌린다.
  - 월드: 개발용 배경·기본 조명도 같은 자리에서 되돌리도록 맞추고, 바닥 색을 보고 알아채던 임시 우회(`LateUpdate`)는 지웠다.

## 5. 확인

- 이 변경은 Unity 없이 작성했다. Unity에서 열어 Console에 빨간 오류가 없는지, EditMode 테스트(`SkeletonStateTests`)가 통과하는지 확인한 뒤 push한다.
- 프리팹을 연결하지 않은 상태에서 `DevRun` 시나리오 J(하루 흐름)와 튜토리얼이 지금과 같게 보여야 한다. 레인 띠만 반짝인다.

## 6. 개발용 배경·기본 조명 (2026-11-13)

씬을 고치지 않고 맵이 Play에서 보이게 하는 임시 연출이다. 모델·조명을 연결하면 저절로 꺼진다.

- 시작할 때부터(튜토리얼, 1구간) 장면 없는 배경 `street`(가로수, 보도, 생울타리, 낮은 건물)과 기본 낮 조명을 보여 준다. 2구간 장면에 들어가면 장면 배경으로 바뀐다.
- 배경은 120m 한 칸을 3칸 이어 붙이고, `SceneLook`의 `Track`이 연결되어 있으면 `TrackSpawner.Distance`만큼 뒤로 흘려보낸다. 연결 전에는 멈춰 있다(3절 표).

- `SceneLook`의 `Backdrops`가 비어 있고 `Show Placeholder Backdrops`가 켜져 있으면, 처음 장면에 들어갈 때 자식 `Placeholder Backdrops`를 만들어 장면별 배경을 보여 준다.
- `SceneLook`의 `Lighting`이 비어 있고 `Add Lighting If Missing`이 켜져 있으면, 처음 장면에 들어갈 때 같은 오브젝트에 `TimeOfDayLighting`을 붙여 기본 때별 조명을 쓴다.
- 배경 도형은 모두 트랙 바깥(가운데에서 좌우 7 이상)에 둔다. 콜라이더가 없어 판정에 관계없다.
- 끄려면 두 칸의 체크를 끈다. 지금과 똑같이 바닥 색만 바뀐다.
