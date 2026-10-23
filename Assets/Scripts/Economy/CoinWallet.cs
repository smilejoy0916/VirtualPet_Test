using System;
using UnityEngine;

namespace VirtualPet.Economy
{
    /// <summary>
    /// The single source of truth for the player's coins. Screens never cache the
    /// balance; they read <see cref="Balance"/> and subscribe to <see cref="BalanceChanged"/>.
    /// </summary>
    public sealed class CoinWallet
    {
        const string BalanceKey = "economy.coins";

        readonly ISaveService save;

        /// <summary>Raised with (newBalance, delta) after every change.</summary>
        public event Action<int, int> BalanceChanged;

        public int Balance { get; private set; }

        public CoinWallet(ISaveService save, int startingBalance)
        {
            this.save = save;
            if (save.HasKey(BalanceKey))
            {
                Balance = Mathf.Max(0, save.GetInt(BalanceKey, startingBalance));
            }
            else
            {
                Balance = startingBalance;
                Persist();
            }
        }

        public void Add(int amount, string reason)
        {
            if (amount <= 0) return;
            Apply(amount, reason);
        }

        public bool TrySpend(int amount, string reason)
        {
            if (amount <= 0 || amount > Balance) return false;
            Apply(-amount, reason);
            return true;
        }

        void Apply(int delta, string reason)
        {
            Balance += delta;
            Persist();
            Debug.Log($"[CoinWallet] {(delta > 0 ? "+" : "")}{delta} ({reason}) -> {Balance}");
            BalanceChanged?.Invoke(Balance, delta);
        }

        void Persist()
        {
            save.SetInt(BalanceKey, Balance);
            save.Flush();
        }
    }
}
