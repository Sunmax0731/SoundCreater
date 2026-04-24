using System.Linq;
using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioProjectTemplateTests
    {
        [Test]
        public void BuiltInTemplates_HaveStableIds()
        {
            var templates = GameAudioProjectTemplateLibrary.BuiltInTemplates;

            Assert.That(templates.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(templates.Select(template => template.Id).Distinct().Count(), Is.EqualTo(templates.Count));
            Assert.That(GameAudioProjectTemplateLibrary.TemplateKind, Is.EqualTo("torusEdison.projectTemplate"));
            Assert.That(GameAudioProjectTemplateLibrary.TemplateFileExtension, Is.EqualTo(".gats-template.json"));
        }

        [Test]
        public void OneShotTemplate_CreatesProjectWithVoiceAndAutoTrim()
        {
            Assert.That(GameAudioProjectTemplateLibrary.TryGetTemplate("coin-pickup", out GameAudioProjectTemplate template), Is.True);

            GameAudioProject project = template.CreateProject(44100, GameAudioChannelMode.Mono);

            Assert.That(project.Name, Is.EqualTo("Coin Pickup"));
            Assert.That(project.SampleRate, Is.EqualTo(44100));
            Assert.That(project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
            Assert.That(project.TotalBars, Is.EqualTo(2));
            Assert.That(project.ExportSettings.DurationMode, Is.EqualTo(GameAudioExportDurationMode.AutoTrim));
            Assert.That(project.Tracks[0].Name, Is.EqualTo("Coin Pickup"));
            Assert.That(project.Tracks[0].DefaultVoice, Is.Not.Null);
            Assert.That(project.Tracks[0].Notes, Has.Count.EqualTo(1));
            Assert.That(project.Tracks[0].Notes[0].MidiNote, Is.EqualTo(76));
        }

        [Test]
        public void LoopTemplate_CreatesLoopProjectWithRepeatedNotes()
        {
            Assert.That(GameAudioProjectTemplateLibrary.TryGetTemplate("simple-loop", out GameAudioProjectTemplate template), Is.True);

            GameAudioProject project = template.CreateProject(48000, GameAudioChannelMode.Stereo);

            Assert.That(project.Name, Is.EqualTo("Simple Loop"));
            Assert.That(project.TotalBars, Is.EqualTo(4));
            Assert.That(project.LoopPlayback, Is.True);
            Assert.That(project.ExportSettings.DurationMode, Is.EqualTo(GameAudioExportDurationMode.ProjectBars));
            Assert.That(project.Tracks[0].Notes, Has.Count.EqualTo(4));
            Assert.That(project.Tracks[0].Notes.Select(note => note.StartBeat), Is.EqualTo(new[] { 0.0f, 1.0f, 2.0f, 3.0f }));
        }

        [Test]
        public void DefaultProjectFactory_RemainsEmptyProject()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();

            Assert.That(project.Name, Is.EqualTo("New Audio Project"));
            Assert.That(project.TotalBars, Is.EqualTo(GameAudioToolInfo.DefaultTotalBars));
            Assert.That(project.Tracks, Has.Count.EqualTo(1));
            Assert.That(project.Tracks[0].Notes, Is.Empty);
        }
    }
}
