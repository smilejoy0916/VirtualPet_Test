using System;
using UnityEngine;

namespace VirtualPet.MiniGames.Sdk
{
    /// <summary>
    /// Base class for every mini-game. The code ships inside the app binary; the prefab
    /// that carries this component (plus its art and tuning data) is delivered remotely
    /// through Addressables. The host instantiates the prefab, calls <see cref="Begin"/>
    /// and waits for <see cref="Finished"/>.
    /// </summary>
    public abstract class MiniGameBehaviour : MonoBehaviour
    {
        bool finished;

        public event Action<MiniGameResult> Finished;

        protected MiniGameSession Session { get; private set; }

        public void Begin(MiniGameSession session)
        {
            Session = session;
            finished = false;
            OnBegin();
        }

        /// <summary>Build UI and start the game. The root RectTransform already fills the host's game area.</summary>
        protected abstract void OnBegin();

        /// <summary>Report the final score. Only the first call per session is delivered.</summary>
        protected void Finish(int score)
        {
            if (finished) return;
            finished = true;
            Finished?.Invoke(new MiniGameResult(Mathf.Max(0, score)));
        }
    }
}
