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
    }
}
