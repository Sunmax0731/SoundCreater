using System;
using System.IO;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Audio
{
    internal sealed class GameAudioWavExportService
    {
        private readonly GameAudioProjectRenderer _renderer = new GameAudioProjectRenderer();

        public string Export(GameAudioProject project, string exportDirectory, string projectName)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            string filePath = GameAudioExportUtility.BuildWaveFilePath(exportDirectory, projectName);
            string directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new InvalidOperationException("Export directory could not be resolved.");
            }

            Directory.CreateDirectory(directoryPath);

            GameAudioRenderResult renderResult = _renderer.Render(project);
            byte[] wavBytes = GameAudioWavEncoder.EncodePcm16(renderResult.Samples, renderResult.SampleRate, renderResult.ChannelCount);
            File.WriteAllBytes(filePath, wavBytes);
            return filePath;
        }
    }
}
