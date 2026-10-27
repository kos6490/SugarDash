using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Events
{
    /// <summary>대처 단계 하나의 결과. 오류 공통 처리로 보정돼도 오답으로 남는다.</summary>
    public sealed class StepAnswered
    {
        public StepAnswered(int eventId, HypoStep step, string choiceId, bool isCorrect, HypoErrorCode errorCode, bool helpInstead, double decisionSec, bool measuredBeforeWarning, double simT, double t)
        {
            EventId = eventId;
            Step = step;
            ChoiceId = choiceId;
            IsCorrect = isCorrect;
            ErrorCode = errorCode;
            HelpInstead = helpInstead;
            DecisionSec = decisionSec;
            MeasuredBeforeWarning = measuredBeforeWarning;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public HypoStep Step { get; }
        /// <summary>고른 선택지. 시간 초과면 null.</summary>
        public string ChoiceId { get; }
        public bool IsCorrect { get; }
        public HypoErrorCode ErrorCode { get; }
        /// <summary>선택 대신 "어른에게 알리기" 버튼을 눌렀다.</summary>
        public bool HelpInstead { get; }
        /// <summary>단계 진입부터 답할 때까지의 판단 시간(T 기준).</summary>
        public double DecisionSec { get; }
        /// <summary>1단계에서 게이지 경고가 켜지기 전에 측정했다(측정 선행).</summary>
        public bool MeasuredBeforeWarning { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
