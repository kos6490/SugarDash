using System;
using System.IO;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Infrastructure.Logging;
using SugarDash.Infrastructure.Logging.Dto;
using NUnit.Framework;

namespace SugarDash.Infrastructure.Tests
{
    public class LogRepositoryTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 27, 9, 30, 15, DateTimeKind.Utc);

        private string _dir;
        private DateTime _now;
        private DomainEventBus _bus;
        private LogRepository _repo;
        private IDisposable _subscription;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SugarDashLogTests-" + Guid.NewGuid().ToString("N"));
            _now = Start;
            _bus = new DomainEventBus();
            _repo = new LogRepository(_dir, () => _now);
            _subscription = _repo.Subscribe(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            _subscription.Dispose();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private string[] RawLines(string path = null) => File.ReadAllLines(path ?? _repo.CurrentPath);

        private LogRecordDto[] Lines(string path = null) =>
            RawLines(path).Select(LogRecordJson.Deserialize).ToArray();

        [Test]
        public void UnrecordedRun_WritesNoFile_IgnoresEvents()
        {
            _repo.BeginUnrecordedRun();
            Assert.That(_repo.CurrentRunId, Is.Null);
            Assert.DoesNotThrow(() =>
            {
                _bus.Publish(new SectionEntered("signal", 12.5, 1, 1));
                _bus.Publish(new GlucoseRangeEntered(95.5, 3, 3));
                _repo.EndRun();
            });

            int files = Directory.Exists(_dir) ? Directory.GetFiles(_dir, "*.jsonl").Length : 0;
            Assert.That(files, Is.EqualTo(0));

            string runId = _repo.BeginRun("cfg-test", TrackType.Type1);
            Assert.That(runId, Is.Not.Null);
            Assert.That(Lines().Select(l => l.Type), Is.EqualTo(new[] { "RunStarted" }));
        }

        [Test]
        public void UnrecordedRun_ThrowsWhenRunOpen()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            Assert.Throws<InvalidOperationException>(() => _repo.BeginUnrecordedRun());
        }

        [Test]
        public void Writes_NewRecordTypes()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new SectionEntered("signal", 12.5, 1, 1));
            _bus.Publish(new GlucoseRangeEntered(95.5, 3, 3));
            _bus.Publish(new GlucoseRangeExited(60.5, 4, 4));
            _bus.Publish(new RunEliminated(0, null, HypoErrorCode.None, EliminationCause.ObstacleHit, 55.5, 5, 5));

            var raw = RawLines();
            var lines = Lines();
            Assert.That(lines.Select(l => l.Type), Is.EqualTo(new[]
            {
                "RunStarted", "SectionEntered", "GlucoseRangeEntered", "GlucoseRangeExited",
                "RunEliminated"
            }));
            Assert.That(lines[1].Section, Is.EqualTo("signal"));
            Assert.That(lines[1].DistanceM, Is.EqualTo(12.5));
            Assert.That(lines[2].Actual, Is.EqualTo(95.5));
            Assert.That(lines[3].Actual, Is.EqualTo(60.5));
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(raw[i], Does.Not.Contain("eventId"), lines[i].Type);
                Assert.That(lines[i].SchemaVersion, Is.EqualTo(2));
            }

            Assert.That(raw[4], Does.Not.Contain("\"step\""));
            Assert.That(raw[4], Does.Not.Contain("errorCode"));
            Assert.That(lines[4].Cause, Is.EqualTo("obstacle_hit"));
            Assert.That(lines[4].DistanceM, Is.EqualTo(55.5));
            Assert.That(lines[4].SchemaVersion, Is.EqualTo(2));
            Assert.That(LogRepository.SchemaVersion, Is.EqualTo(2));
        }

        [Test]
        public void BeginRun_WritesTrackTypeAndConfigVersion()
        {
            _repo.BeginRun("v2", TrackType.Type2);
            _bus.Publish(new SuddenDropStarted(1, 1));

            var raw = RawLines();
            Assert.That(raw[0], Does.Contain("\"type\":\"RunStarted\""));
            Assert.That(raw[0], Does.Contain("\"trackType\":\"type2\""));
            Assert.That(raw[0], Does.Contain("\"configVersion\":\"v2\""));
            Assert.That(raw[1], Does.Contain("\"configVersion\":\"v2\""));
            Assert.That(raw[1], Does.Not.Contain("trackType"));
        }

        [Test]
        public void ConfigVersion_FollowsEachRun()
        {
            string first = _repo.BeginRun("v1", TrackType.Type1);
            _repo.EndRun();
            _repo.BeginRun("v2", TrackType.Type2);

            Assert.That(Lines(Path.Combine(_dir, first + ".jsonl")).Last().ConfigVersion, Is.EqualTo("v1"));
            Assert.That(Lines().Single().ConfigVersion, Is.EqualTo("v2"));
        }

        [Test]
        public void Type2Records_WriteRawFields()
        {
            _repo.BeginRun("v2", TrackType.Type2);
            _bus.Publish(new NoticeStarted(4, NoticeKind.Low, 10, 9, 70));
            _bus.Publish(new FoodEaten("juice", 150, 11, 10));
            _bus.Publish(new RestStarted(160, 12, 11));

            var lines = Lines();
            Assert.That(lines[1].Kind, Is.EqualTo("low"));
            Assert.That(lines[1].EventId, Is.EqualTo(4));
            Assert.That(lines[2].FoodId, Is.EqualTo("juice"));
            Assert.That(RawLines()[2], Does.Not.Contain("eventId"));
            Assert.That(lines[3].Type, Is.EqualTo("RestStarted"));
        }

        [Test]
        public void BeginRun_CreatesRunFileWithRunStarted()
        {
            string runId = _repo.BeginRun("cfg-test", TrackType.Type1);

            Assert.That(runId, Does.StartWith("20261027T093015Z-"));
            Assert.That(_repo.CurrentPath, Is.EqualTo(Path.Combine(_dir, runId + ".jsonl")));

            var first = Lines().Single();
            Assert.That(first.Type, Is.EqualTo("RunStarted"));
            Assert.That(first.SchemaVersion, Is.EqualTo(LogRepository.SchemaVersion));
            Assert.That(first.ConfigVersion, Is.EqualTo("cfg-test"));
            Assert.That(first.RunId, Is.EqualTo(runId));
            Assert.That(first.Seq, Is.EqualTo(1));
            Assert.That(first.AtUtc, Is.EqualTo("2026-10-27T09:30:15.000Z"));
        }

        [Test]
        public void EveryRecord_CarriesVersionsAndIncreasingSeq()
        {
            string runId = _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new HypoEventTriggered(1, true, 30, 26, 68));
            _bus.Publish(new MeterPressed(1, 68, false, false, 31, 27));
            _bus.Publish(new GaugeWarningRaised(32, 28, 79));
            _repo.EndRun();

            var lines = Lines(Path.Combine(_dir, runId + ".jsonl"));
            Assert.That(lines.Select(l => l.Type),
                Is.EqualTo(new[] { "RunStarted", "HypoEventTriggered", "MeterPressed", "GaugeWarningRaised", "RunEnded" }));
            Assert.That(lines.Select(l => l.Seq), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            foreach (var line in lines)
            {
                Assert.That(line.SchemaVersion, Is.EqualTo(LogRepository.SchemaVersion));
                Assert.That(line.ConfigVersion, Is.EqualTo("cfg-test"));
                Assert.That(line.RunId, Is.EqualTo(runId));
            }

            var triggered = lines[1];
            Assert.That(triggered.EventId, Is.EqualTo(1));
            Assert.That(triggered.IsRecurrence, Is.True);
            Assert.That(triggered.SimT, Is.EqualTo(30));
            Assert.That(triggered.T, Is.EqualTo(26));
            Assert.That(triggered.Actual, Is.EqualTo(68));
            Assert.That(RawLines(Path.Combine(_dir, runId + ".jsonl"))[3], Does.Not.Contain("\"eventId\""),
                "게이지 경고는 이벤트에 속하지 않는다");
        }

        [Test]
        public void StepAnswered_WritesCodesAndOmitsEmptyFields()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new StepAnswered(1, HypoStep.Recognize, "meter", true, HypoErrorCode.None, false, 2.5, true,
                30, 26));
            _bus.Publish(new StepAnswered(1, HypoStep.Respond, null, false, HypoErrorCode.RsTimeout, false, 5, false,
                36, 32));

            var lines = Lines();
            var raw = RawLines();
            var correct = lines[1];
            Assert.That(correct.Step, Is.EqualTo("recognize"));
            Assert.That(correct.ChoiceId, Is.EqualTo("meter"));
            Assert.That(correct.IsCorrect, Is.True);
            Assert.That(raw[1], Does.Not.Contain("\"errorCode\""));
            Assert.That(correct.DecisionSec, Is.EqualTo(2.5));
            Assert.That(correct.MeasuredBeforeWarning, Is.True);
            Assert.That(correct.HelpInstead, Is.False);

            var timeout = lines[2];
            Assert.That(timeout.ErrorCode, Is.EqualTo("RS_TIMEOUT"));
            Assert.That(raw[2], Does.Not.Contain("\"choiceId\""), "시간 초과면 고른 선택지가 없다");
        }

        [Test]
        public void HypoEventRecords_KeepRawFields()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new GuardianPromptStarted(1, HypoStep.Dose, HypoErrorCode.DsOver, 40, 35));
            _bus.Publish(new GuardianNotified(1, HypoStep.Dose, 41, 36));
            _bus.Publish(new DoseApplied(1, "juice", 3, true, 41, 36));
            _bus.Publish(new RecheckFastForward(1, 6, 60, 85, 47, 36));
            _bus.Publish(new HypoResolved(1, false, 48, 37));
            _bus.Publish(new GraceStarted(1, 49, 38));
            _bus.Publish(new GraceEnded(1, 52, 41));
            _bus.Publish(new MeterPressed(0, 90, true, true, 50, 39));
            _bus.Publish(new SuddenDropStarted(60, 49));
            _bus.Publish(new RunEliminated(2, HypoStep.Recognize, HypoErrorCode.SMissed,
                EliminationCause.PromptTimeout, 0, 90, 79));
            _bus.Publish(new TrackSnackEaten("juice", 0.5, 95, 84));

            var lines = Lines();
            Assert.That(lines[1].ErrorCode, Is.EqualTo("DS_OVER"));
            Assert.That(lines[1].Step, Is.EqualTo("dose"));
            Assert.That(lines[2].Type, Is.EqualTo("GuardianNotified"));
            Assert.That(lines[3].SnackId, Is.EqualTo("juice"));
            Assert.That(lines[3].Servings, Is.EqualTo(3));
            Assert.That(lines[3].ChosenByChild, Is.True);
            Assert.That(lines[4].SimSecSkipped, Is.EqualTo(6));
            Assert.That(lines[4].GlucoseBefore, Is.EqualTo(60));
            Assert.That(lines[4].GlucoseAfter, Is.EqualTo(85));
            Assert.That(lines[5].AllCorrect, Is.False);
            Assert.That(lines[6].Type, Is.EqualTo("GraceStarted"));
            Assert.That(lines[7].Type, Is.EqualTo("GraceEnded"));
            Assert.That(lines[8].EventId, Is.EqualTo(0));
            Assert.That(lines[8].Unnecessary, Is.True);
            Assert.That(lines[8].InGrace, Is.True);
            Assert.That(lines[9].Type, Is.EqualTo("SuddenDropStarted"));
            Assert.That(lines[10].Step, Is.EqualTo("recognize"));
            Assert.That(lines[10].ErrorCode, Is.EqualTo("S_MISSED"));
            Assert.That(lines[10].Cause, Is.EqualTo("prompt_timeout"));
            Assert.That(lines[11].Type, Is.EqualTo("TrackSnackEaten"));
            Assert.That(lines[11].SnackId, Is.EqualTo("juice"));
            Assert.That(lines[11].Servings, Is.EqualTo(0.5));
            Assert.That(RawLines()[11], Does.Not.Contain("\"eventId\""));
        }

        [Test]
        public void HyperEventRecords_KeepRawFieldsAndCodes()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new HyperEventTriggered(2, 60, 50, 255));
            _bus.Publish(new StepAnswered(2, HypoStep.HyperNotify, null, false, HypoErrorCode.HyTimeout, false, 5, false,
                61, 55));
            _bus.Publish(new GuardianPromptStarted(2, HypoStep.HyperNotify, HypoErrorCode.HyTimeout, 61, 55));
            _bus.Publish(new HyperResolved(2, false, 62, 56));

            var lines = Lines().Skip(1).ToArray();
            Assert.That(lines.Select(l => l.Type),
                Is.EqualTo(new[] { "HyperEventTriggered", "StepAnswered", "GuardianPromptStarted", "HyperResolved" }));
            Assert.That(lines[0].EventId, Is.EqualTo(2));
            Assert.That(lines[0].Actual, Is.EqualTo(255));
            Assert.That(lines[0].IsRecurrence, Is.Null);
            Assert.That(lines[1].Step, Is.EqualTo("hyper_notify"));
            Assert.That(lines[1].ErrorCode, Is.EqualTo("HY_TIMEOUT"));
            Assert.That(lines[2].Step, Is.EqualTo("hyper_notify"));
            Assert.That(lines[3].AllCorrect, Is.False);
        }

        [Test]
        public void Append_KeepsExistingBytes()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new GaugeWarningRaised(1, 1, 79));
            byte[] before = File.ReadAllBytes(_repo.CurrentPath);

            _bus.Publish(new SuddenDropStarted(2, 2));
            byte[] after = File.ReadAllBytes(_repo.CurrentPath);

            Assert.That(after.Length, Is.GreaterThan(before.Length));
            Assert.That(after.Take(before.Length), Is.EqualTo(before));
            Assert.That(before.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), "BOM 없이 쓴다");
        }

        [Test]
        public void EachRun_GetsItsOwnFile()
        {
            string first = _repo.BeginRun("cfg-test", TrackType.Type1);
            _repo.EndRun();
            _now = Start.AddMinutes(3);
            string second = _repo.BeginRun("cfg-test", TrackType.Type1);

            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(Directory.GetFiles(_dir, "*.jsonl"), Has.Length.EqualTo(2));
            Assert.That(Lines(Path.Combine(_dir, first + ".jsonl")).Last().Type, Is.EqualTo("RunEnded"));
            Assert.That(Lines().Single().Type, Is.EqualTo("RunStarted"));
        }

        [Test]
        public void BeginRun_WhileOpen_Throws()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);

            Assert.Throws<InvalidOperationException>(() => _repo.BeginRun("cfg-test", TrackType.Type1));
        }

        [Test]
        public void EndRun_WithoutRun_DoesNothing()
        {
            _repo.EndRun();

            Assert.That(Directory.Exists(_dir), Is.False);
        }

        [Test]
        public void EventWithoutRun_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _bus.Publish(new SuddenDropStarted(1, 1)));
        }

        [Test]
        public void AfterDispose_EventsAreNotRecorded()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _subscription.Dispose();

            _bus.Publish(new SuddenDropStarted(1, 1));

            Assert.That(Lines(), Has.Length.EqualTo(1));
        }

        [Test]
        public void EmptyConfigVersion_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => _repo.BeginRun("", TrackType.Type1));
        }

        [Test]
        public void LogCodes_CoverEveryEnumValue()
        {
            foreach (HypoStep step in Enum.GetValues(typeof(HypoStep)))
                Assert.That(LogCodes.Step(step), Is.Not.Empty);
            foreach (HypoErrorCode code in Enum.GetValues(typeof(HypoErrorCode)))
            {
                if (code == HypoErrorCode.None) Assert.That(LogCodes.Error(code), Is.Null);
                else Assert.That(LogCodes.Error(code), Is.Not.Empty);
            }
            foreach (TrackType type in Enum.GetValues(typeof(TrackType)))
                Assert.That(LogCodes.ParseTrackType(LogCodes.TrackType(type)), Is.EqualTo(type));
            foreach (NoticeKind kind in Enum.GetValues(typeof(NoticeKind)))
                Assert.That(LogCodes.ParseNoticeKind(LogCodes.NoticeKind(kind)), Is.EqualTo(kind));
            foreach (EliminationCause cause in Enum.GetValues(typeof(EliminationCause)))
                Assert.That(LogCodes.Cause(cause), Is.Not.Empty);
            foreach (HabitMoment moment in Enum.GetValues(typeof(HabitMoment)))
                Assert.That(LogCodes.ParseHabitMoment(LogCodes.HabitMoment(moment)), Is.EqualTo(moment));
            foreach (HabitCheckOutcome outcome in Enum.GetValues(typeof(HabitCheckOutcome)))
                Assert.That(LogCodes.ParseHabitOutcome(LogCodes.HabitOutcome(outcome)), Is.EqualTo(outcome));
        }

        [Test]
        public void HabitCheck_IsRecorded_AndMeterMarksMoment()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new MeterPressed(0, 130, false, false, 30, 20, HabitMoment.Bedtime));
            _bus.Publish(new HabitCheckEnded(HabitMoment.Bedtime, HabitCheckOutcome.Checked, 2.5, 30, 20));
            _bus.Publish(new HabitCheckEnded(HabitMoment.PreExercise, HabitCheckOutcome.Missed, null, 40, 30));
            _bus.Publish(new MeterPressed(0, 130, true, false, 41, 31));

            var raw = RawLines();
            var lines = Lines();
            Assert.That(lines[1].Moment, Is.EqualTo("bedtime"));
            Assert.That(lines[2].Type, Is.EqualTo("HabitCheck"));
            Assert.That(lines[2].Moment, Is.EqualTo("bedtime"));
            Assert.That(lines[2].Outcome, Is.EqualTo("checked"));
            Assert.That(lines[2].ReactionSec, Is.EqualTo(2.5));
            Assert.That(lines[3].Outcome, Is.EqualTo("missed"));
            Assert.That(raw[3], Does.Not.Contain("reactionSec"));
            Assert.That(raw[4], Does.Not.Contain("moment"));
        }

        [Test]
        public void SceneEntered_IsRecorded()
        {
            _repo.BeginRun("cfg-test", TrackType.Type1);
            _bus.Publish(new SceneEntered("playground", TimeOfDay.Afternoon, 1, 412.5, 30, 20));

            var raw = RawLines();
            var line = Lines()[1];
            Assert.That(line.Type, Is.EqualTo("SceneEntered"));
            Assert.That(line.Scene, Is.EqualTo("playground"));
            Assert.That(line.TimeOfDay, Is.EqualTo("afternoon"));
            Assert.That(line.DayIndex, Is.EqualTo(1));
            Assert.That(line.DistanceM, Is.EqualTo(412.5));
            Assert.That(line.SimT, Is.EqualTo(30));
            Assert.That(line.T, Is.EqualTo(20));
            Assert.That(line.SchemaVersion, Is.EqualTo(2));
            Assert.That(raw[1], Does.Not.Contain("eventId"));
            Assert.That(raw[1], Does.Contain("\"timeOfDay\":\"afternoon\""));
        }
    }
}
