using System;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Hypo;

namespace SugarDash.Infrastructure.Logging
{
    /// <summary>
    /// 기록에 쓰는 문자열 코드. 도메인 enum 이름을 바꿔도 과거 기록과 어긋나지 않게 명시적으로 대응시킨다.
    /// </summary>
    public static class LogCodes
    {
        public static string Step(HypoStep step)
        {
            switch (step)
            {
                case HypoStep.Recognize: return "recognize";
                case HypoStep.Respond: return "respond";
                case HypoStep.Dose: return "dose";
                case HypoStep.Remeasure: return "remeasure";
                case HypoStep.Notify: return "notify";
                case HypoStep.HyperNotify: return "hyper_notify";
                case HypoStep.LowNotify: return "low_notify";
                default: throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }
        }

        /// <summary>오류가 없으면 null.</summary>
        public static string Error(HypoErrorCode code)
        {
            switch (code)
            {
                case HypoErrorCode.None: return null;
                case HypoErrorCode.SMissed: return "S_MISSED";
                case HypoErrorCode.RsSlow: return "RS_SLOW";
                case HypoErrorCode.RsKeepRunning: return "RS_KEEP_RUNNING";
                case HypoErrorCode.RsNone: return "RS_NONE";
                case HypoErrorCode.RsTimeout: return "RS_TIMEOUT";
                case HypoErrorCode.DsUnder: return "DS_UNDER";
                case HypoErrorCode.DsOver: return "DS_OVER";
                case HypoErrorCode.DsTimeout: return "DS_TIMEOUT";
                case HypoErrorCode.RcNoRecheck: return "RC_NO_RECHECK";
                case HypoErrorCode.RcNoNotify: return "RC_NO_NOTIFY";
                case HypoErrorCode.HelpInstead: return "HELP_INSTEAD";
                case HypoErrorCode.HyKeepRunning: return "HY_KEEP_RUNNING";
                case HypoErrorCode.HyTimeout: return "HY_TIMEOUT";
                case HypoErrorCode.HyJuice: return "HY_JUICE";
                case HypoErrorCode.LoKeepRunning: return "LO_KEEP_RUNNING";
                case HypoErrorCode.LoTimeout: return "LO_TIMEOUT";
                default: throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        public static string Cause(EliminationCause cause)
        {
            switch (cause)
            {
                case EliminationCause.Declined: return "declined";
                case EliminationCause.PromptTimeout: return "prompt_timeout";
                case EliminationCause.ObstacleHit: return "obstacle_hit";
                case EliminationCause.GlucoseLow: return "glucose_low";
                case EliminationCause.GlucoseHigh: return "glucose_high";
                default: throw new ArgumentOutOfRangeException(nameof(cause), cause, null);
            }
        }

        public static string TrackType(TrackType type)
        {
            switch (type)
            {
                case Domain.Config.TrackType.Type1: return "type1";
                case Domain.Config.TrackType.Type2: return "type2";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string TimeOfDay(TimeOfDay time)
        {
            switch (time)
            {
                case Domain.Config.TimeOfDay.Morning: return "morning";
                case Domain.Config.TimeOfDay.Afternoon: return "afternoon";
                case Domain.Config.TimeOfDay.Evening: return "evening";
                case Domain.Config.TimeOfDay.Night: return "night";
                default: throw new ArgumentOutOfRangeException(nameof(time), time, null);
            }
        }

        public static string NoticeKind(NoticeKind kind)
        {
            switch (kind)
            {
                case Domain.Director.NoticeKind.High: return "high";
                case Domain.Director.NoticeKind.Low: return "low";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        // 조회(ReportQueryService)용 역변환. 모르는 코드면 FormatException.

        public static HypoStep ParseStep(string code) => Parse(code, StepValues, Step, "step");

        /// <summary>null이면 오류 없음.</summary>
        public static HypoErrorCode ParseError(string code) =>
            code == null ? HypoErrorCode.None : Parse(code, ErrorValues, Error, "errorCode");

        public static EliminationCause ParseCause(string code) => Parse(code, CauseValues, Cause, "cause");

        /// <summary>없거나 모르는 값이면 1형. 2형 이전 기록에는 trackType이 없다.</summary>
        public static TrackType ParseTrackType(string code) =>
            code == "type2" ? Domain.Config.TrackType.Type2 : Domain.Config.TrackType.Type1;

        public static NoticeKind ParseNoticeKind(string code) =>
            Parse(code, NoticeKindValues, NoticeKind, "kind");

        public static TimeOfDay ParseTimeOfDay(string code) =>
            Parse(code, TimeOfDayValues, TimeOfDay, "timeOfDay");

        private static readonly TimeOfDay[] TimeOfDayValues = (TimeOfDay[])Enum.GetValues(typeof(TimeOfDay));
        private static readonly NoticeKind[] NoticeKindValues = (NoticeKind[])Enum.GetValues(typeof(NoticeKind));
        private static readonly HypoStep[] StepValues = (HypoStep[])Enum.GetValues(typeof(HypoStep));
        private static readonly HypoErrorCode[] ErrorValues = (HypoErrorCode[])Enum.GetValues(typeof(HypoErrorCode));
        private static readonly EliminationCause[] CauseValues =
            (EliminationCause[])Enum.GetValues(typeof(EliminationCause));

        private static T Parse<T>(string code, T[] values, Func<T, string> toCode, string field)
        {
            foreach (var value in values)
            {
                if (toCode(value) == code) return value;
            }
            throw new FormatException($"{field}에 모르는 코드가 있다: {code}");
        }
    }
}
