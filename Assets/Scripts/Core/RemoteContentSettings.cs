namespace VirtualPet.Core
{
    /// <summary>
    /// Runtime-resolved content host. The Addressables RemoteLoadPath profile value is
    /// <c>{VirtualPet.Core.RemoteContentSettings.BaseUrl}/[BuildTarget]</c>; values in curly
    /// braces are evaluated by Addressables at runtime, so the host can be changed in
    /// <see cref="AppConfig"/> without rebuilding the content.
    /// </summary>
    public static class RemoteContentSettings
    {
        static string baseUrl = "http://localhost:8000";

        // Must be set before Addressables initializes (AppBootstrap.Awake does this).
        public static string BaseUrl
        {
            get => baseUrl;
            set => baseUrl = string.IsNullOrWhiteSpace(value) ? baseUrl : value.Trim().TrimEnd('/');
        }
    }
}
