using UnityEngine;

namespace VirtualPet.MiniGames.Sdk
{
    /// <summary>
    /// Read-only information the host hands to a mini-game when it starts. Deliberately
    /// small: games get no access to the wallet, navigation or any other host service.
    /// </summary>
    public sealed class MiniGameSession
    {
        public string MiniGameId { get; }
        public Font Font { get; }

        public MiniGameSession(string miniGameId, Font font)
        {
            MiniGameId = miniGameId;
            Font = font;
        }
    }
}
