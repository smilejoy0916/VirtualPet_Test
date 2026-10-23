using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VirtualPet.UI
{
    /// <summary>
    /// Small helpers for building uGUI hierarchies from code. Screens are composed in
    /// code so the layout is reviewable in diffs and easy to change during a walkthrough.
    /// </summary>
    public static class UIFactory
    {
        const int UILayer = 5;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UILayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static T CreateScreen<T>(string name, Transform parent) where T : Component
        {
            var rect = CreateRect(name, parent).Stretch();
            return rect.gameObject.AddComponent<T>();
        }

        /// <summary>Fills the parent, inset by the given margins.</summary>
        public static RectTransform Stretch(this RectTransform rect, float left = 0, float top = 0, float right = 0,
            float bottom = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Anchors to a single point with a fixed size.</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>Full-width strip anchored to the top (or bottom) edge.</summary>
        public static RectTransform Strip(this RectTransform rect, bool top, float height, float offset = 0,
            float horizontalMargin = 0)
        {
            var edge = top ? 1f : 0f;
            rect.anchorMin = new Vector2(0, edge);
            rect.anchorMax = new Vector2(1, edge);
            rect.pivot = new Vector2(0.5f, edge);
            rect.sizeDelta = new Vector2(-horizontalMargin * 2, height);
            rect.anchoredPosition = new Vector2(0, top ? -offset : offset);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null,
            bool raycastTarget = false)
        {
            var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        public static Image CreatePanel(string name, Transform parent, Color color, float cornerScale = 1f)
        {
            var image = CreateImage(name, parent, color, UISprites.RoundedRect);
            image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, cornerScale);
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = UITheme.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color background,
            Color foreground, int fontSize, UnityAction onClick, float cornerScale = 1f)
        {
            var image = CreatePanel(name, parent, background, cornerScale);
            image.raycastTarget = true;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            CreateText("Label", image.transform, label, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Stretch(16, 0, 16, 0);
            return button;
        }

        public static void SetLabel(this Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
        }
    }
}
