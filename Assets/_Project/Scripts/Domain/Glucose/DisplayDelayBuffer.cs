using System.Collections.Generic;

namespace SugarDash.Domain.Glucose
{
    /// <summary>
    /// 순수 시간 지연: display(t) = actual(t − delay).
    /// (시각, 값) 샘플을 쌓고 지연 시점 값을 선형 보간한다. 시작 전 구간은 첫 샘플 값으로 본다.
    /// </summary>
    internal sealed class DisplayDelayBuffer
    {
        private readonly double _delaySec;
        private readonly List<(double Time, double Value)> _samples = new List<(double, double)>();

        public DisplayDelayBuffer(double delaySec, double startTime, double startValue)
        {
            _delaySec = delaySec;
            _samples.Add((startTime, startValue));
            Current = startValue;
        }

        public double Current { get; private set; }

        public void Add(double time, double value)
        {
            var last = _samples[_samples.Count - 1];
            if (time <= last.Time)
                _samples[_samples.Count - 1] = (last.Time, value);
            else
                _samples.Add((time, value));

            double target = time - _delaySec;
            Prune(target);
            Current = ValueAt(target);
        }

        // target은 단조 증가하므로 target 이전 샘플은 하나만 남기면 된다.
        private void Prune(double target)
        {
            int removable = 0;
            while (removable + 1 < _samples.Count && _samples[removable + 1].Time <= target) removable++;
            if (removable > 0) _samples.RemoveRange(0, removable);
        }

        private double ValueAt(double target)
        {
            var first = _samples[0];
            if (target <= first.Time || _samples.Count == 1) return first.Value;

            var next = _samples[1];
            double ratio = (target - first.Time) / (next.Time - first.Time);
            return first.Value + (next.Value - first.Value) * ratio;
        }
    }
}
