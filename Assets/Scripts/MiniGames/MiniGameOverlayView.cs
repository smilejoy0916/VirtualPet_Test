using System;
using UnityEngine;
using UnityEngine.UI;
using VirtualPet.Economy;
using VirtualPet.UI;

namespace VirtualPet.MiniGames
{
    /// <summary>
    /// Full-screen host chrome around a running mini-game: loading/progress, error + retry,
    /// the game area itself, and the result screen. Always offers an Exit button.
    /// </summary>
    public sealed class MiniGameOverlayView : MonoBehaviour
    {
        public event Action CloseRequested;

        public RectTransform GameContainer { get; private set; }

        Text titleText;
        Button closeButton;

        GameObject loadingPanel;
        Text loadingText;
        RectTransform progressTrack;
        RectTransform progressFill;

        GameObject errorPanel;
        Text errorText;
        Action retryAction;

        GameObject resultPanel;
        Text resultScoreText;
        Text resultCoinsText;

        public static MiniGameOverlayView Create(Transform canvasRoot, CoinWallet wallet)
        {
            var scrim = UIFactory.CreateImage("MiniGameOverlay", canvasRoot, UITheme.Scrim, raycastTarget: true);
            scrim.rectTransform.Stretch();
            var view = scrim.gameObject.AddComponent<MiniGameOverlayView>();

            var safe = UIFactory.CreateRect("SafeArea", scrim.transform).Stretch();
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var header = UIFactory.CreateRect("Header", safe).Strip(top: true, 140);
            view.closeButton = UIFactory.CreateButton("Exit", header, "Exit", new Color(1, 1, 1, 0.16f),
                UITheme.TextOnDark, 38, () => view.CloseRequested?.Invoke(), 2f);
            ((RectTransform)view.closeButton.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(32, 0), new Vector2(190, 96));

            view.titleText = UIFactory.CreateText("Title", header, "", UITheme.HeadingSize, UITheme.TextOnDark,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            view.titleText.rectTransform.Stretch(230, 0, 330, 0);

            var badge = CoinBadge.Create(header, wallet);
            ((RectTransform)badge.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-32, 0),
                new Vector2(280, 96));

            var body = UIFactory.CreateRect("Body", safe).Stretch(0, 150, 0, 0);
            view.GameContainer = UIFactory.CreateRect("GameContainer", body).Stretch(24, 0, 24, 24);
            view.GameContainer.gameObject.AddComponent<RectMask2D>();

            view.BuildLoadingPanel(body);
            view.BuildErrorPanel(body);
            view.BuildResultPanel(body);

            scrim.gameObject.SetActive(false);
            return view;
        }

        void BuildLoadingPanel(Transform parent)
        {
            var panel = UIFactory.CreateRect("Loading", parent).Stretch();
            loadingPanel = panel.gameObject;

            var spinner = LoadingSpinner.Create(panel, 140, UITheme.Primary);
            ((RectTransform)spinner.transform).anchoredPosition = new Vector2(0, 160);

            loadingText = UIFactory.CreateText("Status", panel, "", UITheme.BodySize, UITheme.TextOnDark);
            loadingText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10),
                new Vector2(900, 140));

            progressTrack = UIFactory.CreatePanel("ProgressTrack", panel, new Color(1, 1, 1, 0.15f), 3f).rectTransform;
            progressTrack.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -130),
                new Vector2(760, 28));
            progressFill = UIFactory.CreatePanel("Fill", progressTrack, UITheme.Primary, 3f).rectTransform;
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        }

        void BuildErrorPanel(Transform parent)
        {
            var panel = UIFactory.CreateRect("Error", parent).Stretch();
            errorPanel = panel.gameObject;

            var icon = UIFactory.CreateImage("Icon", panel, UITheme.Danger, UISprites.Circle);
            icon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260),
                new Vector2(150, 150));
            UIFactory.CreateText("Mark", icon.transform, "!", 96, UITheme.TextOnDark, TextAnchor.MiddleCenter,
                FontStyle.Bold).rectTransform.Stretch();

            errorText = UIFactory.CreateText("Message", panel, "", UITheme.CaptionSize + 4, UITheme.TextOnDark);
            errorText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40),
                new Vector2(900, 260));

            var retry = UIFactory.CreateButton("Retry", panel, "Retry", UITheme.Primary, UITheme.TextOnDark,
                UITheme.HeadingSize, () => retryAction?.Invoke());
            ((RectTransform)retry.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -200), new Vector2(480, 130));
        }

        void BuildResultPanel(Transform parent)
        {
            var card = UIFactory.CreatePanel("Result", parent, UITheme.Surface, 1.5f);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60),
                new Vector2(880, 820));
            resultPanel = card.gameObject;

            UIFactory.CreateText("Heading", card.transform, "Round complete!", UITheme.TitleSize, UITheme.TextPrimary,
                    TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(800, 100));

            resultScoreText = UIFactory.CreateText("Score", card.transform, "", 110, UITheme.Secondary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            resultScoreText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110),
                new Vector2(800, 160));

            resultCoinsText = UIFactory.CreateText("Coins", card.transform, "", UITheme.HeadingSize, UITheme.CoinDark,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            resultCoinsText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40),
                new Vector2(800, 100));

            var collect = UIFactory.CreateButton("Collect", card.transform, "Collect & Exit", UITheme.Primary,
                UITheme.TextOnDark, UITheme.HeadingSize, () => CloseRequested?.Invoke());
            ((RectTransform)collect.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70),
                new Vector2(620, 140));
        }

        public void Open(string title)
        {
            titleText.text = title;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            ShowLoading("Preparing…", -1f);
        }

        /// <param name="progress">0..1, or negative to hide the progress bar.</param>
        public void ShowLoading(string status, float progress)
        {
            Show(loadingPanel);
            loadingText.text = status;
            progressTrack.gameObject.SetActive(progress >= 0f);
            if (progress >= 0f) progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        public void ShowError(string message, Action onRetry)
        {
            Show(errorPanel);
            errorText.text = message;
            retryAction = onRetry;
        }

        public void ShowGame() => Show(GameContainer.gameObject);

        public void ShowResult(int score, int coins)
        {
            Show(resultPanel);
            resultScoreText.text = $"{score} pts";
            resultCoinsText.text = coins > 0 ? $"+{coins} coins" : "No coins this time";
        }

        public void Close()
        {
            retryAction = null;
            gameObject.SetActive(false);
        }

        void Show(GameObject panel)
        {
            loadingPanel.SetActive(panel == loadingPanel);
            errorPanel.SetActive(panel == errorPanel);
            resultPanel.SetActive(panel == resultPanel);
            GameContainer.gameObject.SetActive(panel == GameContainer.gameObject);
        }
    }
}
