using System;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Records;

namespace SugarDash.Domain.Scoring
{
    /// <summary>
    /// 주행 점수. 적정 범위에 머문 시간, 달린 거리, 대처 선택지 가중치를 더한다.
    /// 주행 중 실시간 계산과 기록으로 다시 계산하는 경우가 같은 규칙(Apply)을 쓴다. 지표 계산은 하지 않는다.
    /// </summary>
    public sealed class ScoringService
    {
        private readonly ScoreConfig _config;
        private bool _inRange;
        private double _enteredSimT;
        private double _accumulatedSec;

        public ScoringService(ScoreConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>선택지 가중치 합.</summary>
        public double ChoicePoints { get; private set; }

        /// <summary>적정 범위에 머문 시간(simDt 초). 열린 구간은 nowSimT까지 센다.</summary>
        public double InRangeSec(double nowSimT) =>
            _accumulatedSec + (_inRange ? nowSimT - _enteredSimT : 0);

        public double Total(double nowSimT, double distanceM) =>
            InRangeSec(nowSimT) * _config.InRangePerSec + distanceM * _config.PerMeter + ChoicePoints;

        /// <summary>점수에 쓰는 이벤트만 반영하고 나머지는 무시한다.</summary>
        public void Apply(object domainEvent)
        {
            switch (domainEvent)
            {
                case StepAnswered answered:
                    ChoicePoints += answered.IsCorrect
                        ? _config.CorrectWeights[answered.Step]
                        : _config.ErrorWeights[answered.ErrorCode];
                    break;
                case GlucoseRangeEntered entered:
                    if (!_inRange)
                    {
                        _inRange = true;
                        _enteredSimT = entered.SimT;
                    }
                    break;
                case GlucoseRangeExited exited:
                    if (_inRange)
                    {
                        _accumulatedSec += exited.SimT - _enteredSimT;
                        _inRange = false;
                    }
                    break;
                case RecheckFastForward skipped:
                    // 빨리 감기 중에는 범위 판정이 없어 상태가 그대로이므로, 범위 안이었다면 건너뛴 시간을 뺀다.
                    if (_inRange) _accumulatedSec -= skipped.SimSecSkipped;
                    break;
            }
        }

        public IDisposable Subscribe(DomainEventBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            var subs = new[]
            {
                bus.Subscribe<StepAnswered>(Apply),
                bus.Subscribe<GlucoseRangeEntered>(Apply),
                bus.Subscribe<GlucoseRangeExited>(Apply),
                bus.Subscribe<RecheckFastForward>(Apply),
            };
            return new Composite(subs);
        }

        /// <summary>탈락한 주행의 점수를 기록에서 다시 계산한다. 탈락 기록이 없으면 null.</summary>
        public static double? Calculate(RunRecord run, ScoreConfig config)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var service = new ScoringService(config);
            foreach (var e in run.Events)
            {
                if (e is RunEliminated eliminated) return service.Total(eliminated.SimT, eliminated.DistanceM);
                service.Apply(e);
            }
            return null;
        }

        private sealed class Composite : IDisposable
        {
            private readonly IDisposable[] _items;
            public Composite(IDisposable[] items) => _items = items;
            public void Dispose() { foreach (var i in _items) i.Dispose(); }
        }
    }
}
