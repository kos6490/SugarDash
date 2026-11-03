using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SugarDash.Domain.Config;
using SugarDash.Infrastructure.Progress;

namespace SugarDash.Infrastructure.Tests
{
    public class SettingsRepositoryTests
    {
        private string _dir;
        private string _path;
        private SettingsRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SugarDashSettingsTests-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_dir, "settings.json");
            _repo = new SettingsRepository(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [TestCase(TrackType.Type1)]
        [TestCase(TrackType.Type2)]
        public void SaveThenLoad_RoundTrips(TrackType type)
        {
            _repo.SaveTrackType(type);

            Assert.That(_repo.LoadTrackType(), Is.EqualTo(type));
        }

        [Test]
        public void Load_NoFile_ReturnsNull() => Assert.That(_repo.LoadTrackType(), Is.Null);

        [TestCase("{not json")]
        [TestCase("{\"schemaVersion\":1,\"trackType\":\"type3\"}")]
        [TestCase("{\"schemaVersion\":1}")]
        [TestCase("{\"schemaVersion\":99,\"trackType\":\"type1\"}")]
        public void Load_CorruptOrUnknown_ReturnsNull(string content)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, content);

            Assert.That(_repo.LoadTrackType(), Is.Null);

            _repo.SaveTrackType(TrackType.Type2);
            Assert.That(_repo.LoadTrackType(), Is.EqualTo(TrackType.Type2));
        }

        [Test]
        public void Save_Overwrites_NoTempFileLeft()
        {
            _repo.SaveTrackType(TrackType.Type1);
            _repo.SaveTrackType(TrackType.Type2);

            Assert.That(_repo.LoadTrackType(), Is.EqualTo(TrackType.Type2));
            Assert.That(Directory.GetFiles(_dir).Select(Path.GetFileName), Is.EqualTo(new[] { "settings.json" }));
        }

        [Test]
        public void Save_DiskError_DoesNotThrow()
        {
            // 파일 경로 자리에 폴더가 있어 교체가 실패한다.
            Directory.CreateDirectory(_path);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() => _repo.SaveTrackType(TrackType.Type1));
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
