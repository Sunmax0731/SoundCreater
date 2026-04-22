using System.Collections.Generic;
using GameAudioTool.Editor.Domain;

namespace GameAudioTool.Editor.Persistence
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
