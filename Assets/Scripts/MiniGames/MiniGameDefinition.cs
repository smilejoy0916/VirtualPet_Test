using System;
using UnityEngine;

namespace VirtualPet.MiniGames
{
    /// <summary>
    /// Host-side description of a mini-game: where its remote content lives and how its
    /// score converts into coins. Adding a mini-game whose logic already ships in the app
    /// only needs a new entry here plus an Addressables prefab.
    /// </summary>
    [Serializable]
    public sealed class MiniGameDefinition
    {
        public string id = "tap-challenge";
        public string displayName = "Tap Challenge";
        [TextArea] public string description = "Tap as fast as you can for 10 seconds. Every point is a coin!";

        [Tooltip("Addressables address of the remotely hosted mini-game prefab.")]
        public string contentKey = "minigames/tap-challenge";

        [Header("Rewards (host-authoritative)")]
        [Min(0f)] public float coinsPerPoint = 1f;
        [Min(0)] public int maxCoinsPerSession = 200;

        [Header("Presentation")]
        public string badge = "TAP";
        public Color accentColor = new(1f, 0.54f, 0.30f);
    }
}
