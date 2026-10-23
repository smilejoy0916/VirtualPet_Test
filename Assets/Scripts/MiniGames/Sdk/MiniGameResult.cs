namespace VirtualPet.MiniGames.Sdk
{
    /// <summary>What a mini-game reports back to the host when a round ends.</summary>
    public readonly struct MiniGameResult
    {
        public readonly int Score;

        public MiniGameResult(int score) => Score = score;

        public override string ToString() => $"MiniGameResult(score={Score})";
    }
}
