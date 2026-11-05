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

        // 탈락 장면에서는 흔들림을 끄지 않고 줄여서 이어 간다. 일시정지·주행 없음에서는 흔들지 않는다.
        [Test] public void StateScale_Running_IsOne() => Assert.That(SymptomShake.StateScale(SugarDash.App.RunState.Running, 0.3), Is.EqualTo(1));
        [Test] public void StateScale_Eliminated_IsSceneScale() => Assert.That(SymptomShake.StateScale(SugarDash.App.RunState.Eliminated, 0.3), Is.EqualTo(0.3));
        [Test] public void StateScale_Paused_IsZero() => Assert.That(SymptomShake.StateScale(SugarDash.App.RunState.Paused, 0.3), Is.EqualTo(0));
        [Test] public void StateScale_Idle_IsZero() => Assert.That(SymptomShake.StateScale(SugarDash.App.RunState.Idle, 0.3), Is.EqualTo(0));
    }
}
