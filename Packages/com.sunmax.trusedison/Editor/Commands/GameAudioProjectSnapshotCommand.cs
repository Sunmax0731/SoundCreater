using System;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Commands
{
    internal sealed class GameAudioProjectSnapshotCommand : IGameAudioCommand
    {
        private readonly GameAudioProject _beforeProject;
        private readonly GameAudioProject _afterProject;

        public GameAudioProjectSnapshotCommand(string displayName, GameAudioProject beforeProject, GameAudioProject afterProject)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            _beforeProject = GameAudioProjectCloner.CloneProject(beforeProject ?? throw new ArgumentNullException(nameof(beforeProject)));
            _afterProject = GameAudioProjectCloner.CloneProject(afterProject ?? throw new ArgumentNullException(nameof(afterProject)));
            DisplayName = displayName;
        }

        public string DisplayName { get; }

        public GameAudioProject Execute(GameAudioProject currentProject)
        {
            return GameAudioProjectCloner.CloneProject(_afterProject);
        }

        public GameAudioProject Undo(GameAudioProject currentProject)
        {
            return GameAudioProjectCloner.CloneProject(_beforeProject);
        }
    }
}
