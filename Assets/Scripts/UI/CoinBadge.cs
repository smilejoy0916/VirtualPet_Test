using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VirtualPet.Economy;

namespace VirtualPet.UI
{
    /// <summary>
    /// Reusable coin balance pill. Every screen shows one, all bound to the same
    /// <see cref="CoinWallet"/>. It re-reads the balance whenever it is enabled, so a
    /// hidden screen can never show a stale value.
    /// </summary>
    public sealed class CoinBadge : MonoBehaviour
    {
        const float CountDuration = 0.45f;

        CoinWallet wallet;
        Text label;
        RectTransform pulseTarget;
        int displayed;
        Coroutine countRoutine;

        public static CoinBadge Create(Transform parent, CoinWallet wallet)
        {
            var background = UIFactory.CreatePanel("CoinBadge", parent, new Color(0f, 0f, 0f, 0.55f), 2f);
            var rect = background.rectTransform;
            rect.sizeDelta = new Vector2(280, 96);

            var coin = UIFactory.CreateImage("Coin", rect, UITheme.Coin, UISprites.Circle);
            coin.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(64, 64));
            var inner = UIFactory.CreateImage("Inner", coin.transform, UITheme.CoinDark, UISprites.Circle);
            inner.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));

            var badge = rect.gameObject.AddComponent<CoinBadge>();
            badge.pulseTarget = rect;
            badge.label = UIFactory.CreateText("Amount", rect, "0", 44, UITheme.TextOnDark, TextAnchor.MiddleRight,
                FontStyle.Bold);
            badge.label.rectTransform.Stretch(90, 0, 26, 0);
            badge.Bind(wallet);
            return badge;
        }

        public void Bind(CoinWallet coinWallet)
        {
            Unsubscribe();
            wallet = coinWallet;
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            Unsubscribe();
            countRoutine = null;
            pulseTarget.localScale = Vector3.one;
        }

        void Subscribe()
        {
            if (wallet == null) return;
            wallet.BalanceChanged -= OnBalanceChanged;
            wallet.BalanceChanged += OnBalanceChanged;
            SetDisplayed(wallet.Balance);
        }

        void Unsubscribe()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
        }

        void OnBalanceChanged(int balance, int delta)
        {
            if (!isActiveAndEnabled)
            {
                SetDisplayed(balance);
                return;
            }

            if (countRoutine != null) StopCoroutine(countRoutine);
            countRoutine = StartCoroutine(AnimateTo(balance));
        }

        IEnumerator AnimateTo(int target)
        {
            var start = displayed;
            for (var t = 0f; t < CountDuration; t += Time.unscaledDeltaTime)
            {
                var k = t / CountDuration;
                SetDisplayed(Mathf.RoundToInt(Mathf.Lerp(start, target, k)));
                pulseTarget.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }

            SetDisplayed(target);
            pulseTarget.localScale = Vector3.one;
            countRoutine = null;
        }

        void SetDisplayed(int value)
        {
            displayed = value;
            label.text = value.ToString("N0");
        }
    }
}
