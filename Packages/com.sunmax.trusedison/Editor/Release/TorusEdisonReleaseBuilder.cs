using System;
using System.IO;
using UnityEditor;

namespace TorusEdison.Editor.Release
{
    internal static class TorusEdisonReleaseBuilder
    {
        private const string PackageRoot = "Packages/com.sunmax.trusedison";
        private const string StageRoot = "Assets/__TorusEdisonReleaseStaging__";
        private const string StageToolRoot = StageRoot + "/TorusEdison";

        [MenuItem("Tools/Torus Edison/Developer/Build Release UnityPackage")]
        private static void BuildUnityPackageInteractive()
        {
            PackageManifest manifest = LoadManifest();
            string outputPath = EditorUtility.SaveFilePanel(
                "Build Torus Edison UnityPackage",
                Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..")),
                $"TorusEdison-{manifest.version}",
                "unitypackage");

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            BuildUnityPackage(outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }

        public static void BuildUnityPackageBatch()
        {
            string outputPath = Environment.GetEnvironmentVariable("TORUS_EDISON_UNITYPACKAGE_OUTPUT");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("TORUS_EDISON_UNITYPACKAGE_OUTPUT is required.");
            }

            BuildUnityPackage(outputPath);
        }

        internal static void BuildUnityPackage(string outputPath)
        {
            string normalizedOutputPath = NormalizeUnityPackagePath(outputPath);

            try
            {
                CleanupStageRoot();
                CopyDirectoryContents(ProjectToAbsolutePath($"{PackageRoot}/Editor"), ProjectToAbsolutePath($"{StageToolRoot}/Editor"));
                CopyDirectoryContents(ProjectToAbsolutePath($"{PackageRoot}/Documentation~"), ProjectToAbsolutePath($"{StageToolRoot}/Documentation"));
                CopyDirectoryContents(ProjectToAbsolutePath($"{PackageRoot}/Samples~"), ProjectToAbsolutePath($"{StageToolRoot}/Samples"));
                CopyFile(ProjectToAbsolutePath($"{PackageRoot}/CHANGELOG.md"), ProjectToAbsolutePath($"{StageToolRoot}/CHANGELOG.md"));
                CopyFile(ProjectToAbsolutePath($"{PackageRoot}/LICENSE.md"), ProjectToAbsolutePath($"{StageToolRoot}/LICENSE.md"));

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                string outputDirectory = Path.GetDirectoryName(normalizedOutputPath);
                if (string.IsNullOrWhiteSpace(outputDirectory))
                {
                    throw new InvalidOperationException("UnityPackage output directory could not be resolved.");
                }

                Directory.CreateDirectory(outputDirectory);
                AssetDatabase.ExportPackage(StageRoot, normalizedOutputPath, ExportPackageOptions.Recurse);
                UnityEngine.Debug.Log($"Torus Edison unitypackage exported to: {normalizedOutputPath}");
            }
            finally
            {
                CleanupStageRoot();
            }
        }

        private static void CleanupStageRoot()
        {
            FileUtil.DeleteFileOrDirectory(StageRoot);
            FileUtil.DeleteFileOrDirectory($"{StageRoot}.meta");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CopyDirectoryContents(string sourceRoot, string destinationRoot)
        {
            Directory.CreateDirectory(destinationRoot);

            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                string destinationFile = Path.Combine(destinationRoot, relativePath);
                string destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(sourceFile, destinationFile, true);
            }
        }

        private static void CopyFile(string sourceFile, string destinationFile)
        {
            string destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceFile, destinationFile, true);
        }

        private static string ProjectToAbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string NormalizeUnityPackagePath(string path)
        {
            string normalized = Path.GetFullPath(path);
            return normalized.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"{normalized}.unitypackage";
        }

        private static PackageManifest LoadManifest()
        {
            string json = File.ReadAllText(ProjectToAbsolutePath($"{PackageRoot}/package.json"));
            PackageManifest manifest = UnityEngine.JsonUtility.FromJson<PackageManifest>(json);
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.version))
            {
                throw new InvalidOperationException("Package manifest version could not be read.");
            }

            return manifest;
        }

        [Serializable]
        private sealed class PackageManifest
        {
            public string version;
        }
    }
}
