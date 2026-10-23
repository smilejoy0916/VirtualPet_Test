using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualPet.Core;
using VirtualPet.Home;
using VirtualPet.MiniGames.TapChallenge;
using Object = UnityEngine.Object;

namespace VirtualPet.EditorTools
{
    /// <summary>
    /// One-click, idempotent project setup. Generates every asset the prototype needs
    /// (materials, pet prefab, remote mini-game content, Addressables configuration,
    /// AppConfig, Bootstrap scene, player settings). Runs automatically the first time the
    /// project is opened; re-run any time via VirtualPet ▸ Setup Project. Existing assets are
    /// kept, so hand edits survive.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        public const string AppConfigPath = "Assets/Settings/AppConfig.asset";
        public const string RemoteLoadPathValue = "{VirtualPet.Core.RemoteContentSettings.BaseUrl}/[BuildTarget]";
        public const string TapChallengeAddress = "minigames/tap-challenge";
        const string TapChallengeFolder = "Assets/AddressableAssets/MiniGames/TapChallenge";
        const string TapChallengeGroup = "Remote MiniGame - TapChallenge";
        const string BundleId = "com.virtualpet.poc";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Application.isBatchMode && !File.Exists(ScenePath)) Run();
            };
        }

        public static void EnsureSetup()
        {
            if (!File.Exists(ScenePath) || !File.Exists(AppConfigPath)) Run();
            else ConfigureAddressables(LoadTapChallengePrefab());
        }

        [MenuItem("VirtualPet/Setup Project", priority = 0)]
        public static void Run()
        {
            CreateFolders();
            ConfigurePlayerSettings();
            var palette = CreatePetMaterials(out var groundMaterial);
            var petPrefab = CreatePetPrefab(palette);
            var tapChallenge = CreateTapChallengeContent();
            ConfigureAddressables(tapChallenge);
            var config = CreateAppConfig(petPrefab, groundMaterial);
            CreateBootstrapScene(config);

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Project ready. Press Play in Bootstrap.unity, or use VirtualPet ▸ Build menu.");
        }

        static void CreateFolders()
        {
            foreach (var folder in new[]
                     {
                         "Assets/Scenes", "Assets/Prefabs", "Assets/Materials", "Assets/Settings",
                         "Assets/RenderTextures", "Assets/AddressableAssets", "Assets/AddressableAssets/MiniGames",
                         TapChallengeFolder
                     })
            {
                if (AssetDatabase.IsValidFolder(folder)) continue;
                var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "VirtualPetPoC";
            PlayerSettings.productName = "Virtual Pet PoC";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
        }

        // ---------------------------------------------------------------- Home

        static PetBuilder.Palette CreatePetMaterials(out Material ground)
        {
            var runtime = PetBuilder.CreateRuntimePalette();
            ground = SaveMaterial(MaterialFactory.CreateLit("Ground", new Color(0.55f, 0.82f, 0.50f), 0.1f));
            return new PetBuilder.Palette
            {
                Body = SaveMaterial(runtime.Body),
                Belly = SaveMaterial(runtime.Belly),
                Dark = SaveMaterial(runtime.Dark),
                Blush = SaveMaterial(runtime.Blush)
            };
        }

        static Material SaveMaterial(Material material)
        {
            var path = $"Assets/Materials/{material.name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(material);
                return existing;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject CreatePetPrefab(PetBuilder.Palette palette)
        {
            const string path = "Assets/Prefabs/Pet.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var pet = PetBuilder.Build(null, palette);
            var prefab = PrefabUtility.SaveAsPrefabAsset(pet.gameObject, path);
            Object.DestroyImmediate(pet.gameObject);
            return prefab;
        }

        // ---------------------------------------------------------------- Mini-game content

        static GameObject LoadTapChallengePrefab() =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{TapChallengeFolder}/TapChallenge.prefab");

        static GameObject CreateTapChallengeContent()
        {
            var existing = LoadTapChallengePrefab();
            if (existing != null) return existing;

            var config = ScriptableObject.CreateInstance<TapChallengeConfig>();
            config.backgroundSprite = CreateSprite($"{TapChallengeFolder}/Background.png", 720, 1280, Vector4.zero,
                PaintBackground);
            config.targetSprite = CreateSprite($"{TapChallengeFolder}/Target.png", 256, 256, Vector4.zero, PaintPaw);
            config.buttonSprite = CreateSprite($"{TapChallengeFolder}/Panel.png", 96, 96, new Vector4(32, 32, 32, 32),
                (x, y) => RoundedRect(x, y, 96, 32));
            AssetDatabase.CreateAsset(config, $"{TapChallengeFolder}/TapChallengeConfig.asset");

            var root = new GameObject("TapChallenge", typeof(RectTransform));
            var game = root.AddComponent<TapChallengeGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{TapChallengeFolder}/TapChallenge.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Sprite CreateSprite(string path, int width, int height, Vector4 border, Func<int, int, Color32> paint)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                pixels[y * width + x] = paint(x, y);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color32 PaintBackground(int x, int y)
        {
            // Vertical purple → coral gradient with soft confetti dots.
            var t = y / 1280f;
            var color = Color.Lerp(new Color(0.98f, 0.55f, 0.45f), new Color(0.40f, 0.30f, 0.85f), t);
            var cell = new Vector2Int(x / 90, y / 90);
            var hash = (((cell.x * 73856093) ^ (cell.y * 19349663)) & 0x7fffffff) % 1000;
            var center = new Vector2(cell.x * 90 + 20 + hash % 50, cell.y * 90 + 20 + hash / 20 % 50);
            var radius = 6 + hash % 9;
            var dot = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), center));
            return Color.Lerp(color, Color.white, dot * 0.35f);
        }

        static Color32 PaintPaw(int x, int y)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            var disc = Mathf.Clamp01(126f - Vector2.Distance(p, new Vector2(128, 128)));
            var paw = Mathf.Max(
                Ellipse(p, new Vector2(128, 100), 46, 38),
                Mathf.Max(Mathf.Max(Ellipse(p, new Vector2(72, 158), 18, 22), Ellipse(p, new Vector2(104, 186), 18, 23)),
                    Mathf.Max(Ellipse(p, new Vector2(152, 186), 18, 23), Ellipse(p, new Vector2(184, 158), 18, 22))));
            var shade = (byte)(255 - paw * 70);
            return new Color32(shade, shade, shade, (byte)(disc * 255));
        }

        static float Ellipse(Vector2 p, Vector2 center, float rx, float ry)
        {
            var d = new Vector2((p.x - center.x) / rx, (p.y - center.y) / ry).magnitude;
            return Mathf.Clamp01((1f - d) * Mathf.Min(rx, ry));
        }

        static Color32 RoundedRect(int x, int y, int size, int radius)
        {
            float px = x + 0.5f, py = y + 0.5f;
            var c = new Vector2(Mathf.Clamp(px, radius, size - radius), Mathf.Clamp(py, radius, size - radius));
            var alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(px, py), c) + 0.5f);
            return new Color32(255, 255, 255, (byte)(alpha * 255));
        }

        // ---------------------------------------------------------------- Addressables

        static void ConfigureAddressables(GameObject tapChallengePrefab)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            // Remote paths: built into ServerData/<target>, loaded from the runtime-resolved host.
            var profiles = settings.profileSettings;
            profiles.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath, "ServerData/[BuildTarget]");
            profiles.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPathValue);

            // Remote catalog = content can be updated after the app ships.
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.OverridePlayerVersion = "poc"; // stable catalog file name across content builds
            settings.DisableCatalogUpdateOnStartup = true; // MiniGameContentService checks explicitly
            settings.CatalogRequestsTimeout = 10; // seconds; never hang on "Checking content…"
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

            var group = settings.FindGroup(TapChallengeGroup) ?? settings.CreateGroup(TapChallengeGroup, false, false,
                false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bundled.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            bundled.UseAssetBundleCache = true;
            bundled.UseAssetBundleCrc = true;
            bundled.RetryCount = 2;
            bundled.Timeout = 30;

            group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;

            if (tapChallengePrefab != null)
            {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tapChallengePrefab));
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = TapChallengeAddress;
                entry.SetLabel("minigame", true, true, false);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- App config + scene

        static AppConfig CreateAppConfig(GameObject petPrefab, Material groundMaterial)
        {
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(AppConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AppConfig>();
                config.ApplyDefaults();
                AssetDatabase.CreateAsset(config, AppConfigPath);
            }

            if (config.petPrefab == null) config.petPrefab = petPrefab;
            if (config.groundMaterial == null) config.groundMaterial = groundMaterial;
            EditorUtility.SetDirty(config);
            return config;
        }

        static void CreateBootstrapScene(AppConfig config)
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var bootstrap = new GameObject("AppBootstrap").AddComponent<AppBootstrap>();
                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("config").objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else if (SceneManager.GetActiveScene().path != ScenePath && !Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
