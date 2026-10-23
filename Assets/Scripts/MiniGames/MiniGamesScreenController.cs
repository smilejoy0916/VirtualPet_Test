using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualPet.Core;
using VirtualPet.Navigation;
using VirtualPet.UI;
using AppContext = VirtualPet.Core.AppContext;

namespace VirtualPet.MiniGames
{
    /// <summary>Mini-games tab: lists available games with their download/cache status.</summary>
    public sealed class MiniGamesScreenController : ScreenBase
    {
        sealed class Card
        {
            public MiniGameDefinition Definition;
            public Text Status;
            public Button Play;
            public Button ClearCache;
            public int RefreshToken;
        }

        readonly List<Card> cards = new();
        AppContext context;

        public void Initialize(AppContext appContext)
        {
            context = appContext;

            var background = UIFactory.CreateImage("Background", transform, UITheme.Background);
            background.rectTransform.Stretch(0, -400, 0, 0);

            var header = UIFactory.CreateRect("Header", transform).Strip(top: true, 150);
            UIFactory.CreateText("Title", header, "Mini-games", UITheme.TitleSize, UITheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Stretch(48, 0, 340, 0);
            var badge = CoinBadge.Create(header, context.Wallet);
            ((RectTransform)badge.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0),
                new Vector2(280, 96));

            UIFactory.CreateText("Subtitle", transform,
                    $"Game content is downloaded on first play and cached on this device.\nHost: {RemoteContentSettings.BaseUrl}",
                    UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.UpperLeft).rectTransform
                .Strip(top: true, 110, 160, 48);

            var list = UIFactory.CreateRect("List", transform).Stretch(40, 300, 40, 40);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 32;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            foreach (var definition in context.MiniGames.Definitions) cards.Add(CreateCard(list, definition));

            context.MiniGames.SessionEnded += OnSessionEnded;
        }

        void OnDestroy()
        {
            if (context != null) context.MiniGames.SessionEnded -= OnSessionEnded;
        }

        protected override void OnShow() => RefreshAll();

        void OnSessionEnded(MiniGameDefinition _)
        {
            if (IsShown) RefreshAll();
        }

        Card CreateCard(Transform parent, MiniGameDefinition definition)
        {
            var panel = UIFactory.CreatePanel(definition.id, parent, UITheme.Surface);
            panel.rectTransform.sizeDelta = new Vector2(0, 460);
            var card = new Card { Definition = definition };

            var art = UIFactory.CreatePanel("Art", panel.transform, definition.accentColor);
            art.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -36), new Vector2(200, 200));
            UIFactory.CreateText("Badge", art.transform, definition.badge, 54, UITheme.TextOnDark,
                TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();

            var title = UIFactory.CreateText("Name", panel.transform, definition.displayName, UITheme.HeadingSize,
                UITheme.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
            InsetHorizontally(title.rectTransform.Strip(top: true, 64, 40), 270, 36);
            var description = UIFactory.CreateText("Description", panel.transform, definition.description,
                UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.UpperLeft);
            InsetHorizontally(description.rectTransform.Strip(top: true, 130, 110), 270, 36);

            card.Status = UIFactory.CreateText("Status", panel.transform, "", UITheme.CaptionSize, UITheme.TextSecondary,
                TextAnchor.MiddleLeft, FontStyle.Italic);
            card.Status.rectTransform.Strip(top: false, 50, 150, 36);

            card.Play = UIFactory.CreateButton("Play", panel.transform, "Play", UITheme.Primary, UITheme.TextOnDark,
                UITheme.BodySize, () => context.MiniGames.Launch(definition.id));
            ((RectTransform)card.Play.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 30),
                new Vector2(420, 110));

            card.ClearCache = UIFactory.CreateButton("ClearCache", panel.transform, "Clear cache",
                new Color(0, 0, 0, 0.06f), UITheme.TextSecondary, UITheme.CaptionSize, () => ClearCache(card));
            ((RectTransform)card.ClearCache.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(36, 30),
                new Vector2(300, 110));

            return card;
        }

        static void InsetHorizontally(RectTransform rect, float left, float right)
        {
            rect.offsetMin = new Vector2(left, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-right, rect.offsetMax.y);
        }

        void RefreshAll()
        {
            foreach (var card in cards) RefreshCard(card);
        }

        async void RefreshCard(Card card)
        {
            var token = ++card.RefreshToken;
            card.Status.text = "Checking content…";
            try
            {
                var size = await context.Content.GetDownloadSizeAsync(card.Definition.contentKey);
                if (this == null || token != card.RefreshToken) return;

                if (size < 0)
                {
                    card.Status.text = "Content catalog unavailable";
                    card.Play.SetLabel("Retry");
                }
                else if (size == 0)
                {
                    card.Status.text = "Cached on device • ready offline";
                    card.Play.SetLabel("Play");
                }
                else
                {
                    card.Status.text = $"Download required • {MiniGameController.FormatBytes(size)}";
                    card.Play.SetLabel("Download & Play");
                }

                card.ClearCache.interactable = size == 0;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (this != null) card.Status.text = "Could not check content";
            }
        }

        async void ClearCache(Card card)
        {
            if (context.MiniGames.IsRunning) return;
            card.Status.text = "Clearing cache…";
            try
            {
                await context.Content.ClearCacheAsync(card.Definition.contentKey);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (this != null) RefreshCard(card);
        }
    }
}
