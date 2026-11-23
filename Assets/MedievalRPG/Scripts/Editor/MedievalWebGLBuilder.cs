using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Analytics;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.CrashReporting;
using UnityEditor.EngineDiagnostics;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace MedievalRPG.EditorTools
{
    /// <summary>
    /// Cấu hình phát hành, dọn package/Cloud thừa và build bản WebGL cho game Chiến Binh Tri Thức.
    /// </summary>
    public static class MedievalWebGLBuilder
    {
        public const string DefaultOutputFolder = "Builds/WebGL";
        public const string GameProductName = "Chiến Binh Tri Thức";
        public const string GameCompanyName = "MedievalRPG";
        public const string CustomWebTemplate = "PROJECT:ChienBinhTriThuc";

        private static readonly string[] UnusedCloudAndHeavyPackages =
        {
            "com.unity.ai.assistant",
            "com.unity.ai.inference",
            "com.unity.collab-proxy",
            "com.unity.visualscripting",
            "com.unity.modules.unityanalytics"
        };

        private static ListRequest _activeListRequest;
        private static AddAndRemoveRequest _activeRemoveRequest;
        private static string[] _pendingRemovePackages;
        private static bool _isPollingPackageCleanup;

        /// <summary>
        /// Gỡ bỏ các package không dùng gây gọi Unity Cloud / cảnh báo shader Sentis và tắt Cloud Diagnostics.
        /// </summary>
        [MenuItem("MedievalRPG/WebGL/0. Clean Unused Packages (Fix Cloud & Shader Warnings)")]
        public static void CleanUnusedPackagesAndCloudSettings()
        {
            DisableCloudAndDiagnosticsSettings();
            AssetDatabase.SaveAssets();

            if (_activeListRequest != null || _activeRemoveRequest != null)
            {
                Debug.LogWarning("[MedievalWebGLBuilder] Package cleanup is already in progress.");
                return;
            }

            _activeListRequest = Client.List(true, false);
            HookPackageCleanupPoll();
            Debug.Log("[MedievalWebGLBuilder] Checking installed packages to clean unused Cloud/AI/Analytics modules...");
        }

        [MenuItem("MedievalRPG/WebGL/1. Apply WebGL Release Settings")]
        [CliCommand("medieval_apply_webgl_settings", "Apply WebGL release PlayerSettings for Chien Binh Tri Thuc")]
        public static string ApplyWebGLReleaseSettings()
        {
            DisableCloudAndDiagnosticsSettings();

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
                $"  codeOptimization={UnityEditor.WebGL.UserBuildSettings.codeOptimization}\n" +
                $"  engineDiagnosticsEnabled={EngineDiagnosticsSettings.enabled}";

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
            CleanStaleWebGLArtifacts(fullOutput);

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

        /// <summary>
        /// Tắt Unity Cloud Project binding, Engine Diagnostics (Insights), CrashReporting và Analytics
        /// để quá trình build WebGL không gọi Token Exchange lên services.unity.com.
        /// </summary>
        private static void DisableCloudAndDiagnosticsSettings()
        {
            EngineDiagnosticsSettings.enabled = false;
            AnalyticsSettings.enabled = false;
            AnalyticsSettings.initializeOnStartup = false;
            CrashReportingSettings.enabled = false;
            CrashReportingSettings.captureEditorExceptions = false;

            ApplySerializedProperties(
                "ProjectSettings/ProjectSettings.asset",
                ("cloudProjectId", string.Empty),
                ("organizationId", string.Empty),
                ("cloudEnabled", false),
                ("submitAnalytics", false));

            ApplySerializedProperties(
                "ProjectSettings/UnityConnectSettings.asset",
                ("m_Enabled", false),
                ("InsightsSettings.m_EngineDiagnosticsEnabled", false),
                ("CrashReportingSettings.m_CaptureEditorExceptions", false),
                ("UnityAnalyticsSettings.m_Enabled", false),
                ("UnityAnalyticsSettings.m_InitializeOnStartup", false),
                ("UnityAdsSettings.m_Enabled", false),
                ("UnityAdsSettings.m_InitializeOnStartup", false));
        }

        private static void ApplySerializedProperties(string assetPath, params (string Path, object Value)[] updates)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath(assetPath).FirstOrDefault();
            if (asset == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(asset);
            bool changed = false;

            foreach ((string path, object value) in updates)
            {
                SerializedProperty property = serializedObject.FindProperty(path);
                if (property == null)
                {
                    continue;
                }

                if (value is bool boolValue && property.propertyType == SerializedPropertyType.Boolean)
                {
                    if (property.boolValue != boolValue)
                    {
                        property.boolValue = boolValue;
                        changed = true;
                    }
                }
                else if (value is string stringValue && property.propertyType == SerializedPropertyType.String)
                {
                    if (!string.Equals(property.stringValue, stringValue, StringComparison.Ordinal))
                    {
                        property.stringValue = stringValue;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CleanStaleWebGLArtifacts(string fullOutput)
        {
            string buildDir = Path.Combine(fullOutput, "Build");
            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
            }

            string templateDir = Path.Combine(fullOutput, "TemplateData");
            if (Directory.Exists(templateDir))
            {
                Directory.Delete(templateDir, true);
            }

            string indexPath = Path.Combine(fullOutput, "index.html");
            if (File.Exists(indexPath))
            {
                File.Delete(indexPath);
            }
        }

        private static void HookPackageCleanupPoll()
        {
            if (_isPollingPackageCleanup)
            {
                return;
            }

            EditorApplication.update += PollPackageCleanup;
            _isPollingPackageCleanup = true;
        }

        private static void UnhookPackageCleanupPoll()
        {
            if (!_isPollingPackageCleanup)
            {
                return;
            }

            EditorApplication.update -= PollPackageCleanup;
            _isPollingPackageCleanup = false;
        }

        private static void PollPackageCleanup()
        {
            if (_activeListRequest != null)
            {
                if (!_activeListRequest.IsCompleted)
                {
                    return;
                }

                ListRequest completedList = _activeListRequest;
                _activeListRequest = null;

                if (completedList.Status != StatusCode.Success)
                {
                    UnhookPackageCleanupPoll();
                    Debug.LogError($"[MedievalWebGLBuilder] Failed to list packages: {completedList.Error?.message}");
                    return;
                }

                HashSet<string> installed = new HashSet<string>(
                    completedList.Result.Select(p => p.name),
                    StringComparer.Ordinal);
                _pendingRemovePackages = UnusedCloudAndHeavyPackages.Where(installed.Contains).ToArray();

                if (_pendingRemovePackages.Length == 0)
                {
                    UnhookPackageCleanupPoll();
                    Debug.Log("[MedievalWebGLBuilder] OK: No unused Cloud/AI/Analytics packages remain. Cloud diagnostics disabled.");
                    return;
                }

                _activeRemoveRequest = Client.AddAndRemove(Array.Empty<string>(), _pendingRemovePackages);
                return;
            }

            if (_activeRemoveRequest != null)
            {
                if (!_activeRemoveRequest.IsCompleted)
                {
                    return;
                }

                AddAndRemoveRequest completedRemove = _activeRemoveRequest;
                string[] removedNames = _pendingRemovePackages ?? Array.Empty<string>();
                _activeRemoveRequest = null;
                _pendingRemovePackages = null;
                UnhookPackageCleanupPoll();

                if (completedRemove.Status != StatusCode.Success)
                {
                    Debug.LogError($"[MedievalWebGLBuilder] Failed to remove packages: {completedRemove.Error?.message}");
                    return;
                }

                DisableCloudAndDiagnosticsSettings();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[MedievalWebGLBuilder] OK: Removed unused packages ({string.Join(", ", removedNames)}) and disabled Cloud/Diagnostics. " +
                    "Please restart Unity Editor once, then click 'MedievalRPG > WebGL > 2. Build WebGL Release (Builds/WebGL)'.");
                return;
            }

            UnhookPackageCleanupPoll();
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
