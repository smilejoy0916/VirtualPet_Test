using UnityEngine;

namespace VirtualPet.Economy
{
    /// <summary>PlayerPrefs-backed persistence (NSUserDefaults on iOS).</summary>
    public sealed class LocalSaveService : ISaveService
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        // Written immediately so a balance survives the app being killed from the app switcher.
        public void Flush() => PlayerPrefs.Save();
    }
}
