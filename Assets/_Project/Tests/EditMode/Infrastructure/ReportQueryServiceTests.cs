using System;
using System.IO;
using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Infrastructure.Logging;
using NUnit.Framework;

namespace SugarDash.Infrastructure.Tests
{
    public class ReportQueryServiceTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

        private string _dir;
        private DateTime _now;
        private DomainEventBus _bus;
        private LogRepository _repo;
        private IDisposable _subscription;
        private ReportQueryService _query;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SugarDashQueryTests-" + Guid.NewGuid().ToString("N"));
            _now = Start;
            _bus = new DomainEventBus();
            _repo = new LogRepository(_dir, "cfg-test", () => _now);
            _subscription = _repo.Subscribe(_bus);
            _query = new ReportQueryService(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            _subscription.Dispose();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        /// <summary>LogRepository가 기록하는 도메인 이벤트 전 종류. 값은 서로 다르게 둔다.</summary>
        private static object[] AllEvents() => new object[]
        {
            new HypoEventTriggered(1, true, 30.5, 26.25, 68.5),
            new SuddenDropStarted(31, 27),
            new GaugeWarningRaised(32, 28, 79.5),
            new MeterPressed(1, 68, false, true, 33, 29),
            new StepAnswered(1, HypoStep.Recognize, "meter", true, HypoErrorCode.None, false, 2.5, true, 34, 30),
            new StepAnswered(1, HypoStep.Respond, null, false, HypoErrorCode.RsTimeout, false, 5, false, 39, 35),
            new GuardianPromptStarted(1, HypoStep.Respond, HypoErrorCode.RsTimeout, 39, 35),
            new GuardianNotified(1, HypoStep.Respond, 40, 36),
            new DoseApplied(1, "juice", 2, false, 41, 37),
            new RecheckFastForward(1, 6, 60, 85, 47, 37),
            new HypoResolved(1, false, 48, 38),
            new GraceStarted(1, 49, 39),
            new GraceEnded(1, 52, 42),
            new TrackSnackEaten("cracker", 0.5, 53, 43),
            new RunEliminated(2, HypoStep.Dose, HypoErrorCode.DsOver, EliminationCause.Declined, 90, 80),
            new RunCompleted(40, 120, 110),
        };

        /// <summary>런타임 타입으로 발행한다(구독은 타입별이다).</summary>
        private void Publish(object domainEvent) =>
            typeof(DomainEventBus).GetMethod(nameof(DomainEventBus.Publish))
                .MakeGenericMethod(domainEvent.GetType())
                .Invoke(_bus, new[] { domainEvent });

        private static void AssertSameProperties(object expected, object actual)
        {
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()));
            foreach (var property in expected.GetType().GetProperties())
            {
                Assert.That(property.GetValue(actual), Is.EqualTo(property.GetValue(expected)),
                    $"{expected.GetType().Name}.{property.Name}");
            }
        }

        [Test]
        public void LoadRun_RestoresEveryEventInOrder()
        {
            string runId = _repo.BeginRun();
            var published = AllEvents();
            foreach (var e in published) Publish(e);
            _now = Start.AddMinutes(2);
            _repo.EndRun();

            var run = _query.LoadRun(runId);

            Assert.That(run.RunId, Is.EqualTo(runId));
            Assert.That(run.ConfigVersion, Is.EqualTo("cfg-test"));
            Assert.That(run.StartedAtUtc, Is.EqualTo(Start));
            Assert.That(run.EndedAtUtc, Is.EqualTo(Start.AddMinutes(2)));
            Assert.That(run.SkippedLineCount, Is.EqualTo(0));
            Assert.That(run.Events, Has.Count.EqualTo(published.Length));
            for (int i = 0; i < published.Length; i++) AssertSameProperties(published[i], run.Events[i]);
        }

        [Test]
        public void BrokenLastLine_IsSkipped()
        {
            string runId = _repo.BeginRun();
            _bus.Publish(new SuddenDropStarted(1, 1));
            File.AppendAllText(Path.Combine(_dir, runId + ".jsonl"), "{\"schemaVersion\":1,\"ty");

            var run = _query.LoadRun(runId);

            Assert.That(run.Events, Has.Count.EqualTo(1));
            Assert.That(run.SkippedLineCount, Is.EqualTo(1));
            Assert.That(run.EndedAtUtc, Is.Null, "RunEnded가 없으면 중간 종료");
        }

        [Test]
        public void UnknownTypeOtherSchemaOrMissingField_AreSkipped()
        {
            string runId = _repo.BeginRun();
            string path = Path.Combine(_dir, runId + ".jsonl");
            File.AppendAllText(path,
                "{\"schemaVersion\":1,\"configVersion\":\"cfg-test\",\"runId\":\"x\",\"seq\":2,\"type\":\"Future\"}\n" +
                "{\"schemaVersion\":99,\"configVersion\":\"cfg-test\",\"runId\":\"x\",\"seq\":3,\"type\":\"SuddenDropStarted\",\"simT\":1,\"t\":1}\n" +
                "{\"schemaVersion\":1,\"configVersion\":\"cfg-test\",\"runId\":\"x\",\"seq\":4,\"type\":\"SuddenDropStarted\",\"simT\":1}\n" +
                "{\"schemaVersion\":1,\"configVersion\":\"cfg-test\",\"runId\":\"x\",\"seq\":5,\"type\":\"GuardianNotified\",\"eventId\":1,\"step\":\"jump\",\"simT\":1,\"t\":1}\n");

            var run = _query.LoadRun(runId);

            Assert.That(run.Events, Is.Empty);
            Assert.That(run.SkippedLineCount, Is.EqualTo(4));
        }

        [Test]
        public void ListRunIds_ReturnsOldestFirst()
        {
            Assert.That(_query.ListRunIds(), Is.Empty, "기록 폴더가 없으면 빈 목록");

            _now = Start.AddHours(1);
            string later = _repo.BeginRun();
            _repo.EndRun();
            _now = Start;
            string earlier = _repo.BeginRun();
            _repo.EndRun();

            Assert.That(_query.ListRunIds(), Is.EqualTo(new[] { earlier, later }));
        }

        [Test]
        public void LoadRun_MissingRun_ReturnsNull()
        {
            Assert.That(_query.LoadRun("nope"), Is.Null);
        }

        [Test]
        public void LogCodes_ParseRoundTripsEveryValue()
        {
            foreach (HypoStep step in Enum.GetValues(typeof(HypoStep)))
                Assert.That(LogCodes.ParseStep(LogCodes.Step(step)), Is.EqualTo(step));
            foreach (HypoErrorCode code in Enum.GetValues(typeof(HypoErrorCode)))
                Assert.That(LogCodes.ParseError(LogCodes.Error(code)), Is.EqualTo(code));
            foreach (EliminationCause cause in Enum.GetValues(typeof(EliminationCause)))
                Assert.That(LogCodes.ParseCause(LogCodes.Cause(cause)), Is.EqualTo(cause));

            Assert.Throws<FormatException>(() => LogCodes.ParseStep("jump"));
        }
    }
}
