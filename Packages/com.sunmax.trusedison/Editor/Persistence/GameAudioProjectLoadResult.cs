using System.Collections.Generic;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Persistence
{
    public sealed class GameAudioProjectLoadResult
    {
        public GameAudioProjectLoadResult(GameAudioProject project, IReadOnlyList<string> warnings)
        {
            Project = project;
            Warnings = warnings ?? new List<string>();
        }

        public GameAudioProject Project { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
