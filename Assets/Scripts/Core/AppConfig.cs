using System.Collections.Generic;
using UnityEngine;
using VirtualPet.Feed;
using VirtualPet.MiniGames;

namespace VirtualPet.Core
{
    /// <summary>All tunable app data in one asset (Assets/Settings/AppConfig.asset).</summary>
    [CreateAssetMenu(menuName = "VirtualPet/App Config", fileName = "AppConfig")]
    public sealed class AppConfig : ScriptableObject
    {
        [Header("Economy")]
        [Min(0)] public int startingCoins = 100;

        [Header("Remote content")]
        [Tooltip("Base URL hosting the Addressables output (the contents of ServerData/). " +
                 "The platform folder (e.g. /iOS) is appended automatically. Use HTTPS in production.")]
        public string remoteContentBaseUrl = "http://localhost:8000";
        public bool checkForContentUpdatesOnLaunch = true;

        [Header("Mini-games")]
        public List<MiniGameDefinition> miniGames = new();

        [Header("Feed")]
        public List<FeedVideoEntry> feedVideos = new();

        [Header("Home")]
        public GameObject petPrefab;
        public Material groundMaterial;
        public string petName = "Mochi";

        public static AppConfig CreateDefault()
        {
            var config = CreateInstance<AppConfig>();
            config.ApplyDefaults();
            return config;
        }

        public void ApplyDefaults()
        {
            if (miniGames.Count == 0) miniGames.Add(new MiniGameDefinition());

            if (feedVideos.Count == 0)
            {
                feedVideos.Add(new FeedVideoEntry
                {
                    title = "Corestorm: Mecha Arena",
                    subtitle = "Pilot a mech, win the arena.",
                    url = "https://assets.elseland.elser.ai/games/corestorm-mecha-arena/pv-6s.mp4",
                    fallbackUrl = "videos/corestorm-mecha-arena.mp4"
                });
                feedVideos.Add(new FeedVideoEntry
                {
                    title = "Gothic Blast",
                    subtitle = "Blast through the haunted castle.",
                    url = "https://assets.elseland.elser.ai/games/gothic-blast/pv-video.mp4",
                    fallbackUrl = "videos/gothic-blast.mp4"
                });
                feedVideos.Add(new FeedVideoEntry
                {
                    title = "Emoji Cube Match 3D",
                    subtitle = "Match the cubes before time runs out.",
                    url = "https://assets.elseland.elser.ai/games/emoji-cube-match-3d/emoji-cube-match-3d-9-16-5s.mp4",
                    fallbackUrl = "videos/emoji-cube-match-3d.mp4"
                });
            }
        }
    }
}
