using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Records;
using SugarDash.Infrastructure.Logging.Dto;
using Newtonsoft.Json;

namespace SugarDash.Infrastructure.Logging
{
    /// <summary>
    /// <see cref="ILogQuery"/> 구현. LogRepository가 쓴 주행별 .jsonl을 읽어 레코드를 도메인 이벤트 객체로 되돌린다.
    /// 계산은 하지 않는다. 형식이 깨진 줄(쓰기 도중 종료 등)과 모르는 레코드는 건너뛴다.
    /// </summary>
    public sealed class ReportQueryService : ILogQuery
    {
        private const string Extension = ".jsonl";

        private readonly string _directory;

        /// <param name="directory">LogRepository와 같은 기록 폴더.</param>
        public ReportQueryService(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("기록 폴더가 비었다", nameof(directory));
            _directory = directory;
        }

        public IReadOnlyList<string> ListRunIds()
        {
            if (!Directory.Exists(_directory)) return Array.Empty<string>();

            // runId는 시작 시각으로 시작하므로 이름순이 시작 순서다.
            return Directory.GetFiles(_directory, "*" + Extension)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
        }

        public RunRecord LoadRun(string runId)
        {
            if (string.IsNullOrEmpty(runId)) throw new ArgumentException("runId가 비었다", nameof(runId));

            string path = Path.Combine(_directory, runId + Extension);
            if (!File.Exists(path)) return null;

            var events = new List<object>();
            string configVersion = null;
            DateTime? startedAt = null;
            DateTime? endedAt = null;
            int skipped = 0;

            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                LogRecordDto record;
                try
                {
                    record = LogRecordJson.Deserialize(line);
                }
                catch (JsonException)
                {
                    skipped++;
                    continue;
                }
                // 이 버전이 해석할 수 없는 형식은 건너뛴다.
                if (record == null || record.SchemaVersion != LogRepository.SchemaVersion)
                {
                    skipped++;
                    continue;
                }

                configVersion = configVersion ?? record.ConfigVersion;
                try
                {
                    switch (record.Type)
                    {
                        case "RunStarted":
                            startedAt = ParseTime(record.AtUtc);
                            break;
                        case "RunEnded":
                            endedAt = ParseTime(record.AtUtc);
                            break;
                        default:
                            var domainEvent = ToEvent(record);
                            if (domainEvent == null) skipped++;
                            else events.Add(domainEvent);
                            break;
                    }
                }
                catch (FormatException)
                {
                    skipped++;
                }
            }

            return new RunRecord(runId, configVersion, startedAt, endedAt, events, skipped);
        }

        /// <summary>모르는 type이면 null. 필요한 필드가 비어 있으면 FormatException.</summary>
        private static object ToEvent(LogRecordDto r)
        {
            switch (r.Type)
            {
                case "HypoEventTriggered":
                    return new HypoEventTriggered(Req(r.EventId), Req(r.IsRecurrence), Req(r.SimT), Req(r.T),
                        Req(r.Actual));
                case "SuddenDropStarted":
                    return new SuddenDropStarted(Req(r.SimT), Req(r.T));
                case "GaugeWarningRaised":
                    return new GaugeWarningRaised(Req(r.SimT), Req(r.T), Req(r.Displayed));
                case "MeterPressed":
                    return new MeterPressed(Req(r.EventId), Req(r.Measured), Req(r.Unnecessary), Req(r.InGrace),
                        Req(r.SimT), Req(r.T));
                case "StepAnswered":
                    return new StepAnswered(Req(r.EventId), LogCodes.ParseStep(r.Step), r.ChoiceId, Req(r.IsCorrect),
                        LogCodes.ParseError(r.ErrorCode), Req(r.HelpInstead), Req(r.DecisionSec),
                        Req(r.MeasuredBeforeWarning), Req(r.SimT), Req(r.T));
                case "GuardianPromptStarted":
                    return new GuardianPromptStarted(Req(r.EventId), LogCodes.ParseStep(r.Step),
                        LogCodes.ParseError(r.ErrorCode), Req(r.SimT), Req(r.T));
                case "GuardianNotified":
                    return new GuardianNotified(Req(r.EventId), LogCodes.ParseStep(r.Step), Req(r.SimT), Req(r.T));
                case "DoseApplied":
                    return new DoseApplied(Req(r.EventId), r.SnackId, Req(r.Servings), Req(r.ChosenByChild),
                        Req(r.SimT), Req(r.T));
                case "RecheckFastForward":
                    return new RecheckFastForward(Req(r.EventId), Req(r.SimSecSkipped), Req(r.GlucoseBefore),
                        Req(r.GlucoseAfter), Req(r.SimT), Req(r.T));
                case "HypoResolved":
                    return new HypoResolved(Req(r.EventId), Req(r.AllCorrect), Req(r.SimT), Req(r.T));
                case "RunEliminated":
                    return new RunEliminated(Req(r.EventId), LogCodes.ParseStep(r.Step),
                        LogCodes.ParseError(r.ErrorCode), LogCodes.ParseCause(r.Cause), Req(r.SimT), Req(r.T));
                case "GraceStarted":
                    return new GraceStarted(Req(r.EventId), Req(r.SimT), Req(r.T));
                case "GraceEnded":
                    return new GraceEnded(Req(r.EventId), Req(r.SimT), Req(r.T));
                case "TrackSnackEaten":
                    return new TrackSnackEaten(r.SnackId, Req(r.Servings), Req(r.SimT), Req(r.T));
                case "RunCompleted":
                    return new RunCompleted(Req(r.WorldT), Req(r.SimT), Req(r.T));
                default:
                    return null;
            }
        }

        // 필수 필드가 없으면 깨진 레코드로 보고 건너뛴다.
        private static T Req<T>(T? value) where T : struct =>
            value ?? throw new FormatException("필요한 필드가 비었다");

        private static DateTime? ParseTime(string value) =>
            value == null
                ? (DateTime?)null
                : DateTime.ParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }
}
