using UnityEngine;

namespace VirtualPet.Navigation
{
    /// <summary>
    /// Base class for tab screens. Screens are built once and kept alive; switching tabs
    /// only toggles them, so no state is lost and nothing is reloaded.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        public bool IsShown { get; private set; }

        /// <summary>False while a modal (e.g. a running mini-game) covers this screen.</summary>
        public bool HasFocus { get; private set; }

        internal void ShowScreen()
        {
            gameObject.SetActive(true);
            IsShown = true;
            OnShow();
            SetFocus(true);
        }

        internal void HideScreen()
        {
            SetFocus(false);
            IsShown = false;
            OnHide();
            gameObject.SetActive(false);
        }

        internal void SetFocus(bool focus)
        {
            if (HasFocus == focus || (focus && !IsShown)) return;
            HasFocus = focus;
            OnFocusChanged(focus);
        }

        protected virtual void OnShow() { }

        protected virtual void OnHide() { }

        protected virtual void OnFocusChanged(bool hasFocus) { }
    }
}
