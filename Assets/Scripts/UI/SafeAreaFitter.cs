using UnityEngine;

namespace VirtualPet.UI
{
    /// <summary>
    /// Keeps a full-screen RectTransform inside <see cref="Screen.safeArea"/>
    /// (notch, Dynamic Island and home indicator).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;

        void Awake() => rect = (RectTransform)transform;

        void OnEnable() => Apply(force: true);

        void Update() => Apply(force: false);

        void Apply(bool force)
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize) return;
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            rect.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            rect.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
