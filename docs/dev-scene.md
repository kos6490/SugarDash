# 개발용 실행 씬 구성 (Unity 처음 쓰는 사람용)

러너 화면 없이 혈당바, 측정기 버튼, 4단계 선택 패널만으로 코어 루프를 에디터에서 확인하는 씬을 만든다. 개발용 임시 설정(실제 수치 아님)으로 실행한다. 단축키는 macOS 기준이다(Windows는 Cmd 대신 Ctrl).

## 0. 화면 구성과 기본 조작

Unity 에디터 기본 배치:

| 창 | 위치 | 하는 일 |
| --- | --- | --- |
| Hierarchy | 왼쪽 | 지금 열린 씬에 있는 오브젝트 목록. 들여쓰기된 것은 위 오브젝트의 자식이다 |
| Scene / Game | 가운데 | Scene은 편집용 화면, Game은 실행했을 때 보이는 화면 |
| Inspector | 오른쪽 | Hierarchy나 Project에서 선택한 것의 속성. 컴포넌트를 붙이고 값을 바꾸는 곳 |
| Project | 아래 | `Assets` 폴더 안의 파일 목록(디스크의 실제 파일) |
| Console | Project 옆 탭 | 로그와 에러. 안 보이면 `Window > General > Console` (Cmd+Shift+C) |

자주 쓰는 조작:
- **오브젝트 만들기**: Hierarchy 빈 곳(또는 부모로 삼을 오브젝트 위)에서 오른쪽 클릭 → 메뉴에서 고른다. 어떤 오브젝트 위에서 오른쪽 클릭하면 그 오브젝트의 **자식**으로 생긴다.
- **이름 바꾸기**: 오브젝트를 선택하고 Enter를 누르거나, Inspector 맨 위 이름 칸을 고친다.
- **컴포넌트 붙이기**: 오브젝트를 선택 → Inspector 맨 아래 `Add Component` 버튼 → 검색창에 이름 입력(예: `HudPresenter`) → 목록에서 클릭.
- **필드 연결**: Inspector의 빈 칸(예: `Glucose Bar  None (Slider)`)에 Hierarchy의 오브젝트나 Project의 파일을 끌어다 놓는다. 칸 오른쪽 작은 동그라미(⊙)를 눌러 목록에서 골라도 된다. 필드 이름은 띄어쓰기된 형태로 보인다(`glucoseBar` → `Glucose Bar`).
- **저장**: Cmd+S. 씬 이름 옆에 `*`가 있으면 저장 안 된 변경이 있다는 뜻이다.
- **주의**: 실행(Play) 중에 바꾼 값은 실행을 멈추면 모두 되돌아간다. 편집은 반드시 실행을 멈춘 상태에서 한다.

## 1. 씬 만들고 저장하기

`File > New Scene`으로 만든 씬은 **저장하기 전까지 파일이 없다**. Hierarchy 맨 위에 `Untitled`라고 보이면 아직 저장 안 된 씬이다.

1. 메뉴 `File > New Scene` (Cmd+N).
2. 뜨는 `New Scene` 창 왼쪽 목록에서 **`Basic (URP)`**를 고르고 오른쪽 아래 `Create`를 누른다.
   - 이전 씬을 저장할지 물으면 `Don't Save`를 누른다(이미 두 번 만든 빈 씬은 버려도 된다).
3. 메뉴 `File > Save As...` (Cmd+Shift+S).
4. 저장 창에서 프로젝트의 `Assets` → `Scenes` 폴더로 이동하고, 이름을 `DevRun`으로 적은 뒤 `Save`.
5. Project 창에서 `Assets > Scenes`를 열어 `DevRun` 파일이 보이는지 확인한다. Hierarchy 맨 위 이름도 `DevRun`으로 바뀐다.

## 2. 실행 화면 비율 맞추기 (세로 화면)

1. 가운데 위쪽 `Game` 탭을 누른다.
2. Game 탭 바로 아래 왼쪽의 비율 목록(처음엔 `Free Aspect`)을 누른다.
3. 목록 맨 아래 `+`를 누른다 → Type은 `Fixed Resolution`, Width `1080`, Height `1920`, Label은 `Portrait` → `OK`.
4. 목록에서 방금 만든 `Portrait`를 고른다.
5. 다시 `Scene` 탭으로 돌아간다.

## 3. Canvas (UI를 올려놓는 판)

1. Hierarchy 빈 곳 오른쪽 클릭 → `UI > Canvas`. `Canvas`와 `EventSystem` 두 개가 생긴다.
2. `EventSystem`을 선택한다. Inspector에 `Replace with InputSystemUIInputModule` 버튼이 보이면 누른다. 안 보이면 이미 맞게 되어 있는 것이다.
3. `Canvas`를 선택하고 Inspector의 `Canvas Scaler` 컴포넌트에서:
   - `UI Scale Mode`: `Scale With Screen Size`
   - `Reference Resolution`: X `1080`, Y `1920`
4. Scene 창에서 UI가 잘 안 보이면 Hierarchy에서 `Canvas`를 더블클릭하면 화면이 Canvas에 맞춰진다.

## 4. UI 요소 공통 설정 방법

아래 단계에서 글자(Text)와 버튼(Button)은 **Legacy** 메뉴로 만든다. TextMeshPro 기본 글꼴에는 한글이 없다.
- 글자: 오른쪽 클릭 → `UI > Legacy > Text`
- 버튼: 오른쪽 클릭 → `UI > Legacy > Button`

위치와 크기는 Inspector 맨 위 `Rect Transform`에서 `Pos X`, `Pos Y`, `Width`, `Height` 칸에 숫자를 적는다. 화면 가운데가 (0, 0)이고, 위쪽 끝이 Y = 960, 아래쪽 끝이 Y = -960이다.

글자 설정은 `Text (Legacy)` 컴포넌트에서 한다:
- `Text`: 보일 문구
- `Font Size`: 글자 크기
- `Alignment`: 가로 가운데 버튼(가운데 정렬 아이콘)과 세로 가운데 버튼을 누른다
- `Color`: 글자 색

버튼의 글자는 버튼의 자식에 있다. Hierarchy에서 버튼 왼쪽 ▶를 눌러 펼치면 `Text (Legacy)` 자식이 보인다. 그것을 선택해 글자를 바꾼다.

## 5. HUD (혈당바, 측정기)

### 5.1 HUD 오브젝트
1. Hierarchy의 `Canvas` 위에서 오른쪽 클릭 → `Create Empty`. 이름을 `Hud`로 바꾼다.
2. `Hud`를 선택하고 `Add Component` → `HudPresenter`.

### 5.2 Hud 아래에 만들 것
모두 `Hud` 위에서 오른쪽 클릭해 만든다(그래야 `Hud`의 자식이 된다). 만든 뒤 이름을 표의 이름으로 바꾸고 값을 적는다.

| 이름 | 만드는 메뉴 | Pos X, Pos Y | Width × Height | 그 밖의 설정 |
| --- | --- | --- | --- | --- |
| `GlucoseBar` | `UI > Slider` | 0, 850 | 900 × 60 | 자식 `Handle Slide Area`를 선택해 Delete 키로 지운다(선택 사항) |
| `GlucoseText` | `UI > Legacy > Text` | 0, 760 | 900 × 80 | Text 비움, Font Size 48, 가운데 정렬 |
| `WarningText` | `UI > Legacy > Text` | 0, 670 | 900 × 80 | Text `혈당 경고!`, Font Size 48, 가운데 정렬, Color 빨강 |
| `MeterText` | `UI > Legacy > Text` | 0, 580 | 900 × 80 | Text 비움, Font Size 44, 가운데 정렬 |
| `StatusText` | `UI > Legacy > Text` | 0, 0 | 900 × 120 | Text 비움, Font Size 60, 가운데 정렬 |
| `MeterButton` | `UI > Legacy > Button` | 0, -800 | 600 × 150 | 자식 글자: Text `측정기`, Font Size 48 |
| `RetryButton` | `UI > Legacy > Button` | 0, -620 | 600 × 150 | 자식 글자: Text `다시 하기`, Font Size 48 |

### 5.3 HudPresenter 필드 연결
`Hud`를 선택하고 Inspector의 `Hud Presenter` 칸에 Hierarchy에서 끌어다 놓는다.

| 필드 | 끌어다 놓을 것 |
| --- | --- |
| Glucose Bar | `GlucoseBar` |
| Glucose Text | `GlucoseText` |
| Warning Text | `WarningText` |
| Meter Button | `MeterButton` |
| Meter Text | `MeterText` |
| Status Text | `StatusText` |
| Retry Button | `RetryButton` |

### 5.4 개발용 간식 줍기 버튼 (러너가 생기기 전까지)
러너 화면이 없어서 트랙 간식을 주울 수 없으므로, 대신 누르는 버튼을 둔다. 러너 작업 때 지운다.
1. Hierarchy에서 `MeterButton`을 선택하고 Cmd+D로 복제한다. 복제본 이름을 `DevSnackButton`으로 바꾼다.
2. `Pos X` 0, `Pos Y` -440. 자식 글자의 Text를 `간식 줍기(개발)`로 바꾼다.
3. `Hud`를 선택하고 `Hud Presenter`의 `개발용 (러너 전까지)` 아래 `Dev Snack Button` 칸에 `DevSnackButton`을 끌어다 놓는다. `Dev Snack Id`는 `juice` 그대로 둔다.

## 6. 선택 패널 (2~4단계 선택지)

`HypoChoicePanel`이 붙은 오브젝트는 항상 켜져 있어야 한다(꺼지면 동작하지 않는다). 그래서 보였다 숨었다 하는 부분은 그 아래 `Root`로 따로 만든다.

### 6.1 오브젝트 만들기
1. `Canvas` 위에서 오른쪽 클릭 → `Create Empty`. 이름 `HypoChoice`. `Add Component` → `HypoChoicePanel`.
2. `HypoChoice` 위에서 오른쪽 클릭 → `UI > Panel`. 이름 `Root`.
   - Panel은 화면 전체를 덮도록 만들어진다. `Root`의 `Rect Transform`에서 `Left` 60, `Top` 450, `Right` 60, `Bottom` 350으로 바꿔 가운데만 덮게 한다.
3. `Root` 아래에 다음을 만든다(`Root` 위에서 오른쪽 클릭).

| 이름 | 만드는 메뉴 | Pos X, Pos Y | Width × Height | 그 밖의 설정 |
| --- | --- | --- | --- | --- |
| `TitleText` | `UI > Legacy > Text` | 0, 450 | 900 × 100 | Text 비움, Font Size 52, 가운데 정렬 |
| `RecheckText` | `UI > Legacy > Text` | 0, 350 | 900 × 80 | Text 비움, Font Size 40, 가운데 정렬 |
| `Buttons` | `Create Empty` | 0, -150 | 800 × 700 | 아래 6.2 참고 |

### 6.2 버튼을 세로로 늘어놓는 `Buttons`
1. `Buttons`를 선택하고 `Add Component` → `Vertical Layout Group`.
2. `Vertical Layout Group`에서:
   - `Spacing`: 20
   - `Child Alignment`: `Upper Center`
   - `Control Child Size`: Width만 체크(Height는 체크 해제)
   - `Child Force Expand`: Width만 체크(Height는 체크 해제)
3. `Buttons` 위에서 오른쪽 클릭 → `UI > Legacy > Button`. 이름 `ButtonTemplate`.
   - `Height` 110
   - 자식 글자: Font Size 40 (Text 내용은 실행 중에 바뀌므로 그대로 둬도 된다)

실행하면 `ButtonTemplate`은 숨겨지고, 이것을 복제해 선택지 버튼을 만든다.

### 6.3 HypoChoicePanel 필드 연결
`HypoChoice`를 선택하고:

| 필드 | 끌어다 놓을 것 |
| --- | --- |
| Root | `Root` |
| Title Text | `TitleText` |
| Recheck Text | `RecheckText` |
| Button Container | `Buttons` |
| Button Template | `ButtonTemplate` |

## 7. 조립 지점 (GameBootstrapper)

1. Hierarchy **빈 곳**에서 오른쪽 클릭 → `Create Empty`(Canvas 밖, 최상위). 이름 `Bootstrapper`.
2. `Add Component` → `GameBootstrapper`.
3. 필드 연결:

| 필드 | 넣을 것 |
| --- | --- |
| Build Config | Project 창에서 `Assets > _Project > Config` 폴더를 열고 `game-config` 파일을 끌어다 놓는다 |
| Use Dev Config In Editor | 체크 |
| Hud | Hierarchy의 `Hud` |
| Choice Panel | Hierarchy의 `HypoChoice` |

4. Cmd+S로 저장한다.

최종 Hierarchy 모양:

```
DevRun
├── Main Camera
├── Directional Light
├── Global Volume        (Basic (URP) 템플릿에 따라 없을 수 있음)
├── Canvas
│   ├── Hud
│   │   ├── GlucoseBar
│   │   ├── GlucoseText
│   │   ├── WarningText
│   │   ├── MeterText
│   │   ├── StatusText
│   │   ├── MeterButton
│   │   ├── RetryButton
│   │   └── DevSnackButton
│   └── HypoChoice
│       └── Root
│           ├── TitleText
│           ├── RecheckText
│           └── Buttons
│               └── ButtonTemplate
├── EventSystem
└── Bootstrapper
```

## 8. 실행해서 확인하기

▶(Play)를 누르면 실행되고, 다시 누르면 멈춘다. Console에 노란 경고 `개발용 임시 설정(실제 수치 아님)으로 실행한다`가 한 번 뜨는 것은 정상이다. 빨간 에러가 뜨면 그 문구를 그대로 전달한다.

시나리오마다 Play를 새로 누르거나, 끝난 뒤 `다시 하기`를 눌러 처음부터 시작한다. 숫자는 누르는 속도에 따라 조금씩 다르므로 "약"으로 적었다.

### 8.1 알아 둘 것 (개발 설정 기준)
- 화면의 혈당 숫자와 막대는 **3초 늦게** 따라오는 표시 혈당이다. `측정기`를 누르면 그 순간의 **실제** 혈당이 `측정: OO mg/dL`로 나온다.
- 달리는 동안 혈당은 초당 1.5씩, 선택 패널이 떠 있는 동안(세계 정지)에는 초당 0.5씩 내려간다.
- 실제 혈당이 70 아래로 내려가면(시작 후 약 33초) 저혈당이 발동한다. 화면에는 아무 표시가 없고, 이때 화면 숫자는 약 75다.
- 각 단계와 `도움이 필요해요!` 대기는 제한 시간이 5초다.
- 주행 길이는 세계 시간 40초다. 선택 패널이 떠 있는 동안은 세지 않는다.

### 8.2 시나리오 A: 모두 맞게 대처하고 완주
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play | 숫자가 `120 mg/dL`에서 내려가기 시작한다. `측정기`, `간식 줍기(개발)` 버튼이 보인다 |
| 2 | 기다린다(약 30초) | 숫자가 80 아래가 되면서 `혈당 경고!`가 보인다 |
| 3 | 숫자가 **75 아래**가 되면 `측정기` | `측정: 약 68 mg/dL`. 패널이 열리고 제목 `무엇을 먹을까요?`, 버튼 `juice`, `cracker`, `계속 달리기`, `먹을 게 없어요`, `어른에게 알리기` |
| 4 | `juice` | 제목 `juice을(를) 얼마나 먹을까요?`, 버튼 `조금`, `알맞게`, `많이`, `어른에게 알리기` |
| 5 | `알맞게` | 제목 `이제 무엇을 할까요?`, 버튼 `다시 재기`, `다시 달리기`, `어른에게 알리기` |
| 6 | `다시 재기` | `다시 잰 혈당: 약 120 mg/dL`. 버튼 `다시 달리기`, `어른에게 알리기`. 화면 숫자도 약 120으로 올라간다 |
| 7 | `어른에게 알리기` | `곧 다시 달려요`가 1초 보이고 패널이 닫힌다. 숫자가 다시 내려간다 |
| 8 | 기다린다(약 5초) | 가운데 `끝까지 달렸어요!`, `다시 하기` 버튼이 보인다 |
| 9 | `다시 하기` | 숫자가 120으로 돌아가고 글자가 지워진 채 새로 시작한다 |

3번에서 측정값이 70 이상이고 패널이 안 열리면 너무 일찍 누른 것이다(불필요 측정으로 기록만 되고 불이익은 없다). 1초 뒤 다시 누른다.

### 8.3 시나리오 B: 아무것도 안 해서 탈락
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play 후 아무것도 누르지 않는다 | 약 30초에 `혈당 경고!` |
| 2 | 계속 기다린다 | 저혈당 발동 5초 뒤(시작 후 약 38초) 패널 제목 `도움이 필요해요!`, 버튼 `어른에게 알리기`, `다시 달리기` |
| 3 | 5초 더 기다린다 | 패널이 닫히고 `어른이 도와주러 왔어요`, `다시 하기`. 숫자가 더 이상 바뀌지 않는다 |

### 8.4 시나리오 C: 놓쳤지만 어른에게 알려서 이어 가기
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | B의 2번까지 진행 | `도움이 필요해요!` |
| 2 | `어른에게 알리기` | 탈락하지 않고 `무엇을 먹을까요?` 패널로 넘어간다 |
| 3 | A의 4번부터 진행 | A와 같다 |

### 8.5 시나리오 D: 잘못 고른 경우
A의 3번까지 진행한 뒤 아래 중 하나를 해 본다. 오류가 나면 `도움이 필요해요!`가 뜨고, `어른에게 알리기`를 누르면 다음 단계로 넘어간다. 거기서 5초 동안 아무것도 안 누르면 B처럼 탈락한다.

| 할 일 | 결과 |
| --- | --- |
| `cracker`(느린 당) → `어른에게 알리기` → `알맞게` → `다시 재기` | 제목이 `cracker을(를) 얼마나...`. 다시 잰 혈당이 juice보다 낮다(약 80~95). 느린 당은 늦게 오르기 때문이다 |
| `계속 달리기` 또는 `먹을 게 없어요` → `어른에게 알리기` | 어른이 대신 juice를 건넨다. 다음 제목이 `juice을(를) 얼마나...` |
| `juice` → `조금` → `어른에게 알리기` → `다시 재기` | 다시 잰 혈당이 약 90. 덜 먹어서 나중에 저혈당이 다시 올 수 있다 |
| `juice` → `많이` → `어른에게 알리기` → `다시 재기` | 다시 잰 혈당이 약 150 |
| 패널에서 바로 `어른에게 알리기` | 오류(선택 대신 알리기)로 기록되지만 `도움이 필요해요!` 없이 바로 다음 단계로 간다 |
| 5번 단계에서 `다시 달리기` | 재측정을 건너뛴 오류. `도움이 필요해요!` |

### 8.6 시나리오 E: 간식 줍기(개발)로 혈당 유지
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play 후 약 10초마다 `간식 줍기(개발)` | 누를 때마다 실제 혈당이 약 15 오른다. 화면 숫자에는 약 3~4초 뒤에 보인다 |
| 2 | 계속 반복 | 저혈당 없이 40초를 채우면 `끝까지 달렸어요!` |
| 3 | 패널이 떠 있을 때 `간식 줍기(개발)` | 버튼이 흐려져 눌리지 않는다(세계 정지 중에는 주울 수 없다) |

### 8.7 기록 확인
macOS 기준 Finder에서 Cmd+Shift+G를 누르고 `~/Library/Application Support/ZeroSugar/SugarDash/logs/`로 이동한다. 주행마다 `{runId}.jsonl` 파일이 하나씩 생긴다. 텍스트 편집기로 열면 한 줄에 기록 하나가 있고, 모든 줄의 `configVersion`이 `dev-not-real`이다.

## 9. 자주 생기는 문제

| 증상 | 확인할 것 |
| --- | --- |
| Console에 `NullReferenceException`이 뜬다 | 5.3, 6.3, 7의 필드 중 비어 있는(`None`) 칸이 있다 |
| 버튼을 눌러도 반응이 없다 | `EventSystem`에 `Input System UI Input Module`이 붙어 있는지(3단계 2번) |
| 선택 패널이 전혀 안 나온다 | `HypoChoice` 오브젝트가 켜져 있는지(Inspector 이름 왼쪽 체크박스). 끄고 켜는 것은 `Root`만 한다 |
| 한글이 네모로 보인다 | Legacy Text가 아니라 TextMeshPro로 만든 것이다. Legacy로 다시 만든다 |
| 화면 가운데에 작은 `Button`이나 `New Text`가 보인다 | 실수로 하나 더 만든 오브젝트다. Hierarchy에서 `Canvas` 바로 아래의 `Button (Legacy)`, `Text (Legacy)`를 선택하고 Delete 키로 지운다 |
| 바꾼 값이 사라졌다 | Play 중에 바꿨다. 멈춘 상태에서 다시 바꾸고 Cmd+S |
| Console에 `설정을 불러오지 못했다`가 뜬다 | `Use Dev Config In Editor`가 체크되어 있는지 |
