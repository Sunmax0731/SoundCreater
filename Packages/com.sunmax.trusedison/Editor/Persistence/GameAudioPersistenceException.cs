using System;

namespace TorusEdison.Editor.Persistence
{
    public sealed class GameAudioPersistenceException : Exception
    {
        public GameAudioPersistenceException(string message)
            : base(message)
        {
        }

        public GameAudioPersistenceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
