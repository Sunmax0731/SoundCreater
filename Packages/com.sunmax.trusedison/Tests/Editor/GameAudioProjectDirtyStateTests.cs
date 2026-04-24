using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Persistence;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioProjectDirtyStateTests
    {
        [Test]
        public void IsDirty_ReturnsFalseAfterUndoBackToCleanSnapshot_AndTrueAfterRedo()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());
            var dirtyState = new GameAudioProjectDirtyState();
            dirtyState.MarkClean(session.CurrentProject);

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(
                session.CurrentProject,
                "Rename",
                project => project.Name = "Edited"));

            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.False);

            Assert.That(session.Redo(), Is.True);
            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.True);
        }

        [Test]
        public void MarkClean_RebaselinesDirtyStateLikeSaveAs()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());
            var dirtyState = new GameAudioProjectDirtyState();

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(
                session.CurrentProject,
                "Rename One",
                project => project.Name = "Saved Name"));
            dirtyState.MarkClean(session.CurrentProject);

            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.False);

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(
                session.CurrentProject,
                "Rename Two",
                project => project.Name = "Unsaved Name"));

            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.False);

            Assert.That(session.Redo(), Is.True);
            Assert.That(dirtyState.IsDirty(session.CurrentProject), Is.True);
        }
    }
}
