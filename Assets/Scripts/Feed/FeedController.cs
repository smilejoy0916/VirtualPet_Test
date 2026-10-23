using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualPet.Core;
using VirtualPet.Navigation;
using VirtualPet.UI;

namespace VirtualPet.Feed
{
    /// <summary>
    /// Vertical short-video feed. Only the current video plays; its neighbours are kept
    /// prepared (preloaded and paused) and every other video is fully released, so at most
    /// three decoders and RenderTextures exist at any time.
    /// </summary>
    public sealed class FeedController : ScreenBase
    {
        const float SwipeDistanceThreshold = 0.18f; // fraction of page height
        const float SwipeVelocityThreshold = 1400f; // canvas units per second
        const float SnapSmoothTime = 0.12f;
        const float EdgeResistance = 0.35f;
        const int PreloadRadius = 1;

        sealed class Page
        {
            public FeedVideoEntry Entry;
            public RectTransform Rect;
            public VideoPlayerController Player;
        }

        readonly List<Page> pages = new();
        readonly List<Image> dots = new();
        AppContext context;
        RectTransform viewport;

        int currentIndex;
        float scrollPosition;
        float scrollVelocity;
        float dragOffset;
        bool dragging;

        public void Initialize(AppContext appContext)
        {
            context = appContext;

            var background = UIFactory.CreateImage("Background", transform, Color.black);
            background.rectTransform.Stretch(0, -400, 0, 0);

            viewport = UIFactory.CreateImage("Viewport", transform, Color.black, raycastTarget: true).rectTransform.Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();
            var swipe = viewport.gameObject.AddComponent<FeedSwipeInput>();
            swipe.DragStarted += OnDragStarted;
            swipe.Dragged += OnDragged;
            swipe.DragEnded += OnDragEnded;

            foreach (var entry in context.Config.feedVideos) pages.Add(CreatePage(entry, pages.Count));

            BuildOverlay();

            if (pages.Count == 0)
            {
                UIFactory.CreateText("Empty", viewport, "No videos configured.", UITheme.BodySize, UITheme.TextOnDark)
                    .rectTransform.Stretch();
            }
        }

        Page CreatePage(FeedVideoEntry entry, int index)
        {
            var rect = UIFactory.CreateImage($"Page{index}", viewport, Color.black).rectTransform.Stretch();
            var page = new Page { Entry = entry, Rect = rect, Player = VideoPlayerController.Create(rect, entry.Sources()) };

            var caption = UIFactory.CreateImage("Caption", rect, new Color(0, 0, 0, 0.35f));
            caption.rectTransform.Strip(top: false, 200);
            UIFactory.CreateText("Title", caption.transform, entry.title, UITheme.HeadingSize, UITheme.TextOnDark,
                TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Stretch(40, 30, 440, 0);
            UIFactory.CreateText("Subtitle", caption.transform, entry.subtitle, UITheme.CaptionSize,
                new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft).rectTransform.Stretch(40, 100, 440, 0);
            return page;
        }

        void BuildOverlay()
        {
            var header = UIFactory.CreateRect("Header", transform).Strip(top: true, 150);
            UIFactory.CreateText("Title", header, "For You", UITheme.HeadingSize, UITheme.TextOnDark,
                TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Stretch(48, 0, 340, 0);
            var badge = CoinBadge.Create(header, context.Wallet);
            ((RectTransform)badge.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0),
                new Vector2(280, 96));

            var playNow = UIFactory.CreateButton("PlayNow", transform, "Play Now", UITheme.Primary, UITheme.TextOnDark,
                UITheme.HeadingSize, OnPlayNow, 2f);
            ((RectTransform)playNow.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 40),
                new Vector2(380, 120));

            var indicator = UIFactory.CreateRect("PageIndicator", transform);
            indicator.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0),
                new Vector2(24, pages.Count * 44));
            for (var i = 0; i < pages.Count; i++)
            {
                var dot = UIFactory.CreateImage($"Dot{i}", indicator, Color.white, UISprites.Circle);
                dot.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -i * 44),
                    new Vector2(20, 20));
                dots.Add(dot);
            }

            UpdateIndicator();
        }

        protected override void OnFocusChanged(bool hasFocus) => UpdatePlayback();

        protected override void OnHide()
        {
            dragging = false;
            foreach (var page in pages) page.Player.Release();
        }

        void OnApplicationPause(bool paused)
        {
            if (!IsShown || pages.Count == 0) return;
            if (paused) pages[currentIndex].Player.Pause();
            else UpdatePlayback();
        }

        /// <summary>Play the current video, keep neighbours prepared, release the rest.</summary>
        void UpdatePlayback()
        {
            for (var i = 0; i < pages.Count; i++)
            {
                var player = pages[i].Player;
                var distance = Mathf.Abs(i - currentIndex);

                if (i == currentIndex && HasFocus) player.Play();
                else if (distance <= PreloadRadius)
                {
                    player.Pause();
                    player.Preload();
                }
                else player.Release();
            }
        }

        void OnDragStarted()
        {
            dragging = true;
            dragOffset = 0f;
        }

        void OnDragged(float delta)
        {
            if (!dragging) return;
            var height = viewport.rect.height;
            var target = currentIndex + (dragOffset + delta) / height;
            var outOfRange = target < 0f || target > pages.Count - 1;
            dragOffset += outOfRange ? delta * EdgeResistance : delta;
        }

        void OnDragEnded(float velocity)
        {
            if (!dragging) return;
            dragging = false;

            var fraction = dragOffset / viewport.rect.height;
            var target = currentIndex;
            if (fraction > SwipeDistanceThreshold || velocity > SwipeVelocityThreshold) target++;
            else if (fraction < -SwipeDistanceThreshold || velocity < -SwipeVelocityThreshold) target--;

            dragOffset = 0f;
            SetCurrent(Mathf.Clamp(target, 0, pages.Count - 1));
        }

        void SetCurrent(int index)
        {
            if (index == currentIndex) return;
            currentIndex = index;
            UpdateIndicator();
            UpdatePlayback();
        }

        void LateUpdate()
        {
            var height = viewport.rect.height;
            if (height <= 0f) return;

            if (dragging) scrollPosition = currentIndex + dragOffset / height;
            else
                scrollPosition = Mathf.SmoothDamp(scrollPosition, currentIndex, ref scrollVelocity, SnapSmoothTime,
                    Mathf.Infinity, Time.unscaledDeltaTime);

            for (var i = 0; i < pages.Count; i++)
                pages[i].Rect.anchoredPosition = new Vector2(0f, (scrollPosition - i) * height);
        }

        void UpdateIndicator()
        {
            for (var i = 0; i < dots.Count; i++)
                dots[i].color = i == currentIndex ? Color.white : new Color(1, 1, 1, 0.35f);
        }

        void OnPlayNow()
        {
            if (pages.Count == 0) return;
            context.MiniGames.Launch(pages[currentIndex].Entry.miniGameId);
        }
    }
}
