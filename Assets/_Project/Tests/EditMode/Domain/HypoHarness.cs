using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    /// <summary>
    /// HypoEventFsm 테스트용 조립. RunController가 할 프레임 진행을 흉내 낸다.
    /// 기본 설정: 달리기 감소 2/s(그 밖은 ConfigBuilder 기본값). 120에서 26초에 68로 발동한다.
    /// </summary>
    public sealed class HypoHarness
    {
        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("급하강 확률 0에서는 난수를 읽지 않아야 한다");
        }

        public readonly ConfigBuilder Builder;
        public readonly GlucoseModel Model;
        public readonly EventDirector Director;
        public readonly HypoEventFsm Fsm;
        public readonly List<object> Events = new List<object>();

        public HypoHarness(Action<ConfigBuilder> configure = null)
        {
            Builder = new ConfigBuilder { RunDecayPerSec = 2 };
            configure?.Invoke(Builder);
            var config = Builder.Build();
            Assert.That(GameConfigValidator.Validate(config), Is.Empty, "테스트 설정도 불변식을 지켜야 한다");

            var bus = new DomainEventBus();
            Collect<HypoEventTriggered>(bus);
            Collect<SuddenDropStarted>(bus);
            Collect<GaugeWarningRaised>(bus);
            Collect<MeterPressed>(bus);
            Collect<StepAnswered>(bus);
            Collect<GuardianPromptStarted>(bus);
            Collect<GuardianNotified>(bus);
            Collect<DoseApplied>(bus);
            Collect<RunEliminated>(bus);
            Collect<RecheckFastForward>(bus);
            Collect<HypoResolved>(bus);
            Collect<GraceStarted>(bus);
            Collect<GraceEnded>(bus);

            Model = new GlucoseModel(config.Glucose);
            Director = new EventDirector(config.Glucose, Model, new NoRandom(), bus);
            Fsm = new HypoEventFsm(config, Model, Director, bus);
            bus.Subscribe<HypoEventTriggered>(Fsm.Begin);
        }

        private void Collect<T>(DomainEventBus bus) => bus.Subscribe<T>(e => Events.Add(e));

        public IReadOnlyList<T> All<T>() => Events.OfType<T>().ToList();
        public T Last<T>() => Events.OfType<T>().Last();

        public void Frame(double dt, double activity = 1)
        {
            double worldDt = Fsm.IsWorldFrozen ? 0 : dt;
            Model.Tick(dt, worldDt, dt, activity);
            Director.Evaluate(dt);
            Fsm.Update();
        }

        public void Frames(int count, double dt = 1)
        {
            for (int i = 0; i < count; i++) Frame(dt);
        }

        /// <summary>메뉴 일시정지·백그라운드: 세 시계 모두 0.</summary>
        public void Pause(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                Model.Tick(0, 0, 0, 1);
                Director.Evaluate(0);
                Fsm.Update();
            }
        }

        public void RunUntilTriggered(int maxFrames = 100)
        {
            for (int i = 0; i < maxFrames && Fsm.Phase != HypoPhase.Recognize; i++) Frame(1);
            Assert.That(Fsm.Phase, Is.EqualTo(HypoPhase.Recognize), "저혈당 이벤트가 발동하지 않았다");
        }
    }
}
