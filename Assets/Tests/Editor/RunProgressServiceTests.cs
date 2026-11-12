using System;
using System.IO;
using NUnit.Framework;
using UnknownGame.GameFlow;
using UnityEngine;

namespace UnknownGame.Save.Tests
{
    public class RunProgressServiceTests
    {
        private string directory;
        private SaveGameService saves;
        private SaveGameData current;
        private RunProgressService progress;

        [SetUp]
        public void SetUp()
        {
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/RunProgressTests", Guid.NewGuid().ToString("N")));
            saves = new SaveGameService(directory);
            Assert.That(saves.TryCreateNew("Level_01", out current, out var error), Is.True, error);
            progress = new RunProgressService(current, saves);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void CheckpointSnapshotKeepsIdentityAndPersistsStatsAndKey()
        {
            string identity = current.runId;
            Assert.That(progress.TrySave("level01_cp02", 5, 150f, true, out var error), Is.True, error);
            Assert.That(saves.Load(out var loaded, out error), Is.EqualTo(SaveLoadStatus.Loaded), error);
            Assert.That(loaded.runId, Is.EqualTo(identity));
            Assert.That(loaded.levelScene, Is.EqualTo("Level_01"));
            Assert.That(loaded.checkpointId, Is.EqualTo("level01_cp02"));
            Assert.That(loaded.deaths, Is.EqualTo(5));
            Assert.That(loaded.elapsedSeconds, Is.EqualTo(150f));
            Assert.That(loaded.hasSecuredKey, Is.True);
            Assert.That(current.checkpointId, Is.EqualTo(loaded.checkpointId));
        }

        [Test]
        public void AdvancingKeepsRunTotalsAndPreventsWritesFromPreviousLevel()
        {
            Assert.That(progress.TrySave("despertar_cp01", 4, 90f, true, out var error), Is.True, error);
            Assert.That(progress.TryAdvance("Level_02", 5, 115f, out var next, out error), Is.True, error);
            Assert.That(next.runId, Is.EqualTo(current.runId));
            Assert.That(next.checkpointId, Is.Empty);
            Assert.That(next.hasSecuredKey, Is.False);
            Assert.That(progress.TrySave("despertar_cp01", 6, 120f, false, out _), Is.False);

            Assert.That(saves.Load(out var loaded, out error), Is.EqualTo(SaveLoadStatus.Loaded), error);
            Assert.That(loaded.levelScene, Is.EqualTo("Level_02"));
            Assert.That(loaded.deaths, Is.EqualTo(5));
            Assert.That(loaded.elapsedSeconds, Is.EqualTo(115f));
        }

        [Test]
        public void ClosingUpdatesStatsWithoutMovingLastCheckpoint()
        {
            Assert.That(progress.TrySave("level01_cp01", 2, 60f, false, out var error), Is.True, error);
            Assert.That(progress.TrySave(current.checkpointId, 4, 90f, false, out error), Is.True, error);
            saves.Load(out var loaded, out _);
            Assert.That(loaded.checkpointId, Is.EqualTo("level01_cp01"));
            Assert.That(loaded.deaths, Is.EqualTo(4));
            Assert.That(loaded.elapsedSeconds, Is.EqualTo(90f));
        }

        [Test]
        public void OldSessionCannotOverwriteANewlyCreatedGame()
        {
            Assert.That(saves.TryCreateNew("Level_01", out var replacement, out var error), Is.True, error);
            Assert.That(progress.TrySave("level01_cp02", 10, 200f, true, out error), Is.False);
            saves.Load(out var loaded, out _);
            Assert.That(loaded.runId, Is.EqualTo(replacement.runId));
            Assert.That(loaded.checkpointId, Is.Empty);
            Assert.That(current.checkpointId, Is.Empty);
        }

        [Test]
        public void FailedWriteKeepsDiskAndSessionAtPreviousSnapshot()
        {
            Assert.That(progress.TrySave("level01_cp01", 2, 60f, false, out var error), Is.True, error);
            Directory.CreateDirectory(saves.SaveFilePath + ".tmp");
            Assert.That(progress.TrySave("level01_cp02", 7, 120f, true, out error), Is.False);
            Assert.That(current.checkpointId, Is.EqualTo("level01_cp01"));
            Assert.That(current.deaths, Is.EqualTo(2));
            saves.Load(out var loaded, out _);
            Assert.That(loaded.checkpointId, Is.EqualTo("level01_cp01"));
        }

        [Test]
        public void InvalidCheckpointDoesNotMutateCurrentProgress()
        {
            Assert.That(progress.TrySave("../invalid", 1, 10f, false, out _), Is.False);
            Assert.That(current.checkpointId, Is.Empty);
            Assert.That(current.deaths, Is.Zero);
            saves.Load(out var loaded, out _);
            Assert.That(loaded.checkpointId, Is.Empty);
        }

        [Test]
        public void MissingSaveIsNotSilentlyRecreatedByAnOldSession()
        {
            File.Delete(saves.SaveFilePath);
            Assert.That(progress.TrySave("level01_cp01", 1, 10f, false, out _), Is.False);
            Assert.That(File.Exists(saves.SaveFilePath), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KeyCannotBeSecuredWithoutACheckpoint(bool key)
        {
            Assert.That(progress.TrySave(string.Empty, 0, 10f, key, out var error), Is.True, error);
            saves.Load(out var loaded, out _);
            Assert.That(loaded.hasSecuredKey, Is.False);
        }
    }
}
