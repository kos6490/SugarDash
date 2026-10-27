namespace SugarDash.Domain.Config
{
    /// <summary>4단계 대처(HypoEventFsm) 설정. 제한 시간은 decisionDt 기준 초, 유예 시간만 simDt 기준 게임 초.</summary>
    public sealed class HypoConfig
    {
        public HypoConfig(double step1TimeLimitSec, double step2TimeLimitSec, double step3TimeLimitSec,
            double step4TimeLimitSec, double guardianPromptTimeLimitSec, double resumeCountdownSec, double graceSec,
            string guardianSnackId, DoseConfig doses)
        {
            Step1TimeLimitSec = step1TimeLimitSec;
            Step2TimeLimitSec = step2TimeLimitSec;
            Step3TimeLimitSec = step3TimeLimitSec;
            Step4TimeLimitSec = step4TimeLimitSec;
            GuardianPromptTimeLimitSec = guardianPromptTimeLimitSec;
            ResumeCountdownSec = resumeCountdownSec;
            GraceSec = graceSec;
            GuardianSnackId = guardianSnackId;
            Doses = doses;
        }

        /// <summary>1단계(위험 인지) 제한 시간.</summary>
        public double Step1TimeLimitSec { get; }
        /// <summary>2단계(대응 선택) 제한 시간.</summary>
        public double Step2TimeLimitSec { get; }
        /// <summary>3단계(처치량 결정) 제한 시간.</summary>
        public double Step3TimeLimitSec { get; }
        /// <summary>4단계(재확인) 선택 하나당 제한 시간.</summary>
        public double Step4TimeLimitSec { get; }
        /// <summary>오류 뒤 "어른에게 알리기"를 기다리는 시간. 넘기면 탈락한다.</summary>
        public double GuardianPromptTimeLimitSec { get; }
        /// <summary>대처를 마친 뒤 다시 달리기 전 카운트다운.</summary>
        public double ResumeCountdownSec { get; }
        /// <summary>재개 뒤 저혈당 발동·급하강·장애물이 없는 시간(simDt 기준).</summary>
        public double GraceSec { get; }
        /// <summary>아동이 간식을 고르지 못했을 때 어른이 건네는 빠른 당.</summary>
        public string GuardianSnackId { get; }
        public DoseConfig Doses { get; }
    }
}
