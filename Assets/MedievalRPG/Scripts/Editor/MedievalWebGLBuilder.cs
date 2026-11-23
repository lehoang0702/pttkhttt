using System;
using System.IO;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MedievalRPG.EditorTools
{
    /// <summary>
    /// Cấu hình phát hành và build bản WebGL cho game Chiến Binh Tri Thức (tương thích GitHub Pages, itch.io và local HTTP).
    /// </summary>
    public static class MedievalWebGLBuilder
    {
        public const string DefaultOutputFolder = "Builds/WebGL";
        public const string GameProductName = "Chiến Binh Tri Thức";
        public const string GameCompanyName = "MedievalRPG";
        public const string CustomWebTemplate = "PROJECT:ChienBinhTriThuc";

        [MenuItem("MedievalRPG/WebGL/1. Apply WebGL Release Settings")]
        [CliCommand("medieval_apply_webgl_settings", "Apply WebGL release PlayerSettings for Chien Binh Tri Thuc")]
        public static string ApplyWebGLReleaseSettings()
        {
            NamedBuildTarget webTarget = NamedBuildTarget.WebGL;

            PlayerSettings.companyName = GameCompanyName;
            PlayerSettings.productName = GameProductName;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.SetManagedStrippingLevel(webTarget, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(webTarget, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetApiCompatibilityLevel(webTarget, ApiCompatibilityLevel.NET_Standard);

            PlayerSettings.WebGL.template = CustomWebTemplate;
            // Gzip + Decompression Fallback chạy ổn định trên GitHub Pages, itch.io và HTTP thường
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            // Giữ ExplicitlyThrownExceptionsOnly vì SaveFileUtility và QuizManager dùng try/catch
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.wasm2023 = true;

            UnityEditor.WebGL.UserBuildSettings.codeOptimization =
                UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;

            AssetDatabase.SaveAssets();

            string summary =
                $"OK: WebGL release settings saved:\n" +
                $"  productName={PlayerSettings.productName}\n" +
                $"  webScreen={PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}\n" +
                $"  template={PlayerSettings.WebGL.template}\n" +
                $"  compressionFormat={PlayerSettings.WebGL.compressionFormat}\n" +
                $"  decompressionFallback={PlayerSettings.WebGL.decompressionFallback}\n" +
                $"  dataCaching={PlayerSettings.WebGL.dataCaching}\n" +
                $"  exceptionSupport={PlayerSettings.WebGL.exceptionSupport}\n" +
                $"  managedStrippingLevel={PlayerSettings.GetManagedStrippingLevel(webTarget)}\n" +
                $"  il2cppCodeGeneration={PlayerSettings.GetIl2CppCodeGeneration(webTarget)}\n" +
                $"  wasm2023={PlayerSettings.WebGL.wasm2023}\n" +
                $"  codeOptimization={UnityEditor.WebGL.UserBuildSettings.codeOptimization}";

            Debug.Log($"[MedievalWebGLBuilder] {summary}");
            return summary;
        }

        [MenuItem("MedievalRPG/WebGL/2. Build WebGL Release (Builds/WebGL)")]
        [CliCommand("medieval_build_webgl", "Configure and build WebGL release player to Builds/WebGL")]
        public static string BuildWebGLInteractive()
        {
            return BuildWebGLToOutput(DefaultOutputFolder);
        }

        public static void BuildWebGLBatch()
        {
            string output = ResolveBuildOutputFromArgs(DefaultOutputFolder);
            string result = BuildWebGLToOutput(output);
            if (!result.StartsWith("OK:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(result);
            }
        }

        private static string BuildWebGLToOutput(string outputPath)
        {
            ApplyWebGLReleaseSettings();

            if (!Application.isBatchMode && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.WebGL, BuildTarget.WebGL);
                if (!switched)
                {
                    return "FAIL: Could not switch active build target to WebGL.";
                }
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                return "FAIL: No enabled scenes found in EditorBuildSettings.";
            }

            string fullOutput = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(fullOutput);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullOutput,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                string err = $"FAIL: WebGL build finished with result {summary.result} (errors: {summary.totalErrors}, warnings: {summary.totalWarnings}).";
                Debug.LogError($"[MedievalWebGLBuilder] {err}");
                return err;
            }

            double mb = summary.totalSize / (1024.0 * 1024.0);
            string msg = $"OK: WebGL build succeeded at '{fullOutput}' ({mb:F2} MB, {summary.totalTime.TotalSeconds:F1}s).";
            Debug.Log($"[MedievalWebGLBuilder] {msg}");
            return msg;
        }

        private static string ResolveBuildOutputFromArgs(string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-buildOutput", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(args[i], "-outputPath", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        return args[i + 1];
                    }
                }
            }
            return fallback;
        }
    }
}
