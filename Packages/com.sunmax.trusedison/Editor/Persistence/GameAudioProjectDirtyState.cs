using System;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Persistence
{
    internal sealed class GameAudioProjectDirtyState
    {
        private readonly GameAudioProjectSerializer _serializer;
        private string _cleanSnapshot = string.Empty;

        public GameAudioProjectDirtyState(GameAudioProjectSerializer serializer = null)
        {
            _serializer = serializer ?? new GameAudioProjectSerializer();
        }

        public void MarkClean(GameAudioProject project)
        {
            _cleanSnapshot = CreateSnapshot(project);
        }

        public void Clear()
        {
            _cleanSnapshot = string.Empty;
        }

        public bool IsDirty(GameAudioProject project)
        {
            if (project == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_cleanSnapshot))
            {
                return true;
            }

            return !string.Equals(_cleanSnapshot, CreateSnapshot(project), StringComparison.Ordinal);
        }

        private string CreateSnapshot(GameAudioProject project)
        {
            return project == null ? string.Empty : _serializer.Serialize(project);
        }
    }
}
