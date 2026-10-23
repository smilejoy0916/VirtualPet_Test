using System;
using System.Collections.Generic;

namespace VirtualPet.Navigation
{
    /// <summary>
    /// Owns which tab screen is visible and whether a full-screen modal covers it.
    /// Shared data (coins etc.) lives in services, never in screens, so switching is free.
    /// </summary>
    public sealed class NavigationService
    {
        readonly Dictionary<ScreenId, ScreenBase> screens = new();
        int modalDepth;

        public event Action<ScreenId> ScreenChanged;

        public ScreenId? Current { get; private set; }

        public bool IsModalOpen => modalDepth > 0;

        public void Register(ScreenId id, ScreenBase screen)
        {
            screens[id] = screen;
            screen.gameObject.SetActive(false);
        }

        public void Show(ScreenId id)
        {
            if (Current == id) return;
            if (!screens.TryGetValue(id, out var next)) throw new ArgumentException($"Screen {id} is not registered");

            if (Current.HasValue) screens[Current.Value].HideScreen();
            Current = id;
            next.ShowScreen();
            if (IsModalOpen) next.SetFocus(false);
            ScreenChanged?.Invoke(id);
        }

        public void PushModal()
        {
            modalDepth++;
            if (modalDepth == 1 && TryGetCurrent(out var screen)) screen.SetFocus(false);
        }

        public void PopModal()
        {
            if (modalDepth == 0) return;
            modalDepth--;
            if (modalDepth == 0 && TryGetCurrent(out var screen)) screen.SetFocus(true);
        }

        bool TryGetCurrent(out ScreenBase screen)
        {
            screen = null;
            return Current.HasValue && screens.TryGetValue(Current.Value, out screen);
        }
    }
}
