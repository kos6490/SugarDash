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

## 5. 미정

- 서명 키스토어: 보관 위치와 담당자 미정. 공개 저장소이므로 키스토어 파일과 비밀번호는 커밋하지 않는다.
- 버전 올리는 규칙: 미정
- 네트워크 관련 내장 모듈(`unitywebrequest*`, `unityanalytics`) 제거: 미정. `video` 등 다른 모듈이 의존하므로 영향 범위를 따로 확인한다.

## 6. 정리한 템플릿 잔여물

- 쓰지 않는 패키지 `visualscripting`, `multiplayer.center`, `collab-proxy`, `ai.navigation`, `timeline`을 뺐다. 다시 필요하면 버전을 정해 단독 PR로 넣는다.
- `Assets/TutorialInfo/`와 `Assets/Readme.asset`(Unity 시작 템플릿 안내)을 지웠다. README 2-6 설치 확인은 `GameBootstrapper.cs`로 바꿨다.
- `Assets/Scenes/SampleScene.unity`는 지금 유일한 빌드 씬이라 남겨 둔다. 정식 게임 씬을 만들 때 정리한다.
