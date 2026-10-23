using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VirtualPet.MiniGames.Sdk;

namespace VirtualPet.MiniGames.TapChallenge
{
    /// <summary>
    /// Tap the target as often as possible within the round time. Score = taps x pointsPerTap.
    /// </summary>
    public sealed class TapChallengeGame : MiniGameBehaviour
    {
        [SerializeField] TapChallengeConfig config;

        RectTransform playArea;
        RectTransform target;
        GameObject startPanel;
        Text scoreText;
        Text timerText;

        float remaining;
        int score;
        bool playing;
        Coroutine punch;

        protected override void OnBegin()
        {
            if (config == null) config = ScriptableObject.CreateInstance<TapChallengeConfig>();
            remaining = config.roundSeconds;
            BuildLayout();
            ShowStartPanel();
        }

        void BuildLayout()
        {
            var root = MiniGameUI.Fill((RectTransform)transform);
            var font = Session.Font;

            var background = MiniGameUI.CreateImage("Background", root, config.backgroundSprite, config.backgroundTint);
            MiniGameUI.Fill(background.rectTransform);

            var hud = MiniGameUI.CreateRect("Hud", root);
            hud.anchorMin = new Vector2(0, 1);
            hud.anchorMax = Vector2.one;
            hud.pivot = new Vector2(0.5f, 1);
            hud.sizeDelta = new Vector2(0, 140);
            hud.anchoredPosition = Vector2.zero;

            scoreText = MiniGameUI.CreateText("Score", hud, font, "", 56, config.textColor, FontStyle.Bold);
            var scoreRect = MiniGameUI.Fill(scoreText.rectTransform, 40, 0, 0, 0);
            scoreRect.anchorMax = new Vector2(0.5f, 1);
            scoreRect.offsetMax = Vector2.zero;
            scoreText.alignment = TextAnchor.MiddleLeft;

            timerText = MiniGameUI.CreateText("Timer", hud, font, "", 56, config.textColor, FontStyle.Bold);
            var timerRect = MiniGameUI.Fill(timerText.rectTransform, 0, 0, 40, 0);
            timerRect.anchorMin = new Vector2(0.5f, 0);
            timerRect.offsetMin = Vector2.zero;
            timerText.alignment = TextAnchor.MiddleRight;

            var version = MiniGameUI.CreateText("ContentVersion", root, font, $"content v{config.contentVersion}", 26,
                new Color(config.textColor.r, config.textColor.g, config.textColor.b, 0.6f));
            MiniGameUI.Place(version.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(600, 40));

            playArea = MiniGameUI.Fill(MiniGameUI.CreateRect("PlayArea", root), 40, 160, 40, 90);

            var targetButton = MiniGameUI.CreateButton("Target", playArea, config.targetSprite, config.targetColor, OnTargetTapped);
            target = MiniGameUI.Place((RectTransform)targetButton.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.one * config.targetSize);
            var tapLabel = MiniGameUI.CreateText("Label", target, font, "TAP!", 52, config.textColor, FontStyle.Bold);
            MiniGameUI.Fill(tapLabel.rectTransform);

            startPanel = BuildStartPanel(root, font);
            UpdateHud();
        }

        GameObject BuildStartPanel(RectTransform root, Font font)
        {
            var panel = MiniGameUI.CreateImage("StartPanel", root, config.buttonSprite, new Color(0, 0, 0, 0.55f));
            MiniGameUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 760));

            var title = MiniGameUI.CreateText("Title", panel.transform, font, config.title, 76, config.textColor, FontStyle.Bold);
            MiniGameUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(780, 100));

            var info = MiniGameUI.CreateText("Instructions", panel.transform, font,
                $"{config.instructions}\n\n{config.roundSeconds:0} seconds  •  {config.pointsPerTap} pt per tap", 40,
                config.textColor);
            MiniGameUI.Place(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(760, 300));

            var start = MiniGameUI.CreateButton("StartButton", panel.transform, config.buttonSprite, config.accentColor, StartRound);
            MiniGameUI.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(520, 140));
            var startLabel = MiniGameUI.CreateText("Label", start.transform, font, "START", 56, config.textColor, FontStyle.Bold);
            MiniGameUI.Fill(startLabel.rectTransform);

            return panel.gameObject;
        }

        void ShowStartPanel()
        {
            playing = false;
            startPanel.SetActive(true);
            target.gameObject.SetActive(false);
        }

        void StartRound()
        {
            score = 0;
            remaining = config.roundSeconds;
            playing = true;
            startPanel.SetActive(false);
            target.gameObject.SetActive(true);
            target.anchoredPosition = Vector2.zero;
            UpdateHud();
        }

        void OnTargetTapped()
        {
            if (!playing) return;
            score += config.pointsPerTap;
            if (config.moveTargetOnTap) MoveTarget();
            if (punch != null) StopCoroutine(punch);
            punch = StartCoroutine(Punch());
            UpdateHud();
        }

        void Update()
        {
            if (!playing) return;

            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                remaining = 0f;
                playing = false;
                target.gameObject.SetActive(false);
                UpdateHud();
                Finish(score);
                return;
            }

            UpdateHud();
        }

        void MoveTarget()
        {
            var area = playArea.rect;
            var half = config.targetSize * 0.5f;
            var x = area.width > config.targetSize ? Random.Range(area.xMin + half, area.xMax - half) : 0f;
            var y = area.height > config.targetSize ? Random.Range(area.yMin + half, area.yMax - half) : 0f;
            target.anchoredPosition = new Vector2(x, y);
        }

        IEnumerator Punch()
        {
            const float duration = 0.12f;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * (1f - 0.15f * Mathf.Sin(t / duration * Mathf.PI));
                yield return null;
            }

            target.localScale = Vector3.one;
            punch = null;
        }

        void UpdateHud()
        {
            scoreText.text = $"Score {score}";
            timerText.text = $"{Mathf.CeilToInt(remaining)}s";
        }
    }
}
