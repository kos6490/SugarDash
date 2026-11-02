using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SugarDash.Presentation.Tests
{
    public class SymptomEffectPresenterTests
    {
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
                Assert.That(_presenter.LastOffset.magnitude, Is.LessThanOrEqualTo(_presenter.maxAmplitude * 1.5f));
                Assert.That(Mathf.Abs(_presenter.LastOffset.x), Is.LessThanOrEqualTo(_presenter.maxAmplitude + 1e-5f));
                Assert.That(Mathf.Abs(_presenter.LastOffset.y), Is.LessThanOrEqualTo(_presenter.maxAmplitude + 1e-5f));
            }
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
    }
}
