using System;
using SugarDash.App;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Director;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Tests;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SugarDash.Presentation.Tests
{
    public class SymptomEffectPresenterTests
    {
        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException();
        }

        private sealed class NullLog : ILogRepository
        {
            public string BeginRun(string configVersion, SugarDash.Domain.Config.TrackType trackType) => "run";
            public void EndRun() { }
        }

        private static RunController StartRig(double initialGlucose)
        {
            var config = new ConfigBuilder { Initial = initialGlucose }.Build();
            var bus = new DomainEventBus();
            RunController run = null;
            run = new RunController(config.Runner, config.Sections, () =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus, () => run.DistanceM);
                var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                return new RunSession(config, model, director, fsm, scoring, bus.Subscribe<HypoEventTriggered>(fsm.Begin),
                    scoring.Subscribe(bus));
            }, new NullLog(), bus);
            run.StartRun();
            return run;
        }

        private static readonly Vector3 P = new Vector3(1f, 2f, 3f);
        private GameObject _go;
        private GameObject _cam;
        private SymptomEffectPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _cam = new GameObject("Cam");
            _cam.transform.position = P;
            _go = new GameObject("Symptom");
            _presenter = _go.AddComponent<SymptomEffectPresenter>();
            _presenter.cameraTarget = _cam.transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_cam);
        }

        [Test]
        public void Offset_DoesNotAccumulate()
        {
            for (int i = 0; i < 20; i++)
            {
                _presenter.ApplyShake(1, i * 0.05f);
                Assert.That((_cam.transform.position - P - _presenter.LastOffset).magnitude, Is.LessThan(1e-4f));
                Assert.That(_presenter.LastOffset.magnitude, Is.GreaterThan(0f));
                Assert.That(_presenter.LastOffset.magnitude, Is.LessThanOrEqualTo(_presenter.maxAmplitude * 1.5f));
                Assert.That(Mathf.Abs(_presenter.LastOffset.x), Is.LessThanOrEqualTo(_presenter.maxAmplitude + 1e-5f));
                Assert.That(Mathf.Abs(_presenter.LastOffset.y), Is.LessThanOrEqualTo(_presenter.maxAmplitude + 1e-5f));
            }
        }

        [Test]
        public void ExternalAbsoluteWrite_BecomesBase()
        {
            _presenter.ApplyShake(1, 0.2f);
            var q = new Vector3(5f, 6f, 7f);
            _cam.transform.position = q; // PlayerController가 매 프레임 절대 위치를 쓰는 상황

            _presenter.ApplyShake(1, 0.4f);

            Assert.That((_cam.transform.position - q - _presenter.LastOffset).magnitude, Is.LessThan(1e-4f));

            _presenter.ApplyShake(0, 0.5f);

            Assert.That((_cam.transform.position - q).magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void ZeroIntensity_RestoresCamera()
        {
            _presenter.ApplyShake(1, 0.37f);
            _presenter.ApplyShake(0, 0.5f);

            Assert.That((_cam.transform.position - P).magnitude, Is.LessThan(1e-4f));
            Assert.That(_presenter.LastOffset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void NoRun_RestoresCamera()
        {
            _presenter.ApplyShake(1, 0.37f);

            _presenter.LateUpdate();

            Assert.That((_cam.transform.position - P).magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void BoundToRunningRun_LowGlucose_ShakesCamera()
        {
            var run = StartRig(80);
            Assert.That(run.State, Is.EqualTo(RunState.Running));
            Assert.That(run.Actual, Is.LessThan(90));
            using var _ = _presenter.Bind(run, 90, 40);

            _presenter.LateUpdate();

            Assert.That(_presenter.LastOffset.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void OnDisable_RestoresCamera()
        {
            _presenter.ApplyShake(1, 0.37f);
            Assert.That((_cam.transform.position - P).magnitude, Is.GreaterThan(0f));

            // EditMode에서는 ExecuteAlways가 없으면 Unity가 OnDisable을 부르지 않아 직접 부른다.
            typeof(SymptomEffectPresenter)
                .GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(_presenter, null);

            Assert.That((_cam.transform.position - P).magnitude, Is.LessThan(1e-4f));
        }
    }
}
