namespace VirtualPet.Economy
{
    /// <summary>
    /// Minimal key/value persistence abstraction. Swappable for a cloud save or a
    /// server-authoritative economy later without touching the wallet's callers.
    /// </summary>
    public interface ISaveService
    {
        bool HasKey(string key);
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Flush();
    }
}
