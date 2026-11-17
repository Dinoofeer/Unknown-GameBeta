using System;
using System.IO;
using NUnit.Framework;
using UnknownGame.GameFlow;
using UnknownGame.Save;
using UnityEngine;

namespace UnknownGame.Save.Tests
{
    public class MenuGameFlowTests
    {
        private string directory;
        private SaveGameService saves;
        private MenuGameFlow flow;

        [SetUp]
        public void SetUp()
        {
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/MenuFlowTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
            saves = new SaveGameService(directory);
            flow = new MenuGameFlow(saves, scene => scene == "Level_01" || scene == "Level_02", "Level_01");
        }

        [TearDown]
        public void TearDown()
        {
            GameSession.Clear();
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void FirstGameIsSavedBeforeItCanBeLaunched()
        {
            Assert.That(flow.GetSaveStatus(), Is.EqualTo(SaveLoadStatus.Missing));
            Assert.That(flow.TryStartNew(false, out var data, out var error), Is.True, error);
            Assert.That(saves.Load(out var saved, out _), Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(saved.runId, Is.EqualTo(data.runId));
            Assert.That(saved.levelScene, Is.EqualTo("Level_01"));
        }

        [Test]
        public void ExistingGameCannotBeReplacedWithoutConfirmation()
        {
            Assert.That(flow.TryStartNew(false, out var previous, out var error), Is.True, error);
            Assert.That(flow.TryStartNew(false, out var replacement, out error), Is.False);
            Assert.That(replacement, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            saves.Load(out var saved, out _);
            Assert.That(saved.runId, Is.EqualTo(previous.runId));
        }

        [Test]
        public void ConfirmedReplacementCreatesAnotherIdentity()
        {
            Assert.That(flow.TryStartNew(false, out var previous, out var error), Is.True, error);
            Assert.That(flow.TryStartNew(true, out var replacement, out error), Is.True, error);
            Assert.That(replacement.runId, Is.Not.EqualTo(previous.runId));
            Assert.That(saves.Load(out var saved, out error), Is.EqualTo(SaveLoadStatus.Loaded), error);
            Assert.That(saved.runId, Is.EqualTo(replacement.runId));
        }

        [Test]
        public void ContinueReadsCurrentProgressWithoutWritingIt()
        {
            var previous = SaveGameData.CreateNew("Level_02");
            previous.checkpointId = "checkpoint_02";
            previous.deaths = 8;
            Assert.That(saves.TrySave(previous, out var error), Is.True, error);
            string contents = File.ReadAllText(saves.SaveFilePath);
            Assert.That(flow.TryContinue(out var data, out error), Is.True, error);
            Assert.That(data.runId, Is.EqualTo(previous.runId));
            Assert.That(data.levelScene, Is.EqualTo("Level_02"));
            Assert.That(data.checkpointId, Is.EqualTo("checkpoint_02"));
            Assert.That(data.deaths, Is.EqualTo(8));
            Assert.That(File.ReadAllText(saves.SaveFilePath), Is.EqualTo(contents));
        }

        [Test]
        public void MissingSaveCannotBeContinued()
        {
            Assert.That(flow.TryContinue(out var data, out var error), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(Directory.GetFiles(directory), Is.Empty);
        }

        [Test]
        public void InvalidSaveAlsoRequiresConfirmationBeforeReplacement()
        {
            File.WriteAllText(saves.SaveFilePath, "broken");
            Assert.That(flow.TryStartNew(false, out _, out _), Is.False);
            Assert.That(File.ReadAllText(saves.SaveFilePath), Is.EqualTo("broken"));
            Assert.That(flow.TryStartNew(true, out _, out var error), Is.True, error);
        }

        [Test]
        public void UnavailableFirstLevelDoesNotReplaceTheExistingGame()
        {
            Assert.That(flow.TryStartNew(false, out _, out var error), Is.True, error);
            string previous = File.ReadAllText(saves.SaveFilePath);
            var unavailable = new MenuGameFlow(saves, scene => false, "Level_01");
            Assert.That(unavailable.TryStartNew(true, out var data, out _), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(File.ReadAllText(saves.SaveFilePath), Is.EqualTo(previous));
        }

        [Test]
        public void UnavailableSavedLevelDoesNotModifyTheSave()
        {
            Assert.That(saves.TryCreateNew("Level_03", out _, out var error), Is.True, error);
            string previous = File.ReadAllText(saves.SaveFilePath);
            Assert.That(flow.TryContinue(out var data, out error), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(File.ReadAllText(saves.SaveFilePath), Is.EqualTo(previous));
        }

        [Test]
        public void SessionCanCarryLoadedDataAndBeCleared()
        {
            Assert.That(flow.TryStartNew(false, out var data, out var error), Is.True, error);
            GameSession.Begin(data);
            Assert.That(GameSession.Current.runId, Is.EqualTo(data.runId));
            GameSession.Clear();
            Assert.That(GameSession.Current, Is.Null);
        }
    }
}
