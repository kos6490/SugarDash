using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SugarDash.Domain.Events;
using SugarDash.Domain.Ports;
using SugarDash.Infrastructure.Logging.Dto;

namespace SugarDash.Infrastructure.Logging
{
    /// <summary>
    /// <see cref="ILogRepository"/> 구현. DomainEventBus의 도메인 이벤트를 원시 기록으로 바꿔
    /// 주행마다 파일 하나(<c>{runId}.jsonl</c>)에 한 줄씩 추가만 한다. 기존 내용을 읽거나 다시 쓰지 않는다.
    /// 레코드마다 configVersion과 schemaVersion을 싣는다. 형식은 docs/log-format.md에 있다.
    /// </summary>
    public sealed class LogRepository : ILogRepository
    {
        /// <summary>레코드 형식을 바꾸면 올린다.</summary>
        public const int SchemaVersion = 1;

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _directory;
        private readonly string _configVersion;
        private readonly Func<DateTime> _utcNow;

        private string _runId;
        private string _path;
        private int _seq;

        /// <param name="directory">기록 폴더. Bootstrap이 <c>Application.persistentDataPath</c> 아래 경로를 넘긴다.</param>
        /// <param name="configVersion">불러온 설정의 버전.</param>
        /// <param name="utcNow">주행 시작·종료 시각과 runId에 쓰는 UTC 시계. 생략하면 시스템 시계.</param>
        public LogRepository(string directory, string configVersion, Func<DateTime> utcNow = null)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("기록 폴더가 비었다", nameof(directory));
            if (string.IsNullOrEmpty(configVersion))
                throw new ArgumentException("configVersion이 비었다", nameof(configVersion));

            _directory = directory;
            _configVersion = configVersion;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>열린 주행의 runId. 없으면 null.</summary>
        public string CurrentRunId => _runId;

        /// <summary>열린 주행의 기록 파일 경로. 없으면 null.</summary>
        public string CurrentPath => _path;

        /// <summary>
        /// 기록할 도메인 이벤트를 모두 구독한다. GameBootstrapper가 연결하고, 씬 전환·파괴 시 반환값을 Dispose한다.
        /// </summary>
        public IDisposable Subscribe(DomainEventBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            return new CompositeDisposable(new[]
            {
                bus.Subscribe<HypoEventTriggered>(e => Append(Event("HypoEventTriggered", e.EventId, e.SimT, e.T, r =>
                {
                    r.IsRecurrence = e.IsRecurrence;
                    r.Actual = e.Actual;
                }))),
                bus.Subscribe<SuddenDropStarted>(e => Append(Event("SuddenDropStarted", null, e.SimT, e.T))),
                bus.Subscribe<GaugeWarningRaised>(e => Append(Event("GaugeWarningRaised", null, e.SimT, e.T,
                    r => r.Displayed = e.Displayed))),
                bus.Subscribe<MeterPressed>(e => Append(Event("MeterPressed", e.EventId, e.SimT, e.T, r =>
                {
                    r.Measured = e.Measured;
                    r.Unnecessary = e.Unnecessary;
                    r.InGrace = e.InGrace;
                }))),
                bus.Subscribe<StepAnswered>(e => Append(Event("StepAnswered", e.EventId, e.SimT, e.T, r =>
                {
                    r.Step = LogCodes.Step(e.Step);
                    r.ChoiceId = e.ChoiceId;
                    r.IsCorrect = e.IsCorrect;
                    r.ErrorCode = LogCodes.Error(e.ErrorCode);
                    r.HelpInstead = e.HelpInstead;
                    r.DecisionSec = e.DecisionSec;
                    r.MeasuredBeforeWarning = e.MeasuredBeforeWarning;
                }))),
                bus.Subscribe<GuardianPromptStarted>(e => Append(Event("GuardianPromptStarted", e.EventId, e.SimT,
                    e.T, r =>
                    {
                        r.Step = LogCodes.Step(e.Step);
                        r.ErrorCode = LogCodes.Error(e.ErrorCode);
                    }))),
                bus.Subscribe<GuardianNotified>(e => Append(Event("GuardianNotified", e.EventId, e.SimT, e.T,
                    r => r.Step = LogCodes.Step(e.Step)))),
                bus.Subscribe<DoseApplied>(e => Append(Event("DoseApplied", e.EventId, e.SimT, e.T, r =>
                {
                    r.SnackId = e.SnackId;
                    r.Servings = e.Servings;
                    r.ChosenByChild = e.ChosenByChild;
                }))),
                bus.Subscribe<RecheckFastForward>(e => Append(Event("RecheckFastForward", e.EventId, e.SimT, e.T,
                    r =>
                    {
                        r.SimSecSkipped = e.SimSecSkipped;
                        r.GlucoseBefore = e.GlucoseBefore;
                        r.GlucoseAfter = e.GlucoseAfter;
                    }))),
                bus.Subscribe<HypoResolved>(e => Append(Event("HypoResolved", e.EventId, e.SimT, e.T,
                    r => r.AllCorrect = e.AllCorrect))),
                bus.Subscribe<RunEliminated>(e => Append(Event("RunEliminated", e.EventId, e.SimT, e.T, r =>
                {
                    r.Step = LogCodes.Step(e.Step);
                    r.ErrorCode = LogCodes.Error(e.Reason);
                    r.Cause = LogCodes.Cause(e.Cause);
                }))),
                bus.Subscribe<GraceStarted>(e => Append(Event("GraceStarted", e.EventId, e.SimT, e.T))),
                bus.Subscribe<GraceEnded>(e => Append(Event("GraceEnded", e.EventId, e.SimT, e.T))),
                bus.Subscribe<TrackSnackEaten>(e => Append(Event("TrackSnackEaten", null, e.SimT, e.T, r =>
                {
                    r.SnackId = e.SnackId;
                    r.Servings = e.Servings;
                }))),
                bus.Subscribe<RunCompleted>(e => Append(Event("RunCompleted", null, e.SimT, e.T,
                    r => r.WorldT = e.WorldT))),
            });
        }

        public string BeginRun()
        {
            if (_runId != null) throw new InvalidOperationException($"이미 열린 주행이 있다 ({_runId})");

            DateTime now = _utcNow();
            Directory.CreateDirectory(_directory);
            _runId = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) + "-" +
                     Guid.NewGuid().ToString("N").Substring(0, 8);
            _path = Path.Combine(_directory, _runId + ".jsonl");
            _seq = 0;

            Append(new LogRecordDto { Type = "RunStarted", AtUtc = Iso(now) });
            return _runId;
        }

        public void EndRun()
        {
            if (_runId == null) return;

            Append(new LogRecordDto { Type = "RunEnded", AtUtc = Iso(_utcNow()) });
            _runId = null;
            _path = null;
        }

        private static LogRecordDto Event(string type, int? eventId, double simT, double t,
            Action<LogRecordDto> fill = null)
        {
            var record = new LogRecordDto { Type = type, EventId = eventId, SimT = simT, T = t };
            fill?.Invoke(record);
            return record;
        }

        private void Append(LogRecordDto record)
        {
            // 주행 밖에서 도메인 이벤트가 오면 조립(Bootstrap)이나 주행 흐름의 버그다.
            if (_runId == null) throw new InvalidOperationException($"열린 주행 없이 기록하려 했다 ({record.Type})");

            record.SchemaVersion = SchemaVersion;
            record.ConfigVersion = _configVersion;
            record.RunId = _runId;
            record.Seq = ++_seq;

            string line = LogRecordJson.Serialize(record) + "\n";
            try
            {
                File.AppendAllText(_path, line, Utf8NoBom);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // 도메인 이벤트 발행 도중이라 예외를 올리면 상태 기계가 전이 중간에 멈춘다. 기록만 잃고 게임은 계속한다.
                UnityEngine.Debug.LogException(e);
            }
        }

        private static string Iso(DateTime utc) =>
            utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        private sealed class CompositeDisposable : IDisposable
        {
            private IReadOnlyList<IDisposable> _items;

            public CompositeDisposable(IReadOnlyList<IDisposable> items) => _items = items;

            public void Dispose()
            {
                if (_items == null) return;
                foreach (var item in _items) item.Dispose();
                _items = null;
            }
        }
    }
}
