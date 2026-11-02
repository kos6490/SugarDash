using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class SymptomShakeTests
    {
        [Test] public void AboveStart_IsZero() => Assert.That(SymptomShake.Intensity(100, 90, 40), Is.EqualTo(0));
        [Test] public void AtStart_IsZero() => Assert.That(SymptomShake.Intensity(90, 90, 40), Is.EqualTo(0));
        [Test] public void AtLow_IsOne() => Assert.That(SymptomShake.Intensity(40, 90, 40), Is.EqualTo(1));
        [Test] public void BelowLow_IsOne() => Assert.That(SymptomShake.Intensity(10, 90, 40), Is.EqualTo(1));
        [Test] public void Midpoint_IsHalf() => Assert.That(SymptomShake.Intensity(65, 90, 40), Is.EqualTo(0.5).Within(1e-9));
        [Test] public void NaN_IsZero() => Assert.That(SymptomShake.Intensity(double.NaN, 90, 40), Is.EqualTo(0));
    }
}
