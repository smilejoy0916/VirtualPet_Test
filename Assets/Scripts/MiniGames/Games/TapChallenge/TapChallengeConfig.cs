using UnityEngine;

namespace VirtualPet.MiniGames.TapChallenge
{
    /// <summary>
    /// Remotely delivered tuning + art for Tap Challenge. Lives in the same Addressables
    /// bundle as the prefab, so changing it is a content update, not an app update.
    /// </summary>
    [CreateAssetMenu(menuName = "VirtualPet/Mini-games/Tap Challenge Config", fileName = "TapChallengeConfig")]
    public sealed class TapChallengeConfig : ScriptableObject
    {
        [Header("Content")]
        public string contentVersion = "1.0.0";
        public string title = "Tap Challenge";
        [TextArea] public string instructions = "Tap the paw as many times as you can before the timer runs out!";

        [Header("Rules")]
        [Min(1f)] public float roundSeconds = 10f;
        [Min(1)] public int pointsPerTap = 1;
        public bool moveTargetOnTap = true;
        [Min(80f)] public float targetSize = 300f;

        [Header("Art")]
        public Sprite backgroundSprite;
        public Sprite targetSprite;
        public Sprite buttonSprite;
        public Color backgroundTint = Color.white;
        public Color targetColor = new(1f, 0.55f, 0.3f);
        public Color accentColor = new(0.47f, 0.40f, 0.95f);
        public Color textColor = Color.white;
    }
}
