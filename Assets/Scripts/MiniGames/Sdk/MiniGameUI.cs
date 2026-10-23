using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VirtualPet.MiniGames.Sdk
{
    /// <summary>Tiny uGUI helpers for mini-games, which cannot reference host UI code.</summary>
    public static class MiniGameUI
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Fill(RectTransform rect, float left = 0, float top = 0, float right = 0,
            float bottom = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        public static Text CreateText(string name, Transform parent, Font font, string content, int size, Color color,
            FontStyle style = FontStyle.Normal)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Sprite sprite, Color color, UnityAction onClick)
        {
            var image = CreateImage(name, parent, sprite, color, raycast: true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            return button;
        }
    }
}
