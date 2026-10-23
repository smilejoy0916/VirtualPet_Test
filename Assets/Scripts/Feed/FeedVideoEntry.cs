using System;
using System.Collections.Generic;
using VirtualPet.Core;

namespace VirtualPet.Feed
{
    [Serializable]
    public sealed class FeedVideoEntry
    {
        public string title;
        public string subtitle;
        public string url;

        /// <summary>
        /// Mobile-safe rendition used when <see cref="url"/> cannot be decoded or loaded
        /// (see Tools/transcode_videos.sh). A relative path is resolved against the content host.
        /// </summary>
        public string fallbackUrl;

        /// <summary>Mini-game opened by this video's "Play Now" button.</summary>
        public string miniGameId = "tap-challenge";

        /// <summary>Sources in the order they should be tried.</summary>
        public List<string> Sources()
        {
            var sources = new List<string>();
            if (!string.IsNullOrWhiteSpace(url)) sources.Add(url.Trim());
            if (!string.IsNullOrWhiteSpace(fallbackUrl))
            {
                var fallback = fallbackUrl.Trim();
                sources.Add(fallback.Contains("://") ? fallback : $"{RemoteContentSettings.BaseUrl}/{fallback.TrimStart('/')}");
            }

            return sources;
        }
    }
}
