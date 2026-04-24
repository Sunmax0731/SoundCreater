using System;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Application
{
    public sealed class GameAudioProjectTemplate
    {
        private readonly Func<int, GameAudioChannelMode, GameAudioProject> _createProject;

        public GameAudioProjectTemplate(
            string id,
            string category,
            string displayName,
            string description,
            Func<int, GameAudioChannelMode, GameAudioProject> createProject)
        {
            Id = id ?? string.Empty;
            Category = category ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            _createProject = createProject ?? throw new ArgumentNullException(nameof(createProject));
        }

        public string Id { get; }

        public string Category { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public GameAudioProject CreateProject(int sampleRate, GameAudioChannelMode channelMode)
        {
            return _createProject(sampleRate, channelMode);
        }
    }
}
