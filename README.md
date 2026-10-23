# Virtual Pet – Unity Architecture Proof of Concept

A small Unity iOS prototype with three tabs (**Home**, **Mini-games**, **Feed**) that share one coin balance. It shows:
- a modular C# architecture,
- remote mini-game content delivered and cached with Addressables,
- a vertical video feed with preloading.

| | |
|---|---|
| Engine | Unity **6000.6.7f1** (Unity 6.6), Built-in Render Pipeline |
| Target | iOS Simulator (arm64), portrait |
| Key packages | Addressables 2.11.2, uGUI 2.0, Input System 1.19, Video module |
| Native code / WebView | none |

---

## 1. Quick start

```bash
# 1. Install the editor (needs ~15 GB free and Unity Hub signed in)
Tools/install_unity.sh

# 2. Open the project once in Unity Hub (Add ▸ this folder).
#    First open runs VirtualPet ▸ Setup Project automatically: it generates the
#    scene, prefabs, materials, mini-game content and Addressables config.

# 3. Build the remote content + iOS Simulator app, install and launch it
Tools/serve_content.sh &          # local content host on http://localhost:8000
Tools/build_ios_sim.sh "iPhone 17"
```

**In the editor:** open `Assets/Scenes/Bootstrap.unity` and press Play. The default Addressables play mode is *Use Asset Database*, so it needs no server. To test real downloads in the editor:
1. Set `Window ▸ Asset Management ▸ Addressables ▸ Groups ▸ Play Mode Script` to **Use Existing Build**.
2. Run `VirtualPet ▸ Content ▸ Build Remote Content`.
3. Start `Tools/serve_content.sh`.

### Hosting content over HTTPS
1. Run `VirtualPet ▸ Content ▸ Build Remote Content`. It writes `ServerData/iOS/`, which holds the catalog `.bin`/`.hash` and the `.bundle` files.
2. Upload the **contents of `ServerData/`** to any static HTTPS host, for example S3/CloudFront, Cloudflare R2, Firebase Hosting or GitHub Pages. The result should look like `https://cdn.example.com/vpet/iOS/catalog_poc.hash`.
3. Set `Assets/Settings/AppConfig.asset ▸ Remote Content Base Url` to `https://cdn.example.com/vpet`. No `/iOS` is needed; the platform folder is appended automatically.
4. Rebuild the app.

The build script allows plain HTTP only when the configured URL is `http://`, which is meant for local testing.

---

## 2. Project structure

```
Assets/
  Scenes/Bootstrap.unity          single scene: one GameObject with AppBootstrap
  Scripts/
    Core/        AppBootstrap (composition root), AppContext, AppConfig, RemoteContentSettings
    Navigation/  NavigationService, ScreenBase, ScreenId, TabBar
    Home/        HomeController, PetView, PetStage, PetBuilder, MaterialFactory
    MiniGames/   MiniGameController (session), MiniGameContentService (Addressables),
                 MiniGameOverlayView, MiniGamesScreenController, MiniGameDefinition
      Sdk/                 ← separate assembly: the ONLY contract between host and games
      Games/TapChallenge/  ← separate assembly: game logic, references only the Sdk
    Feed/        FeedController, VideoPlayerController, FeedSwipeInput, FeedVideoEntry
    Economy/     CoinWallet, RewardService, ISaveService, LocalSaveService
    UI/          UIFactory, UITheme, UISprites, CoinBadge, SafeAreaFitter, LoadingSpinner
    Editor/      ProjectSetup (generates assets/config), BuildScripts (content + iOS builds)
  Prefabs/Pet.prefab              original primitive-built pet ("Mochi")
  AddressableAssets/MiniGames/TapChallenge/   remote content: prefab, config, sprites
  Materials/  RenderTextures/  Settings/AppConfig.asset
Tools/   install_unity.sh, serve_content.sh, build_ios_sim.sh
```

### Assemblies

```
VirtualPet.MiniGames.Sdk  ◄──  VirtualPet.MiniGames.TapChallenge
          ▲
          └─────────────  VirtualPet (host app)  ──► Addressables, uGUI, Input System
```

The host never references a concrete game, and a game cannot reach the wallet, navigation or any other host service. The compiler enforces this boundary, not just convention.

### Runtime composition

`AppBootstrap.Awake` builds everything in code, in this order:
1. Services: `CoinWallet`, `RewardService`, `NavigationService`, `MiniGameContentService`, `MiniGameController`.
2. The UI shell: canvas, safe area and tab bar.
3. The three screens.
4. Everything is wired through an explicit `AppContext`, with no singletons or `Find` calls.
5. Home is shown.

Screens are created once and toggled, so switching tabs never reloads anything.

| PRD component | Implementation |
|---|---|
| AppBootstrap | `Core/AppBootstrap.cs` |
| NavigationService | `Navigation/NavigationService.cs`: tabs plus a modal stack. A modal takes focus from the current screen, which pauses Feed video. |
| HomeController | `Home/HomeController.cs` + `PetView.cs`: tap via `PhysicsRaycaster`, code-driven squash-and-stretch jump/spin |
| MiniGameContentService | `MiniGames/MiniGameContentService.cs` |
| MiniGameController | `MiniGames/MiniGameController.cs` (host session) + `Games/TapChallenge/TapChallengeGame.cs` (game logic and scoring) |
| CoinWallet | `Economy/CoinWallet.cs`: `BalanceChanged` event, persisted on every change |
| LocalSaveService | `Economy/LocalSaveService.cs` (PlayerPrefs) |
| FeedController | `Feed/FeedController.cs` |
| VideoPlayerController | `Feed/VideoPlayerController.cs` |

### Technical decisions
- **UI is built in code (uGUI) rather than authored prefabs.** Layouts are readable in diffs, easy to change live during the walkthrough, and free of merge conflicts. Panels and buttons use procedurally generated 9-slice sprites, so the host ships no texture assets.
- **One shared balance.** `CoinBadge` is a single reusable component on every screen. It subscribes to `CoinWallet.BalanceChanged` and re-reads the balance in `OnEnable`, so a hidden screen can never show a stale value.
- **The host owns the economy.** A mini-game reports only a score. `RewardService` converts it to coins using a rate and cap from the host-side `MiniGameDefinition`, so downloaded content can't mint coins.
- **Persistence:** PlayerPrefs, flushed on every change. `ISaveService` makes it swappable for cloud save or a server.
- **Generated assets.** `ProjectSetup` generates assets (scene, prefabs, materials, mini-game sprites, Addressables groups and profile) from code. The repo stays small and reproducible, and the setup is idempotent: existing assets and hand edits are kept.

---

## 3. Mini-game download and caching

### Packaging
- The group **"Remote MiniGame - TapChallenge"** contains a single addressable entry, `minigames/tap-challenge`, which is the `TapChallenge.prefab`.
- Its dependencies are pulled into the same bundle: `TapChallengeConfig` (rules and colours) and its sprites.
- Bundle settings: one LZ4 bundle, hash-appended file name, CRC check, 2 retries and a 30 s timeout.
- The group is built to `ServerData/[BuildTarget]` and loaded from:
  ```
  RemoteLoadPath = {VirtualPet.Core.RemoteContentSettings.BaseUrl}/[BuildTarget]
  ```
  Addressables evaluates the `{…}` part **at runtime**. `AppBootstrap` sets it from `AppConfig` before Addressables initializes, so the content host is configuration, not a rebuild.

### Flow (`MiniGameController.RunSessionAsync`)
1. The overlay opens and the current tab loses focus, which pauses Feed video.
2. `GetDownloadSizeAsync(key)` returns one of:
   - **> 0:** `DownloadDependenciesAsync` runs, with a byte-size label and progress bar.
   - **0:** the content is already cached, so nothing is downloaded. The Mini-games list then shows *"Cached on device • ready offline"*.
3. On failure, the overlay shows an error panel with **Retry**. **Exit** is always available.
4. `InstantiateAsync` loads the prefab into the game area, then `MiniGameBehaviour.Begin(session)` starts the game.
5. The game raises `Finished(score)`. The host then calls `RewardService.GrantMiniGameReward`, `CoinWallet.Add` saves the balance, and every `CoinBadge` animates to the new value.
6. **Collect & Exit** (or **Exit**) calls `Addressables.ReleaseInstance`, so the bundle unloads when it's no longer referenced. The overlay then closes and focus returns to the previous tab.

### Caching
- `UseAssetBundleCache` stores bundles in Unity's on-device cache (`Library/Caches` on iOS), keyed by bundle name and hash. It survives app restarts.
- A later launch finds a download size of 0 and loads from disk, even offline.
- The **Clear cache** button on the card calls `ClearDependencyCacheAsync`, so a first-download run can be shown again.
- Logs to look for:
  - `[Content] Downloading 'minigames/tap-challenge' (N bytes)`
  - `[Content] … is cached on device, no download needed`

---

## 4. Remote content updates (no app update)
- The remote catalog is enabled.
- `OverridePlayerVersion = "poc"` keeps the catalog file name stable (`catalog_poc.bin` / `.hash`).
- At launch, `MiniGameContentService` calls `CheckForCatalogUpdates`. If the hosted `.hash` differs, it applies the new catalog with `UpdateCatalogs`.

To ship a content change, for example the round length, colours or art:
1. Edit `AddressableAssets/MiniGames/TapChallenge/TapChallengeConfig.asset`, for example `contentVersion` 1.0.0 → 1.1.0 and `roundSeconds` 10 → 15.
2. Run `VirtualPet ▸ Content ▸ Build Content Update (for shipped app)`. It uses `addressables_content_state.bin` from the last full build, and that file must be kept in git.
3. Upload `ServerData/iOS`.
4. Relaunch the installed app. It downloads only the changed bundle, and the game shows `content v1.1.0`.

The group is marked *Can Change Post Release*, so updated bundles replace the old ones. Their hashed file names also make the old cache entries obsolete.

---

## 5. Adding mini-games and the future UGC approach

**A new game that uses existing logic (content only):**
1. Create a prefab with an existing `MiniGameBehaviour`, for example a re-skinned Tap Challenge with its own config.
2. Mark it addressable in a remote group.
3. Add a `MiniGameDefinition` to `AppConfig`.

The next step (not built here) is to make the definitions list itself a remote addressable/JSON catalog. New entries would then appear without an app update.

**A new game type (new logic):**
1. Add `Scripts/MiniGames/Games/<Name>/` with its own asmdef that references only the Sdk.
2. Subclass `MiniGameBehaviour`.
3. Ship the logic in an app update; its content is then remote as above.
4. Add the assembly to `link.xml`, so IL2CPP stripping keeps code that only remote prefabs reference.

**User-generated mini-games (future architecture):** UGC would be **data, never code**.
- The app ships a set of trusted, sandboxed *game templates*: tap/timing, match-3, runner, quiz.
- A creator tool produces a JSON level/config document and art (textures, audio).
- That content is validated and moderated server-side, built into Addressables bundles in CI, and served from a CDN.
- The client only instantiates templates with that data. Template parameters are clamped, so a UGC game can't break the economy.
- If richer behaviour is ever needed, use a small interpreted rule system (a declarative event→action graph) evaluated by app code, not downloaded scripts.

### iOS executable-code restrictions
- iOS forbids downloading and executing new native or managed code (App Store Review Guideline 2.5.2). IL2CPP compiles all C# ahead of time, so DLLs in AssetBundles can't run.
- **Here, all logic is compiled into the app.** Bundles contain only assets and serialized data: prefabs, ScriptableObjects, sprites. A prefab's `MonoBehaviour` is a reference to a class that already ships in the binary.
- **Consequence:** new game *types* need an app update. New *content* for existing types doesn't.
- No WebView, JavaScript or Lua runtime is used.

---

## 6. Video feed: loading and preloading
- Each page has a `VideoPlayerController` with these pieces:
  - `VideoPlayer` (URL source)
  - a `RenderTexture` created at the video's native size once prepared
  - a `RawImage` with an `AspectRatioFitter (FitInParent)`, so video is letterboxed and never stretched; pixel aspect ratio is respected
- **Playback window:**

  | Page | State |
  |---|---|
  | Current | plays (with audio) |
  | Previous and next | prepared and paused; the next page's first frame is already decoded, so swiping shows video instantly |
  | All others | `Stop()`, RenderTexture released |

  At most 3 decoders and textures exist at once. Leaving the Feed tab releases all of them. Opening a mini-game or backgrounding the app pauses the current video.
- **Loading:** a spinner shows while preparing, and also while playing if frames stop advancing for 0.6 s (buffering).
- **Errors and fallback:** a decode/network error or a 30 s prepare timeout makes the player switch to the entry's `fallbackUrl`. Only if every source fails does it show *"Couldn't play this video"* with **Tap to retry**.
- **Why the fallback exists.** The supplied source files are not all mobile-safe:

  | Video | Source encoding | Problem on iOS |
  |---|---|---|
  | `gothic-blast/pv-video.mp4` | H.264 **High 4:4:4 Predictive**, yuv444p, 493×876 | AVFoundation can't decode it (`-11821 Cannot Decode`) |
  | `emoji-cube-match-3d…5s.mp4` | HEVC 1440×2560 **60 fps, 23.6 Mbps** (15 MB / 5 s) | very heavy decode and data cost for a feed |
  | all three | `moov` atom at the end (not fast-start) | playback can't start until the index is fetched |

  `Tools/transcode_videos.sh` (run by `build_ios_sim.sh`) creates H.264 High 4:2:0, ≤720×1280, 30 fps, ~2.5 Mbps, fast-start renditions in `ServerData/videos/`. They are hosted alongside the mini-game content, and a relative `fallbackUrl` resolves against the same content host. In production, transcoding would happen in the upload pipeline (ideally to HLS), and the original files should be re-exported as H.264/HEVC 4:2:0 with fast-start.
- **Swipe:** `FeedSwipeInput` (uGUI drag events) moves the pages. A drag of 18% of the page height, or a fast flick, changes the video; the ends have rubber-band resistance, and pages snap with `SmoothDamp`.
- **Play Now** launches the mini-game linked to the current video, through the same `MiniGameController` as the Mini-games tab.

---

## 7. Testing checklist (matches the simulator recording)
1. **Fresh launch:** Home shows Mochi and **100** coins. Tapping Mochi makes it jump; every third tap adds a spin.
2. **Mini-games tab:** shows *Download required • N KB*. Tap **Download & Play** to see progress, then the game.
3. **Score and coins:** score 20 and tap **Collect & Exit**. All badges show **120**.
4. **Tab sync:** Home and Feed both show 120.
5. **Feed:** swipe through the 3 videos. Playback, preloading and the indicator dots work.
6. **Play Now** from Feed: the game opens instantly with no download (*from cache*). After it closes, the video resumes.
7. **Persistence and cache reuse:** stop the app (`xcrun simctl terminate booted com.virtualpet.poc`) and relaunch it. The coins are still there, the Mini-games card shows *Cached on device*, and play works even with `serve_content.sh` stopped.
8. **Error handling:** stop the server and tap **Clear cache**, then play. The error panel and **Retry** appear. Restart the server and tap Retry to recover.

Record the demo with `xcrun simctl io booted recordVideo demo.mp4`.

---

## 8. Known limitations and trade-offs
- The pet is built from primitives with code-driven animation (no Animator), which is enough for the PoC.
- The Legacy `UnityEngine.UI.Text` with the built-in font avoids an import step for TextMesh Pro Essentials. For production, switch to TextMesh Pro.
- The economy is client-side (PlayerPrefs), as the PRD allows. Production needs server-authoritative rewards with signed score submissions.
- Feed video stays at native resolution. With longer or HD feeds, use HLS adaptive streaming or a smaller rendition per device.
