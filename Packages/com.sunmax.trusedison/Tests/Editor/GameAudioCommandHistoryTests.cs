using System.Linq;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using NUnit.Framework;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioCommandHistoryTests
    {
        [Test]
        public void Session_AddNote_UndoAndRedoRoundTrip()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());
            string trackId = session.CurrentProject.Tracks[0].Id;

            session.Execute(GameAudioProjectCommandFactory.AddNote(
                session.CurrentProject,
                trackId,
                new GameAudioNote
                {
                    Id = "note-added",
                    StartBeat = 1.0f,
                    DurationBeat = 0.5f,
                    MidiNote = 72,
                    Velocity = 0.9f
                }));

            Assert.That(session.CurrentProject.Tracks[0].Notes.Count, Is.EqualTo(1));
            Assert.That(session.CanUndo, Is.True);
            Assert.That(session.History.NextUndoDisplayName, Is.EqualTo("Add Note"));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes, Is.Empty);
            Assert.That(session.CanRedo, Is.True);

            Assert.That(session.Redo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes.Count, Is.EqualTo(1));
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].Id, Is.EqualTo("note-added"));
        }

        [Test]
        public void Session_ChangeNotes_TreatsMultipleSelectionAsOneUndoableOperation()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            string trackId = project.Tracks[0].Id;
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-a", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 60, Velocity = 0.8f });
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-b", StartBeat = 1.0f, DurationBeat = 1.0f, MidiNote = 64, Velocity = 0.8f });

            var session = new GameAudioEditorSession(project);
            session.Execute(GameAudioProjectCommandFactory.ChangeNotes(
                session.CurrentProject,
                new[] { "note-a", "note-b" },
                "Transpose Notes",
                note => note.MidiNote += 12));

            Assert.That(session.CurrentProject.Tracks[0].Notes.Select(note => note.MidiNote).ToArray(), Is.EqualTo(new[] { 72, 76 }));
            Assert.That(session.History.UndoCount, Is.EqualTo(1));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes.Select(note => note.MidiNote).ToArray(), Is.EqualTo(new[] { 60, 64 }));
            Assert.That(session.CurrentProject.Tracks[0].Id, Is.EqualTo(trackId));
        }

        [Test]
        public void Session_DuplicateNotes_UndoesAsSingleCommand()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-a", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 60, Velocity = 0.8f });
            project.Tracks[0].Notes.Add(new GameAudioNote { Id = "note-b", StartBeat = 2.0f, DurationBeat = 1.0f, MidiNote = 67, Velocity = 0.8f });

            var session = new GameAudioEditorSession(project);
            session.Execute(GameAudioProjectCommandFactory.DuplicateNotes(session.CurrentProject, new[] { "note-a", "note-b" }, 4.0f));

            GameAudioNote[] notes = session.CurrentProject.Tracks[0].Notes.ToArray();
            Assert.That(notes.Length, Is.EqualTo(4));
            Assert.That(notes.Count(note => note.StartBeat >= 4.0f), Is.EqualTo(2));
            Assert.That(notes.Select(note => note.Id).Distinct().Count(), Is.EqualTo(4));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes.Select(note => note.Id).ToArray(), Is.EqualTo(new[] { "note-a", "note-b" }));
        }

        [Test]
        public void Session_TrackAndProjectCommands_UndoAndRedoSafely()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());
            string originalTrackId = session.CurrentProject.Tracks[0].Id;

            session.Execute(GameAudioProjectCommandFactory.AddTrack(
                session.CurrentProject,
                new GameAudioTrack
                {
                    Id = "track-extra",
                    Name = "Track Extra",
                    DefaultVoice = GameAudioProjectFactory.CreateDefaultVoice()
                }));

            session.Execute(GameAudioProjectCommandFactory.ChangeTracks(
                session.CurrentProject,
                new[] { "track-extra" },
                "Mute Track",
                track =>
                {
                    track.Mute = true;
                    track.Pan = -0.25f;
                }));

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(
                session.CurrentProject,
                "Change Project",
                project =>
                {
                    project.Bpm = 150;
                    project.LoopPlayback = true;
                }));

            Assert.That(session.CurrentProject.Tracks.Count, Is.EqualTo(2));
            Assert.That(session.CurrentProject.Tracks[1].Mute, Is.True);
            Assert.That(session.CurrentProject.Bpm, Is.EqualTo(150));
            Assert.That(session.CurrentProject.LoopPlayback, Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Bpm, Is.EqualTo(120));
            Assert.That(session.CurrentProject.LoopPlayback, Is.False);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[1].Mute, Is.False);
            Assert.That(session.CurrentProject.Tracks[1].Pan, Is.EqualTo(0.0f));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks.Count, Is.EqualTo(1));
            Assert.That(session.CurrentProject.Tracks[0].Id, Is.EqualTo(originalTrackId));

            Assert.That(session.Redo(), Is.True);
            Assert.That(session.Redo(), Is.True);
            Assert.That(session.Redo(), Is.True);
            Assert.That(session.CurrentProject.Tracks.Count, Is.EqualTo(2));
            Assert.That(session.CurrentProject.Tracks[1].Mute, Is.True);
            Assert.That(session.CurrentProject.Bpm, Is.EqualTo(150));
        }

        [Test]
        public void Session_MoveResizeRemoveNoteAndRemoveTrack_RoundTrip()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            string firstTrackId = project.Tracks[0].Id;
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "note-main",
                StartBeat = 0.0f,
                DurationBeat = 1.0f,
                MidiNote = 60,
                Velocity = 0.8f
            });

            var session = new GameAudioEditorSession(project);
            session.Execute(GameAudioProjectCommandFactory.AddTrack(
                session.CurrentProject,
                new GameAudioTrack
                {
                    Id = "track-destination",
                    Name = "Destination",
                    DefaultVoice = GameAudioProjectFactory.CreateDefaultVoice()
                }));

            string secondTrackId = session.CurrentProject.Tracks[1].Id;

            session.Execute(GameAudioProjectCommandFactory.MoveNote(session.CurrentProject, "note-main", secondTrackId, 3.5f));
            Assert.That(session.CurrentProject.Tracks[0].Notes, Is.Empty);
            Assert.That(session.CurrentProject.Tracks[1].Notes[0].StartBeat, Is.EqualTo(3.5f));

            session.Execute(GameAudioProjectCommandFactory.ResizeNote(session.CurrentProject, "note-main", 2.0f));
            Assert.That(session.CurrentProject.Tracks[1].Notes[0].DurationBeat, Is.EqualTo(2.0f));

            session.Execute(GameAudioProjectCommandFactory.RemoveNote(session.CurrentProject, secondTrackId, "note-main"));
            Assert.That(session.CurrentProject.Tracks[1].Notes, Is.Empty);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[1].Notes[0].DurationBeat, Is.EqualTo(2.0f));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[1].Notes[0].DurationBeat, Is.EqualTo(1.0f));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Id, Is.EqualTo(firstTrackId));
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].Id, Is.EqualTo("note-main"));
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].StartBeat, Is.EqualTo(0.0f));

            session.Execute(GameAudioProjectCommandFactory.RemoveTrack(session.CurrentProject, secondTrackId));
            Assert.That(session.CurrentProject.Tracks.Count, Is.EqualTo(1));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks.Count, Is.EqualTo(2));
            Assert.That(session.CurrentProject.Tracks[1].Id, Is.EqualTo(secondTrackId));
        }

        [Test]
        public void Session_ClearsRedoAfterNewCommandAndRespectsHistoryLimit()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject(), 2);

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(session.CurrentProject, "Rename One", project => project.Name = "One"));
            session.Execute(GameAudioProjectCommandFactory.ChangeProject(session.CurrentProject, "Rename Two", project => project.Name = "Two"));
            session.Execute(GameAudioProjectCommandFactory.ChangeProject(session.CurrentProject, "Rename Three", project => project.Name = "Three"));

            Assert.That(session.History.UndoCount, Is.EqualTo(2));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Name, Is.EqualTo("Two"));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Name, Is.EqualTo("One"));
            Assert.That(session.Undo(), Is.False);

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(session.CurrentProject, "Rename Four", project => project.Name = "Four"));
            Assert.That(session.CanRedo, Is.False);
            Assert.That(session.CurrentProject.Name, Is.EqualTo("Four"));
        }
    }
}
