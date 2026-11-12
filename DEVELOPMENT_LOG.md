# RED MOON: THE LAST RIDER — Development Log

Chronological record of implementation, verification and architectural decisions.

---

## 2026-11-12 — Phase 1.1: Environment Inspection

**Status:** Complete

- Inspected Windows 11 Insider build `10.0.26340`, Ryzen 7 7435HS, 16 GB
  DDR5-5600, RTX 3050 Laptop 4 GB, 216.7 GB free disk.
- Verified Unity Hub `3.22.6`, Unity Editor `6000.6.8f1`, URP `17.6.0`,
  Windows Standalone Mono support.
- Verified Git `2.56.0`, OpenCode `2.0.21`, Node `22.23.3`, npm `12.2.2`,
  Python `3.11.0`.
- Found the Unity MCP bridge configured but offline at the time of inspection.
- Decision: environment hardware/toolchain ready; MCP bridge required a running
  Editor session.

---

## 2026-11-12 — Phase 1.2: Project Initialization

**Status:** Complete

- Created the Unity project at `C:\Users\premk\Videos\MAKE`.
- Installed URP `17.6.0` and configured the `REDMOON_URP` pipeline asset.
- Configured Linear color space, HDR, HDR color grading, soft shadows,
  2 cascades, 2048 shadow map, 55 m shadow distance, 4 additional lights.
- Assigned URP to GraphicsSettings and all quality levels.
- Set project identity:
  - Company: `RED MOON`
  - Product: `RED MOON: THE LAST RIDER`
  - Resolution: 1920 × 1080, FullScreenWindow
- Created `Bootstrap.unity` with Main Camera and Directional Light.
- Installed Unity MCP `10.3.0` and verified the live HTTP bridge on
  `127.0.0.1:8080`.
- Verified URP activation, scene loading, hierarchy, console and screenshot
  capture through MCP.

**Verification:** URP active; MCP instance `MAKE@fdf77b424d2a24b7`; no compile
errors.

---

## 2026-11-12 — Phase 1.3: Professional Project Architecture

**Status:** Complete

### Audit

- Unity `6000.6.8f1`, 40 packages installed.
- URP `17.6.0` active; quality level `High`.
- Legacy input handler active (`activeInputHandler: 0`).
- Bootstrap scene active and clean.
- No assembly definitions existed.
- No Git repository existed.

### Folder architecture

- Created the full `Assets/_Project` taxonomy:
  - Art, Audio, Prefabs, Scenes, Scripts, Settings, UI.
  - Level folders `Level01` through `Level15`.
- Moved `Bootstrap.unity` to
  `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`.
- Updated Build Settings so the scene remains build index 0.
- Moved `RedMoonBootstrap.cs` to `Assets/_Project/Scripts/Editor/`.

### Input System

- Installed Unity Input System `1.20.1`.
- Created the canonical Input Actions asset:
  `Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions`.
- Action maps: Player, Horse, Combat, Camera, UI.
- Control schemes: KeyboardMouse, Gamepad.
- Added centralized runtime wrapper:
  `Assets/_Project/Scripts/Core/Input/RedMoonInput.cs`.
- Switched `activeInputHandler` to `1` (new Input System backend) and
  restarted the Editor. The legacy Input Manager warning is gone.
- No gameplay behaviour was implemented.

### Assemblies

- Created `RedMoon.Core` and domain assemblies for Player, Horse, Camera,
  Combat, Enemies, AI, Missions, Inventory, Progression, World, Audio, UI,
  SaveSystem, Utilities and Editor.
- Editor assembly is Editor-only and references the URP runtime assemblies.

### Documentation

- Added `README.md`.
- Added `PROJECT_ARCHITECTURE.md`.
- Added `DEVELOPMENT_LOG.md`.

### Verification

- Unity console: no compile errors.
- URP configuration preserved at GUID
  `d25701c8bdcd70448a18f7edf8e2f1f9`.
- Bootstrap scene preserved at GUID
  `3a6364363efabbf428637b09a07ce989` and still build index 0.
- MCP bridge remained live during the architecture pass and reconnected after
  the Editor restart.
- Input Actions asset imported as `UnityEngine.InputSystem.InputActionAsset`
  with 5 maps, 31 actions and 2 control schemes.
- `RedMoon.Core` and `RedMoon.Editor` assemblies compiled and loaded.
- Project identity: bundle version `0.1.0`, application identifier
  `com.redmoon.lastrider`.

### Notes

- `.inputactions` files are JSON text assets. `AssetDatabase.CreateAsset`
  produces an unreadable file; the builder now writes `asset.ToJson()` and
  imports the result with `AssetDatabase.ImportAsset`.
- The empty legacy `Assets/Editor` and `Assets/_Project/Materials` folders were
  removed after their contents were moved.
- A non-blocking MCP WebSocket transport warning can appear in the console;
  the HTTP bridge is the active transport and all MCP operations succeed.

---

## 2026-11-12 — Phase 1.4 Baseline Audit

**Status:** Complete (read-only baseline captured before configuration)

### Project and packages

- Unity `6000.6.8f1`.
- URP `17.6.0`; Input System `1.20.1`; MCP `10.3.0`.
- `Packages/manifest.json` and `packages-lock.json` present and valid.
- Project targets `StandaloneWindows64`, Mono2x, .NET Standard 2.0.
- Application identifier `com.redmoon.lastrider`; bundle version `0.1.0`.

### Rendering baseline

- Active pipeline: `REDMOON_URP` (GUID `d25701c8bdcd70448a18f7edf8e2f1f9`).
- Renderer: `REDMOON_Renderer`, Forward rendering, no renderer features.
- HDR on, Linear color space, render scale `1.0`, MSAA `1`.
- Main light shadows: 2048, 2 cascades, 55 m distance.
- Soft shadows on; additional-light shadows off.
- Maximum additional per-object lights: 4.
- Camera depth texture on; opaque texture off.
- Current quality: High.

### Quality baseline

- Six profiles: Very Low, Low, Medium, High, Very High, Ultra.
- All six reference the same `REDMOON_URP` asset.
- No per-tier URP asset differentiation yet.

### Tags, layers, physics, audio

- Built-in tags only; no custom Horse, Enemy, Interactable or Environment tags.
- Named layers: Default, TransparentFX, Ignore Raycast, Water, UI.
- No gameplay layers defined.
- Physics: gravity `-9.81`, fixed update, all named layer collisions enabled.
- No audio mixers exist.
- AudioManager: 48 kHz, 512 virtual voices, 32 real voices, rolloff scale `1`,
  Doppler factor `1`.

### Input and assemblies

- Input Actions asset: 5 maps, 31 actions, 2 control schemes.
- `RedMoonInput` enables, disables and disposes correctly.
- 16 assembly definitions; `RedMoon.Core` and `RedMoon.Editor` compiled.
- Domain assemblies contain no scripts yet.

### Scene and build

- Bootstrap scene at `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`.
- Build index 0, enabled.
- Scene roots: Main Camera and Directional Light.
- Camera: 60° FOV, HDR allowed, MSAA allowed, pipeline rendering path.
- Directional light: soft shadows, intensity `1`.

### Git baseline

- No Git repository in the project.
- Remote `main` exists at commit `24aa52d` with no project files.
- Git LFS `3.8.0` installed; GitHub CLI not installed.
- Git identity configured as `prem-2323 <premkalai2006@gmail.com>`.

---

## 2026-11-12 — Phase 1.4: Final Configuration and QA

**Status:** Complete

### Rendering

- Preserved `REDMOON_URP` and `REDMOON_Renderer` unchanged.
- Created three additional URP pipeline assets that share the existing
  `REDMOON_Renderer`:
  - `REDMOON_URP_Low`
  - `REDMOON_URP_Medium`
  - `REDMOON_URP_Ultra`
- Kept Forward rendering after confirming the Bootstrap scene has no dense
  lighting workload and no profiling evidence supports Forward+.
- Exact tier settings:

| Tier | Render scale | MSAA | Main shadow | Cascades | Shadow distance | Add. lights | Soft shadows |
|---|---:|---:|---:|---:|---:|---:|---|
| Low | 0.80 | Off | 1024 | 1 | 30 m | 0 | Off |
| Medium | 0.90 | Off | 2048 | 2 | 40 m | 2 | On |
| High | 1.00 | Off | 2048 | 2 | 55 m | 4 | On |
| Ultra | 1.00 | 4× | 4096 | 4 | 70 m | 8 | On |

- Additional-light shadows remain disabled on every tier.
- Camera depth texture is enabled; opaque texture is disabled.
- HDR and HDR color grading remain enabled.
- The six existing quality profiles were preserved. The four required tiers
  now use distinct URP assets; Very Low and Very High remain compatibility
  fallbacks mapped to Low and Ultra.
- `High` is the current quality level and the default for Windows standalone.
- QualitySettings differences were verified for shadow distance, shadow
  resolution, pixel light budget, LOD bias, anisotropic filtering, particle
  raycast budget and reflection probes.

### Tags, layers and physics

Added custom tags:

- `Horse`
- `Enemy`
- `Interactable`
- `Environment`

Added user layers:

| Index | Layer |
|---:|---|
| 8 | `Player` |
| 9 | `Enemy` |
| 10 | `Horse` |
| 11 | `Interactable` |
| 12 | `Environment` |
| 13 | `Ground` |
| 14 | `Projectile` |

Built-in layers and tags were preserved. The collision matrix was verified after
configuration. Only these self-collision pairs were disabled:

- `Interactable` ↔ `Interactable`
- `Ground` ↔ `Ground`
- `Projectile` ↔ `Projectile`

All character, horse, enemy, environment, interactable and projectile
cross-collisions remain enabled.

### Audio

- Created `Assets/_Project/Settings/Audio/REDMOON_AudioMixer.mixer`.
- Verified hierarchy: Master with Music, Ambience, SFX, Voice and UI children.
- No audio clips or AudioSources were imported or created.
- Spatial audio conventions and routing policy were documented in
  `PROJECT_ARCHITECTURE.md`.

### Input

- Revalidated `REDMOON_InputActions.inputactions`: 5 maps, 31 actions,
  80 bindings, 2 control schemes.
- Revalidated `RedMoonInput` on an in-memory clone: create, EnableGameplay,
  EnableUI, DisableGameplay, DisableUI and Dispose all behaved correctly.
- No duplicate input system or gameplay implementation was added.

### Assemblies

- Verified all 16 assembly definitions: unique names, valid references, no
  cycles, no runtime `UnityEditor` references and no runtime reference to
  `RedMoon.Editor`.
- `RedMoon.Editor` is Editor-only.
- Found and fixed a real build blocker: `RedMoon.Editor.asmdef` stored
  `includePlatforms` as the string `"Editor"` instead of `["Editor"]`. This
  caused the editor scripts to compile for the player and produced 10 errors.
  After correcting the array, the build succeeded.
- Domain assemblies are still empty scaffolding.

### Windows build

- Target: `StandaloneWindows64`, x86_64.
- Scripting backend: Mono (`Mono2x`); .NET Standard 2.0.
- Company/product: `RED MOON` / `RED MOON: THE LAST RIDER`.
- Bundle version: `0.1.0`.
- Application identifier: `com.redmoon.lastrider`.
- Development build: enabled; script debugging and profiler connection disabled.
- Bootstrap scene: index 0, enabled.
- First development build failed with 10 compiler errors caused by the
  malformed Editor assembly definition.
- Second development build succeeded:
  - Output: `Builds/WindowsDev/REDMOON.exe`
  - Size: 166.12 MB
  - Duration: 113.05 seconds
  - Errors: 0
  - Warnings: 14 (empty domain assemblies)
- Built-player smoke test: launched the executable, confirmed the process stayed
  alive for 12 seconds, initialized D3D12, Input System and PhysX, and produced
  no exceptions. The process was then terminated intentionally.
- Play Mode smoke test: Bootstrap loaded, console remained clean, play mode
  exited normally.
- Windows IL2CPP support is not installed and was not required for the Mono
  development build.

### QA results

| Check | Result |
|---|---|
| Unity script compilation | PASS |
| Assembly validation | PASS |
| Scene loading / Play Mode | PASS |
| Input asset validation | PASS |
| URP settings validation | PASS |
| Quality tier validation | PASS |
| Tags and layers validation | PASS |
| Collision matrix validation | PASS |
| Audio mixer validation | PASS |
| Build Settings validation | PASS |
| Windows x64 development build | PASS (0 errors) |
| Built-player launch smoke test | PASS |
| MCP connection and operations | PASS |
| Console inspection | PASS (warnings documented) |

### Non-blocking warnings

- 14 build warnings: every empty domain assembly is skipped until its first
  script is added. This is expected scaffolding behavior.
- Intermittent MCP WebSocket warning: the HTTP transport remains operational.
- Built-player D3D12 message: `failed to query info queue interface
  (0x80004002)`; rendering initialized and the player remained stable.
- Windows IL2CPP build support is absent; release builds will require it.

### Files created or changed

- `Assets/_Project/Settings/URP/REDMOON_URP_Low.asset`
- `Assets/_Project/Settings/URP/REDMOON_URP_Medium.asset`
- `Assets/_Project/Settings/URP/REDMOON_URP_Ultra.asset`
- `Assets/_Project/Settings/Audio/REDMOON_AudioMixer.mixer`
- `Assets/_Project/Scripts/Editor/RedMoonQualityConfigurator.cs`
- `Assets/_Project/Scripts/Editor/AudioMixerBuilder.cs`
- `Assets/_Project/Scripts/Editor/RedMoon.Editor.asmdef`
- `ProjectSettings/TagManager.asset`
- `ProjectSettings/DynamicsManager.asset`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/GraphicsSettings.asset` (URP validation/no regression)
- `ProjectSettings/EditorBuildSettings.asset` (unchanged, revalidated)
- `README.md`
- `PROJECT_ARCHITECTURE.md`
- `DEVELOPMENT_LOG.md`

---

## 2026-11-12 — Phase 1.5: Git and GitHub

**Status:** Complete

### Pre-Git audit

- No `.git` directory existed.
- Remote `main` contained one empty-history commit:
  `24aa52df5799527b192ffa79dba0b3bf05213b6b` — “Remove all files and
  history”.
- Git `2.56.0`; Git LFS `3.8.0`; GitHub CLI not installed.
- Git identity: `prem-2323 <premkalai2006@gmail.com>`.
- No project files were present on the remote before the merge.

### Repository initialization

- Initialized Git in `C:\Users\premk\Videos\MAKE`.
- Established branch `main`.
- Added remote `origin`:
  `https://github.com/prem-2323/RED-MOON-THE-LAST-RIDER.git`.
- Initialized Git LFS locally and configured tracking for:
  `*.psd`, `*.tif`, `*.tiff`, `*.blend`, `*.fbx`, `*.wav`, `*.mp3`, `*.mp4`,
  `*.mov`.
- No LFS objects exist yet; no binary source media has been imported.
- `.gitignore` verified to exclude `Library/`, `Temp/`, `Obj/`, `Logs/`,
  `UserSettings/`, `Build/`, `Builds/`, `MemoryCaptures/`, generated
  screenshots, IDE files and generated project files.
- `.gitattributes` normalizes Unity text assets to LF and keeps binary source
  media in LFS.

### Commits

| SHA | Message |
|---|---|
| `ceff97c` | `chore: initialize Unity 6 project configuration` |
| `b0ab73d` | `feat: add Bootstrap scene, input foundation and domain assemblies` |
| `621ce54` | `feat: configure quality tiers and audio mixer foundation` |
| `4341af5` | `Merge remote-tracking branch 'origin/main'` |

The remote history was preserved with a normal merge using
`--allow-unrelated-histories`. The remote had no files, so there were no
conflicts. No force-push or hard reset was used.

### Push and remote verification

- Push succeeded: `24aa52d..4341af5 main -> main`.
- Remote `main` resolved to
  `4341af52d8a631e0017c286b7b1b5a2ffc266d3f`.
- Remote tree contains 163 tracked files before the final documentation commit.
- Required project files verified on the remote:
  - `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`
  - `Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions`
  - `Assets/_Project/Settings/URP/REDMOON_URP.asset`
  - `Assets/_Project/Settings/URP/REDMOON_URP_Low.asset`
  - `Assets/_Project/Settings/URP/REDMOON_URP_Medium.asset`
  - `Assets/_Project/Settings/URP/REDMOON_URP_Ultra.asset`
  - `Assets/_Project/Settings/Audio/REDMOON_AudioMixer.mixer`
  - `Packages/manifest.json`
  - `Packages/packages-lock.json`
  - `ProjectSettings/ProjectVersion.txt`
  - `.gitignore`
  - `.gitattributes`
- Forbidden paths verified absent: `Library/`, `Temp/`, `Obj/`, `Logs/`,
  `UserSettings/`, `Build/`, `Builds/`, `MemoryCaptures/`,
  `Assets/Screenshots/`.
- `git lfs ls-files --all` returned no files, which is correct because there are
  no LFS-tracked assets yet.
- README, PROJECT_ARCHITECTURE and this development log are committed in the
  final documentation commit and pushed after this entry.

### Verification summary

| Check | Result |
|---|---|
| Git initialized | PASS |
| `main` branch established | PASS |
| Remote configured | PASS |
| Unity `.gitignore` verified | PASS |
| Git LFS installed and configured | PASS |
| LFS objects present | NOT APPLICABLE (no binary media yet) |
| Remote history inspected | PASS |
| Unrelated histories merged without force | PASS |
| Local commits created | PASS |
| GitHub push | PASS |
| Remote project files verified | PASS |
| Remote `Library` / `Temp` absent | PASS |
| GitHub CLI installed | NOT INSTALLED (not required; Git used) |
| Windows IL2CPP support | NOT INSTALLED (Mono development builds used) |

### Known limitations

- The remote history initially contained only an empty commit; it is preserved
  in the merge history rather than rewritten.
- No Git LFS objects exist until the first binary source asset is added.
- GitHub CLI is not installed; remote inspection was performed with Git.
- The final documentation commit and its remote verification are reported in
  the Phase 1.5 final report.

---

## Template for Future Entries

```
## YYYY-MM-DD — Phase X.Y: Title

**Status:** In progress / Complete

### Implemented
- ...

### Changed
- ...

### Verified
- ...

### Known issues
- ...
```
