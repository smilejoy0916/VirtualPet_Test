using VirtualPet.Economy;
using VirtualPet.MiniGames;
using VirtualPet.Navigation;

namespace VirtualPet.Core
{
    /// <summary>
    /// The app's shared services, created once by <see cref="AppBootstrap"/> and passed
    /// explicitly to each screen (no singletons, no global lookups).
    /// </summary>
    public sealed class AppContext
    {
        public AppConfig Config { get; }
        public CoinWallet Wallet { get; }
        public NavigationService Navigation { get; }
        public MiniGameContentService Content { get; }
        public MiniGameController MiniGames { get; }

        public AppContext(AppConfig config, CoinWallet wallet, NavigationService navigation,
            MiniGameContentService content, MiniGameController miniGames)
        {
            Config = config;
            Wallet = wallet;
            Navigation = navigation;
            Content = content;
            MiniGames = miniGames;
        }
    }
}
