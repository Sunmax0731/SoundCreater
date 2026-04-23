using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Commands
{
    public interface IGameAudioCommand
    {
        string DisplayName { get; }

        GameAudioProject Execute(GameAudioProject currentProject);

        GameAudioProject Undo(GameAudioProject currentProject);
    }
}
