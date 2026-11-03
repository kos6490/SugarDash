# 기록 형식 (이벤트 로그)

`LogRepository`(Infrastructure)가 DomainEventBus의 도메인 이벤트를 원시 기록으로 바꿔 쓴다. 지표는 저장하지 않고 조회 시 MetricsCalculator가 계산한다.

## 1. 파일

- 위치: Bootstrap이 넘기는 폴더(`Application.persistentDataPath` 아래). 주행 하나에 파일 하나, `{runId}.jsonl`.
- `runId`: `yyyyMMddTHHmmssZ-xxxxxxxx` (주행 시작 UTC 시각 + GUID 앞 8자리). 이름순 정렬이 시작 순서다.
- 한 줄에 레코드 하나(JSON Lines), UTF-8(BOM 없음), 줄 끝 `\n`. 추가만 하고 기존 내용을 읽거나 다시 쓰지 않는다.
- `RunEnded`가 없는 파일은 중간 종료된 주행이다. 한 판은 탈락으로만 끝나므로 정상 종료면 `RunEnded` 앞에 `RunEliminated`가 있다. 마지막 줄이 깨져 있을 수 있으므로 조회 시 형식이 깨진 줄은 건너뛴다.
- 디스크 오류로 한 줄을 못 쓰면 로그만 남기고 게임은 계속한다(`seq`에 빈 번호가 생긴다).

## 2. 공통 필드

| 필드 | 값 |
| --- | --- |
| `schemaVersion` | 레코드 형식 버전(`LogRepository.SchemaVersion`). 형식을 바꾸면 올린다. 무한 주행 변경(아래 3절의 새 레코드)에서 2로 올린다 |
| `configVersion` | 주행에 쓴 설정의 `configVersion` |
| `runId` | 파일 이름과 같다 |
| `seq` | 주행 안에서 1부터 1씩 늘어나는 줄 번호 |
| `type` | 레코드 종류(아래 표) |
| `simT`, `t` | simDt 누적 시각, decisionDt 누적 시각(`docs/glucose-model.md` 2.1절). `RunStarted`·`RunEnded`에는 없다 |
| `eventId` | 저혈당·고혈당 이벤트 번호(같은 순서를 함께 쓴다). 이벤트에 속하지 않는 레코드에는 없다(`MeterPressed`는 이벤트 밖이면 0). `RunEliminated`의 `eventId`도 대처 이벤트 밖(주행 중 장애물·혈당 탈락)이면 0 |

값이 null인 필드는 쓰지 않는다. 예: 시간 초과한 `StepAnswered`에는 `choiceId`가 없고, 정답이면 `errorCode`가 없다.

## 3. 레코드 종류

| `type` | 고유 필드 |
| --- | --- |
| `RunStarted` | `atUtc` |
| `RunEnded` | `atUtc` |
| `HypoEventTriggered` | `isRecurrence`, `actual` |
| `HyperEventTriggered` | `actual` |
| `SuddenDropStarted` | - |
| `GaugeWarningRaised` | `displayed` |
| `MeterPressed` | `measured`, `unnecessary`, `inGrace` |
| `StepAnswered` | `step`, `choiceId`, `isCorrect`, `errorCode`, `helpInstead`, `decisionSec`, `measuredBeforeWarning` |
| `GuardianPromptStarted` | `step`, `errorCode` |
| `GuardianNotified` | `step` |
| `DoseApplied` | `snackId`, `servings`, `chosenByChild` |
| `RecheckFastForward` | `simSecSkipped`, `glucoseBefore`, `glucoseAfter` |
| `HypoResolved` | `allCorrect` |
| `HyperResolved` | `allCorrect`(오류 없이 바로 알렸으면 true) |
| `RunEliminated` | `step`, `errorCode`(탈락으로 이어진 오류. 대처 중 시간 초과에서 혈당으로 탈락하면 그 단계와 시간 초과 오류, 주행 중 장애물·혈당 탈락이면 둘 다 없음), `cause`, `distanceM` |
| `GraceStarted`, `GraceEnded` | - |
| `TrackSnackEaten` | `snackId`, `servings` |
| `SectionEntered` | `section`(`signal`, `condition`, `complex`), `distanceM`. 주행 시작 시 첫 구간도 기록 |
| `GlucoseRangeEntered`, `GlucoseRangeExited` | `actual`. 적정 범위(`normalRangeMin` 이상 `hyperThreshold` 미만) 들어감·나감. 점수 재계산용. 주행 시작 때 기록 순서는 `RunStarted` → `SectionEntered` → `GlucoseRangeEntered`(시작 시 범위 안일 때만) |

- `atUtc`: `yyyy-MM-ddTHH:mm:ss.fffZ`
- `step`: `recognize`, `respond`, `dose`, `remeasure`, `notify` (`remeasure`와 `notify`가 함께 4단계), `hyper_notify`(고혈당 알리기, `docs/hyper-event.md`)
- `errorCode`: CLAUDE.md 2절의 코드 그대로(`S_MISSED`, `RS_SLOW`, ..., `HELP_INSTEAD`, `HY_KEEP_RUNNING`, `HY_TIMEOUT`)
- `cause`: `declined`(알리기 대기에서 "다시 달리기"), `prompt_timeout`(알리기 제한 시간 초과), `obstacle_hit`(장애물에 부딪힘), `glucose_low`·`glucose_high`(실제 혈당이 안전 범위를 벗어남)
- `RunCompleted`는 없앴다(한 판은 탈락으로만 끝난다).
- 점수는 저장하지 않는다. `StepAnswered`의 선택, `GlucoseRangeEntered/Exited`, `RecheckFastForward`, `RunEliminated.distanceM`으로 ScoringService가 다시 계산한다.
- `decisionSec`: 단계 진입부터 답할 때까지의 `t` 차이. 단계 제한 시간을 넘지 않는다(큰 프레임 뒤 시간 초과도 제한 시간으로 기록). 결과 문구 창(`hypo.answerFeedbackSec`)이 끝난 뒤부터 잰다. 그래서 기록의 `t` 차이와는 문구 창만큼 다를 수 있다.
- 문자열 코드는 도메인 enum 이름과 따로 `LogCodes`에 고정한다. enum 이름을 바꿔도 과거 기록과 어긋나지 않게 하기 위해서다.

## 4. 조회

- `ReportQueryService`(Infrastructure, `ILogQuery` 구현)가 주행 파일을 읽어 레코드를 도메인 이벤트 객체로 되돌리고 `RunRecord`(Domain)로 넘긴다. 계산은 하지 않는다.
- 형식이 깨진 줄, `schemaVersion`이 다른 줄, 모르는 `type`, 필수 필드가 빠진 줄은 건너뛰고 `RunRecord.SkippedLineCount`에 센다.
- 지표는 `MetricsCalculator`(Domain)가 계산한다(`docs/glucose-model.md` 6.1절).
- 주행이 끝나면(탈락) GameFlowController가 기록 파일을 닫은 뒤 다시 읽어 결과 화면용 요약을 만든다. 화면과 기록이 같은 출처를 쓴다.

## 5. 아직 기록하지 않는 것

- 주행 종류(일반 / 평가). 구간은 한 판 안에서 바뀌므로 `RunStarted`가 아니라 `SectionEntered`로 기록한다. 이벤트의 구간은 그 앞의 마지막 `SectionEntered`다.
