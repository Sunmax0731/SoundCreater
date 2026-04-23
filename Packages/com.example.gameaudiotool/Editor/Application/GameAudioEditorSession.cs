using System;
using GameAudioTool.Editor.Commands;
using GameAudioTool.Editor.Domain;

namespace GameAudioTool.Editor.Application
{
    public sealed class GameAudioEditorSession
    {
        private readonly GameAudioCommandHistory _history;

        public GameAudioEditorSession(GameAudioProject project, int undoHistoryLimit = 100)
        {
            CurrentProject = GameAudioProjectCloner.CloneProject(project ?? throw new ArgumentNullException(nameof(project)));
            _history = new GameAudioCommandHistory(undoHistoryLimit);
        }

        public GameAudioProject CurrentProject { get; private set; }

        public GameAudioCommandHistory History => _history;

        public bool CanUndo => _history.CanUndo;

        public bool CanRedo => _history.CanRedo;

        public void ReplaceProject(GameAudioProject project, bool clearHistory = true)
        {
            CurrentProject = GameAudioProjectCloner.CloneProject(project ?? throw new ArgumentNullException(nameof(project)));
            if (clearHistory)
            {
                _history.Clear();
            }
        }

        public void Execute(IGameAudioCommand command)
        {
            CurrentProject = _history.Execute(CurrentProject, command ?? throw new ArgumentNullException(nameof(command)));
        }

        public void Execute(string displayName, Action<GameAudioProject> applyChange)
        {
            Execute(GameAudioProjectCommandFactory.CreateMutation(CurrentProject, displayName, applyChange));
        }

        public bool Undo()
        {
            if (!_history.CanUndo)
            {
                return false;
            }

            CurrentProject = _history.Undo(CurrentProject);
            return true;
        }

        public bool Redo()
        {
            if (!_history.CanRedo)
            {
                return false;
            }

            CurrentProject = _history.Redo(CurrentProject);
            return true;
        }
    }
}
