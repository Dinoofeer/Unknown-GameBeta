using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnknownGame.Save.Tests
{
    public class SaveGameServiceTests
    {
        private string directory;
        private SaveGameService service;

        [SetUp]
        public void SetUp()
        {
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/SaveServiceTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
            service = new SaveGameService(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void MissingSaveDoesNotCreateFiles()
        {
            Assert.That(service.Load(out var data, out var error), Is.EqualTo(SaveLoadStatus.Missing));
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Null);
            Assert.That(Directory.GetFileSystemEntries(directory), Is.Empty);
        }

        [Test]
        public void SavedProgressCanBeReadByAnotherServiceInstance()
        {
            var original = SaveGameData.CreateNew("Level_01");
            original.checkpointId = "checkpoint_02";
            original.deaths = 7;
            original.elapsedSeconds = 123.5f;
            original.hasSecuredKey = true;
            Assert.That(service.TrySave(original, out var writeError), Is.True, writeError);

            var reader = new SaveGameService(directory);
            Assert.That(reader.Load(out var loaded, out var readError), Is.EqualTo(SaveLoadStatus.Loaded), readError);
            Assert.That(loaded.runId, Is.EqualTo(original.runId));
            Assert.That(loaded.levelScene, Is.EqualTo("Level_01"));
            Assert.That(loaded.checkpointId, Is.EqualTo("checkpoint_02"));
            Assert.That(loaded.deaths, Is.EqualTo(7));
            Assert.That(loaded.elapsedSeconds, Is.EqualTo(123.5f));
            Assert.That(loaded.hasSecuredKey, Is.True);
        }

        [TestCase("Level_01", "Level_03")]
        [TestCase("Level_02", "Level_02")]
        public void LegacySaveResumesTheSameSceneWithItsProgress(string legacyScene, string currentScene)
        {
            var legacy = SaveGameData.CreateNew(legacyScene);
            legacy.version = 1;
            legacy.checkpointId = "level01_cp02";
            legacy.deaths = 7;
            legacy.elapsedSeconds = 123.5f;
            legacy.hasSecuredKey = true;
            string contents = JsonUtility.ToJson(legacy);
            File.WriteAllText(service.SaveFilePath, contents);

            Assert.That(service.Load(out var loaded, out var error), Is.EqualTo(SaveLoadStatus.Loaded), error);
            Assert.That(loaded.version, Is.EqualTo(SaveGameData.CurrentVersion));
            Assert.That(loaded.runId, Is.EqualTo(legacy.runId));
            Assert.That(loaded.levelScene, Is.EqualTo(currentScene));
            Assert.That(loaded.checkpointId, Is.EqualTo(legacy.checkpointId));
            Assert.That(loaded.deaths, Is.EqualTo(legacy.deaths));
            Assert.That(loaded.elapsedSeconds, Is.EqualTo(legacy.elapsedSeconds));
            Assert.That(loaded.hasSecuredKey, Is.True);
            Assert.That(File.ReadAllText(service.SaveFilePath), Is.EqualTo(contents));
        }

        [Test]
        public void NewGameReplacesPreviousProgressAndBacksUpPreviousSave()
        {
            var previous = SaveGameData.CreateNew("Level_02");
            previous.checkpointId = "checkpoint_02";
            previous.deaths = 9;
            previous.elapsedSeconds = 100f;
            previous.hasSecuredKey = true;
            Assert.That(service.TrySave(previous, out var error), Is.True, error);
            Assert.That(service.TryCreateNew("Level_01", out var created, out error), Is.True, error);
            Assert.That(service.Load(out var loaded, out error), Is.EqualTo(SaveLoadStatus.Loaded), error);
            Assert.That(loaded.runId, Is.EqualTo(created.runId).And.Not.EqualTo(previous.runId));
            Assert.That(loaded.levelScene, Is.EqualTo("Level_01"));
            Assert.That(loaded.checkpointId, Is.Empty);
            Assert.That(loaded.deaths, Is.Zero);
            Assert.That(loaded.elapsedSeconds, Is.Zero);
            Assert.That(loaded.hasSecuredKey, Is.False);
            string backupPath = Path.ChangeExtension(service.SaveFilePath, ".bak");
            Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(new[] { service.SaveFilePath, backupPath }));
            var backup = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(backupPath));
            Assert.That(backup.runId, Is.EqualTo(previous.runId));
            Assert.That(backup.checkpointId, Is.EqualTo(previous.checkpointId));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("{broken}")]
        [TestCase("{\"version\":1")]
        public void CorruptSaveIsRejectedAndPreserved(string contents)
        {
            File.WriteAllText(service.SaveFilePath, contents);
            Assert.That(service.Load(out var data, out var error), Is.EqualTo(SaveLoadStatus.Invalid));
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(File.ReadAllText(service.SaveFilePath), Is.EqualTo(contents));
        }

        [Test]
        public void FutureVersionIsRejectedWithoutOverwritingIt()
        {
            var future = SaveGameData.CreateNew("Level_01");
            future.version++;
            string contents = JsonUtility.ToJson(future);
            File.WriteAllText(service.SaveFilePath, contents);
            Assert.That(service.Load(out var data, out _), Is.EqualTo(SaveLoadStatus.UnsupportedVersion));
            Assert.That(data, Is.Null);
            Assert.That(File.ReadAllText(service.SaveFilePath), Is.EqualTo(contents));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidTimeDoesNotOverwriteValidSave(float seconds)
        {
            Assert.That(service.TryCreateNew("Level_01", out var data, out var error), Is.True, error);
            string previous = File.ReadAllText(service.SaveFilePath);
            data.elapsedSeconds = seconds;
            Assert.That(service.TrySave(data, out _), Is.False);
            Assert.That(File.ReadAllText(service.SaveFilePath), Is.EqualTo(previous));
        }

        [TestCase("")]
        [TestCase("../Level_01")]
        [TestCase("Assets/Scenes/Level_01.unity")]
        public void InvalidLevelCannotReplacePreviousGame(string level)
        {
            Assert.That(service.TryCreateNew("Level_01", out _, out var error), Is.True, error);
            string previous = File.ReadAllText(service.SaveFilePath);
            Assert.That(service.TryCreateNew(level, out var created, out _), Is.False);
            Assert.That(created, Is.Null);
            Assert.That(File.ReadAllText(service.SaveFilePath), Is.EqualTo(previous));
        }

        [Test]
        public void FailedWritePreservesPreviousGame()
        {
            Assert.That(service.TryCreateNew("Level_01", out var previous, out var error), Is.True, error);
            Directory.CreateDirectory(service.SaveFilePath + ".tmp");
            Assert.That(service.TryCreateNew("Level_02", out var created, out error), Is.False);
            Assert.That(created, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(service.Load(out var loaded, out error), Is.EqualTo(SaveLoadStatus.Loaded), error);
            Assert.That(loaded.runId, Is.EqualTo(previous.runId));
        }

        [Test]
        public void InterruptedTemporaryFileIsNeverLoadedAsASecondSave()
        {
            File.WriteAllText(service.SaveFilePath + ".tmp", JsonUtility.ToJson(SaveGameData.CreateNew("Level_01")));
            Assert.That(service.Load(out _, out _), Is.EqualTo(SaveLoadStatus.Missing));
            Assert.That(service.TryCreateNew("Level_01", out _, out var error), Is.True, error);
            Assert.That(File.Exists(service.SaveFilePath + ".tmp"), Is.False);
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
        }

        [Test]
        public void ReadFailureIsDifferentFromMissingSave()
        {
            Directory.CreateDirectory(service.SaveFilePath);
            Assert.That(service.Load(out var data, out var error), Is.EqualTo(SaveLoadStatus.IOError));
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
    }
}
