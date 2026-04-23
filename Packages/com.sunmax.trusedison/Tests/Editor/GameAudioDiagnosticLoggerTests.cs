using NUnit.Framework;
using TorusEdison.Editor.Utilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioDiagnosticLoggerTests
    {
        [TearDown]
        public void TearDown()
        {
            GameAudioDiagnosticLogger.Configure(false, GameAudioDiagnosticLogLevel.Info);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DisabledLogger_SuppressesInfoLogs()
        {
            GameAudioDiagnosticLogger.Configure(false, GameAudioDiagnosticLogLevel.Verbose);
            GameAudioDiagnosticLogger.Info("Tests", "info suppressed");
        }

        [Test]
        public void WarningLevel_LogsWarningsButSuppressesInfo()
        {
            GameAudioDiagnosticLogger.Configure(true, GameAudioDiagnosticLogLevel.Warning);

            LogAssert.Expect(LogType.Warning, "[Torus Edison][Tests][Warning] warning visible");

            GameAudioDiagnosticLogger.Info("Tests", "info suppressed");
            GameAudioDiagnosticLogger.Warning("Tests", "warning visible");
        }

        [Test]
        public void VerboseLevel_LogsVerboseMessages()
        {
            GameAudioDiagnosticLogger.Configure(true, GameAudioDiagnosticLogLevel.Verbose);

            LogAssert.Expect(LogType.Log, "[Torus Edison][Tests][Verbose] verbose visible");

            GameAudioDiagnosticLogger.Verbose("Tests", "verbose visible");
        }
    }
}
