# DropThatSugar

제1형 당뇨병 아동의 자가 관리 교육을 위한 기능성 게임 (Serious Game)

> 처음 [2. 초기 설정](#2-초기-설정)을 순서대로 끝까지 진행할 것, 막히면 [3. 문제 해결](#3-문제-해결)부터 확인할 것

## 1. 기술 스택

| 항목                             | 내용                                                                                              |
| -------------------------------- | ------------------------------------------------------------------------------------------------- |
| Unity                            | `ProjectSettings/ProjectVersion.txt` 기준 **6000.3.26f1**, 패치 번호까지 동일하게 설치            |
| 렌더 파이프라인(Render Pipeline) | URP, `com.unity.render-pipelines.universal` 17.3.0 (기본 에셋 `Assets/Settings/PC_RPAsset.asset`) |
| XCharts                          | `com.monitor1394.xcharts`, Git URL 설치, 태그 `v3.15.0` 고정                                      |
| Newtonsoft Json                  | `com.unity.nuget.newtonsoft-json` 3.2.2                                                           |
| Git, Git LFS(Large File Storage) | 필수                                                                                              |
| 개발 환경                        | Windows / macOS(Apple Silicon)                                                                    |

## 2. 초기 설정

### 2-1. 사전 설치

OS 최소 버전

- Windows: Windows 10 21H1(빌드 19043) 이상
- macOS: Ventura 13 이상

Git

- Windows: [git-scm.com](https://git-scm.com)에서 Git for Windows 설치, 설치 중 Git LFS 항목이 있으면 체크 확인
- macOS: `xcode-select --install`

Git LFS

- Windows: Git for Windows 설치 후 `git lfs version`으로 확인, 오류 나면 [git-lfs.com](https://git-lfs.com)에서 설치
- macOS: Homebrew([brew.sh](https://brew.sh))는 `brew install git-lfs`, 없으면 [git-lfs.com](https://git-lfs.com) 안내대로 설치
- 설치 후 터미널을 새로 열고 `git lfs version`으로 확인 (버전이 출력되면 성공)

Git 사용자 정보 (커밋 작성자, 전역 설정이라 컴퓨터당 1회)

- 아래 명령어를 실행해 현재 설정 확인, 이름과 이메일이 출력되면 설정 완료

```bash
git config --global user.name    # 사용자명 출력
git config --global user.email   # 이메일 출력
```

- 아무것도 출력되지 않으면 아래 명령어를 실행해 설정 후 위 확인 명령어를 다시 실행

```bash
git config --global user.name "<GitHub 사용자명>"
git config --global user.email "<GitHub 이메일>"
```

Unity Hub

- [unity.com](https://unity.com)에서 설치 후 로그인, 이 단계에서 에디터는 설치하지 않음

### 2-2. 리포지토리 받기

> **Clone 할 폴더 위치는 반드시 공백·한글 없는 경로로 할 것** (예: `C:\dev`, `~/dev`), 경로 관련 오류 예방

위 폴더 위치로 이동 후 아래 명령어를 순서대로 실행

```bash
git clone https://github.com/kos6490/DropThatSugar.git
cd DropThatSugar
git lfs install
git lfs pull
git config core.hooksPath .githooks
```

> **`core.hooksPath` 설정은 리포를 새로 받을 때마다 1회 필수**, 로컬 설정이라 공유되지 않으며 빠뜨리면 커밋 전 검사가 동작하지 않음

### 2-3. Unity 에디터 설치

1. 현재 `6000.3.26f1` 버전 기반
2. Unity Hub `Installs > Install Editor`에서 정확히 같은 버전 설치
3. 목록에 없으면 [unity.com](https://unity.com)의 에디터 다운로드 아카이브에서 `Install with Unity Hub`
4. `6000.3.26f1` 외 다른 버전 설치 금지, 컴파일 에러 발생 가능
5. 설치 경로는 Hub 기본값 그대로 둘 것 (Windows `C:\Program Files\Unity\Hub\Editor`, macOS `/Applications/Unity/Hub/Editor`), 2-7의 경로가 이 기준
6. 빌드 지원(Build Support) 모듈은 본인 OS용 Mono가 기본 포함되어 추가 설치 불필요, 다른 OS용 빌드가 필요할 때만 해당 Build Support 체크
7. 그 외 모듈은 선택 사항, Windows는 Visual Studio Community 선택 가능 (VS Code 사용 시 불필요)
8. macOS(Apple Silicon)에서 Rosetta 설치를 요청하면 설치

### 2-4. 프로젝트 열기

1. Hub `Projects > Add > Add project from disk`
2. 2-2에서 클론한 `DropThatSugar` 폴더(`Assets/`가 있는 폴더, 예: `C:\dev\DropThatSugar`, `~/dev/DropThatSugar`) 선택 후 열기
3. 첫 실행은 오래 걸릴 수 있음

### 2-5. 스크립트 편집기 연동

1. IDE 설치: Windows는 Visual Studio Community(2-3에서 선택) 또는 VS Code, macOS는 VS Code 또는 Rider
2. VS Code 사용 시 확장(Extension) 3개 설치
   - C# (`ms-dotnettools.csharp`)
   - C# Dev Kit (`ms-dotnettools.csdevkit`)
   - Unity (`visualstudiotoolsforunity.vstuc`)
3. Unity에서 외부 스크립트 편집기(External Script Editor)를 사용할 IDE로 지정
   - Windows: `Edit > Preferences > External Tools > External Script Editor`
   - macOS: `Unity > Settings > External Tools > External Script Editor`
4. 같은 화면에서 `Regenerate project files` 클릭 (패키지 참조가 IDE에 반영되어 XCharts 등 자동완성 동작)
5. Unity 쪽 패키지(`com.unity.ide.visualstudio` 2.0.26, `com.unity.ide.rider` 3.0.40)는 프로젝트에 포함되어 추가 설치 불필요

### 2-6. 설치 확인 (필수)

- [ ] Console에 빨간 에러 없음 (노란 경고는 무시 가능)
- [ ] 상단 메뉴에 XCharts 표시
- [ ] Project 창에서 `Assets/TutorialInfo/Scripts/Readme.cs`를 클릭하면 2-5에서 지정한 IDE로 열림
- [ ] 에디터 버전이 `6000.3.26f1`과 일치 (Windows `Help > About Unity`, macOS `Unity > About Unity`)
- [ ] Unity 종료 후 `git status`가 `nothing to commit, working tree clean` or `no changes added to commit`

위 항목 모두 확인 후 다음 단계 진행, 변경 사항이 나오면 커밋하지 말고 출력 결과를 공유할 것

### 2-7. 병합 도구 설정 (UnityYAMLMerge)

씬·프리팹 충돌 시 Unity 전용 병합 도구를 쓰기 위한 로컬 설정, 리포마다 1회 필요

1. 2-2에서 클론한 `DropThatSugar` 폴더(예: `C:\dev\DropThatSugar`, `~/dev/DropThatSugar`)에서 터미널을 열고, 아래 명령어를 실행해 로컬 설정 파일을 열기

```bash
git config --local -e
```

2. 열린 파일 맨 아래에 아래 내용을 추가 (`<UnityYAMLMerge 경로>`는 아래 표의 본인 OS 경로로 변경)

```ini
[merge]
	tool = unityyamlmerge
[mergetool "unityyamlmerge"]
	trustExitCode = false
	cmd = '<UnityYAMLMerge 경로>' merge -p \"$BASE\" \"$REMOTE\" \"$LOCAL\" \"$MERGED\"
```

| OS      | `<UnityYAMLMerge 경로>`                                                                |
| ------- | -------------------------------------------------------------------------------------- |
| Windows | `C:/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Data/Tools/UnityYAMLMerge.exe`   |
| macOS   | `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/Helpers/UnityYAMLMerge` |

> Hub 기본 경로에 설치한 경우 기준, Windows도 경로 구분자는 `/`로 작성 (`\`는 설정 파일에서 오류 발생)

설정 후 병합 충돌이 났을 때만 같은 폴더에서 아래 명령어를 실행

```bash
git mergetool
```

## 3. 문제 해결

### `git: 'lfs' is not a git command`

- 원인: Git LFS 미설치
- 해결: [2-1](#2-1-사전-설치)대로 설치 후 터미널 재시작, `git lfs version` 확인

### 이미지·모델이 분홍색이거나 깨짐

- 원인: LFS 파일 미수신 (포인터 파일만 있음)
- 해결: 아래 명령어 실행
  ```bash
  git lfs install   # LFS 사용 설정
  git lfs pull      # 실제 파일 다운로드
  ```

### XCharts 설치 실패 / git 실행 파일을 찾지 못함

- 원인: 패키지 관리자(Package Manager)가 git을 못 찾음 (XCharts는 Git URL 설치)
- 해결
  - Windows: Hub와 에디터를 완전히 종료 후 재실행
  - macOS: `xcode-select --install` 후 Hub 재시작

### `error CS0619: 'Object.GetInstanceID()' is obsolete`

- 원인: Unity 버전 불일치 (6.5 이상)
- 해결: `6000.3.26f1`로 다시 열기, 다른 버전으로 열어 생긴 변경은 아래 명령어를 실행해 되돌림
  ```bash
  git restore .   # 변경 사항 되돌리기
  ```
  > **`git restore .`는 커밋하지 않은 모든 변경을 삭제함**, 먼저 `git status`로 확인할 것

### 훅 실행 시 `/bin/sh^M: bad interpreter`

- 원인: 훅 파일 줄바꿈이 CRLF로 바뀜 (`.gitattributes`는 `.githooks/*`를 LF로 지정)
- 해결: 아래 명령어 실행
  ```bash
  git checkout -- .githooks/   # LF 상태로 복원
  ```

### 커밋이 훅 메시지로 막힘

`.githooks/pre-commit`의 검사 결과, 출력 앞의 태그로 원인 확인

| 태그      | 의미                                                                                                      | 해결                                                                                                                                               |
| --------- | --------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[LFS]`   | `git-lfs` 미설치                                                                                          | Git LFS 설치 후 `git lfs install`                                                                                                                  |
| `[META]`  | `Assets/` 파일·폴더와 `.meta`의 짝이 안 맞음 (`.meta` 누락, 원본 없는 `.meta`, 원본 삭제 후 `.meta` 잔존) | 메시지대로 `.meta`도 함께 `git add`하거나 함께 삭제 (삭제는 Unity 에디터에서)                                                                      |
| `[MERGE]` | 충돌 마커(`<<<<<<<`, `>>>>>>>`) 잔존                                                                      | 출력된 파일의 충돌 해결 후 재커밋                                                                                                                  |
| `[SIZE]`  | LFS 아닌 파일이 10MB 초과                                                                                 | `.gitattributes`에 확장자를 LFS로 추가해야 하므로 팀에 먼저 공유                                                                                   |
| `[UNITY]` | `ProjectSettings/ProjectVersion.txt` 변경됨                                                               | 의도하지 않았으면 `git restore --staged --worktree ProjectSettings/ProjectVersion.txt`, 합의된 업그레이드면 `ALLOW_UNITY_UPGRADE=1 git commit ...` |

## 4. 협업 규칙

- **`main` 직접 push 불가**, `dev` → `main`으로 PR (배포 시점) → 팀원 1명 승인 → Merge
- **`dev` 직접 push 불가**, 작업 브랜치(`feat`, `fix` 등) → `dev`로 PR → 팀원 1명 승인 → Merge
- 브랜치 전략: `main`은 항상 배포 가능한 상태 유지, `dev`는 잘 동작하는 개발 코드 상태 유지, `dev`에서 `feat`, `fix` 등 브랜치 생성 후 기능 추가, 버그 수정 등 작업 수행

아래 명령어로 'dev'에서 작업 브랜치를 새로 만들어 개발 진행

```bash
git switch dev                  # dev 브랜치로 이동
git pull                        # 최신 dev 받기
git switch -c <작업 브랜치명>    # dev에서 작업 브랜치 생성
```

- 같은 씬(Scene)을 동시에 두 명이 수정하지 않기, 작업 전 알리기
- 파일 이동·이름 변경·삭제는 Unity 에디터 안에서 (`.meta` 때문)
- Unity 버전·패키지 변경은 합의 후 단독 PR (다른 작업과 섞지 않고 해당 변경만 담은 PR, 팀원의 에디터·패키지 버전이 같아야 함)
- Newtonsoft.Json으로 역직렬화하는 클래스(DTO·세이브 데이터 등)에는 `[Preserve]` (`UnityEngine.Scripting`)를 붙이기. IL2CPP 빌드(iOS/Android)의 코드 스트리핑이 생성자·프로퍼티를 지워 기기에서만 깨지는 것을 방지
- **공개 리포이므로 유료 에셋, API 키 커밋 금지**
