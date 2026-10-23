using UnityEngine;
using UnityEngine.UI;
using VirtualPet.Core;
using VirtualPet.Navigation;
using VirtualPet.UI;

namespace VirtualPet.Home
{
    /// <summary>Home tab: shows the 3D pet, reacts to taps, displays the shared coin balance.</summary>
    public sealed class HomeController : ScreenBase
    {
        const float BubbleSeconds = 1.6f;

        static readonly string[] Reactions =
        {
            "Wheee!", "Hehe, that tickles!", "Again! Again!", "Boing!", "I love you!", "Let's play a mini-game!"
        };

        AppContext context;
        PetStage stage;
        GameObject bubble;
        Text bubbleText;
        Text tapCounter;
        float bubbleHideTime;
        int taps;

        public void Initialize(AppContext appContext, PetStage petStage)
        {
            context = appContext;
            stage = petStage;
            stage.Pet.Tapped += OnPetTapped;

            // No full-screen background graphic here: taps must reach the 3D pet behind the UI.
            var header = UIFactory.CreateRect("Header", transform).Strip(top: true, 150);
            UIFactory.CreateText("PetName", header, context.Config.petName, UITheme.TitleSize, UITheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Stretch(48, 0, 340, 0);
            var badge = CoinBadge.Create(header, context.Wallet);
            ((RectTransform)badge.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0),
                new Vector2(280, 96));

            var bubbleImage = UIFactory.CreatePanel("SpeechBubble", transform, UITheme.Surface, 2f);
            bubbleImage.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230),
                new Vector2(760, 130));
            bubble = bubbleImage.gameObject;
            bubbleText = UIFactory.CreateText("Text", bubble.transform, "", UITheme.BodySize, UITheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            bubbleText.rectTransform.Stretch(24, 0, 24, 0);
            bubble.SetActive(false);

            tapCounter = UIFactory.CreateText("Hint", transform, "", UITheme.BodySize, UITheme.TextPrimary);
            tapCounter.rectTransform.Strip(top: false, 70, 200, 48);
            UpdateHint();

            var play = UIFactory.CreateButton("PlayMiniGames", transform, "Earn coins in Mini-games", UITheme.Secondary,
                UITheme.TextOnDark, UITheme.BodySize, () => context.Navigation.Show(ScreenId.MiniGames));
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 50),
                new Vector2(720, 120));
        }

        void OnDestroy()
        {
            if (stage != null && stage.Pet != null) stage.Pet.Tapped -= OnPetTapped;
        }

        protected override void OnShow() => stage.gameObject.SetActive(true);

        protected override void OnHide() => stage.gameObject.SetActive(false);

        void OnPetTapped()
        {
            taps++;
            stage.Pet.React();
            bubbleText.text = Reactions[Random.Range(0, Reactions.Length)];
            bubble.SetActive(true);
            bubbleHideTime = Time.unscaledTime + BubbleSeconds;
            UpdateHint();
        }

        void Update()
        {
            if (bubble.activeSelf && Time.unscaledTime > bubbleHideTime) bubble.SetActive(false);
        }

        void UpdateHint() =>
            tapCounter.text = taps == 0 ? $"Tap {context.Config.petName} to play!" : $"Pets this session: {taps}";
    }
}
