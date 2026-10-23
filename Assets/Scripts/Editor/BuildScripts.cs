using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using VirtualPet.Core;

namespace VirtualPet.EditorTools
{
    /// <summary>
    /// Menu + command-line entry points for building remote content and the iOS Simulator
    /// Xcode project. See Tools/build_ios_sim.sh.
    /// </summary>
    public static class BuildScripts
    {
        public const string IosOutputPath = "Builds/iOS-Simulator";

        /// <summary>Full content build: bundles + catalog into ServerData/[BuildTarget].</summary>
        [MenuItem("VirtualPet/Content/Build Remote Content (full)", priority = 20)]
        public static void BuildContent()
        {
            ProjectSetup.EnsureSetup();
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error)) throw new Exception($"Addressables build failed: {result.Error}");
            Debug.Log($"[Build] Remote content built in {result.Duration:0.0}s → ServerData/{EditorUserBuildSettings.activeBuildTarget}");
        }

        /// <summary>
        /// Content update for an app that is already installed: rebuilds changed bundles and the
        /// remote catalog against the content state saved by the last full build.
        /// </summary>
        [MenuItem("VirtualPet/Content/Build Content Update (for shipped app)", priority = 21)]
        public static void BuildContentUpdate()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var statePath = ContentUpdateScript.GetContentStateDataPath(false);
            var result = ContentUpdateScript.BuildContentUpdate(settings, statePath);
            if (result == null || !string.IsNullOrEmpty(result.Error))
                throw new Exception($"Content update failed: {result?.Error ?? "no previous content state found"}");
            Debug.Log("[Build] Content update built. Upload ServerData/<target> to the content host.");
        }

        [MenuItem("VirtualPet/Build/iOS Simulator (Xcode project)", priority = 40)]
        public static void BuildIosSimulator()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            ProjectSetup.EnsureSetup();
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            ConfigureHttpPolicy();
            BuildContent();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = IosOutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"iOS build failed: {report.summary.result}");
            Debug.Log($"[Build] Xcode project written to {IosOutputPath}");
        }

        // Command-line wrappers return a process exit code for CI / Tools scripts.
        public static void BuildIosSimulatorCli() => RunCli(BuildIosSimulator);

        public static void BuildContentCli() => RunCli(BuildContent);

        public static void BuildContentUpdateCli() => RunCli(BuildContentUpdate);

        static void RunCli(Action action)
        {
            try
            {
                action();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Plain HTTP is only allowed when the configured content host is http:// (local testing).</summary>
        static void ConfigureHttpPolicy()
        {
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(ProjectSetup.AppConfigPath);
            var usesHttp = config != null && config.remoteContentBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            PlayerSettings.insecureHttpOption = usesHttp ? InsecureHttpOption.AlwaysAllowed : InsecureHttpOption.NotAllowed;
            if (usesHttp)
                Debug.LogWarning($"[Build] Content host is plain HTTP ({config.remoteContentBaseUrl}); allowed for local testing only.");
        }
    }
}
