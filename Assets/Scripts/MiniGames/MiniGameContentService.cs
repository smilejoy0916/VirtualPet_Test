using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace VirtualPet.MiniGames
{
    /// <summary>
    /// Thin wrapper around Addressables for remote mini-game content:
    /// catalog updates, download-size checks, download with progress, instantiation,
    /// release and cache clearing. Bundles are cached on device by Unity's AssetBundle
    /// cache, keyed by bundle hash, so a second launch performs no network download.
    /// </summary>
    public sealed class MiniGameContentService
    {
        Task initialization;

        public sealed class LoadResult
        {
            public GameObject Instance;
            public bool Downloaded;
            public long DownloadedBytes;
            public string Error;

            public bool Succeeded => Instance != null;
        }

        /// <summary>Initializes Addressables once and optionally applies a newer remote catalog.</summary>
        public Task InitializeAsync(bool checkForCatalogUpdates = true) =>
            initialization ??= InitializeInternalAsync(checkForCatalogUpdates);

        async Task InitializeInternalAsync(bool checkForCatalogUpdates)
        {
            try
            {
                var init = Addressables.InitializeAsync(false);
                await init.Task;
                if (init.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogError($"[Content] Addressables initialization failed: {init.OperationException}");
                Addressables.Release(init);

                if (checkForCatalogUpdates) await TryUpdateCatalogsAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Downloads a newer remote catalog if the hosted <c>.hash</c> differs from the cached one.
        /// This is how content is updated without shipping a new app build.
        /// </summary>
        public async Task<bool> TryUpdateCatalogsAsync()
        {
            var check = Addressables.CheckForCatalogUpdates(false);
            await check.Task;
            var catalogs = check.Status == AsyncOperationStatus.Succeeded ? check.Result : null;
            Addressables.Release(check);

            if (catalogs == null || catalogs.Count == 0)
            {
                Debug.Log("[Content] Remote catalog is up to date (or unreachable).");
                return false;
            }

            var update = Addressables.UpdateCatalogs(catalogs, false);
            await update.Task;
            var updated = update.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(update);
            Debug.Log($"[Content] Catalog update {(updated ? "applied" : "failed")}: {string.Join(", ", catalogs)}");
            return updated;
        }

        /// <summary>Bytes still to download for <paramref name="key"/>; 0 = cached, -1 = unknown key / error.</summary>
        public async Task<long> GetDownloadSizeAsync(string key)
        {
            await InitializeAsync();
            var handle = Addressables.GetDownloadSizeAsync(key);
            await handle.Task;
            var size = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : -1;
            Addressables.Release(handle);
            return size;
        }

        /// <summary>Downloads (if needed) and instantiates the content prefab under <paramref name="parent"/>.</summary>
        public async Task<LoadResult> LoadAsync(string key, Transform parent, Action<float, long> onProgress)
        {
            await InitializeAsync();

            var size = await GetDownloadSizeAsync(key);
            if (size < 0) return new LoadResult { Error = $"Content '{key}' was not found in the content catalog." };

            if (size > 0)
            {
                Debug.Log($"[Content] Downloading '{key}' ({size} bytes)");
                var download = Addressables.DownloadDependenciesAsync(key, false);
                while (!download.IsDone)
                {
                    var status = download.GetDownloadStatus();
                    onProgress?.Invoke(status.Percent, size);
                    await Awaitable.NextFrameAsync();
                }

                var downloaded = download.Status == AsyncOperationStatus.Succeeded;
                var error = download.OperationException?.Message;
                Addressables.Release(download);
                if (!downloaded)
                    return new LoadResult { Error = $"Download failed. Check your connection and try again.\n\n{Shorten(error)}" };
                onProgress?.Invoke(1f, size);
            }
            else
            {
                Debug.Log($"[Content] '{key}' is cached on device, no download needed");
            }

            var instantiate = Addressables.InstantiateAsync(key, parent, false);
            await instantiate.Task;
            if (instantiate.Status != AsyncOperationStatus.Succeeded)
            {
                var error = instantiate.OperationException?.Message;
                Addressables.Release(instantiate);
                return new LoadResult { Error = $"Could not load mini-game content.\n\n{Shorten(error)}" };
            }

            return new LoadResult { Instance = instantiate.Result, Downloaded = size > 0, DownloadedBytes = size };
        }

        /// <summary>Destroys an instance and lets Addressables unload its bundle when no longer referenced.</summary>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;
            if (!Addressables.ReleaseInstance(instance)) UnityEngine.Object.Destroy(instance);
        }

        /// <summary>Removes cached bundles for <paramref name="key"/> so the next launch downloads again.</summary>
        public async Task<bool> ClearCacheAsync(string key)
        {
            await InitializeAsync();
            var handle = Addressables.ClearDependencyCacheAsync(key, false);
            await handle.Task;
            var cleared = handle.Status == AsyncOperationStatus.Succeeded && handle.Result;
            Addressables.Release(handle);
            Debug.Log($"[Content] Cleared cache for '{key}': {cleared}");
            return cleared;
        }

        static string Shorten(string message)
        {
            if (string.IsNullOrEmpty(message)) return "Unknown error";
            return message.Length > 220 ? message.Substring(0, 220) + "…" : message;
        }
    }
}
