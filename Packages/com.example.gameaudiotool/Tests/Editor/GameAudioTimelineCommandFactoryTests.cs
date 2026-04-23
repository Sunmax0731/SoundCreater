using System.Linq;
using GameAudioTool.Editor.Application;
using GameAudioTool.Editor.Domain;
using NUnit.Framework;

namespace GameAudioTool.Editor.Tests
{
    public sealed class GameAudioTimelineCommandFactoryTests
    {
        [Test]
        public void AddTimelineNote_CreatesDefaultNoteInTargetTrack()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            string trackId = project.Tracks[0].Id;
            var session = new GameAudioEditorSession(project);

            session.Execute(GameAudioTimelineCommandFactory.AddTimelineNote(session.CurrentProject, trackId, 1.5f, 0.5f));

            GameAudioNote note = session.CurrentProject.Tracks[0].Notes.Single();
            Assert.That(note.StartBeat, Is.EqualTo(1.5f));
            Assert.That(note.DurationBeat, Is.EqualTo(0.5f));
            Assert.That(note.MidiNote, Is.EqualTo(60));
        }

        [Test]
        public void MoveNotes_CanMoveAcrossTracksAndResizeAsOneCommand()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-a", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 60, Velocity = 0.8f });
            project.Tracks.Add(GameAudioProjectFactory.CreateDefaultTrack(2));
            string destinationTrackId = project.Tracks[1].Id;
            var session = new GameAudioEditorSession(project);

            session.Execute(GameAudioTimelineCommandFactory.MoveNotes(session.CurrentProject, new[]
            {
                new GameAudioTimelineNotePlacement("note-a", destinationTrackId, 3.0f, 2.0f)
            }));

            Assert.That(session.CurrentProject.Tracks[0].Notes, Is.Empty);
            GameAudioNote movedNote = session.CurrentProject.Tracks[1].Notes.Single();
            Assert.That(movedNote.StartBeat, Is.EqualTo(3.0f));
            Assert.That(movedNote.DurationBeat, Is.EqualTo(2.0f));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes.Single().StartBeat, Is.EqualTo(0.0f));
        }

        [Test]
        public void DeleteNotes_RemovesMultipleNotesAsSingleCommand()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-a", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 60, Velocity = 0.8f });
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-b", StartBeat = 1.0f, DurationBeat = 1.0f, MidiNote = 62, Velocity = 0.8f });
            var session = new GameAudioEditorSession(project);

            session.Execute(GameAudioTimelineCommandFactory.DeleteNotes(session.CurrentProject, new[] { "note-a", "note-b" }));

            Assert.That(session.CurrentProject.Tracks[0].Notes, Is.Empty);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes.Select(note => note.Id).ToArray(), Is.EqualTo(new[] { "note-a", "note-b" }));
        }
    }
}
