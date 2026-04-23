using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameAudioTool.Editor.Audio
{
    internal sealed class GameAudioPreviewClipAssetCache
    {
        private const string CacheFolderName = "__TorusEdisonPreviewCache__";
        private readonly string _sessionToken = Guid.NewGuid().ToString("N");

        private string _outputAssetPath;
        private string _loopAssetPath;

        public AudioClip CreateOutputClip(string projectName, GameAudioRenderResult renderResult)
        {
            if (renderResult == null)
            {
                throw new ArgumentNullException(nameof(renderResult));
            }

            _outputAssetPath = WriteClipAsset(projectName, "output", renderResult.Samples, renderResult.FrameCount, renderResult.ChannelCount, renderResult.SampleRate);
            return LoadClip(_outputAssetPath);
        }

        public AudioClip CreateLoopClip(string projectName, float[] samples, int frameCount, int channelCount, int sampleRate)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            _loopAssetPath = WriteClipAsset(projectName, "loop", samples, frameCount, channelCount, sampleRate);
            return LoadClip(_loopAssetPath);
        }

        public void Clear()
        {
            DeleteAsset(ref _outputAssetPath);
            DeleteAsset(ref _loopAssetPath);
        }

        private static AudioClip LoadClip(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        private static void DeleteAsset(ref string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            assetPath = null;
        }

        private string WriteClipAsset(string projectName, string role, float[] samples, int frameCount, int channelCount, int sampleRate)
        {
            if (frameCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            }

            if (channelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channelCount));
            }

            EnsureCacheFolder();

            string safeProjectName = SanitizeFileName(projectName);
            string fileName = $"{safeProjectName}-{role}-{_sessionToken}.wav";
            string absolutePath = Path.Combine(UnityEngine.Application.dataPath, CacheFolderName, fileName);
            string assetPath = $"Assets/{CacheFolderName}/{fileName}";
            byte[] wavBytes = GameAudioWavEncoder.EncodePcm16(samples, sampleRate, channelCount);

            File.WriteAllBytes(absolutePath, wavBytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(assetPath) is AudioImporter importer)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                settings.quality = 1.0f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = false;
                importer.forceToMono = channelCount == 1;
                importer.SaveAndReimport();
            }

            return assetPath;
        }

        private static void EnsureCacheFolder()
        {
            string folderAssetPath = $"Assets/{CacheFolderName}";
            if (!AssetDatabase.IsValidFolder(folderAssetPath))
            {
                AssetDatabase.CreateFolder("Assets", CacheFolderName);
            }
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "preview";
            }

            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            char[] buffer = value.ToCharArray();
            for (int index = 0; index < buffer.Length; index++)
            {
                if (Array.IndexOf(invalidCharacters, buffer[index]) >= 0)
                {
                    buffer[index] = '_';
                }
            }

            string sanitized = new string(buffer).Trim();
            return string.IsNullOrWhiteSpace(sanitized)
                ? "preview"
                : sanitized.Replace(' ', '_');
        }
    }
}
