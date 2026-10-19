# 아키텍처

코드는 4개 계층(Domain, App, Infrastructure, Presentation)과 조립 지점(Bootstrap)으로 나눈다. 계층마다 어셈블리 정의(.asmdef)를 두어, 허용하지 않은 참조는 컴파일 단계에서 막힌다.

## 1. 폴더 구조

```
Assets/_Project/
├── Scripts/
│   ├── Domain/          DropThatSugar.Domain
│   ├── App/             DropThatSugar.App
│   ├── Infrastructure/  DropThatSugar.Infrastructure
│   ├── Presentation/    DropThatSugar.Presentation
│   └── Bootstrap/       DropThatSugar.Bootstrap
└── Tests/
    └── EditMode/
        ├── Domain/          DropThatSugar.Domain.Tests
        └── Infrastructure/  DropThatSugar.Infrastructure.Tests
```

- 어셈블리 이름과 루트 네임스페이스는 같다. 예: `DropThatSugar.Domain`
- 테스트는 EditMode 테스트로 작성한다. 다른 계층의 테스트 어셈블리는 필요할 때 `DropThatSugar.{계층}.Tests` 형식으로 추가한다.

## 2. 계층별 역할

| 계층 | 역할 |
| --- | --- |
| Domain | 혈당 모델, 이벤트 결정, 4단계 대처 상태 기계, 점수·지표·진행 판정 같은 게임 규칙. Unity와 직렬화 라이브러리에 의존하지 않는 순수 C# 코드 |
| App | 화면·게임 상태 흐름, 주행 진행, 구간 해금과 퀴즈 관문 흐름. 판정은 Domain에 맡긴다 |
| Infrastructure | 설정 읽기, 기록 저장·조회 같은 파일 입출력. Domain이 정의한 인터페이스(포트)를 구현한다 |
| Presentation | 입력, 캐릭터·트랙, HUD, 선택지 UI, 결과 화면, 보호자 리포트 차트(XCharts) 같은 화면과 MonoBehaviour |
| Bootstrap | 모든 계층을 참조하는 조립 지점. 구현체를 생성해 연결하고 주입한다 |

- 의존성은 Bootstrap에서만 만들고 연결한다. 다른 계층에서 싱글턴이나 `FindObjectOfType` 같은 오브젝트 검색으로 의존성을 얻지 않는다.

## 3. 참조 규칙

| 어셈블리 | 참조할 수 있는 어셈블리 |
| --- | --- |
| Domain | 없음 |
| App | Domain |
| Infrastructure | Domain, Newtonsoft Json |
| Presentation | App, Domain, XCharts, Input System |
| Bootstrap | Domain, App, Infrastructure, Presentation |
| 테스트 | 테스트 대상 계층 (예: Infrastructure.Tests → Infrastructure, Domain) |

- Presentation은 Domain의 이벤트 구독과 이벤트 타입에만 Domain을 쓴다. 게임 규칙 호출은 App을 거친다.
- 어셈블리 참조는 전이되지 않는다. A가 B를 참조하고 B가 C를 참조해도, A에서 C의 타입을 쓰려면 A가 C를 직접 참조해야 한다.

## 4. 컴파일 단계에서 막히는 것

| 막히는 것 | 막는 설정 |
| --- | --- |
| Domain에서 `UnityEngine` 사용 | Domain asmdef의 `noEngineReferences: true` |
| Domain에서 Newtonsoft 사용 | Domain asmdef의 `overrideReferences: true` (DLL 참조 없음) |
| Presentation, App에서 Infrastructure 사용 | 두 asmdef의 `references`에 Infrastructure가 없음 |
| Infrastructure 밖에서 Newtonsoft 사용 | Domain, App, Presentation, Bootstrap asmdef의 `overrideReferences: true`. Infrastructure만 `Newtonsoft.Json.dll`을 명시적으로 참조 |

- Newtonsoft 패키지 DLL은 기본적으로 모든 어셈블리에 자동 참조된다. 그래서 `overrideReferences: true`로 막는다.
- 테스트 어셈블리는 Newtonsoft가 자동 참조되지만, 빌드에 포함되지 않으므로 허용한다.

## 5. 입력

- 프로젝트 설정의 Active Input Handling이 Input System 전용이다.
- 입력은 Input System(`UnityEngine.InputSystem`)으로만 처리한다. 구 입력 API(`UnityEngine.Input`)를 호출하면 런타임 예외가 난다.

## 6. 저장

- 저장은 JSON 파일로 한다. SQLite는 사용하지 않는다.
- 파일은 `Application.persistentDataPath` 아래에 둔다.
- 이벤트 로그는 주행 단위 파일에 JSON Lines(`.jsonl`) 형식으로 한 줄씩 추가만 한다.
- 진행도·설정처럼 덮어쓰는 파일은 임시 파일에 쓴 뒤 교체한다.
- JSON 직렬화는 Infrastructure에서만 한다. 역직렬화용 DTO는 Infrastructure에 두고 `[Preserve]`를 붙인 뒤, 도메인 객체로 변환해 넘긴다.

## 7. 테스트

### 에디터에서 실행

`Window > General > Test Runner`를 열고 EditMode 탭에서 `Run All` 또는 원하는 테스트를 실행한다.

### 명령줄(batchmode) 실행

에디터에서 프로젝트를 열고 있으면 먼저 닫는다.

```bash
"<UNITY_EXE>" -batchmode -projectPath "<PROJECT_PATH>" \
  -runTests -testPlatform EditMode \
  -testResults "<PROJECT_PATH>/TestResults/editmode.xml" \
  -logFile -
```

- `<UNITY_EXE>` (Hub 기본 설치 경로 기준)
  - macOS: `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity`
  - Windows: `C:/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe`
- `-quit`은 붙이지 않는다. 테스트가 끝나기 전에 종료된다.
- 결과 파일이 생기는 `TestResults/`는 `.gitignore`에 포함되어 있다.

### 테스트 어셈블리 추가

1. Project 창에서 `Assets/_Project/Tests/EditMode/`를 선택하고 `Assets > Create > Testing > Tests Assembly Folder`를 실행한다.
2. 생성된 폴더 이름을 계층 이름으로 바꾼다. 예: `App`
3. asmdef 파일 이름을 `DropThatSugar.{계층}.Tests.asmdef`로 바꾼다. 이름 변경은 에디터에서 한다(`.meta` 유지).
4. asmdef 내용을 아래처럼 고친다. `optionalUnityReferences`는 템플릿 그대로 둔다.
   - `name`, `rootNamespace`: `DropThatSugar.{계층}.Tests`
   - `references`: 테스트 대상 계층과, 그 계층이 쓰는 타입의 어셈블리
   - `includePlatforms: ["Editor"]` 추가 (메뉴 템플릿은 Editor 전용이 아니어서 그대로 두면 PlayMode 테스트 어셈블리가 된다)

```json
{
    "name": "DropThatSugar.App.Tests",
    "rootNamespace": "DropThatSugar.App.Tests",
    "references": [
        "DropThatSugar.App",
        "DropThatSugar.Domain"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "includePlatforms": [
        "Editor"
    ]
}
```
