using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;

namespace SugarDash.Domain.Glucose
{
    /// <summary>
    /// 실제 혈당과 표시 혈당 계산(docs/glucose-model.md 1·2·5절).
    /// 변화율이 구간별로 일정하므로 수치 적분 대신 구간 겹침으로 정확히 계산한다.
    /// 그래서 프레임 길이를 어떻게 나눠도 결과가 같다(min/max에 닿는 경우 제외).
    /// </summary>
    public sealed class GlucoseModel
    {
        private readonly GlucoseConfig _config;
        private readonly Dictionary<string, SnackConfig> _snacks;
        private readonly List<RateWindow> _windows = new List<RateWindow>();
        private readonly DisplayDelayBuffer _display;
        private readonly List<double> _breakpoints = new List<double>();

        private double _displayClock;
        private double _suddenDropEnd = double.NegativeInfinity;

        public GlucoseModel(GlucoseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _snacks = config.Snacks.ToDictionary(s => s.Id);
            Actual = config.Levels.Initial;
            _display = new DisplayDelayBuffer(config.Display.DelaySec, 0, Actual);
        }

        public double Actual { get; private set; }
        public double Displayed => _display.Current;
        /// <summary>혈당 모델 시계. 정지하거나 빨리 감긴다.</summary>
        public double SimTime { get; private set; }
        /// <summary>항상 흐르는 시계. 단계 제한 시간과 판단 시간에 쓴다.</summary>
        public double RealElapsed { get; private set; }
        public GlucoseClockMode Mode { get; private set; } = GlucoseClockMode.Running;
        public bool IsSuddenDropActive => SimTime < _suddenDropEnd;

        public void SetMode(GlucoseClockMode mode) => Mode = mode;

        /// <param name="dtReal">실제 경과 시간(초).</param>
        /// <param name="activity">달리기 강도 0~1. 주행 중이 아니면 0.</param>
        public void Tick(double dtReal, double activity)
        {
            if (!(dtReal >= 0)) throw new ArgumentOutOfRangeException(nameof(dtReal), dtReal, "0 이상이어야 한다");
            if (!(activity >= 0 && activity <= 1))
                throw new ArgumentOutOfRangeException(nameof(activity), activity, "0~1 이어야 한다");

            RealElapsed += dtReal;
            switch (Mode)
            {
                case GlucoseClockMode.Running:
                    Advance(dtReal, activity);
                    break;
                case GlucoseClockMode.ActualFrozen:
                    _displayClock += dtReal;
                    _display.Add(_displayClock, Actual);
                    break;
                case GlucoseClockMode.AllFrozen:
                    break;
            }
        }

        /// <summary>
        /// 4단계 재확인용 빨리 감기. 달리기 항은 0으로 두고 기본 하강과 간식 흡수만 적용한다.
        /// 모드와 무관하게 simTime과 표시 시계를 함께 진행하고, realElapsed는 바꾸지 않는다.
        /// </summary>
        public void FastForward(double simSec)
        {
            if (!(simSec >= 0)) throw new ArgumentOutOfRangeException(nameof(simSec), simSec, "0 이상이어야 한다");
            Advance(simSec, 0);
        }

        /// <summary>아동이 실제로 고른 간식과 양을 그대로 반영한다(정책 A).</summary>
        public void EatSnack(string snackId, double servings)
        {
            if (snackId == null || !_snacks.TryGetValue(snackId, out var snack))
                throw new ArgumentException($"설정에 없는 간식이다: {snackId}", nameof(snackId));
            if (!(servings >= 0))
                throw new ArgumentOutOfRangeException(nameof(servings), servings, "0 이상이어야 한다");

            double start = SimTime + snack.OnsetSec;
            _windows.Add(new RateWindow(start, start + snack.AbsorbSec, snack.TotalRise * servings / snack.AbsorbSec));
        }

        /// <summary>EventDirector가 판정한 급하강을 적용한다. amount를 durationSec 동안 균등하게 내린다.</summary>
        public void StartSuddenDrop()
        {
            if (IsSuddenDropActive) throw new InvalidOperationException("급하강이 이미 진행 중이다");

            var drop = _config.SuddenDrop;
            _suddenDropEnd = SimTime + drop.DurationSec;
            _windows.Add(new RateWindow(SimTime, _suddenDropEnd, -drop.Amount / drop.DurationSec));
        }

        private void Advance(double simDt, double activity)
        {
            double from = SimTime;
            double to = from + simDt;

            // 구간 경계에서 나눠 각 조각을 직선으로 만든다. 표시 혈당 보간도 정확해진다.
            _breakpoints.Clear();
            foreach (var window in _windows)
            {
                if (window.Start > from && window.Start < to) _breakpoints.Add(window.Start);
                if (window.End > from && window.End < to) _breakpoints.Add(window.End);
            }
            _breakpoints.Add(to);
            _breakpoints.Sort();

            var levels = _config.Levels;
            double decayPerSec = levels.BaseDecayPerSec + levels.RunDecayPerSec * activity;
            double segmentStart = from;
            foreach (double segmentEnd in _breakpoints)
            {
                double delta = -decayPerSec * (segmentEnd - segmentStart);
                foreach (var window in _windows) delta += window.DeltaOver(segmentStart, segmentEnd);

                Actual = Math.Max(levels.Min, Math.Min(levels.Max, Actual + delta));
                _displayClock += segmentEnd - segmentStart;
                _display.Add(_displayClock, Actual);
                segmentStart = segmentEnd;
            }

            SimTime = to;
            _windows.RemoveAll(w => w.End <= to);
        }
    }
}
