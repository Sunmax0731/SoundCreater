using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Audio;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Persistence;
using UnityEngine;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioAcceptanceScenarioTests
    {
        [TestCase("BasicSE/basic-se.gats.json", false)]
        [TestCase("SimpleLoop/simple-loop.gats.json", true)]
        public void BundledSamples_CanLoadRenderAndExport(string relativeSamplePath, bool expectedLoopPlayback)
        {
            string samplePath = GetPackagePath("Samples~", relativeSamplePath);
            var serializer = new GameAudioProjectSerializer();

            GameAudioProjectLoadResult loadResult = serializer.LoadFromFile(samplePath);

            Assert.That(loadResult.Warnings, Is.Empty);
            Assert.That(loadResult.Project.LoopPlayback, Is.EqualTo(expectedLoopPlayback));
            Assert.That(loadResult.Project.Tracks.Count, Is.GreaterThan(0));
            Assert.That(loadResult.Project.Tracks.Sum(track => track.Notes.Count), Is.GreaterThan(0));

            var renderer = new GameAudioProjectRenderer();
            GameAudioRenderResult renderResult = renderer.Render(loadResult.Project);
            float peak = renderResult.Samples.Max(sample => Math.Abs(sample));

            Assert.That(renderResult.Samples.Length, Is.GreaterThan(0));
            Assert.That(peak, Is.GreaterThan(0.05f));

            string exportRoot = Path.Combine(Path.GetTempPath(), "TorusEdisonAcceptanceTests", Path.GetRandomFileName());

            try
            {
                string filePath = new GameAudioWavExportService().Export(loadResult.Project, exportRoot, loadResult.Project.Name);
                byte[] bytes = File.ReadAllBytes(filePath);

                Assert.That(File.Exists(filePath), Is.True);
                Assert.That(bytes.Length, Is.GreaterThan(44));
                Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("RIFF"));
            }
            finally
            {
                if (Directory.Exists(exportRoot))
                {
                    Directory.Delete(exportRoot, true);
                }
            }
        }

        [Test]
        public void EditingScenario_RemainsRenderableAfterUndoRedo()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());
            string trackId = session.CurrentProject.Tracks[0].Id;

            session.Execute(GameAudioProjectCommandFactory.AddNote(
                session.CurrentProject,
                trackId,
                new GameAudioNote
                {
                    Id = "acceptance-note",
                    StartBeat = 0.0f,
                    DurationBeat = 1.0f,
                    MidiNote = 60,
                    Velocity = 0.8f
                }));

            session.Execute(GameAudioProjectCommandFactory.ChangeNotes(
                session.CurrentProject,
                new[] { "acceptance-note" },
                "Adjust Note",
                note =>
                {
                    note.MidiNote += 7;
                    note.StartBeat = 1.5f;
                }));

            session.Execute(GameAudioProjectCommandFactory.ResizeNote(
                session.CurrentProject,
                "acceptance-note",
                1.5f));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].DurationBeat, Is.EqualTo(1.0f));

            Assert.That(session.Redo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].DurationBeat, Is.EqualTo(1.5f));
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].MidiNote, Is.EqualTo(67));
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].StartBeat, Is.EqualTo(1.5f));

            GameAudioRenderResult renderResult = new GameAudioProjectRenderer().Render(session.CurrentProject);
            float peak = renderResult.Samples.Max(sample => Math.Abs(sample));

            Assert.That(renderResult.Samples.Length, Is.GreaterThan(0));
            Assert.That(peak, Is.GreaterThan(0.05f));
        }

        private static string GetPackagePath(string firstSegment, string secondSegment)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Packages", "com.sunmax.trusedison", firstSegment, secondSegment);
        }
    }
}
