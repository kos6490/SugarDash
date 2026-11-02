using System;
using SugarDash.Domain.Director;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Scoring;

namespace SugarDash.App
{
    /// <summary>
    /// 주행 하나 동안 쓰는 도메인 객체 묶음. 상태가 있어 주행마다 새로 만든다.
    /// 생성과 구독 연결(HypoEventTriggered → HypoEventFsm.Begin, HyperEventTriggered → HypoEventFsm.BeginHyper)은
    /// GameBootstrapper의 팩토리가 하고, Dispose하면 그 구독을 해제한다.
    /// </summary>
    public sealed class RunSession : IDisposable
    {
        private IDisposable[] _subscriptions;

        public RunSession(GlucoseModel model, EventDirector director, HypoEventFsm fsm,
            ScoringService scoring, params IDisposable[] subscriptions)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Director = director ?? throw new ArgumentNullException(nameof(director));
            Fsm = fsm ?? throw new ArgumentNullException(nameof(fsm));
            Scoring = scoring ?? throw new ArgumentNullException(nameof(scoring));
            _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
            if (Array.IndexOf(subscriptions, null) >= 0)
                throw new ArgumentNullException(nameof(subscriptions), "null인 구독이 있다");
        }

        public GlucoseModel Model { get; }
        public EventDirector Director { get; }
        public HypoEventFsm Fsm { get; }
        public ScoringService Scoring { get; }

        public void Dispose()
        {
            if (_subscriptions == null) return;
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions = null;
        }
    }
}
