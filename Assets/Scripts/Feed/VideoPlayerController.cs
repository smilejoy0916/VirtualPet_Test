using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using VirtualPet.UI;

namespace VirtualPet.Feed
{
    /// <summary>
    /// Plays one remote video into a RenderTexture shown by a RawImage. The texture is
    /// sized to the video once prepared, and an AspectRatioFitter letterboxes it so it is
    /// never stretched. Handles preparing (preload), playback, stalls, errors with retry,
    /// and releases its decoder + texture when told to. If a source fails (e.g. a codec the
    /// device cannot decode) it moves on to the next source before showing an error.
    /// </summary>
    public sealed class VideoPlayerController : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Preparing,
            Ready,
            Playing,
            Paused,
            Error
        }

        const float PrepareTimeoutSeconds = 30f;
        const float StallSeconds = 1f;

        IReadOnlyList<string> sources;
        int sourceIndex;
        VideoPlayer player;
        RawImage image;
        AspectRatioFitter fitter;
        LoadingSpinner spinner;
        GameObject errorPanel;
        Text errorText;
        RenderTexture renderTexture;

        bool wantsPlayback;
        float prepareStartTime;
        long lastFrame;
        float lastFrameTime;

        public State Current { get; private set; }

        string CurrentUrl => sources.Count > 0 ? sources[sourceIndex] : null;

        public static VideoPlayerController Create(RectTransform page, IReadOnlyList<string> sources)
        {
            var controller = page.gameObject.AddComponent<VideoPlayerController>();
            controller.sources = sources;
            controller.Build(page);
            return controller;
        }

        void Build(RectTransform page)
        {
            image = UIFactory.CreateRect("Video", page).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.enabled = false;
            fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 9f / 16f;

            spinner = LoadingSpinner.Create(page, 120, Color.white);
            spinner.SetVisible(false);

            var error = UIFactory.CreateRect("Error", page);
            error.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 420));
            errorPanel = error.gameObject;
            errorText = UIFactory.CreateText("Message", error, "", UITheme.BodySize, UITheme.TextOnDark);
            errorText.rectTransform.Strip(top: true, 220);
            var retry = UIFactory.CreateButton("Retry", error, "Tap to retry", new Color(1, 1, 1, 0.2f),
                UITheme.TextOnDark, UITheme.BodySize, Retry, 2f);
            ((RectTransform)retry.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40),
                new Vector2(440, 120));
            errorPanel.SetActive(false);

            player = page.gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.isLooping = true;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            player.prepareCompleted += OnPrepared;
            player.errorReceived += OnError;
            player.started += OnStarted;
        }

        /// <summary>Prepares the video (downloads headers + first frames) without playing it.</summary>
        public void Preload()
        {
            if (Current == State.Idle) BeginPrepare();
        }

        public void Play()
        {
            wantsPlayback = true;
            switch (Current)
            {
                case State.Idle:
                    BeginPrepare();
                    break;
                case State.Ready:
                case State.Paused:
                    StartPlayback();
                    break;
            }
        }

        public void Pause()
        {
            wantsPlayback = false;
            if (Current != State.Playing) return;
            player.Pause();
            spinner.SetVisible(false);
            SetState(State.Paused);
        }

        /// <summary>Stops playback and frees the decoder and RenderTexture.</summary>
        public void Release()
        {
            wantsPlayback = false;
            if (Current == State.Idle) return;
            player.Stop();
            ReleaseTexture();
            spinner.SetVisible(false);
            errorPanel.SetActive(false);
            SetState(State.Idle);
        }

        public void Retry()
        {
            if (Current != State.Error) return;
            wantsPlayback = true;
            sourceIndex = 0;
            BeginPrepare();
        }

        void BeginPrepare()
        {
            if (CurrentUrl == null)
            {
                ShowError("No video source configured.");
                return;
            }

            errorPanel.SetActive(false);
            spinner.SetVisible(true);
            player.url = CurrentUrl;
            player.Prepare();
            prepareStartTime = Time.unscaledTime;
            SetState(State.Preparing);
        }

        void OnPrepared(VideoPlayer source)
        {
            if (Current != State.Preparing) return;

            var width = (int)source.width;
            var height = (int)source.height;
            if (width <= 0 || height <= 0)
            {
                OnError(source, "Video has no picture track.");
                return;
            }

            var pixelAspect = source.pixelAspectRatioDenominator > 0
                ? (float)source.pixelAspectRatioNumerator / source.pixelAspectRatioDenominator
                : 1f;
            fitter.aspectRatio = width * pixelAspect / height;
            EnsureTexture(width, height);
            image.enabled = true;
            spinner.SetVisible(false);
            SetState(State.Ready);

            if (wantsPlayback) StartPlayback();
            else if (source.canStep) source.StepForward(); // show the first frame of a preloaded video
        }

        void StartPlayback()
        {
            player.Play();
            lastFrame = player.frame;
            lastFrameTime = Time.unscaledTime;
            SetState(State.Playing);
        }

        void OnStarted(VideoPlayer source) => spinner.SetVisible(false);

        void OnError(VideoPlayer source, string message)
        {
            if (Current is State.Idle or State.Error || IsEchoOfPreviousSource(message)) return;

            Debug.LogWarning($"[Feed] Video error for {CurrentUrl}: {message}");
            var resume = wantsPlayback || Current == State.Playing;
            player.Stop();
            ReleaseTexture();

            if (sourceIndex + 1 < sources.Count)
            {
                sourceIndex++;
                Debug.Log($"[Feed] Falling back to {CurrentUrl}");
                wantsPlayback = resume;
                BeginPrepare();
                return;
            }

            ShowError("Couldn't play this video.\nCheck your connection.");
        }

        // AVFoundation can report one failure several times; drop late reports about the source we already left.
        bool IsEchoOfPreviousSource(string message)
        {
            if (sourceIndex == 0 || string.IsNullOrEmpty(message)) return false;
            return message.Contains(sources[sourceIndex - 1]) && !message.Contains(CurrentUrl);
        }

        void ShowError(string message)
        {
            wantsPlayback = false;
            spinner.SetVisible(false);
            errorText.text = message;
            errorPanel.SetActive(true);
            SetState(State.Error);
        }

        void Update()
        {
            switch (Current)
            {
                case State.Preparing when Time.unscaledTime - prepareStartTime > PrepareTimeoutSeconds:
                    OnError(player, "Timed out while preparing.");
                    break;
                case State.Playing:
                    // Show the spinner while buffering (frames not advancing).
                    if (player.frame != lastFrame)
                    {
                        lastFrame = player.frame;
                        lastFrameTime = Time.unscaledTime;
                        spinner.SetVisible(false);
                    }
                    else if (Time.unscaledTime - lastFrameTime > StallSeconds)
                    {
                        spinner.SetVisible(true);
                    }

                    break;
            }
        }

        void EnsureTexture(int width, int height)
        {
            if (renderTexture != null && renderTexture.width == width && renderTexture.height == height) return;
            ReleaseTexture();

            renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = $"Feed_{name}" };
            renderTexture.Create();
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = previous;

            player.targetTexture = renderTexture;
            image.texture = renderTexture;
        }

        void ReleaseTexture()
        {
            image.enabled = false;
            image.texture = null;
            if (player != null) player.targetTexture = null;
            if (renderTexture == null) return;
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        void SetState(State state) => Current = state;

        void OnDestroy()
        {
            if (player != null)
            {
                player.prepareCompleted -= OnPrepared;
                player.errorReceived -= OnError;
                player.started -= OnStarted;
            }

            ReleaseTexture();
        }
    }
}
