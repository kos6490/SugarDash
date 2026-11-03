using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SugarDash.Domain.Hypo;
using SugarDash.Infrastructure.Config.Dto;
using SugarDash.Infrastructure.Logging;
using SugarDash.Domain.Config;
using SugarDash.Infrastructure.Config;
using NUnit.Framework;
using UnityEngine;

namespace SugarDash.Infrastructure.Tests
{
    public class ConfigProviderTests
    {
        private const string BuildConfigPath = "_Project/Config/game-config.json";
        private const string DevConfigPath = "_Project/Config/Dev/game-config.dev.json";
        private const string BuildType2ConfigPath = "_Project/Config/game-config.type2.json";
        private const string DevType2ConfigPath = "_Project/Config/Dev/game-config.type2.dev.json";

        // 테스트 전용 2형 픽스처. 실제 게임 수치가 아니다.
        private const string ValidType2Json = @"{
  ""configVersion"": ""test-type2"",
  ""trackType"": ""type2"",
  ""unit"": ""mg/dL"",
  ""time"": { ""timeScale"": 150 },
  ""glucose"": {
    ""initial"": 120, ""min"": 20, ""max"": 400, ""hypoThreshold"": 70, ""hypoReleaseMargin"": 10,
    ""hyperThreshold"": 180, ""hyperReleaseMargin"": 20, ""baseDecayPerSec"": 0.3, ""runDecayPerSec"": 0.7,
    ""normalRangeMin"": 72
  },
  ""type2"": {
    ""highHoldSec"": 4, ""lowHoldSec"": 4, ""noticeSec"": 2, ""tellTimeLimitSec"": 8,
    ""guardianPromptTimeLimitSec"": 5, ""answerFeedbackSec"": 1, ""resumeCountdownSec"": 3, ""graceSec"": 3,
    ""guardianSnackId"": ""juice"", ""juiceChoiceFoodId"": ""juice""
  },
  ""foods"": [
    { ""id"": ""water"", ""totalRise"": 0, ""onsetRealMin"": 1, ""absorbRealMin"": 1, ""source"": ""test"" },
    { ""id"": ""juice"", ""totalRise"": 30, ""onsetRealMin"": 2.5, ""absorbRealMin"": 5, ""source"": ""test"" }
  ],
  ""runner"": {
    ""foodRowIntervalMinSec"": 4, ""foodRowIntervalMaxSec"": 7,
    ""restIntervalMinSec"": 15, ""restIntervalMaxSec"": 25, ""restDurationSec"": 3
  },
  ""elimination"": { ""glucoseLow"": 40, ""glucoseHigh"": 300 },
  ""score"": {
    ""inRangePerSec"": 1, ""perMeter"": 0.1,
    ""correct"": { ""hyperNotify"": 10, ""lowNotify"": 11 },
    ""errors"": { ""HY_KEEP_RUNNING"": 0, ""HY_JUICE"": 2, ""HY_TIMEOUT"": 0, ""LO_KEEP_RUNNING"": 0, ""LO_TIMEOUT"": 0 }
  },
  ""sections"": [ { ""id"": ""signal"" } ]
}";

        // 테스트 전용 픽스처. 실제 게임 수치가 아니다.
        private const string ValidJson = @"{
  ""configVersion"": ""test"",
  ""trackType"": ""type1"",
  ""unit"": ""mg/dL"",
  ""time"": { ""timeScale"": 150, ""recheckIntervalRealMin"": 15, ""recheckIntervalSource"": ""test"",
    ""fastForwardTickSimSec"": 0.5 },
  ""glucose"": {
    ""initial"": 120, ""min"": 20, ""max"": 400, ""hypoThreshold"": 70, ""hypoReleaseMargin"": 10,
    ""hyperThreshold"": 250, ""hyperReleaseMargin"": 20, ""baseDecayPerSec"": 0.5, ""runDecayPerSec"": 1, ""normalRangeMin"": 72
  },
  ""recurrence"": { ""cooldownSec"": 10 },
  ""display"": { ""delaySec"": 3, ""warningThreshold"": 80 },
  ""hypo"": {
    ""step1TimeLimitSec"": 5, ""step2TimeLimitSec"": 5, ""step3TimeLimitSec"": 5, ""step4TimeLimitSec"": 5,
    ""guardianPromptTimeLimitSec"": 5, ""resumeCountdownSec"": 1, ""graceSec"": 3, ""answerFeedbackSec"": 0,
    ""guardianSnackId"": ""juice"", ""doses"": { ""under"": 1, ""proper"": 2, ""over"": 3 }
  },
  ""hyper"": { ""notifyTimeLimitSec"": 5 },
  ""suddenDrop"": { ""chancePerSec"": 0.01, ""amount"": 30, ""durationSec"": 5 },
  ""snacks"": [
    { ""id"": ""juice"", ""kind"": ""fast"", ""totalRise"": 30, ""onsetSec"": 1, ""absorbSec"": 2, ""source"": ""test"" },
    { ""id"": ""cracker"", ""kind"": ""slow"", ""totalRise"": 30, ""onsetSec"": 4, ""absorbSec"": 8, ""source"": ""test"" }
  ],
  ""runner"": { ""trackSnackServings"": 0.5,
    ""trackSnackIntervalMinSec"": 6, ""trackSnackIntervalMaxSec"": 10 },
  ""elimination"": { ""glucoseLow"": 40, ""glucoseHigh"": 300 },
  ""symptom"": { ""shakeStartGlucose"": 90 },
  ""score"": {
    ""inRangePerSec"": 1, ""perMeter"": 0.1,
    ""correct"": { ""recognize"": 10, ""respond"": 11, ""dose"": 12, ""remeasure"": 13, ""notify"": 14, ""hyperNotify"": 15 },
    ""errors"": { ""S_MISSED"": 0, ""RS_SLOW"": 2, ""RS_KEEP_RUNNING"": 0, ""RS_NONE"": 0, ""RS_TIMEOUT"": 0,
      ""HELP_INSTEAD"": 2, ""DS_UNDER"": 2, ""DS_OVER"": 2, ""DS_TIMEOUT"": 0,
      ""RC_NO_RECHECK"": 0, ""RC_NO_NOTIFY"": 0, ""HY_KEEP_RUNNING"": 0, ""HY_TIMEOUT"": 0 }
  },
  ""sections"": [ { ""id"": ""signal"" } ]
}";

        /// <summary>픽스처에서 정확히 한 곳을 바꾼다.</summary>
        private static string With(string json, string from, string to)
        {
            int index = json.IndexOf(from, System.StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "픽스처에 없는 문자열: " + from);
            Assert.That(json.IndexOf(from, index + 1, System.StringComparison.Ordinal), Is.EqualTo(-1),
                "픽스처에 두 번 이상 나오는 문자열: " + from);
            return json.Substring(0, index) + to + json.Substring(index + from.Length);
        }

        private static ConfigValidationException LoadFails(string json) =>
            Assert.Throws<ConfigValidationException>(() => new ConfigProvider(json).Load());

        [Test]
        public void ValidJson_MapsToDomainConfig()
        {
            var config = new ConfigProvider(ValidJson).Load();

            Assert.That(config.ConfigVersion, Is.EqualTo("test"));
            Assert.That(config.Glucose.Levels.HypoThreshold, Is.EqualTo(70));
            Assert.That(config.Glucose.Display.DelaySec, Is.EqualTo(3));
            Assert.That(config.Glucose.Time.RecheckIntervalGameSec, Is.EqualTo(6).Within(1e-9));
            Assert.That(config.Hypo.Step1TimeLimitSec, Is.EqualTo(5));
            Assert.That(config.Glucose.Snacks, Has.Count.EqualTo(2));
            Assert.That(config.Glucose.Snacks[0].Kind, Is.EqualTo(SnackKind.Fast));
            Assert.That(config.Glucose.Snacks[1].Kind, Is.EqualTo(SnackKind.Slow));
            Assert.That(config.Hypo.Step3TimeLimitSec, Is.EqualTo(5));
            Assert.That(config.Hypo.GuardianSnackId, Is.EqualTo("juice"));
            Assert.That(config.Hypo.Doses.Proper, Is.EqualTo(2));
            Assert.That(config.Hypo.AnswerFeedbackSec, Is.EqualTo(0));
            Assert.That(config.Glucose.Time.FastForwardTickSimSec, Is.EqualTo(0.5));
            Assert.That(config.Glucose.Levels.NormalRangeMin, Is.EqualTo(72));
            Assert.That(config.Runner.TrackSnackServings, Is.EqualTo(0.5));
            Assert.That(config.Runner.TrackSnackIntervalMinSec, Is.EqualTo(6));
            Assert.That(config.Runner.TrackSnackIntervalMaxSec, Is.EqualTo(10));
            Assert.That(config.Elimination.GlucoseLow, Is.EqualTo(40));
            Assert.That(config.Symptom.ShakeStartGlucose, Is.EqualTo(90));
            Assert.That(config.Elimination.GlucoseHigh, Is.EqualTo(300));
            Assert.That(config.Score.InRangePerSec, Is.EqualTo(1));
            Assert.That(config.Score.ErrorWeights[HypoErrorCode.SMissed], Is.EqualTo(0));
            Assert.That(config.Score.ErrorWeights[HypoErrorCode.RsSlow], Is.EqualTo(2));
            Assert.That(config.Score.CorrectWeights[HypoStep.HyperNotify], Is.EqualTo(15));
            Assert.That(config.Score.CorrectWeights[HypoStep.Dose], Is.EqualTo(12));
            Assert.That(config.Sections.Single().Id, Is.EqualTo("signal"));
            Assert.That(config.Sections.Single().LengthM, Is.Null);
            Assert.That(config.Glucose.Levels.HyperReleaseMargin, Is.EqualTo(20));
            Assert.That(config.Hyper.NotifyTimeLimitSec, Is.EqualTo(5));
        }

        [Test]
        public void UnknownKey_Fails()
        {
            var json = With(ValidJson, "\"initial\": 120,", "\"initial\": 120, \"initail\": 120,");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Count.EqualTo(1));
            Assert.That(e.Violations[0], Does.Contain("initail"));
        }

        [Test]
        public void NullValues_AreAllReportedByPath()
        {
            var json = With(ValidJson, "\"initial\": 120", "\"initial\": null");
            json = With(json, "\"onsetSec\": 4", "\"onsetSec\": null");
            json = With(json, "\"configVersion\": \"test\"", "\"configVersion\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths,
                Is.EquivalentTo(new[] { "glucose.initial", "snacks[1].onsetSec", "configVersion" }));
            Assert.That(e.Violations, Is.Empty, "빈 값이 있으면 불변식 검증은 하지 않는다");
        }

        [Test]
        public void MissingSection_IsReportedOnce()
        {
            int start = ValidJson.IndexOf("\"hypo\"", System.StringComparison.Ordinal);
            int end = ValidJson.IndexOf("\"hyper\"", System.StringComparison.Ordinal);
            var json = ValidJson.Remove(start, end - start);

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "hypo" }));
        }

        [Test]
        public void NewHypoFields_NullAreReportedByPath()
        {
            var json = With(ValidJson, "\"graceSec\": 3", "\"graceSec\": null");
            json = With(json, "\"proper\": 2", "\"proper\": null");
            json = With(json, "\"guardianSnackId\": \"juice\"", "\"guardianSnackId\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths,
                Is.EquivalentTo(new[] { "hypo.graceSec", "hypo.doses.proper", "hypo.guardianSnackId" }));
        }

        [Test]
        public void AnswerFeedback_MissingIsReportedByPath()
        {
            var json = With(ValidJson, ", \"answerFeedbackSec\": 0", "");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "hypo.answerFeedbackSec" }));
        }

        [Test]
        public void AnswerFeedback_NegativeIsViolation()
        {
            var json = With(ValidJson, "\"answerFeedbackSec\": 0", "\"answerFeedbackSec\": -1");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Count.EqualTo(1));
            Assert.That(e.Violations[0], Does.Contain("hypo.answerFeedbackSec"));
        }

        [Test]
        public void RunnerFields_NullAreReportedByPath()
        {
            var json = With(ValidJson, "\"trackSnackServings\": 0.5", "\"trackSnackServings\": null");
            json = With(json, "\"trackSnackIntervalMinSec\": 6", "\"trackSnackIntervalMinSec\": null");
            json = With(json, "\"trackSnackIntervalMaxSec\": 10", "\"trackSnackIntervalMaxSec\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths,
                Is.EquivalentTo(new[] { "runner.trackSnackServings",
                    "runner.trackSnackIntervalMinSec", "runner.trackSnackIntervalMaxSec" }));
        }

        [Test]
        public void Load_MissingErrorWeight_ReportsPath()
        {
            var json = With(ValidJson, "\"S_MISSED\": 0", "\"S_MISSED\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "score.errors.S_MISSED" }));
        }

        [Test]
        public void Load_MissingCorrectWeight_ReportsPath()
        {
            var json = With(ValidJson, "\"hyperNotify\": 15", "\"hyperNotify\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "score.correct.hyperNotify" }));
        }

        [Test]
        public void Load_MiddleSectionWithoutLength_ReportsPath()
        {
            var json = With(ValidJson, "\"sections\": [ { \"id\": \"signal\" } ]",
                "\"sections\": [ { \"id\": \"signal\" }, { \"id\": \"condition\" } ]");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "sections[0].lengthM" }));
        }

        private static System.Collections.Generic.List<string> JsonKeys(Type dto) =>
            dto.GetProperties()
                // 테스트 어셈블리가 Newtonsoft를 참조하지 않으므로 속성 이름으로 찾는다.
                .Select(p => p.CustomAttributes.Single(a => a.AttributeType.Name == "JsonPropertyAttribute")
                    .ConstructorArguments[0].Value as string)
                .ToList();

        [Test]
        public void ErrorKeys_MatchLogCodes()
        {
            var codes = new[]
            {
                HypoErrorCode.SMissed, HypoErrorCode.RsSlow, HypoErrorCode.RsKeepRunning, HypoErrorCode.RsNone,
                HypoErrorCode.RsTimeout, HypoErrorCode.DsUnder, HypoErrorCode.DsOver, HypoErrorCode.DsTimeout,
                HypoErrorCode.RcNoRecheck, HypoErrorCode.RcNoNotify, HypoErrorCode.HelpInstead,
                HypoErrorCode.HyKeepRunning, HypoErrorCode.HyTimeout,
            }.Select(LogCodes.Error).ToList();

            Assert.That(JsonKeys(typeof(ScoreErrorsDto)), Is.EquivalentTo(codes));
        }

        [Test]
        public void Type2ErrorKeys_MatchLogCodes()
        {
            var codes = new[]
            {
                HypoErrorCode.HyKeepRunning, HypoErrorCode.HyJuice, HypoErrorCode.HyTimeout,
                HypoErrorCode.LoKeepRunning, HypoErrorCode.LoTimeout,
            }.Select(LogCodes.Error).ToList();

            Assert.That(JsonKeys(typeof(Type2ScoreErrorsDto)), Is.EquivalentTo(codes));
        }

        [Test]
        public void SymptomField_NullIsReportedByPath()
        {
            var json = With(ValidJson, "\"shakeStartGlucose\": 90", "\"shakeStartGlucose\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "symptom.shakeStartGlucose" }));
        }

        [Test]
        public void HyperFields_NullAreReportedByPath()
        {
            var json = With(ValidJson, "\"hyperReleaseMargin\": 20", "\"hyperReleaseMargin\": null");
            json = With(json, "\"notifyTimeLimitSec\": 5", "\"notifyTimeLimitSec\": null");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths,
                Is.EquivalentTo(new[] { "glucose.hyperReleaseMargin", "hyper.notifyTimeLimitSec" }));
        }

        [Test]
        public void MissingHyperSection_IsReportedOnce()
        {
            var json = With(ValidJson, "\"hyper\": { \"notifyTimeLimitSec\": 5 },", "");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "hyper" }));
        }

        [Test]
        public void MissingDosesSection_IsReportedOnce()
        {
            var json = With(ValidJson, ", \"doses\": { \"under\": 1, \"proper\": 2, \"over\": 3 }", "");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "hypo.doses" }));
        }

        [Test]
        public void UnknownSnackKind_Fails()
        {
            var json = With(ValidJson, "\"kind\": \"fast\"", "\"kind\": \"medium\"");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Count.EqualTo(1));
            Assert.That(e.Violations[0], Does.Contain("snacks[0].kind"));
        }

        [Test]
        public void Step1NotLongerThanDelay_Fails()
        {
            var json = With(ValidJson, "\"step1TimeLimitSec\": 5", "\"step1TimeLimitSec\": 3");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Count.EqualTo(1));
            Assert.That(e.Violations[0], Does.Contain("step1TimeLimitSec"));
        }

        [Test]
        public void MalformedJson_Fails()
        {
            var e = Assert.Throws<ConfigValidationException>(() => new ConfigProvider("{ \"unit\": ").Load());

            Assert.That(e.Violations, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// 빌드에 실리는 설정 파일을 실제로 읽어 불변식을 검증한다(CLAUDE.md 2절).
        /// 담당자가 수치를 정하기 전(PLACEHOLDER = null)에는 빈 경로를 남기고 건너뛴다.
        /// </summary>
        [Test]
        public void BuildConfig_LoadsAndSatisfiesInvariants()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, BuildConfigPath));

            GameConfig config;
            try
            {
                config = new ConfigProvider(json).Load();
            }
            catch (ConfigValidationException e) when (e.Violations.Count == 0)
            {
                Assert.Ignore("PLACEHOLDER 값이 아직 정해지지 않음: " + string.Join(", ", e.MissingPaths));
                return;
            }

            Assert.That(config.Hypo.Step1TimeLimitSec, Is.GreaterThan(config.Glucose.Display.DelaySec));
        }

        /// <summary>에디터 전용 개발 설정은 빈 값 없이 불변식을 지켜야 하고, 실제 수치가 아님이 기록에 남아야 한다.</summary>
        [Test]
        public void DevConfig_LoadsAndIsMarkedNotReal()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, DevConfigPath));

            var config = new ConfigProvider(json).Load();

            Assert.That(config.ConfigVersion, Is.EqualTo("dev-not-real"));
            Assert.That(config.Hypo.AnswerFeedbackSec, Is.EqualTo(1));
            Assert.That(config.Hypo.ResumeCountdownSec, Is.EqualTo(3));
        }

        [Test]
        public void Type2Json_Loads_AndConvertsRealMinutes()
        {
            var config = new ConfigProvider(ValidType2Json).Load();

            Assert.That(config.TrackType, Is.EqualTo(TrackType.Type2));
            Assert.That(config.ConfigVersion, Is.EqualTo("test-type2"));
            Assert.That(config.Glucose.Time.TimeScale, Is.EqualTo(150));
            var juice = config.Glucose.Snacks.Single(f => f.Id == "juice");
            Assert.That(juice.OnsetSec, Is.EqualTo(1).Within(1e-9));
            Assert.That(juice.AbsorbSec, Is.EqualTo(2).Within(1e-9));
            Assert.That(juice.TotalRise, Is.EqualTo(30));
            Assert.That(config.Type2.NoticeSec, Is.EqualTo(2));
            Assert.That(config.Type2.JuiceChoiceFoodId, Is.EqualTo("juice"));
            Assert.That(config.Hyper.NotifyTimeLimitSec, Is.EqualTo(config.Type2.TellTimeLimitSec));
            Assert.That(config.Runner.TrackSnackIntervalMinSec, Is.EqualTo(config.Type2.FoodRowIntervalMinSec));
            Assert.That(config.Score.CorrectWeights[HypoStep.LowNotify], Is.EqualTo(11));
            Assert.That(config.Score.ErrorWeights[HypoErrorCode.HyJuice], Is.EqualTo(2));
        }

        [Test]
        public void MissingTrackType_FailsWithPath()
        {
            var json = With(ValidJson, "\"trackType\": \"type1\",", "");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Does.Contain("trackType"));
        }

        [Test]
        public void UnknownTrackType_FailsWithViolation()
        {
            var json = With(ValidJson, "\"type1\"", "\"type3\"");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Some.Contains("trackType"));
        }

        [Test]
        public void Type2Json_Type1OnlyKey_Fails()
        {
            var json = With(ValidType2Json, "\"time\":", "\"hypo\": { }, \"time\":");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Some.Contains("JSON 형식 오류"));
        }

        [Test]
        public void Type2Json_NullValues_ReportsAllPaths()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, BuildType2ConfigPath));
            if (!json.Contains("\"highHoldSec\": null"))
                Assert.Ignore("2형 빌드 설정이 채워짐");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Does.Contain("type2.highHoldSec"));
            Assert.That(e.MissingPaths, Does.Contain("foods[0].totalRise"));
            Assert.That(e.MissingPaths, Does.Contain("foods[0].source"));
            Assert.That(e.MissingPaths, Does.Contain("runner.restDurationSec"));
            Assert.That(e.MissingPaths, Does.Contain("score.errors.HY_JUICE"));
        }

        [Test]
        public void DevType2Config_LoadsAndPassesInvariants()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, DevType2ConfigPath));

            var config = new ConfigProvider(json).Load();

            Assert.That(config.ConfigVersion, Is.EqualTo("dev-not-real-type2"));
            Assert.That(config.TrackType, Is.EqualTo(TrackType.Type2));
            Assert.That(config.Glucose.Snacks, Has.Count.EqualTo(7));
        }

        [Test]
        public void BuildType2Config_PassesInvariants_WhenFilled()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, BuildType2ConfigPath));

            GameConfig config;
            try
            {
                config = new ConfigProvider(json).Load();
            }
            catch (ConfigValidationException e) when (e.Violations.Count == 0)
            {
                Assert.Ignore("PLACEHOLDER 값이 아직 정해지지 않음: " + string.Join(", ", e.MissingPaths));
                return;
            }

            Assert.That(config.TrackType, Is.EqualTo(TrackType.Type2));
        }

        private const string SignalOnly = "\"sections\": [ { \"id\": \"signal\" } ]";

        private static string WithScenes(string json, string scenes) => With(json, SignalOnly,
            "\"sections\": [ { \"id\": \"signal\", \"lengthM\": 10 }, { \"id\": \"condition\", \"scenes\": [ " + scenes + " ] } ]");

        [Test]
        public void Load_Scenes_MapsAllFields()
        {
            var json = WithScenes(ValidJson,
                "{ \"id\": \"a\", \"timeOfDay\": \"morning\", \"lengthM\": 100, " +
                "\"overrides\": { \"trackSnackIntervalMinSec\": 3, \"trackSnackIntervalMaxSec\": null, \"trackSnackIds\": [\"juice\", \"juice\"] } }, " +
                "{ \"id\": \"b\", \"timeOfDay\": \"night\", \"lengthM\": 50, \"spawns\": false }");

            var config = new ConfigProvider(json).Load();

            Assert.That(config.Sections[0].LengthM, Is.EqualTo(10));
            Assert.That(config.Sections[0].HasScenes, Is.False);
            var scenes = config.Sections[1].Scenes;
            Assert.That(scenes.Select(x => x.Id), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(scenes[0].TimeOfDay, Is.EqualTo(TimeOfDay.Morning));
            Assert.That(scenes[1].TimeOfDay, Is.EqualTo(TimeOfDay.Night));
            Assert.That(scenes[0].LengthM, Is.EqualTo(100));
            Assert.That(scenes[1].LengthM, Is.EqualTo(50));
            Assert.That(scenes[0].Spawns, Is.True);
            Assert.That(scenes[1].Spawns, Is.False);
            Assert.That(scenes[0].Overrides.TrackSnackIntervalMinSec, Is.EqualTo(3));
            Assert.That(scenes[0].Overrides.TrackSnackIntervalMaxSec, Is.Null);
            Assert.That(scenes[0].Overrides.TrackSnackIds, Is.EqualTo(new[] { "juice", "juice" }));
            Assert.That(scenes[1].Overrides.RunDecayPerSec, Is.Null);
        }

        [Test]
        public void Load_Type2Scenes_MapsOverrides()
        {
            var json = WithScenes(ValidType2Json,
                "{ \"id\": \"a\", \"timeOfDay\": \"evening\", \"lengthM\": 100, " +
                "\"overrides\": { \"runDecayPerSec\": 2, \"restIntervalMinSec\": 20 } }");

            var scene = new ConfigProvider(json).Load().Sections[1].Scenes.Single();

            Assert.That(scene.TimeOfDay, Is.EqualTo(TimeOfDay.Evening));
            Assert.That(scene.Overrides.RunDecayPerSec, Is.EqualTo(2));
            Assert.That(scene.Overrides.RestIntervalMinSec, Is.EqualTo(20));
        }

        [Test]
        public void Load_SceneLengthNull_ReportsPath()
        {
            var json = WithScenes(ValidJson, "{ \"id\": \"a\", \"timeOfDay\": \"morning\", \"lengthM\": null }");

            var e = LoadFails(json);

            Assert.That(e.MissingPaths, Is.EqualTo(new[] { "sections[1].scenes[0].lengthM" }));
        }

        [Test]
        public void Load_SceneUnknownTimeOfDay_Fails()
        {
            var json = WithScenes(ValidJson, "{ \"id\": \"a\", \"timeOfDay\": \"noon\", \"lengthM\": 100 }");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Count.EqualTo(1));
            Assert.That(e.Violations[0], Does.Contain("timeOfDay"));
        }

        [Test]
        public void Load_SceneUnknownOverrideKey_Fails()
        {
            var json = WithScenes(ValidJson,
                "{ \"id\": \"a\", \"timeOfDay\": \"morning\", \"lengthM\": 100, \"overrides\": { \"speed\": 1 } }");

            var e = LoadFails(json);

            Assert.That(e.Violations, Has.Count.EqualTo(1));
            Assert.That(e.Violations[0], Does.Contain("JSON 형식 오류"));
        }

        // 테스트 어셈블리는 Newtonsoft를 참조하지 않으므로 빈 경로 목록과 원문으로 확인한다.
        [TestCase(BuildConfigPath)]
        [TestCase(BuildType2ConfigPath)]
        public void BuildConfig_Sections_HaveDayScenes(string path)
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, path));

            var e = LoadFails(json);
            Assert.That(e.MissingPaths, Is.SupersetOf(new[]
            {
                "sections[0].lengthM", "sections[1].scenes[0].lengthM", "sections[1].scenes[1].lengthM",
                "sections[1].scenes[2].lengthM", "sections[1].scenes[3].lengthM",
            }));
            Assert.That(e.MissingPaths, Has.None.Contains("sections[1].scenes[4]"));

            var scenes = Regex.Matches(json, "\"id\":\\s*\"(\\w+)\",\\s*\"timeOfDay\":\\s*\"(\\w+)\"")
                .Cast<Match>().ToList();
            Assert.That(scenes.Select(m => m.Groups[1].Value), Is.EqualTo(new[] { "schoolyard", "playground", "outing", "home" }));
            Assert.That(scenes.Select(m => m.Groups[2].Value), Is.EqualTo(new[] { "morning", "afternoon", "evening", "night" }));
            Assert.That(json, Does.Match("\"id\":\\s*\"home\"[^}]*\"spawns\":\\s*false"));
            Assert.That(json, Does.Not.Match("\"overrides\":\\s*\\{\\s*[^}\\s]"), "빌드 설정의 overrides는 비어 있어야 한다");
        }

        [TestCase(DevConfigPath)]
        [TestCase(DevType2ConfigPath)]
        public void DevConfig_Sections_HaveDayScenes(string path)
        {
            var config = new ConfigProvider(File.ReadAllText(Path.Combine(Application.dataPath, path))).Load();

            Assert.That(config.Sections[0].LengthM, Is.EqualTo(300));
            Assert.That(config.Sections[1].Scenes.Select(x => x.Id), Is.EqualTo(new[] { "schoolyard", "playground", "outing", "home" }));
            Assert.That(config.Sections[1].Scenes.Select(x => x.LengthM), Is.EqualTo(new double?[] { 200, 200, 200, 80 }));
            Assert.That(config.Sections[1].Scenes[3].Spawns, Is.False);
        }
    }
}
