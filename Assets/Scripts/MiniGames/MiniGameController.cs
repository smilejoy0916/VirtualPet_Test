using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VirtualPet.Economy;
using VirtualPet.MiniGames.Sdk;
using VirtualPet.Navigation;
using VirtualPet.UI;

namespace VirtualPet.MiniGames
{
    /// <summary>
    /// Runs one mini-game session end to end:
    /// open overlay → download/instantiate content (with retry) → play → score → coins → close.
    /// Any screen can start a session through <see cref="Launch"/> (Mini-games list, Feed "Play Now").
    /// </summary>
    public sealed class MiniGameController
    {
        readonly List<MiniGameDefinition> definitions;
        readonly MiniGameContentService content;
        readonly RewardService rewards;
        readonly NavigationService navigation;
        readonly MiniGameOverlayView overlay;

        public event Action<MiniGameDefinition> SessionEnded;

        public bool IsRunning { get; private set; }

        public IReadOnlyList<MiniGameDefinition> Definitions => definitions;

        public MiniGameController(List<MiniGameDefinition> definitions, MiniGameContentService content,
            RewardService rewards, NavigationService navigation, MiniGameOverlayView overlay)
        {
            this.definitions = definitions;
            this.content = content;
            this.rewards = rewards;
            this.navigation = navigation;
            this.overlay = overlay;
        }

        public MiniGameDefinition Find(string id) => definitions.Find(d => d.id == id);

        public void Launch(string miniGameId)
        {
            var definition = Find(miniGameId);
            if (definition == null)
            {
                Debug.LogWarning($"[MiniGames] Unknown mini-game '{miniGameId}'");
                return;
            }

            if (IsRunning) return;
            _ = RunSessionAsync(definition);
        }

        async Task RunSessionAsync(MiniGameDefinition definition)
        {
            IsRunning = true;
            var closed = new TaskCompletionSource<bool>();
            void OnCloseRequested() => closed.TrySetResult(true);

            overlay.CloseRequested += OnCloseRequested;
            navigation.PushModal();
            overlay.Open(definition.displayName);

            MiniGameBehaviour game = null;
            try
            {
                game = await LoadGameAsync(definition, closed.Task);
                if (game == null) return;

                overlay.ShowGame();
                var finished = new TaskCompletionSource<MiniGameResult>();
                game.Finished += result => finished.TrySetResult(result);
                game.Begin(new MiniGameSession(definition.id, UITheme.Font));

                if (await Task.WhenAny(finished.Task, closed.Task) != finished.Task)
                {
                    Debug.Log($"[MiniGames] '{definition.id}' exited early, no reward");
                    return;
                }

                var score = finished.Task.Result.Score;
                var coins = rewards.GrantMiniGameReward(definition.id, score, definition.coinsPerPoint,
                    definition.maxCoinsPerSession);

                content.ReleaseInstance(game.gameObject);
                game = null;

                overlay.ShowResult(score, coins);
                await closed.Task;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (game != null) content.ReleaseInstance(game.gameObject);
                overlay.CloseRequested -= OnCloseRequested;
                overlay.Close();
                navigation.PopModal();
                IsRunning = false;
                SessionEnded?.Invoke(definition);
            }
        }

        /// <summary>Returns the started game, or null if the player closed the overlay first.</summary>
        async Task<MiniGameBehaviour> LoadGameAsync(MiniGameDefinition definition, Task closed)
        {
            while (!closed.IsCompleted)
            {
                overlay.ShowLoading("Checking content…", -1f);
                var load = content.LoadAsync(definition.contentKey, overlay.GameContainer,
                    (progress, bytes) =>
                    {
                        // An abandoned download keeps reporting; don't let it repaint a later session.
                        if (!closed.IsCompleted)
                            overlay.ShowLoading($"Downloading {FormatBytes(bytes)}…  {progress:P0}", progress);
                    });

                if (await Task.WhenAny(load, closed) != load)
                {
                    // Let the download finish in the background (it lands in the cache) and drop the instance.
                    _ = load.ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion) content.ReleaseInstance(t.Result.Instance);
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                    return null;
                }

                var result = load.Result;
                if (result.Succeeded)
                {
                    var game = result.Instance.GetComponent<MiniGameBehaviour>();
                    if (game != null)
                    {
                        if (result.Instance.transform is RectTransform rect) rect.Stretch();
                        Debug.Log($"[MiniGames] '{definition.id}' ready ({(result.Downloaded ? $"downloaded {result.DownloadedBytes} bytes" : "from cache")})");
                        return game;
                    }

                    content.ReleaseInstance(result.Instance);
                    result.Error = "Downloaded content does not contain a mini-game.";
                }

                var retry = new TaskCompletionSource<bool>();
                overlay.ShowError(result.Error, () => retry.TrySetResult(true));
                await Task.WhenAny(retry.Task, closed);
            }

            return null;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:0.#} KB";
            return $"{bytes / (1024f * 1024f):0.##} MB";
        }
    }
}
