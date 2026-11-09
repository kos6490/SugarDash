# 빌드 설정

대상 플랫폼은 Android 하나다. 서버 없이 기기에서 단독으로 동작한다.
아래 값은 `ProjectSettings/` 파일 기준이다. 바꿀 때는 에디터의 Player Settings에서 바꾸고, 이 문서도 함께 고친다.

## 1. 앱 정보

| 항목 | 값 | 비고 |
| --- | --- | --- |
| Company Name | `ZeroSugar` | `Application.persistentDataPath` 경로에 들어간다. 바꾸면 저장 위치가 바뀐다 |
| Product Name | `SugarDash` | |
| Package Name (Android) | `com.zerosugar.sugardash` | 스토어 등록 후에는 바꿀 수 없다 |
| Version | `0.1.0` | `bundleVersion` |
| Bundle Version Code | `1` | `AndroidBundleVersionCode` |

## 2. Android

| 항목 | 값 | 비고 |
| --- | --- | --- |
| Scripting Backend | IL2CPP | |
| Target Architectures | ARM64 | |
| Minimum API Level | 25 (Android 7.1) | |
| Target API Level | 자동 (설치된 최신) | `AndroidTargetSdkVersion: 0` |
| Internet Access | 강제 안 함 | `ForceInternetPermission: 0`. 네트워크를 쓰지 않으므로 INTERNET 권한을 넣지 않는다 |
| Default Orientation | Portrait (세로 고정) | 자동 회전 허용 항목은 Auto Rotation일 때만 쓰이므로 무시된다 |

## 3. 그래픽

| 항목 | 값 | 비고 |
| --- | --- | --- |
| Color Space | Linear | |
| Render Pipeline (Android) | `Assets/Settings/Mobile_RPAsset.asset` | Android 기본 품질 레벨이 `Mobile`이다 |
| Render Pipeline (에디터 기본) | `Assets/Settings/PC_RPAsset.asset` | Graphics Settings 기본값. 에디터 화면과 실기기 화질이 다를 수 있다 |

## 4. 빌드 씬

| 순서 | 씬 |
| --- | --- |
| 0 | `Assets/Scenes/SampleScene.unity` |

## 5. 서명 키스토어

스토어에 올리지 않고 APK 파일을 기기에 직접 설치한다. 그래서 **나눠 주는 APK는 모두 같은 키스토어로 서명한다.**

- 왜: 서명 키가 다른 APK는 기존 앱 위에 덮어 설치되지 않는다. 앱을 지우고 다시 설치하면 기기에 저장된 주행 기록과 최고 점수(`persistentDataPath` 아래 파일)도 함께 지워진다.
- 키스토어는 하나만 둔다. 파일 이름 `sugardash-release.keystore`. 구조 담당 팀원이 한 번만 만들고 혼자 보관한다.
- 저장소 밖에 두고 백업을 두 곳에 둔다(잃어버리면 다시 만들 수 없고, 새 키로는 덮어 설치가 안 된다). 비밀번호는 키스토어와 다른 곳에 따로 적어 둔다.
- 키스토어 파일과 비밀번호는 커밋하지 않는다. `.gitignore`에 `*.keystore`, `*.jks`가 있다.
- 나눠 줄 APK는 구조 담당 팀원만 빌드한다. 영역 담당 팀원이 자기 기기에서 시험할 때는 키스토어 없이(Unity 디버그 키) 빌드해도 되지만, 그 APK는 다른 사람에게 주지 않는다.

### 5.1 키스토어 만들기 (처음 한 번)

메뉴 이름은 Unity 매뉴얼(Android Keystore Manager) 기준이다. 화면이 다르면 구조 담당 팀원이 이 절을 고친다.

1. `Edit > Project Settings` → 왼쪽 `Player` → 위쪽 Android(로봇 모양) 탭 → `Publishing Settings`를 펼친다.
2. `Keystore Manager` 버튼 → 창 왼쪽 위 `Keystore` → `Create New` → `Anywhere`를 고르고, 저장소 **밖**의 폴더에 `sugardash-release.keystore`로 저장한다.
3. 키스토어 비밀번호와 확인 칸을 채운다. 아래 `New Key Values`에 Alias `sugardash`, 키 비밀번호, Validity(Years) `50`을 넣는다. 이름 칸은 팀 이름(`ZeroSugar`) 정도만 적고 개인 정보는 넣지 않는다.
4. `Add Key` → 키스토어를 쓸지 물으면 `Yes`.
5. 키스토어 파일을 백업 두 곳에 복사하고, 비밀번호 두 개를 따로 적어 둔다.
6. 이 단계에서 `ProjectSettings/ProjectSettings.asset`에 키스토어 경로가 적힌다. 커밋하지 않고 되돌린다(5.2절 6번).

### 5.2 나눠 줄 APK 빌드하기

1. `dev`(또는 배포 때는 `main`)를 pull한 깨끗한 상태에서 시작한다. `git status`에 바뀐 파일이 없어야 한다.
2. 6절대로 버전을 올리는 커밋을 먼저 하고 push한다.
3. `Edit > Project Settings > Player > Android > Publishing Settings`에서 `Custom Keystore`를 켜고, `Select`로 키스토어 파일을 고르고, 비밀번호와 Alias(`sugardash`), 키 비밀번호를 넣는다(Unity는 비밀번호를 저장하지 않으므로 빌드할 때마다 넣는다).
4. `File > Build Profiles`에서 Android로 `Build`. 파일 이름은 `SugarDash-<버전>-<빌드 번호>.apk`(예: `SugarDash-0.1.0-1.apk`). APK는 저장소 밖에 둔다.
5. 7절 빌드 기록 표에 한 줄을 더해 커밋한다.
6. Unity를 닫고 `git status`를 본다. `ProjectSettings/ProjectSettings.asset`이 바뀌어 있으면(키스토어 경로) `git restore ProjectSettings/ProjectSettings.asset`으로 되돌린다. 왜: 경로가 커밋되면 다른 사람 컴퓨터에는 그 파일이 없어 빌드 설정이 깨지고, 내 폴더 경로가 공개된다.

## 6. 버전

| 값 | 어디 | 규칙 |
| --- | --- | --- |
| 빌드 번호 | `AndroidBundleVersionCode` (Player > Android > Other Settings > `Bundle Version Code`) | 나눠 주는 APK마다 1씩 올린다. 줄이지 않는다. 이 번호가 기존 설치보다 커야 덮어 설치된다 |
| 버전 이름 | `bundleVersion` (Player > `Version`) | `0.x.y`. `dev`를 `main`에 머지해 배포할 때 `x`를 올리고 `y`는 0으로. 그 사이 고친 판을 다시 나눠 줄 때 `y`를 올린다 |

- 버전을 올리는 커밋은 빌드 직전에 따로 한다. 예: `chore: Bump version to 0.2.0`. 1절 표의 Version·Bundle Version Code 값도 같이 고친다.
- 첫 배포는 지금 값(`0.1.0`, 빌드 번호 `1`) 그대로 쓴다. 빌드용 설정(`game-config.json`, `game-config.type2.json`)의 수치가 정해져야 첫 APK가 실행된다.

## 7. 빌드 기록

나눠 준 APK마다 한 줄. 기록 파일의 `configVersion`과 함께 어느 판에서 나온 기록인지 맞춰 보는 데 쓴다. 용도에는 `1차 플레이 테스트`, `발표 시연`처럼 적고 아동·보호자 이름 같은 개인 정보는 적지 않는다(공개 저장소).

| 버전 | 빌드 번호 | 날짜 | 커밋 | 용도 |
| --- | --- | --- | --- | --- |
| | | | | |

## 8. 미정

- 네트워크 관련 내장 모듈(`unitywebrequest*`, `unityanalytics`) 제거: 미정. `video` 등 다른 모듈이 의존하므로 영향 범위를 따로 확인한다.

## 9. 정리한 템플릿 잔여물

- 쓰지 않는 패키지 `visualscripting`, `multiplayer.center`, `collab-proxy`, `ai.navigation`, `timeline`을 뺐다. 다시 필요하면 버전을 정해 단독 PR로 넣는다.
- `Assets/TutorialInfo/`와 `Assets/Readme.asset`(Unity 시작 템플릿 안내)을 지웠다. README 2-6 설치 확인은 `GameBootstrapper.cs`로 바꿨다.
- `Assets/Scenes/SampleScene.unity`는 지금 유일한 빌드 씬이라 남겨 둔다. 정식 게임 씬을 만들 때 정리한다.
