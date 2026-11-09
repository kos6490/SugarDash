# 월드 빈 틀 채우기: 연결 안내 (팀원2)

- 작성: 팀원2(월드), 2026-11-09
- 브랜치: `feat/section2-frame`
- 대상: 구조 담당 팀원(씬 연결, 스크립트 확인)

## 1. 채운 빈 틀

| 빈 틀 | 한 일 |
| --- | --- |
| `World/TrackItemLook` | 물체 종류·음식 id별 프리팹으로 만든다. 장면별 묶음 → 기본 묶음 → 기본 도형(PrimitiveViews) 순서로 찾는다. 프리팹이 하나도 없으면 지금과 똑같이 기본 도형이 나온다 |
| `World/SceneLook` | 장면 id별 맵 배경을 켜고 끄고, 때별 조명·하늘을 바꾼다. 다음 날 첫 장면은 암전 중이므로 섞지 않고 바로 바꾼다. 연결하지 않으면 지금처럼 바닥 색만 바뀐다 |
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

## 3. 씬에 연결할 것 (구조 담당 팀원)

프리팹·배경이 준비되면 `DevRun`(또는 정식 씬)에서 아래를 연결한다. 연결하지 않아도 오류 없이 지금 모습 그대로 돈다.

| 오브젝트 | 칸 | 넣을 것 |
| --- | --- | --- |
| `TrackSpawner` | `Look` | `TrackItemLook`이 붙은 오브젝트 |
| `TrackItemLook` | `Default Prefabs` | 낮은 장애물, 높은 장애물, 레인 막이, 쉬는 곳, 바닥 표시, 음식 id별(`water`, `juice`, `soda`, `apple`, `chicken`, `pizza`, `burger`, 1형 `cracker`) 프리팹 |
| `TrackItemLook` | `Scene Prefabs` | 장면 id(`schoolyard`, `playground`, `outing`, `home`)별로 바꿀 장애물 프리팹 |
| `SceneLook` | `Backdrops` | `SceneBackdropSwitcher`가 붙은 배경 루트 |
| `SceneLook` | `Lighting` | `TimeOfDayLighting`이 붙은 조명 오브젝트(Directional Light, Main Camera 연결) |

- 프리팹은 발밑(바닥)을 기준점으로 만든다. 크기는 레인 폭(2.5)에 맞춘다.
- 높은 장애물 프리팹은 바닥에서 1.6 높이 언저리를 막는 모양이어야 슬라이드로 지나가는 판정과 맞는다(판정은 계산이라 모양과 별개다. 모양이 판정과 어긋나 보이지 않게만 맞춘다).
- `TimeOfDayLighting`의 `Use Scene Lighting As Fallback`이 켜져 있으면, 목록을 채우기 전에는 조명이 바뀌지 않는다.

## 4. 요청 (구조 담당 팀원)

- `SceneLook.Bind`의 `SectionEntered` 구독이 `SetGround(null)`만 부른다. 새 판을 시작해 1구간(장면 없음)으로 돌아가도 맵 배경과 조명은 마지막 장면(예: 집·밤) 그대로 남는다.
  - 고칠 방법 제안: 그 구독에서 `backdrops.Show(null)`, `lighting.Apply(null)`도 부르게 하거나, 그 자리에 `TODO(월드)` 메서드를 두기.
  - `Bind`는 빈 틀에서 바꿀 수 없는 곳이라 직접 고치지 않았다.

## 5. 확인

- 이 변경은 Unity 없이 작성했다. Unity에서 열어 Console에 빨간 오류가 없는지, EditMode 테스트(`SkeletonStateTests`)가 통과하는지 확인한 뒤 push한다.
- 프리팹을 연결하지 않은 상태에서 `DevRun` 시나리오 J(하루 흐름)와 튜토리얼이 지금과 같게 보여야 한다. 레인 띠만 반짝인다.
