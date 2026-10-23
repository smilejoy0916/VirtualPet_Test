using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VirtualPet.Economy;
using VirtualPet.Feed;
using VirtualPet.Home;
using VirtualPet.MiniGames;
using VirtualPet.Navigation;
using VirtualPet.UI;

namespace VirtualPet.Core
{
    /// <summary>
    /// Composition root and the only object in Bootstrap.unity. Creates services, builds
    /// the UI shell and the three screens, wires them together, then shows Home.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] AppConfig config;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // Native decoder warnings would otherwise pop up Unity's on-screen console in simulator builds.
            Debug.developerConsoleEnabled = false;

            if (config == null)
            {
                Debug.LogWarning("[Bootstrap] No AppConfig assigned, using built-in defaults.");
                config = AppConfig.CreateDefault();
            }

            // Must happen before Addressables initializes: it resolves RemoteLoadPath from this value.
            RemoteContentSettings.BaseUrl = config.remoteContentBaseUrl;

            // Services
            var wallet = new CoinWallet(new LocalSaveService(), config.startingCoins);
            var rewards = new RewardService(wallet);
            var navigation = new NavigationService();
            var content = new MiniGameContentService();

            // UI shell
            EnsureEventSystem();
            var canvas = CreateCanvas();
            var bottomFill = UIFactory.CreateImage("BottomFill", canvas.transform, UITheme.Surface);
            bottomFill.rectTransform.Strip(top: false, TabBar.Height);
            var safeArea = UIFactory.CreateRect("SafeArea", canvas.transform).Stretch();
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var screens = UIFactory.CreateRect("Screens", safeArea).Stretch(0, 0, 0, TabBar.Height);
            TabBar.Create(safeArea, navigation);
            var overlay = MiniGameOverlayView.Create(canvas.transform, wallet);

            var miniGames = new MiniGameController(config.miniGames, content, rewards, navigation, overlay);
            var context = new AppContext(config, wallet, navigation, content, miniGames);

            // Screens
            var home = UIFactory.CreateScreen<HomeController>("Home", screens);
            home.Initialize(context, PetStage.Create(config));
            navigation.Register(ScreenId.Home, home);

            var miniGamesScreen = UIFactory.CreateScreen<MiniGamesScreenController>("MiniGames", screens);
            miniGamesScreen.Initialize(context);
            navigation.Register(ScreenId.MiniGames, miniGamesScreen);

            var feed = UIFactory.CreateScreen<FeedController>("Feed", screens);
            feed.Initialize(context);
            navigation.Register(ScreenId.Feed, feed);

            navigation.Show(ScreenId.Home);

            // Start Addressables + remote catalog check in the background.
            _ = content.InitializeAsync(config.checkForContentUpdatesOnLaunch);
        }

        static Canvas CreateCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform)) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
