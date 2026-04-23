using System;
using System.Collections.Generic;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Commands
{
    public sealed class GameAudioCommandHistory
    {
        private readonly int _limit;
        private readonly List<IGameAudioCommand> _undoCommands = new List<IGameAudioCommand>();
        private readonly List<IGameAudioCommand> _redoCommands = new List<IGameAudioCommand>();

        public GameAudioCommandHistory(int limit = 100)
        {
            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            _limit = limit;
        }

        public int Limit => _limit;

        public int UndoCount => _undoCommands.Count;

        public int RedoCount => _redoCommands.Count;

        public bool CanUndo => _undoCommands.Count > 0;

        public bool CanRedo => _redoCommands.Count > 0;

        public string NextUndoDisplayName => CanUndo
            ? _undoCommands[_undoCommands.Count - 1].DisplayName
            : string.Empty;

        public string NextRedoDisplayName => CanRedo
            ? _redoCommands[_redoCommands.Count - 1].DisplayName
            : string.Empty;

        public void Clear()
        {
            _undoCommands.Clear();
            _redoCommands.Clear();
        }

        public GameAudioProject Execute(GameAudioProject currentProject, IGameAudioCommand command)
        {
            if (currentProject == null)
            {
                throw new ArgumentNullException(nameof(currentProject));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            GameAudioProject nextProject = command.Execute(currentProject);
            PushUndo(command);
            _redoCommands.Clear();
            return nextProject;
        }

        public GameAudioProject Undo(GameAudioProject currentProject)
        {
            if (currentProject == null)
            {
                throw new ArgumentNullException(nameof(currentProject));
            }

            if (!CanUndo)
            {
                return currentProject;
            }

            IGameAudioCommand command = PopLast(_undoCommands);
            GameAudioProject previousProject = command.Undo(currentProject);
            _redoCommands.Add(command);
            return previousProject;
        }

        public GameAudioProject Redo(GameAudioProject currentProject)
        {
            if (currentProject == null)
            {
                throw new ArgumentNullException(nameof(currentProject));
            }

            if (!CanRedo)
            {
                return currentProject;
            }

            IGameAudioCommand command = PopLast(_redoCommands);
            GameAudioProject nextProject = command.Execute(currentProject);
            PushUndo(command);
            return nextProject;
        }

        private void PushUndo(IGameAudioCommand command)
        {
            if (_undoCommands.Count >= _limit)
            {
                _undoCommands.RemoveAt(0);
            }

            _undoCommands.Add(command);
        }

        private static IGameAudioCommand PopLast(List<IGameAudioCommand> commands)
        {
            int lastIndex = commands.Count - 1;
            IGameAudioCommand command = commands[lastIndex];
            commands.RemoveAt(lastIndex);
            return command;
        }
    }
}
