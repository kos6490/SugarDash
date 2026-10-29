using System;
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
                default: throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        public static string Cause(EliminationCause cause)
        {
            switch (cause)
            {
                case EliminationCause.Declined: return "declined";
                case EliminationCause.PromptTimeout: return "prompt_timeout";
                default: throw new ArgumentOutOfRangeException(nameof(cause), cause, null);
            }
        }

        // 조회(ReportQueryService)용 역변환. 모르는 코드면 FormatException.

        public static HypoStep ParseStep(string code) => Parse(code, StepValues, Step, "step");

        /// <summary>null이면 오류 없음.</summary>
        public static HypoErrorCode ParseError(string code) =>
            code == null ? HypoErrorCode.None : Parse(code, ErrorValues, Error, "errorCode");

        public static EliminationCause ParseCause(string code) => Parse(code, CauseValues, Cause, "cause");

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
