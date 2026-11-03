namespace SugarDash.Domain.Config
{
    /// <summary>
    /// 2형 주행(choose → move → notice → tell) 설정. 시간은 따로 적지 않으면 초 단위다.
    /// 유지·notice 시간은 simDt, 제한 시간·문구 창은 decisionDt, 쉬는 구간 간격은 worldDt 기준이다.
    /// </summary>
    public sealed class Type2Config
    {
        public Type2Config(double highHoldSec, double lowHoldSec, double noticeSec, double tellTimeLimitSec,
            double guardianPromptTimeLimitSec, double answerFeedbackSec, double resumeCountdownSec, double graceSec,
            string guardianSnackId, string juiceChoiceFoodId, double foodRowIntervalMinSec,
            double foodRowIntervalMaxSec, double restIntervalMinSec, double restIntervalMaxSec,
            double restDurationSec)
        {
            HighHoldSec = highHoldSec;
            LowHoldSec = lowHoldSec;
            NoticeSec = noticeSec;
            TellTimeLimitSec = tellTimeLimitSec;
            GuardianPromptTimeLimitSec = guardianPromptTimeLimitSec;
            AnswerFeedbackSec = answerFeedbackSec;
            ResumeCountdownSec = resumeCountdownSec;
            GraceSec = graceSec;
            GuardianSnackId = guardianSnackId;
            JuiceChoiceFoodId = juiceChoiceFoodId;
            FoodRowIntervalMinSec = foodRowIntervalMinSec;
            FoodRowIntervalMaxSec = foodRowIntervalMaxSec;
            RestIntervalMinSec = restIntervalMinSec;
            RestIntervalMaxSec = restIntervalMaxSec;
            RestDurationSec = restDurationSec;
        }

        /// <summary>실제 혈당이 고혈당 기준 이상으로 끊김 없이 이어져야 하는 시간. 넘기면 고혈당 notice가 시작된다.</summary>
        public double HighHoldSec { get; }
        /// <summary>실제 혈당이 저혈당 기준 미만으로 끊김 없이 이어져야 하는 시간.</summary>
        public double LowHoldSec { get; }
        /// <summary>notice 신호만 보여 주는 시간. 끝나면 tell이 시작된다.</summary>
        public double NoticeSec { get; }
        /// <summary>tell 선택 제한 시간. 넘기면 오류(HY_TIMEOUT, LO_TIMEOUT)다.</summary>
        public double TellTimeLimitSec { get; }
        /// <summary>오류 뒤 "어른에게 알리기"를 기다리는 시간.</summary>
        public double GuardianPromptTimeLimitSec { get; }
        /// <summary>선택 결과 문구 창 길이.</summary>
        public double AnswerFeedbackSec { get; }
        public double ResumeCountdownSec { get; }
        public double GraceSec { get; }
        /// <summary>저혈당 tell에서 아동이 고르지 못했을 때 어른이 건네는 음식 id.</summary>
        public string GuardianSnackId { get; }
        /// <summary>tell 선택지 "주스 마시기"가 먹이는 음식 id.</summary>
        public string JuiceChoiceFoodId { get; }
        public double FoodRowIntervalMinSec { get; }
        public double FoodRowIntervalMaxSec { get; }
        public double RestIntervalMinSec { get; }
        public double RestIntervalMaxSec { get; }
        public double RestDurationSec { get; }
    }
}
