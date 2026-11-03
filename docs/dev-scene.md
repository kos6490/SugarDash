# 개발용 실행 씬 구성 (Unity 처음 쓰는 사람용)

3레인 러너, 혈당바, 측정기 버튼, 4단계 선택 패널로 코어 루프를 에디터에서 확인하는 씬을 만든다. 개발용 임시 설정(실제 수치 아님)으로 실행한다. 단축키는 macOS 기준이다(Windows는 Cmd 대신 Ctrl).

> **이미 `DevRun` 씬을 만들어 둔 경우**: 처음부터 다시 만들 필요 없다. 아래 순서만 하면 된다.
> 1. 0절을 다시 훑어본다.
> 2. Hierarchy에서 `Canvas > Hud > DevSnackButton`이 남아 있으면 선택해 Delete 키로 지운다(러너의 트랙 간식이 대신한다).
> 3. `Player`, `Track`, `Input`이 없으면 7절(러너)을 따라 만들고, 8절 3번 표의 `Player`, `Track`, `Input` 칸을 연결한다.
> 4. Hierarchy에서 `Canvas > Hud > RetryButton`을 선택해 Delete 키로 지운다(`다시 하기`는 결과 화면이 맡는다).
> 5. 6.5절(결과 화면)을 따라 만들고, 8절 3번 표의 `Result Screen` 칸에 `ResultScreen`을 끌어다 놓는다.
> 6. 결과 화면을 이미 만들었으면 6.5.3절만 따라 한다.
> 7. 점수 표시가 없는 씬이면 5.2절 표대로 `Hud` 아래에 `ScoreText`를 만들고 5.3절대로 `Hud Presenter`의 `Score Text` 칸에 연결한다. 예전에 만든 `HeartsText`가 있으면 선택해 Delete 키로 지운다(하트는 없어졌다). 결과 화면은 씬 변경이 없다.
> 8. 유형 선택·2형을 더한 뒤라면 6.6~6.8절을 따라 화면 오브젝트를 만들고, 6.5.4절대로 `ResultScreen`에 `타이틀로` 버튼을 더한다. 8절 3번 표에서 새로 생긴 칸(`Build Config Type2`, `Title Screen`, `Type Select`, `Settings`, `Character State`, `Notice Signal`)을 연결한다.
> 9. Cmd+S로 저장하고 9절 순서로 확인한다.

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
| `ScoreText` | `UI > Legacy > Text` | 225, 770 | 450 × 60 | Text `점수 0`, Font Size 40, Alignment는 가로 오른쪽·세로 가운데. 혈당바 오른쪽 아래에 보인다. 실행하면 점수로 바뀐다 |
| `StatusText` | `UI > Legacy > Text` | 0, 0 | 900 × 120 | Text 비움, Font Size 60, 가운데 정렬 |
| `MeterButton` | `UI > Legacy > Button` | 0, -800 | 600 × 150 | 자식 글자: Text `측정기`, Font Size 48 |
| `CountdownText` | `UI > Legacy > Text` | 0, 300 | 600 × 300 | Text 비움(칸의 글자를 모두 지운다), Font Size 200, 가운데 정렬. 대처를 마친 뒤 다시 달리기 전에 화면 가운데에 `3`, `2`, `1`이 크게 나온다 |

이미 HUD를 만들어 두었다면 `CountdownText`만 새로 만든다. `Hud` 위에서 오른쪽 클릭 → `UI > Legacy > Text`를 고르고, 생긴 `Text (Legacy)`의 이름을 `CountdownText`로 바꾼 뒤 위 표의 값을 적는다. 그다음 5.3의 `Countdown Text` 칸을 연결한다. 연결하지 않으면 숫자만 안 보이고 나머지는 그대로 동작한다.

### 5.3 HudPresenter 필드 연결
`Hud`를 선택하고 Inspector의 `Hud Presenter` 칸에 Hierarchy에서 끌어다 놓는다.

| 필드 | 끌어다 놓을 것 |
| --- | --- |
| Glucose Bar | `GlucoseBar` |
| Glucose Text | `GlucoseText` |
| Warning Text | `WarningText` |
| Meter Button | `MeterButton` |
| Meter Text | `MeterText` |
| Score Text | `ScoreText` |
| Status Text | `StatusText` |
| Countdown Text | `CountdownText` |

## 6. 선택 패널 (2~4단계 선택지)

`HypoChoicePanel`이 붙은 오브젝트는 항상 켜져 있어야 한다(꺼지면 동작하지 않는다). 그래서 보였다 숨었다 하는 부분은 그 아래 `Root`로 따로 만든다.

패널 동작:
- 버튼을 누르면(또는 제한 시간이 지나면) 다음 화면으로 바로 가지 않고, 약 1초 동안 제목에 무엇을 골랐는지만 보여 준다(예: `juice을(를) 골랐어요`, `알맞게 먹었어요`, `시간이 다 됐어요`). 이때 버튼은 없다. 맞았는지 틀렸는지는 말하지 않는다.
- 이 1초는 다음 단계의 제한 시간에 들어가지 않는다. 1초가 지나면 다음 단계 제목과 버튼이 나온다.
- 대처를 마치면 같은 방식으로 문구를 1초 보여 준 뒤 패널이 닫히고, 화면 가운데에 `3`, `2`, `1`이 크게 나온다(5.2의 `CountdownText`). 숫자가 사라지면 다시 달린다.

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

## 6.5 결과 화면

주행이 끝나면(탈락) 제목, 요약 한 줄, `다시 하기` 버튼을 화면 가운데 흰 박스에 보여 준다. 선택 패널과 같이, 컴포넌트가 붙은 오브젝트는 항상 켜 두고 그 아래 `Root`만 껐다 켠다.

### 6.5.1 오브젝트 만들기
1. `Canvas` 위에서 오른쪽 클릭 → `Create Empty`. 이름 `ResultScreen`. `Add Component` → `ResultScreenView`.
   - Hierarchy에서 `ResultScreen`이 `HypoChoice`보다 **아래**에 있어야 한다(아래에 있을수록 화면 앞에 그려진다). 위에 있으면 끌어서 `HypoChoice` 아래로 옮긴다.
2. `ResultScreen` 위에서 오른쪽 클릭 → `UI > Panel`. 이름 `Root`. 화면 전체를 덮는 그대로 둔다.
   - `Root`의 `Image` 컴포넌트에서 `Color` 칸을 눌러 R `0`, G `0`, B `0`, A `150`으로 바꾼다. 뒤의 게임 화면이 어둡게 보인다. 더 어둡게 하려면 A를 올린다(255면 완전히 가려진다).
3. `Root` 위에서 오른쪽 클릭 → `UI > Panel`. 이름 `Box`. 이것이 결과를 담는 흰 박스다.
   - `Rect Transform` 왼쪽 위 과녁 모양 네모(Anchor Presets)를 누르고, 4×4 격자의 한가운데(`middle` / `center`) 칸을 누른다(Alt·Shift는 누르지 않는다). 또는 `Anchors`를 펼쳐 `Min`, `Max`의 X, Y 네 칸을 모두 `0.5`로 적는다.
   - 칸 이름이 `Left`, `Top`, `Right`, `Bottom`에서 `Pos X`, `Pos Y`, `Width`, `Height`로 바뀌었는지 **꼭 확인한다**. 안 바뀐 채 숫자를 넣으면 박스가 화면 구석에 작게 생기고 `다시 하기`가 잘린다.
   - Pos X `0`, Pos Y `0`, Width `880`, Height `500`
   - `Image`의 `Color`: R `255`, G `255`, B `255`, A `255`(불투명). 뒤의 혈당 숫자가 비쳐 보이지 않는다.
4. `Box` 아래에 다음을 만든다(`Box` 위에서 오른쪽 클릭). 위치는 박스 가운데 기준이다.

| 이름 | 만드는 메뉴 | Pos X, Pos Y | Width × Height | 그 밖의 설정 |
| --- | --- | --- | --- | --- |
| `TitleText` | `UI > Legacy > Text` | 0, 130 | 800 × 90 | Text 비움, Font Size 52, 가운데 정렬 |
| `SummaryText` | `UI > Legacy > Text` | 0, 0 | 800 × 160 | Text 비움, Font Size 34, 가운데 정렬. 점수 줄(`점수`, `최고 점수`, 새 기록이면 `새 최고 기록!`)과 저혈당·고혈당 줄이 함께 들어간다. 줄이 잘리면 Height를 키우고 글자가 `TitleText`나 `RetryButton`에 겹치지 않는지 본다 |
| `RetryButton` | `UI > Legacy > Button` | 0, -140 | 440 × 110 | 자식 글자: Text `다시 하기`, Font Size 40 |

### 6.5.2 ResultScreenView 필드 연결
`ResultScreen`을 선택하고:

| 필드 | 끌어다 놓을 것 |
| --- | --- |
| Root | `ResultScreen` 아래의 `Root` |
| Title Text | `ResultScreen > Root > Box` 아래의 `TitleText` |
| Summary Text | `SummaryText` |
| Retry Button | `ResultScreen > Root > Box` 아래의 `RetryButton` |
| Title Button | `ResultScreen > Root > Box` 아래의 `TitleButton` (6.5.4절에서 만든다) |

`HypoChoice`에도 `Root`, `TitleText`가 있으므로 같은 이름을 끌어다 놓을 때 `ResultScreen` 아래의 것인지 확인한다. `Root` 칸에는 `Box`가 아니라 `Root`를 넣는다(어두운 배경과 박스가 함께 껐다 켜진다).

### 6.5.3 이미 만든 결과 화면 고치기
저혈당 칸(`Events`, `EventTemplate`)이 있는 옛 결과 화면을 만들었으면 이렇게 고친다. 남은 필드 연결은 그대로 두면 된다.
1. Hierarchy에서 `ResultScreen > Root > Box > Events`를 선택하고 Delete 키로 지운다(자식 `EventTemplate`도 함께 지워진다).
2. `Box`, `TitleText`, `SummaryText`, `RetryButton`의 위치·크기·글자 크기를 6.5.1절 3, 4번 값으로 고친다. 고혈당 줄이 안 보이면 `SummaryText`가 옛 값(Pos Y `25`, Height `110`)이라 세 번째 줄이 잘린 것이다.
3. Cmd+S로 저장한다.

### 6.5.4 `타이틀로` 버튼 더하기
결과 화면에서 타이틀(유형 고르기, 설정)로 돌아가는 버튼이다.
1. `Box` 위에서 오른쪽 클릭 → `UI > Legacy > Button`. 이름 `TitleButton`.
2. Pos X `0`, Pos Y `-270`, Width `440`, Height `90`. 자식 글자: Text `타이틀로`, Font Size 36.
3. `Box`의 Height가 `500`이면 버튼이 박스 밖으로 나가 잘린다. `Box`의 Height를 `640`으로 키우고, 다른 오브젝트 위치는 그대로 둔다.
4. `ResultScreen`을 선택하고 `Result Screen View`의 `Title Button` 칸에 `TitleButton`을 끌어다 놓는다.
5. 기대 결과: 탈락해 결과 화면이 뜨면 `다시 하기` 아래에 `타이틀로`가 보이고, 누르면 타이틀 화면으로 간다(9.10절).

## 6.6 화면 흐름 (타이틀, 유형 선택, 설정)

실행하면 처음에 유형 선택 화면(저장된 유형이 없을 때) 또는 타이틀 화면이 뜨고, `시작`을 눌러야 주행이 시작된다. 세 화면은 모두 `Canvas` 아래에 만든다.

공통 규칙:
- 화면마다 `Root`라는 **자식** 오브젝트를 둔다. 화면 스크립트(`TitleScreenView` 등)는 빈 오브젝트에 붙이고, 보이고 숨기는 것은 그 아래 `Root`만 한다.
- `Root` 칸에 **스크립트가 붙은 오브젝트 자신**을 넣으면 안 된다. 자신을 끄면 스크립트도 같이 멈춰서 화면이 다시는 나타나지 않는다. 반드시 자식 `Root`를 넣는다.
- 스크립트가 붙은 오브젝트(`TitleScreen` 등)는 항상 켜 둔다(Inspector 이름 왼쪽 체크박스가 켜진 상태).
- 세 화면은 `Hud`, `HypoChoice`보다 **아래**에 둔다(아래에 있을수록 앞에 그려진다). `ResultScreen`보다는 위에 둬도 된다.
- 버튼 글자는 씬의 Text로 적는다(코드가 바꾸지 않는다). 아래 표의 글자를 그대로 쓴다.
- `UI > Legacy > Button`, `UI > Legacy > Text`로 만든다. 위치와 크기는 예시이며 마음대로 바꿔도 된다.

### 6.6.1 TitleScreen
1. `Canvas` 위에서 오른쪽 클릭 → `Create Empty`. 이름 `TitleScreen`. `Add Component` → `TitleScreenView`.
2. `TitleScreen` 위에서 오른쪽 클릭 → `UI > Panel`. 이름 `Root`. 크기는 화면 전체를 덮는 기본값 그대로 둔다. `Image`의 `Color` A를 `255`로 올려 뒤 화면을 가린다.
3. `Root` 위에서 오른쪽 클릭하고, 아래 표의 오브젝트 3개(`TypeText`, `StartButton`, `SettingsButton`)를 만든다.

| 이름 | 만드는 메뉴 | Pos X, Pos Y | Width × Height | 그 밖의 설정 |
| --- | --- | --- | --- | --- |
| `TypeText` | `UI > Legacy > Text` | 0, 200 | 800 × 100 | Text 비움(코드가 `1형으로 달려요`/`2형으로 달려요`를 넣는다), Font Size 48, 가운데 정렬 |
| `StartButton` | `UI > Legacy > Button` | 0, 0 | 440 × 120 | 자식 글자: Text `시작`, Font Size 44 |
| `SettingsButton` | `UI > Legacy > Button` | 0, -160 | 440 × 120 | 자식 글자: Text `설정`, Font Size 44 |

4. `TitleScreen`을 선택하고 `Title Screen View`의 칸을 연결한다.

| 필드 | 끌어다 놓을 것 |
| --- | --- |
| Root | `TitleScreen` 아래의 `Root` |
| Type Text | `TypeText` |
| Start Button | `StartButton` |
| Settings Button | `SettingsButton` |

### 6.6.2 TypeSelect
1. `Canvas` 위에서 오른쪽 클릭 → `Create Empty`. 이름 `TypeSelect`. `Add Component` → `TypeSelectView`.
2. `TypeSelect` 아래에 `UI > Panel`을 `Root`로 만들고(6.6.1절 2번과 같게), `Root` 아래에 만든다.

| 이름 | 만드는 메뉴 | Pos X, Pos Y | Width × Height | 그 밖의 설정 |
| --- | --- | --- | --- | --- |
| `TitleText` | `UI > Legacy > Text` | 0, 300 | 900 × 100 | Text 비움(코드가 `어떤 당뇨인가요?`를 넣는다), Font Size 52, 가운데 정렬 |
| `GuideText` | `UI > Legacy > Text` | 0, 190 | 900 × 80 | Text 비움(코드가 `보호자와 함께 골라 주세요`를 넣는다), Font Size 34, 가운데 정렬 |
| `Type1Button` | `UI > Legacy > Button` | 0, 20 | 440 × 140 | 자식 글자: Text `1형`, Font Size 48 |
| `Type2Button` | `UI > Legacy > Button` | 0, -150 | 440 × 140 | 자식 글자: Text `2형`, Font Size 48 |

3. `TypeSelect`를 선택하고 `Type Select View`의 칸을 연결한다: `Root`(`TypeSelect` 아래의 `Root`), `Title Text`, `Guide Text`, `Type 1 Button`, `Type 2 Button`.

### 6.6.3 Settings
1. `Canvas` 위에서 오른쪽 클릭 → `Create Empty`. 이름 `Settings`. `Add Component` → `SettingsView`.
2. `Settings` 아래에 `UI > Panel`을 `Root`로 만들고, `Root` 아래에 만든다.

| 이름 | 만드는 메뉴 | Pos X, Pos Y | Width × Height | 그 밖의 설정 |
| --- | --- | --- | --- | --- |
| `TitleText` | `UI > Legacy > Text` | 0, 300 | 900 × 100 | Text 비움(코드가 `설정`을 넣는다), Font Size 52, 가운데 정렬 |
| `CurrentTypeText` | `UI > Legacy > Text` | 0, 190 | 900 × 80 | Text 비움(코드가 지금 유형 `1형`/`2형`을 넣는다), Font Size 40, 가운데 정렬 |
| `Type1Button` | `UI > Legacy > Button` | 0, 20 | 440 × 120 | 자식 글자: Text `1형`, Font Size 44 |
| `Type2Button` | `UI > Legacy > Button` | 0, -130 | 440 × 120 | 자식 글자: Text `2형`, Font Size 44 |
| `BackButton` | `UI > Legacy > Button` | 0, -300 | 440 × 120 | 자식 글자: Text `돌아가기`, Font Size 44 |

3. `Settings`를 선택하고 `Settings View`의 칸을 연결한다: `Root`(`Settings` 아래의 `Root`), `Title Text`, `Current Type Text`, `Type 1 Button`, `Type 2 Button`, `Back Button`.
4. 같은 이름(`Root`, `TitleText`, `Type1Button`)이 여러 화면에 있으므로 끌어다 놓을 때 맞는 화면 아래의 것인지 확인한다.

## 6.7 2형 신호 글자 (NoticeSignal)

2형에서 고혈당·저혈당 신호(notice)가 나올 때 글자를 보여 준다. 글자만 있는 단순한 오브젝트다.
1. `Canvas` 위에서 오른쪽 클릭 → `UI > Legacy > Text`. 이름 `NoticeSignal`. Pos X `0`, Pos Y `600`, Width `800`, Height `100`, Text 비움, Font Size 44, 가운데 정렬.
2. `Add Component` → `NoticeSignalView`. `Signal Text` 칸에 `NoticeSignal` 자신(같은 오브젝트의 Text)을 끌어다 놓는다.
   - 이 화면은 `Root`가 없다. 신호가 없으면 코드가 글자를 비우므로 켜 둔 채로 둔다.
3. 기대 결과: 2형 주행 중 고혈당 신호가 나오면 `목말라요`, 저혈당 쪽 신호가 나오면 `(개발용) 낮음 신호`가 보인다(9.10절). 문구는 개발용이다.

## 6.8 캐릭터 상태 (CharacterStateView)

2형에서 캐릭터 상태(보통, 쉬는 중, 지침)를 받아 보여 주는 빈 틀이다. 지금은 화면에 보이는 것이 없다.
1. Hierarchy의 `Player`를 선택하고 `Add Component` → `CharacterStateView`. 칸은 건드리지 않는다.
   - `Sandbox Mode`는 연습용 씬에서 주행과 상관없이 상태를 바꿔 볼 때만 쓴다. 이 씬에서는 꺼 둔다.
2. 8절 3번 표의 `Character State` 칸에 `Player`를 끌어다 놓는다.

## 6.9 2구간 맵 바닥 (SceneLook)

2구간에서 장면(운동장, 놀이터, 나들이, 집)이 바뀔 때 바닥 색을 바꾸는 빈 틀이다. 트랙 바닥(회색)은 실행하면 코드가 만들므로, 트랙 양옆으로 넓게 깔리는 바닥을 따로 하나 만든다.
1. Hierarchy **빈 곳**에서 오른쪽 클릭 → `Create Empty`. 이름 `SceneLook`. Transform `⋮` → `Reset`. `Add Component` → `SceneLook`.
2. `SceneLook` 위에서 오른쪽 클릭 → `3D Object > Plane`. 이름 `SceneGround`. Position `0, -0.12, 40`, Rotation `0, 0, 0`, Scale `10, 1, 12`(가로 100m, 세로 120m).
   - 트랙 바닥(두께 0.1, 맨 아래 -0.1)보다 조금 낮게 두어 겹쳐 깜빡이지 않게 한다.
3. `SceneLook`을 선택하고 `Scene Look`의 `Ground` 칸에 `SceneGround`를 끌어다 놓는다.
4. 기대 결과: 1구간에서는 Plane 기본 색(흰색)이다. 2구간에 들어가면 장면마다 색이 바뀐다(9.12절).

## 6.10 수면 전환 (SleepTransition)

하루가 넘어갈 때 화면을 어둡게 했다가 밝히는 빈 틀이다.
1. `Canvas` 위에서 오른쪽 클릭 → `UI > Image`. 이름 `SleepTransition`.
2. Rect Transform 왼쪽 위 네모(Anchor Presets)를 누르고 Alt(Mac은 Option)를 누른 채 오른쪽 아래 `stretch`·`stretch`를 고른다. Left, Top, Right, Bottom이 모두 `0`이 되어 화면 전체를 덮는다.
3. `Image`의 Color를 검은색, 알파(A) `0`으로 한다. `Raycast Target` 체크를 끈다(꺼야 아래 버튼이 눌린다).
4. Hierarchy에서 `SleepTransition`을 `Canvas` 자식 중 **맨 아래**로 끌어 둔다(다른 UI 위에 그려진다).
5. `Add Component` → `SleepTransitionView`. `Fade` 칸에 `SleepTransition` 자신(같은 오브젝트의 Image)을 끌어다 놓는다. `Fade Out Sec` `0.4`, `Hold Sec` `0.2`, `Fade In Sec` `0.6`은 기본값 그대로 둔다.

## 7. 러너 (캐릭터, 트랙, 입력)

캐릭터와 트랙의 모양(캡슐, 상자, 구)은 실행하면 코드가 만든다. 여기서는 빈 오브젝트 3개에 컴포넌트를 붙이고 카메라만 연결한다.

1. Hierarchy **빈 곳**에서 오른쪽 클릭 → `Create Empty`. 이름 `Player`. `Add Component` → `PlayerController`.
2. 같은 방법으로 `Track`(컴포넌트 `TrackSpawner`), `Input`(컴포넌트 `InputAdapter`)을 만든다.
3. 세 오브젝트 모두 위치를 원점으로 맞춘다. 하나씩 선택하고 Inspector의 `Transform` 칸 오른쪽 끝 `⋮`(점 세 개) → `Reset`. Position이 0, 0, 0이 되면 된다.
4. `Player`를 선택하고 `Player Controller`의 `Camera Rig` 칸에 Hierarchy의 `Main Camera`를 끌어다 놓는다. 카메라 위치는 실행하면 코드가 맞춘다.
5. `Main Camera`를 선택하고 `Add Component` → `SymptomEffectPresenter`. `Camera Target` 칸에 `Main Camera` 자신을 끌어다 놓는다(실제 혈당이 낮을수록 카메라만 흔들리고 UI는 흔들리지 않는다).
6. `Track`의 `TrackSpawner`에 `Look` 칸(`TrackItemLook`)이 있다. 월드 담당 팀원이 `TrackItemLook`을 채워 push하면 구조 담당 팀원이 연결한다. 그전에는 비워 둔다. 비어 있으면 코드가 만든 기본 도형(상자, 구)이 나온다.
7. 그 밖의 값(속도, 점프 높이, 흔들림 폭 등)은 기본값 그대로 둔다. 나중에 감각을 맞출 때 Inspector에서 바꾼다(Play 중에 바꾼 값은 멈추면 되돌아간다).

## 8. 조립 지점 (GameBootstrapper)

1. Hierarchy **빈 곳**에서 오른쪽 클릭 → `Create Empty`(Canvas 밖, 최상위). 이름 `Bootstrapper`.
2. `Add Component` → `GameBootstrapper`.
3. 필드 연결:

| 필드 | 넣을 것 |
| --- | --- |
| Build Config | Project 창에서 `Assets > _Project > Config` 폴더를 열고 `game-config` 파일(1형)을 끌어다 놓는다 |
| Build Config Type2 | 같은 폴더의 `game-config.type2` 파일(`Assets/_Project/Config/game-config.type2.json`)을 끌어다 놓는다. 에디터에서 `Use Dev Config In Editor`가 체크되어 있으면 개발용 파일(`Config/Dev/game-config.type2.dev.json`)을 대신 쓰지만, 빌드에는 이 칸의 파일이 실리므로 비워 두지 않는다 |
| Use Dev Config In Editor | 체크 |
| Hud | Hierarchy의 `Hud` |
| Choice Panel | Hierarchy의 `HypoChoice` |
| Result Screen | Hierarchy의 `ResultScreen` |
| Player | Hierarchy의 `Player` |
| Track | Hierarchy의 `Track` |
| Symptom Effect | Hierarchy의 `Main Camera` (Symptom Effect Presenter가 붙은 오브젝트) |
| Input | Hierarchy의 `Input` |
| Title Screen | Hierarchy의 `TitleScreen` (`TitleScreenView`가 붙은 오브젝트, 6.6.1절) |
| Type Select | Hierarchy의 `TypeSelect` (6.6.2절) |
| Settings | Hierarchy의 `Settings` (6.6.3절) |
| Character State | Hierarchy의 `Player` (`CharacterStateView`가 붙은 오브젝트, 6.8절) |
| Notice Signal | Hierarchy의 `NoticeSignal` (6.7절) |
| Scene Look | Hierarchy의 `SceneLook` (6.9절) |
| Sleep Transition | Hierarchy의 `SleepTransition` (6.10절) |

화면 칸(`Title Screen`~`Sleep Transition`)은 비워 두면 그 화면은 연결되지 않고 건너뛴다. 단 `Title Screen`, `Type Select`가 비면 화면 흐름이 진행되지 않으므로 둘은 꼭 연결한다.

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
│   │   ├── ScoreText
│   │   ├── StatusText
│   │   └── MeterButton
│   ├── HypoChoice
│   │   └── Root
│   │       ├── TitleText
│   │       ├── RecheckText
│   │       └── Buttons
│   │           └── ButtonTemplate
│   ├── ResultScreen
│   │   └── Root
│   │       └── Box
│   │           ├── TitleText
│   │           ├── SummaryText
│   │           ├── RetryButton
│   │           └── TitleButton
│   ├── TitleScreen
│   │   └── Root           (TypeText, StartButton, SettingsButton)
│   ├── TypeSelect
│   │   └── Root           (TitleText, GuideText, Type1Button, Type2Button)
│   ├── Settings
│   │   └── Root           (TitleText, CurrentTypeText, Type1Button, Type2Button, BackButton)
│   ├── NoticeSignal
│   └── SleepTransition  (맨 아래)
├── EventSystem
├── Player               (PlayerController, CharacterStateView)
├── Track
├── Input
├── SceneLook
│   └── SceneGround
└── Bootstrapper
```

## 9. 실행해서 확인하기

▶(Play)를 누르면 실행되고, 다시 누르면 멈춘다. Console에 노란 경고 `개발용 임시 설정(실제 수치 아님)으로 실행한다`가 한 번 뜨는 것은 정상이다. 빨간 에러가 뜨면 그 문구를 그대로 전달한다.

유형 선택·타이틀 화면이 있는 씬이면 Play 뒤 먼저 유형 선택(또는 타이틀) 화면이 뜬다. 시나리오 A~G는 1형이므로 `1형`을 고르고 타이틀에서 `시작`을 눌러 시작한다(9.10절). 아래 표의 "Play"는 이 시작까지를 뜻한다.

시나리오마다 Play를 새로 누르거나, 끝난 뒤 `다시 하기`를 눌러 처음부터 시작한다. 숫자는 누르는 속도에 따라 조금씩 다르므로 "약"으로 적었다.

### 9.1 알아 둘 것 (개발 설정 기준)
- 화면의 혈당 숫자와 막대는 **3초 늦게** 따라오는 표시 혈당이다. `측정기`를 누르면 그 순간의 **실제** 혈당이 `측정: OO mg/dL`로 나오고 3초 뒤 사라진다(`Hud Presenter`의 `Meter Text Show Sec`).
- 달리는 동안 혈당은 초당 1.5씩, 선택 패널이 떠 있는 동안(세계 정지)에는 초당 0.5씩 내려간다.
- 실제 혈당이 70 아래로 내려가면(시작 후 약 33초) 저혈당이 발동한다. 화면에는 아무 표시가 없고, 이때 화면 숫자는 약 75다.
- 실제 혈당이 80 아래로 내려가면 화면(카메라)이 흔들리기 시작하고, 40에 가까워질수록 더 크게 흔들린다. 혈당 바 등 UI는 흔들리지 않는다.
- 1~4단계는 제한 시간이 8초, `도움이 필요해요!` 대기는 5초다. 급하강은 한 번에 20 떨어진다(개발 설정 값).
- 패널에서 버튼을 누를 때마다(또는 제한 시간이 지날 때마다) 결과 문구가 **1초** 나온 뒤 다음 화면으로 간다(6절). 이 1초는 다음 단계 제한 시간에 들어가지 않는다. 아래 시나리오의 "결과"는 이 1초 뒤의 화면이다.
- 대처를 마치면 결과 문구 1초 뒤 패널이 닫히고 화면 가운데에 `3`, `2`, `1`이 1초씩 나온 뒤 다시 달린다.
- 실제 혈당이 135 이상이 되면 고혈당이 발동한다(개발 설정만 낮춰 둔 값). 고혈당 알리기 제한 시간은 8초다.
- 주행은 끝이 없다. 탈락할 때까지 이어지고, 완주 화면은 없다. 장애물에 한 번 부딪히면 바로 탈락한다. 달릴수록 점점 빨라진다.
- 실제 혈당이 40 미만이거나 200 초과여도 바로 탈락하지 않는다. 제한 시간(1~4단계, 고혈당 알리기)을 넘긴 순간에만 탈락한다.
- 점수는 혈당이 안전 범위에 있는 시간(초당 1점)과 달린 거리(1미터당 0.1점)로 쌓이고, 선택지 점수도 더해진다. 화면 `점수 N`은 내림한 값이다.
- 선택지 버튼 순서는 패널이 열릴 때마다 바뀐다(`어른에게 알리기`는 항상 맨 아래). 아래 시나리오는 순서가 아니라 버튼 이름으로 적었다.
- 노란 공(트랙 간식)은 6~10초 간격으로 하나씩 나온다. 먹으면 혈당이 약 15 오른다. **시나리오 A~D는 저혈당이 와야 하므로 간식을 피해서 달린다**(간식과 다른 레인에 있으면 된다).

### 9.2 시나리오 A: 모두 맞게 대처하고 장애물에 부딪힐 때까지 달리기
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play | 숫자가 `120 mg/dL`에서 내려가기 시작하고 캐릭터가 달린다. 노란 공은 피한다 |
| 2 | 기다린다(약 30초) | 숫자가 80 아래가 되면서 `혈당 경고!`가 보인다 |
| 3 | 숫자가 **75 아래**가 되면 `측정기` | `측정: 약 68 mg/dL`. 패널이 열리고 제목 `무엇을 먹을까요?`, 버튼 `juice`, `cracker`, `계속 달리기`, `먹을 게 없어요`(순서는 매번 다름), 맨 아래 `어른에게 알리기` |
| 4 | `juice` | 제목 `juice을(를) 얼마나 먹을까요?`, 버튼 `조금`, `알맞게`, `많이`(순서는 매번 다름), 맨 아래 `어른에게 알리기` |
| 5 | `알맞게` | 제목 `이제 무엇을 할까요?`, 버튼 `다시 재기`, `다시 달리기`(순서는 매번 다름), 맨 아래 `어른에게 알리기` |
| 6 | `다시 재기` | `다시 잰 혈당: 약 120 mg/dL`. 버튼 `다시 달리기`, `어른에게 알리기`. 화면 숫자도 약 120으로 올라간다 |
| 7 | `어른에게 알리기` | 제목 `어른에게 알렸어요`가 1초 보이고 패널이 닫힌다. 화면 가운데에 `3`, `2`, `1`이 나온 뒤 다시 달린다. 숫자가 다시 내려간다 |
| 8 | 계속 달리며 장애물에 한 번 부딪힌다 | 부딪히는 즉시 어두워진 게임 화면 위에 흰 리포트 박스가 뜬다. 제목 `어른이 도와주러 왔어요`, 그 아래 `점수 N점`, `최고 점수 M점`(첫 판이면 `새 최고 기록!`), `저혈당 1번 모두 잘 해냈어요`, 아래에 `다시 하기` 버튼 |
| 9 | `다시 하기` | 결과 화면이 닫히고, 숫자가 120으로, `점수 0`으로 돌아가고 측정값 글자가 지워진 채 새로 시작한다 |

3번에서 측정값이 70 이상이고 패널이 안 열리면 너무 일찍 누른 것이다(불필요 측정으로 기록만 되고 불이익은 없다). 1초 뒤 다시 누른다.

### 9.3 시나리오 B: 아무것도 안 해서 탈락
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play 후 아무것도 누르지 않는다 | 약 30초에 `혈당 경고!` |
| 2 | 계속 기다린다 | 저혈당 발동 8초 뒤 패널에 `시간이 다 됐어요`가 1초 보이고, 이어서 제목 `도움이 필요해요!`, 버튼 `어른에게 알리기`, `다시 달리기` |
| 3 | 5초 더 기다린다 | 패널이 닫히고 결과 화면이 뜬다. 제목 `어른이 도와주러 왔어요`, 그 아래 `점수 N점`, `최고 점수 M점`, `저혈당 1번 모두 아쉬웠어요` / `다음에 더 잘해봐요!` |

### 9.4 시나리오 C: 놓쳤지만 어른에게 알려서 이어 가기
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | B의 2번까지 진행 | `도움이 필요해요!` |
| 2 | `어른에게 알리기` | 탈락하지 않고 `무엇을 먹을까요?` 패널로 넘어간다 |
| 3 | A의 4번부터 진행 | A와 같다. 단 결과 화면 요약은 `저혈당 1번 모두 아쉬웠어요` / `다음에 더 잘해봐요!`(놓친 단계가 있으므로) |

### 9.5 시나리오 D: 잘못 고른 경우
A의 3번까지 진행한 뒤 아래 중 하나를 해 본다. 오류가 나면 `도움이 필요해요!`가 뜨고, `어른에게 알리기`를 누르면 다음 단계로 넘어간다. 거기서 5초 동안 아무것도 안 누르면 B처럼 탈락한다.

| 할 일 | 결과 |
| --- | --- |
| `cracker`(느린 당) → `어른에게 알리기` → `알맞게` → `다시 재기` | 제목이 `cracker을(를) 얼마나...`. 다시 잰 혈당이 juice보다 낮다(약 80~95). 느린 당은 늦게 오르기 때문이다 |
| `계속 달리기` 또는 `먹을 게 없어요` → `어른에게 알리기` | 어른이 대신 juice를 건넨다. 다음 제목이 `juice을(를) 얼마나...` |
| `juice` → `조금` → `어른에게 알리기` → `다시 재기` | 다시 잰 혈당이 약 90. 덜 먹어서 나중에 저혈당이 다시 올 수 있다 |
| `juice` → `많이` → `어른에게 알리기` → `다시 재기` | 다시 잰 혈당이 약 150. 대처를 마치고 다시 달린 뒤 약 3초에 고혈당이 온다(시나리오 F) |
| 패널에서 바로 `어른에게 알리기` | 오류(선택 대신 알리기)로 기록되지만 `도움이 필요해요!` 없이 바로 다음 단계로 간다 |
| 5번 단계에서 `다시 달리기` | 재측정을 건너뛴 오류. `도움이 필요해요!` |

### 9.6 시나리오 E: 러너 조작
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play | 파란 캡슐(캐릭터)이 가운데 레인에 있고, 흰 줄무늬가 다가온다. 약 3초 뒤부터 장애물이 온다 |
| 2 | ←/→ 또는 A/D | 옆 레인으로 옮긴다. 양 끝 레인에서 더 바깥으로는 가지 않는다 |
| 3 | ↑ 또는 W | 점프한다. 빨간 낮은 상자는 점프로 넘는다 |
| 4 | ↓ 또는 S | 몸을 낮춘다. 공중에 뜬 주황 막대는 낮춰서 지나간다 |
| 5 | 보라색 높은 벽 | 점프·슬라이드로는 못 넘는다. 옆 레인으로 피한다 |
| 6 | 장애물에 일부러 부딪힌다 | 장애물에 부딪힌 직후 결과 화면이 뜬다(비틀거리는 모습은 잠깐만 보인다)(제목 `어른이 도와주러 왔어요`). 혈당 숫자에는 변화가 없다 |
| 7 | 마우스로 화면을 누른 채 끌었다 놓는다 | 끈 방향(좌/우/위/아래)으로 키보드와 같은 동작을 한다. 짧게 클릭만 하면 움직이지 않는다 |
| 8 | 노란 공과 같은 레인에서 지나간다 | 공이 사라지고 3~4초 뒤 혈당 숫자가 약 15 오른다 |
| 9 | 노란 공을 계속 먹는다 | 혈당이 크게 떨어지지 않는다. 고혈당(135 이상)이 오면 시나리오 F처럼 알린다. 끝나지 않고 점점 빨라지며, 장애물에 한 번 부딪히면 탈락한다. 저혈당이 한 번도 없었으면 결과 화면에 `이번에는 저혈당이 없었어요`가 나온다 |
| 10 | 간식을 피하다가 저혈당 → `측정기` | 패널이 열리면 캐릭터와 트랙이 멈추고 방향키가 듣지 않는다 |
| 11 | 시나리오 A의 4~7번으로 대처를 마친다 | `3`, `2`, `1` 카운트다운 동안 트랙의 장애물이 모두 사라진다. 다시 달린 뒤 약 3초 동안 장애물이 없다 |
| 12 | 끝난 뒤 `다시 하기` | 캐릭터가 가운데로 돌아가고 트랙이 처음부터 다시 나온다 |

### 9.7 시나리오 F: 고혈당 어른에게 알리기
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | A의 4번까지 진행한 뒤 `많이` → `어른에게 알리기` → `다시 재기` → `어른에게 알리기` | `3`, `2`, `1` 카운트다운 뒤 다시 달린다 |
| 2 | 기다린다(약 3초) | 캐릭터와 트랙이 멈추고 패널 제목 `혈당이 높아요. 어떻게 할까요?`, 버튼 `계속 달리기`, `어른에게 알리기`. `측정기`는 누를 수 없다 |
| 3 | `어른에게 알리기` | `어른에게 알렸어요`가 1초 보이고 `3`, `2`, `1` 카운트다운 뒤 다시 달린다 |
| 4 | 장애물에 한 번 부딪힌다 | 결과 화면 요약이 `저혈당 1번 모두 아쉬웠어요` / `고혈당 1번 모두 바로 알렸어요` / `다음에 더 잘해봐요!`(저혈당에서 '많이'를 골라 오류가 있으므로) |

2번에서 다르게 해 본다.

| 할 일 | 결과 |
| --- | --- |
| `계속 달리기` | 오류. `도움이 필요해요!`가 뜬다. `어른에게 알리기`를 누르면 이어 가고, 결과 요약에 `고혈당 1번 모두 아쉬웠어요`, 맨 아래 `다음에 더 잘해봐요!` |
| 8초 동안 아무것도 안 누른다 | 시간 초과 오류. 위와 같이 `도움이 필요해요!` |
| `도움이 필요해요!`에서 `다시 달리기` 또는 5초 기다리기 | 탈락. 결과 화면 제목 `어른이 도와주러 왔어요` |

### 9.8 시나리오 G: 장애물 한 번 부딪혀 탈락, 최고 점수
| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play | 위쪽에 `점수 0`. 점수가 시간이 갈수록 오른다 |
| 2 | 장애물에 일부러 한 번 부딪힌다 | 바로 탈락한다 |
| 3 | 결과 화면이 뜬다 | 결과 화면 제목 `어른이 도와주러 왔어요`. 요약 맨 위에 `점수 N점`, `최고 점수 M점`, 첫 판이면 `새 최고 기록!` |
| 4 | `다시 하기` 후 더 일찍 탈락한다 | 이번 점수가 이전보다 낮으면 `새 최고 기록!`이 없고 `최고 점수`는 그대로다 |

최고 점수 파일 `best-score.json`은 `~/Library/Application Support/ZeroSugar/SugarDash/` 폴더에 저장된다. 9.9절의 `logs/` 폴더가 아니라 그 바로 위 폴더이다. 이 파일을 지우면 다음 판이 다시 첫 판처럼 `새 최고 기록!`이 나온다. 혈당이 40 미만이거나 200 초과여도 제한 시간을 넘기기 전에는 탈락하지 않는다.

### 9.9 기록 확인
macOS 기준 Finder에서 Cmd+Shift+G를 누르고 `~/Library/Application Support/ZeroSugar/SugarDash/logs/`로 이동한다. 주행마다 `{runId}.jsonl` 파일이 하나씩 생긴다. 텍스트 편집기로 열면 한 줄에 기록 하나가 있고, 모든 줄의 `configVersion`이 `dev-not-real`이다.

### 9.10 시나리오 H: 유형 선택과 화면 흐름
저장된 유형 파일(`settings.json`)이 없는 첫 실행을 기준으로 한다. 파일은 `~/Library/Application Support/ZeroSugar/SugarDash/` 폴더에 생긴다. 이 파일을 지우면 다시 첫 실행이 된다.

| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | Play | 유형 선택 화면: `어떤 당뇨인가요?`, `보호자와 함께 골라 주세요`, `1형`, `2형` 버튼. 주행은 아직 시작하지 않는다 |
| 2 | `1형` | 타이틀 화면: `1형으로 달려요`, `시작`, `설정` |
| 3 | `설정` | 설정 화면: `설정`, 지금 유형 `1형`, 버튼 `1형`, `2형`, `돌아가기` |
| 4 | `2형` | 타이틀 화면으로 돌아가고 `2형으로 달려요`로 바뀐다 |
| 5 | Play를 멈추고 다시 Play | 유형 선택 없이 바로 타이틀 화면, `2형으로 달려요`(고른 유형이 저장되어 있다) |
| 6 | 타이틀에서 `설정` → `돌아가기` | 유형이 그대로인 채 타이틀로 돌아온다 |

### 9.11 시나리오 I: 2형 한 판
9.10절 4번 뒤 타이틀에서 `시작`을 누른다. 개발 설정(`game-config.type2.dev.json`, 실제 수치 아님) 기준이다.

| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | `시작` | 캐릭터가 달린다. 혈당 숫자는 `120 mg/dL`에서 시작하고 바로 반영된다(1형의 3초 지연이 없다). `측정기` 버튼, `혈당 경고!`, 측정 글자는 없다. 약 3초 뒤부터 장애물이 온다 |
| 2 | 음식 줄을 본다 | 4~7초마다 음식이 2레인에 하나씩, 나머지 한 레인은 비어 나온다. 두 음식은 서로 다르다. 개발용 기본 도형이라 이름은 코드가 정한 모양으로만 보인다 |
| 3 | `juice`·`soda` 쪽 레인으로 계속 먹는다 | 먹은 순간부터 혈당이 서서히 오른다. 먹었다고 점수나 정답·오답 표시는 없다. `water`는 혈당이 오르지 않는다 |
| 4 | 혈당이 `180` 이상인 채 약 4초 이어진다 | 말풍선 글자 `목말라요`가 보이고 주행은 계속된다. 약 2초 뒤 캐릭터와 트랙이 멈춘다 |
| 5 | 패널 확인 | 제목 `몸이 힘들어요. 어떻게 할까요?`, 버튼 `계속 달리기`, `주스 마시기`(순서는 매번 다름), 맨 아래 `어른에게 알리기`. 제한 시간 8초 |
| 6 | `어른에게 알리기` | 정답. 결과 글자가 1초 보이고 `3`, `2`, `1` 카운트다운 뒤 다시 달린다 |
| 7 | 15~25초마다 나오는 쉬는 곳(개발용 도형)과 같은 레인으로 들어간다 | 캐릭터가 3초 동안 멈춘다(거리 점수가 쌓이지 않는다). 그동안 방향키는 듣지 않는다. 쉬는 중에도 혈당 숫자는 움직이되 달릴 때보다 덜 내려간다 |
| 8 | 음식을 피하며 오래 달린다 | 혈당이 `70` 아래로 내려가 약 4초 이어지면 `(개발용) 낮음 신호`가 보이고, 이어서 패널이 뜬다. 제목은 같고 버튼은 `계속 달리기`, `어른에게 알리기` |
| 9 | `어른에게 알리기` | 어른이 `juice`를 건네 혈당이 오른다. 카운트다운 뒤 다시 달린다 |
| 10 | 장애물에 한 번 부딪힌다 | 결과 화면이 뜬다. 제목 `어른이 도와주러 왔어요`. 요약은 2형 문구다. 예: `혈당이 높을 때 1번 모두 바로 알렸어요`, `혈당이 낮을 때 1번 모두 바로 알렸어요`(이벤트가 있었을 때만). 아래에 `다시 하기`, `타이틀로` |
| 11 | `타이틀로` | 타이틀 화면. 설정에서 `1형`으로 바꾸고 `시작`하면 9.2절과 같은 1형 주행이 된다 |

2형에서 확인할 오답:

| 할 일 | 결과 |
| --- | --- |
| 5번에서 `주스 마시기` | 주스 1회분이 혈당에 반영되고 오류로 기록된다. `도움이 필요해요!`가 뜨고 `어른에게 알리기`를 누르면 이어 간다 |
| 5번에서 `계속 달리기` 또는 8초 기다리기 | 오류. 위와 같이 `도움이 필요해요!` |
| `도움이 필요해요!`에서 `다시 달리기` 또는 5초 기다리기 | 탈락. 결과 화면 |
| tell 제한 시간을 넘긴 순간 혈당이 `40` 미만이거나 `300` 초과 | 오류 처리 없이 바로 탈락 |

2형 화면에 1형 모습(화면 흔들림, 측정기)이 보이면 `Bootstrapper`의 `Build Config Type2` 연결과 고른 유형을 확인한다. 최고 점수는 1형 `best-score.json`, 2형 `best-score.type2.json`으로 따로 저장된다.

### 9.12 시나리오 J: 2구간 하루 흐름
1형·2형 각각 한 번씩 한다. 개발 설정(실제 수치 아님) 기준: 1구간 300m, 운동장·놀이터·나들이 200m씩, 집 80m. 장애물은 피하고 대처가 나오면 정답으로 넘긴다.

| 순서 | 할 일 | 결과 |
| --- | --- | --- |
| 1 | `시작` 뒤 달린다 | 1구간 동안 `SceneGround`는 흰색이다 |
| 2 | 300m를 넘는다 | 바닥이 운동장 색(모래색)으로 바뀐다 |
| 3 | 200m마다 | 놀이터(초록) → 나들이(파랑) → 집(보라) 색으로 바뀐다 |
| 4 | 1형 나들이 | 간식이 3~4초 간격으로 자주 나온다(개발 설정의 덮어쓰기 값) |
| 5 | 집 | 새 장애물·간식·음식·쉬는 곳이 나오지 않는다. 집에 들어가기 전에 놓인 것은 지나갈 때까지 남는다 |
| 6 | 집 끝(80m) | 화면이 약 1초 동안 어두워졌다 밝아지고 바닥이 운동장 색으로 돌아간다. 암전 중에도 캐릭터는 계속 달린다. 바로 앞에 장애물이 갑자기 생기지 않는다 |
| 7 | 계속 달린다 | 하루가 끝없이 되풀이된다. 속도는 하루가 넘어가도 처음으로 돌아가지 않는다 |
| 8 | 결과 화면 → `다시 하기` | 바닥이 흰색(1구간)으로 돌아가고 암전이 남아 있지 않다 |

기록 파일(9.9절)에 `SceneEntered` 줄이 장면마다 하나씩 생기고, `dayIndex`가 둘째 날부터 `1`, `2`로 오른다.

## 10. 자주 생기는 문제

| 증상 | 확인할 것 |
| --- | --- |
| Console에 `NullReferenceException`이 뜬다 | 5.3, 6.3, 7, 8절의 필드 중 비어 있는(`None`) 칸이 있다 |
| 버튼을 눌러도 반응이 없다 | `EventSystem`에 `Input System UI Input Module`이 붙어 있는지(3단계 2번) |
| 선택 패널이 전혀 안 나온다 | `HypoChoice` 오브젝트가 켜져 있는지(Inspector 이름 왼쪽 체크박스). 끄고 켜는 것은 `Root`만 한다 |
| 주행이 끝나도 결과 화면이 안 나온다 | `ResultScreen` 오브젝트가 켜져 있는지, `Bootstrapper`의 `Result Screen` 칸이 연결됐는지(6.5.2, 8절) |
| 결과 화면이 한쪽으로 치우치거나 `다시 하기`가 잘린다 | `Box`의 Anchor가 가운데로 바뀌지 않았다(6.5.1절 3번). 칸 이름이 `Pos X`, `Width`인지 확인한다 |
| Console에 `Can't Generate Mesh, No Font Asset has been assigned.` 노란 경고 | Unity 에디터 자체 화면의 경고다. 게임과 관계없으니 무시한다 |
| 캐릭터나 트랙이 안 보인다 | `Player`, `Track`의 Position이 0, 0, 0인지(7절 3번), `Bootstrapper`의 `Player`, `Track` 칸이 연결됐는지 |
| 방향키를 눌러도 안 움직인다 | `Bootstrapper`의 `Input` 칸이 연결됐는지. Game 창을 한 번 클릭해 키 입력을 받게 한다 |
| 한글이 네모로 보인다 | Legacy Text가 아니라 TextMeshPro로 만든 것이다. Legacy로 다시 만든다 |
| 화면 가운데에 작은 `Button`이나 `New Text`가 보인다 | 실수로 하나 더 만든 오브젝트다. Hierarchy에서 `Canvas` 바로 아래의 `Button (Legacy)`, `Text (Legacy)`를 선택하고 Delete 키로 지운다 |
| 바꾼 값이 사라졌다 | Play 중에 바꿨다. 멈춘 상태에서 다시 바꾸고 Cmd+S |
| Console에 `설정을 불러오지 못했다`가 뜬다 | `Use Dev Config In Editor`가 체크되어 있는지 |
| 실행하면 아무 화면도 안 나오고 주행도 안 된다 | `Bootstrapper`의 `Title Screen`, `Type Select` 칸이 연결됐는지(8절) |
| 타이틀·유형 선택·설정 화면이 한 번 사라진 뒤 다시 안 나온다 | 화면 스크립트의 `Root` 칸에 스크립트가 붙은 오브젝트 자신을 넣었다. 자식 `Root`를 넣는다(6.6절) |
| 결과 화면에 `타이틀로` 버튼이 없거나 눌러도 반응이 없다 | `ResultScreen`의 `Title Button` 칸이 비어 있다(6.5.4절). 비어 있어도 오류는 없지만 타이틀로 갈 길이 없다 |
| 2형을 골랐는데 `설정을 불러오지 못했다`가 뜬다 | `Bootstrapper`의 `Build Config Type2`와 `Use Dev Config In Editor` 칸. 오류 문구에 어느 유형인지 적혀 있다 |
