using UnityEngine;

namespace VirtualPet.Economy
{
    /// <summary>
    /// Converts mini-game scores into coins. The conversion rate and cap live on the
    /// host side, so downloaded content can report a score but can never mint coins directly.
    /// </summary>
    public sealed class RewardService
    {
        readonly CoinWallet wallet;

        public RewardService(CoinWallet wallet) => this.wallet = wallet;

        public static int CoinsForScore(int score, float coinsPerPoint, int maxCoins)
        {
            if (score <= 0) return 0;
            return Mathf.Clamp(Mathf.FloorToInt(score * coinsPerPoint), 0, maxCoins);
        }

        public int GrantMiniGameReward(string miniGameId, int score, float coinsPerPoint, int maxCoins)
        {
            var coins = CoinsForScore(score, coinsPerPoint, maxCoins);
            wallet.Add(coins, $"mini-game '{miniGameId}' score {score}");
            return coins;
        }
    }
}
