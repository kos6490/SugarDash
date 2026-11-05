using SugarDash.App;

namespace SugarDash.Presentation
{
    /// <summary>실제 혈당에서 카메라 흔들림 세기(0~1)를 계산한다.</summary>
    public static class SymptomShake
    {
        public static double Intensity(double actual, double startGlucose, double lowGlucose)
        {
            if (double.IsNaN(actual) || actual >= startGlucose) return 0;
            if (actual <= lowGlucose) return 1;
            return (startGlucose - actual) / (startGlucose - lowGlucose);
        }

        /// <summary>
        /// 주행 상태에 따른 흔들림 배율. 달리는 중 1, 탈락 장면(주행이 탈락 상태로 열려 있음) 동안 sceneScale,
        /// 일시정지·주행 없음은 0이다. 탈락 장면에서는 끄지 않고 살짝만 이어 간다.
        /// </summary>
        public static double StateScale(RunState state, double sceneScale)
        {
            switch (state)
            {
                case RunState.Running: return 1;
                case RunState.Eliminated: return sceneScale;
                default: return 0;
            }
        }
    }
}
