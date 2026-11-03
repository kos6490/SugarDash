using System;
using System.IO;
using System.Linq;
using SugarDash.Infrastructure.Progress;
using NUnit.Framework;

namespace SugarDash.Infrastructure.Tests
{
    public class BestScoreRepositoryTests
    {
        private string _dir;
        private string _path;
        private BestScoreRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SugarDashBestTests-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_dir, "best-score.json");
            _repo = new BestScoreRepository(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void SaveThenLoad_ReturnsScore()
        {
            _repo.Save("cfg", 123.4);
            Assert.That(_repo.Load("cfg"), Is.EqualTo(123.4));

            _repo.Save("cfg", 200);
            Assert.That(_repo.Load("cfg"), Is.EqualTo(200), "기존 파일이 있어도 교체된다");
        }

        [Test]
        public void Load_NoFile_ReturnsNull() => Assert.That(_repo.Load("cfg"), Is.Null);

        [Test]
        public void Load_CorruptFile_ReturnsNull()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, "{not json");

            Assert.That(_repo.Load("cfg"), Is.Null);

            _repo.Save("cfg", 7);
            Assert.That(_repo.Load("cfg"), Is.EqualTo(7));
        }

        [Test]
        public void Load_OtherConfigVersion_ReturnsNull()
        {
            _repo.Save("cfg-a", 50);

            Assert.That(_repo.Load("cfg-b"), Is.Null);
        }

        [Test]
        public void Load_OtherSchemaVersion_ReturnsNull()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, "{\"schemaVersion\":99,\"configVersion\":\"cfg\",\"bestScore\":5}");

            Assert.That(_repo.Load("cfg"), Is.Null);
        }

        [Test]
        public void Save_LeavesNoTempFile()
        {
            _repo.Save("cfg", 1);
            _repo.Save("cfg", 2);

            Assert.That(Directory.GetFiles(_dir).Select(Path.GetFileName), Is.EqualTo(new[] { "best-score.json" }));
        }
    }
}
