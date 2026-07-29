using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ImmPlayer.Editor
{
    /// <summary>
    /// Exports the two IMM UPM packages as a single self-contained
    /// .unitypackage for release, so a project can consume IMM by dragging one
    /// file in rather than editing manifest.json.
    ///
    /// The exported asset paths keep their "Packages/..." prefix, so on import
    /// they land as EMBEDDED PACKAGES rather than loose files in Assets/. That
    /// keeps the assembly definitions, plugin import settings and package
    /// identity intact, and lets a consumer delete the folder to uninstall.
    ///
    /// Newtonsoft Json is NOT bundled: SharpQuill's reader/writer genuinely use
    /// it, and shipping a second copy would collide with any project that
    /// already has it. Consumers add com.unity.nuget.newtonsoft-json from the
    /// Unity registry (Package Manager -> Add package by name).
    /// </summary>
    public static class ImmPackageExporter
    {
        private const string ImmUnityPath = "Packages/com.immersive-foundation.imm-unity";
        private const string StrokeReaderPath = "Packages/com.immersive-foundation.imm-stroke-reader";

        [MenuItem("IMM/Export Unity Package (release artifact)")]
        public static void ExportFromMenu()
        {
            string path = Export();
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>Batchmode entry point: Unity.exe -executeMethod ImmPlayer.Editor.ImmPackageExporter.ExportBatch</summary>
        public static void ExportBatch()
        {
            try
            {
                string path = Export();
                Debug.Log($"[IMM_EXPORT] wrote {path}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IMM_EXPORT] failed: {e}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        public static string Export()
        {
            string version = ReadPackageVersion(ImmUnityPath);

            // dist/ sits beside the Unity project and is gitignored: a release
            // artifact should not become another large blob in this repo's
            // history (see PACKAGING.md).
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string distDir = Path.Combine(projectRoot, "dist");
            Directory.CreateDirectory(distDir);

            string outputPath = Path.Combine(distDir, $"IMM-Unity-{version}.unitypackage");

            string[] roots = { ImmUnityPath, StrokeReaderPath };
            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                    throw new DirectoryNotFoundException($"Package not found: {root}");
            }

            AssetDatabase.ExportPackage(
                roots,
                outputPath,
                ExportPackageOptions.Recurse);

            var info = new FileInfo(outputPath);
            Debug.Log($"[IMM_EXPORT] IMM-Unity-{version}.unitypackage " +
                      $"({info.Length / 1024 / 1024} MB) -> {outputPath}");
            return outputPath;
        }

        private static string ReadPackageVersion(string packagePath)
        {
            string json = File.ReadAllText(Path.Combine(packagePath, "package.json"));
            const string key = "\"version\"";
            int i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return "0.0.0";
            int open = json.IndexOf('"', json.IndexOf(':', i) + 1);
            int close = json.IndexOf('"', open + 1);
            return json.Substring(open + 1, close - open - 1);
        }
    }
}
