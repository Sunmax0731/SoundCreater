using GameAudioTool.Editor.Domain;

namespace GameAudioTool.Editor.Commands
{
    public interface IGameAudioCommand
    {
        string DisplayName { get; }

        GameAudioProject Execute(GameAudioProject currentProject);

        GameAudioProject Undo(GameAudioProject currentProject);
    }
}
