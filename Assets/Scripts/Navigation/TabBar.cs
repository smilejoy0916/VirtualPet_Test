using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualPet.UI;

namespace VirtualPet.Navigation
{
    /// <summary>Bottom navigation bar; highlights the tab matching the current screen.</summary>
    public sealed class TabBar : MonoBehaviour
    {
        public const float Height = 170f;

        sealed class Tab
        {
            public ScreenId Id;
            public Text Label;
            public Image Indicator;
        }

        readonly List<Tab> tabs = new();
        NavigationService navigation;

        public static TabBar Create(Transform parent, NavigationService navigation)
        {
            var background = UIFactory.CreateImage("TabBar", parent, UITheme.Surface, raycastTarget: true);
            background.rectTransform.Strip(top: false, Height);

            var divider = UIFactory.CreateImage("Divider", background.transform, new Color(0, 0, 0, 0.08f));
            divider.rectTransform.Strip(top: true, 2);

            var row = UIFactory.CreateRect("Tabs", background.transform).Stretch();
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var bar = background.gameObject.AddComponent<TabBar>();
            bar.navigation = navigation;
            bar.AddTab(row, ScreenId.Home, "Home");
            bar.AddTab(row, ScreenId.MiniGames, "Mini-games");
            bar.AddTab(row, ScreenId.Feed, "Feed");

            navigation.ScreenChanged += bar.Highlight;
            return bar;
        }

        void OnDestroy()
        {
            if (navigation != null) navigation.ScreenChanged -= Highlight;
        }

        void AddTab(Transform row, ScreenId id, string title)
        {
            var hit = UIFactory.CreateImage(title, row, Color.clear, raycastTarget: true);
            var button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => navigation.Show(id));

            var indicator = UIFactory.CreatePanel("Indicator", hit.transform, UITheme.Primary, 2f);
            indicator.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14),
                new Vector2(110, 12));

            var label = UIFactory.CreateText("Label", hit.transform, title, 38, UITheme.TabInactive,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Stretch(0, 20, 0, 0);

            tabs.Add(new Tab { Id = id, Label = label, Indicator = indicator });
        }

        void Highlight(ScreenId current)
        {
            foreach (var tab in tabs)
            {
                var selected = tab.Id == current;
                tab.Label.color = selected ? UITheme.Primary : UITheme.TabInactive;
                tab.Indicator.enabled = selected;
            }
        }
    }
}
