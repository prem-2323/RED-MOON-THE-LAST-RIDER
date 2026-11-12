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
- Final scene validation: entering Play Mode caused URP to add the required
  `UniversalAdditionalCameraData` component to the Bootstrap Main Camera. The
  scene was saved, and an incremental Windows development build revalidated the
  final scene state:
  - Errors: 0
  - Warnings: 14 (empty domain assemblies)
  - Duration: 8.24 seconds
  - Built-player smoke test repeated: process stayed alive, Input System and
    PhysX initialized, no exceptions. The process was terminated intentionally.
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
| Final post-save incremental build | PASS (0 errors) |
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
- `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` (URP camera data saved)
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

### Post-push documentation verification

- Documentation commit `c520a6c` pushed successfully:
  `4341af5..c520a6c main -> main`.
- Remote `main` resolved to
  `c520a6c52987a0ec259806b3b6be45c9da733c3b`.
- A fresh shallow clone from the public GitHub URL was audited.
- Remote tree contains 166 tracked files.
- `README.md`, `DEVELOPMENT_LOG.md`, `PROJECT_ARCHITECTURE.md`, `.gitignore`
  and `.gitattributes` are present.
- All required project files are present.
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build/`, `Builds/`,
  `MemoryCaptures/` and `Assets/Screenshots/` are absent.
- `git lfs ls-files` in the fresh clone returned no files; no binary source
  media has been committed yet.
- The local working tree is clean after the documentation commit.

### Known limitations

- The remote history initially contained only an empty commit; it is preserved
  in the merge history rather than rewritten.
- No Git LFS objects exist until the first binary source asset is added.
- GitHub CLI is not installed; remote inspection was performed with Git.
- The final post-verification documentation commit is recorded in the Phase 1.5
  final report.

---

## 2026-11-12 — Phase 2: Main Character Development

**Status:** Complete

### Implemented
- **Player Character Foundation (Stage 2.1):**
  - Created `PlayerConfig` ScriptableObject at `Assets/_Project/Settings/Player/Player_Config.asset` with tunable parameters for walk (2.2 m/s), run (4.8 m/s), sprint (7.5 m/s), jump height (1.25 m), gravity (-22 m/s²), air control (0.65), and ground check parameters.
  - Implemented `PlayerController` motor wrapping Unity's `CharacterController` (height 1.8m, radius 0.32m, center `(0, 0.9, 0)`).
  - Created deterministic `PlayerStateMachine` handling states: `Idle`, `Walking`, `Running`, `Sprinting`, `Jumping`, `Falling`, `Landing`.
  - Added `PlayerInputFrame` struct decoupling hardware input from motor logic.
  - Added `PlayerInputReader` binding runtime gameplay input through `RedMoonInput` and `REDMOON_InputActions.inputactions`.
- **Character Model & Sockets (Stage 2.2):**
  - Integrated humanoid rigged model `Player_Interim_Ninja.fbx` with CC0 license into `Assets/_Project/Art/Characters/Player/Models/`.
  - Configured `ModelImporter` to Humanoid with `CreateFromThisModel` Avatar.
  - Created `PlayerModelSocket` with `SwordSocket` attached directly to bone `RightHand` and `CloakRoot` attached to bone `Spine02`.
- **Locomotion & Movement (Stage 2.3):**
  - Implemented smooth acceleration/deceleration (`Vector3.MoveTowards`), air control multiplier, camera-relative directional steering, and smooth rotation (`Quaternion.RotateTowards`).
  - Implemented robust ground detection combining `CharacterController.isGrounded` and downward `Physics.CheckSphere` at `(0, 0.08, 0)` against `Ground` (Layer 13) and `Environment` (Layer 12).
- **Locomotion Animation (Stage 2.4):**
  - Configured `UniversalAnimationLibrary_Standard.fbx` with Humanoid Avatar for Mecanim retargeting.
  - Created `Player_AnimatorController.controller` at `Assets/_Project/Settings/Player/` with parameters `Speed`, `VerticalSpeed`, `Grounded`, `Turn`, `State`, `Jump`, `Land`.
  - Wired states: `Idle` (`Idle_Loop`), `Walk` (`Walk_Loop`), `Run` (`Jog_Fwd_Loop`), `Sprint` (`Sprint_Loop`), `Jump` (`Jump_Start`), `Fall` (`Jump_Loop`), `Land` (`Jump_Land`).
  - Implemented `PlayerAnimator` bridging `PlayerController` velocity, turn angle, and state transitions to the Animator.
- **Character Testing Harness & Verification (Stage 2.5):**
  - Created development test scene at `Assets/_Project/Scenes/Development/PlayerTest.unity`.
  - Created `PlayerTestDriver` component rendering on-screen telemetry GUI and logging state changes.
  - Created `Player_Interim.prefab` at `Assets/_Project/Prefabs/Characters/` assembling motor, visual model, sockets, input reader, and animator bridge.

### Verified
- Zero compiler errors, zero warnings across all assemblies.
- Executed Play Mode smoke test in `PlayerTest.unity`: console remained 100% clean.
- Verified ground detection, CharacterController grounding, and physics collision.
- Verified Bootstrap scene integrity (index 0 in Build Settings preserved).
- Unity MCP tools verified hierarchy, asset database, and live inspector state.

---

## 2026-11-12 — Phase 3.2: Camera Foundation & Configuration

**Status:** Complete

### Implemented
- **Assembly Normalization:** Validated `RedMoon.Camera.asmdef` and normalized `includePlatforms` to `[]`. Added `RedMoon.Camera` reference to `RedMoon.Editor.asmdef`.
- **Camera Configuration Data (`CameraConfig.cs`):** Created ScriptableObject in `Assets/_Project/Scripts/Camera/` under namespace `RedMoon.Camera`. Tunable parameters:
  - Focus Offset: `(0, 1.4, 0)`
  - Shoulder offsets: Default right `0.45m`, left `-0.45m`, transition damping `4.0 /s`
  - Distance & Zoom: Default `3.2m`, min `1.5m`, max `5.0m`, zoom speed `2.0 m/s`
  - Orbit & Rotation: Yaw sensitivity `120°/s`, pitch sensitivity `100°/s`, pitch range `[-35°, 65°]`, invert Y `false`
  - Damping: Position smoothing `0.08s`, rotation smoothing `0.04s`
  - Collision & Obstruction: SphereCast radius `0.2m`, padding `0.15m`, layers `Environment` (12) and `Ground` (13)
  - FOV Modulation: Default `60°`, sprint `64°`, blend speed `3.0 /s`
- **Configuration Asset:** Generated canonical asset `Assets/_Project/Settings/Camera/Camera_Config.asset`.
- **PlayerController Dependency Injection:** Added `CameraTransform` property and `SetCamera(Transform targetCamera)` method to `PlayerController.cs`. Preserved safe automatic fallback to `Camera.main`. Completely decoupled from `RedMoon.Camera` to avoid circular assembly dependencies.
- **Editor Tooling:** Created `CameraPhase3Builder.cs` in `Assets/_Project/Scripts/Editor/` with menu item `RED MOON/Camera/Create Camera Config`. Disambiguated `UnityEngine.Camera` in `PlayerPhase2Builder.cs`.

### Verified
- C# compilation: 0 errors, 0 warnings.
- Play Mode regression in `PlayerTest.unity`: clean console (0 errors, 0 warnings).
- Play Mode regression in `Bootstrap.unity`: clean console (0 errors, 0 warnings), build index 0 preserved.
- Zero circular assembly dependencies between `RedMoon.Player` and `RedMoon.Camera`.

---

## 2026-11-12 — Phase 3.3: Third-Person Camera Motor & Orbit Implementation

**Status:** Complete

### Implemented
- **Orbital Camera Motor (`CameraController.cs`):** Created custom third-person camera controller in `Assets/_Project/Scripts/Camera/` within `RedMoon.Camera` assembly.
  - Reads `CameraLook` from `RedMoonInput` supporting both mouse and gamepad.
  - Computes orbital spherical coordinates around focus target (`target.position + config.focusOffset`).
  - Implements pitch limits clamping (`[-35°, 65°]`) and smooth rotation damping via `Mathf.SmoothDampAngle`.
  - Implements position smoothing via `Vector3.SmoothDamp` (`0.08s`).
  - Updates in `LateUpdate()` to guarantee player locomotion and root motion are finalized.
  - Automatically injects its transform into `PlayerController.SetCamera(transform)`.
  - Operates independently in world space, eliminating orientation feedback loops.
- **Automated Verification Harness (`CameraTestHarness.cs`):** Created runtime QA test harness in `Assets/_Project/Scripts/Camera/` testing mouse input, gamepad input, pitch clamping, target tracking, and player binding in Play Mode.
- **Scene Setup Automation (`CameraPhase3Builder.cs`):** Added `RED MOON/Camera/Setup Test Scene Camera` menu tool to configure `Main Camera` with `CameraController`, `CameraConfig`, and `CameraTestHarness` in `PlayerTest.unity`.

### Verified
- C# compilation: 0 errors, 0 warnings.
- Play Mode QA test harness execution in `PlayerTest.unity`:
  - PlayerController binding: PASS (`PlayerController.CameraTransform` correctly bound).
  - Mouse look input: PASS (ΔYaw=71.97°, ΔPitch=29.99°, ΔPos=3.99m).
  - Gamepad look input: PASS (ΔYaw=11.97°, ΔPitch=7.99°, ΔPos=0.78m).
  - Pitch limits clamping: PASS (strictly clamped within `[-35°, 65°]`).
  - World-space focus follow: PASS (focus point tracked displacement by 1.98m).
  - Overall harness result: `ALL PHASE 3.3 TESTS PASSED`.
- Regression check on `Bootstrap.unity`: 0 errors, 0 warnings in Play Mode; build index 0 preserved.

---

## 2026-11-12 — Package Integrity & Maintenance: AI Assistant Removal & Deprecation Fixes

**Status:** Complete

### Resolved Issues
- **Unity 6 AI Package Auto-Injection:**
  - Encountered `Failed to determine dll type for Packages/com.unity.ai.inference/Editor/ONNX/Google.Protobuf_Packed.dll` and related `com.unity.ai.assistant` errors.
  - Root cause: Unity 6.6 (`6000.6.8f1`) automatically introduced preview packages `com.unity.ai.assistant` (`2.21.0-pre.2`) and `com.unity.ai.inference` (`2.6.1`) with packed/obfuscated DLLs failing type determination on Windows Standalone.
  - Fix: Purged both packages from `Packages/manifest.json` and `Packages/packages-lock.json`. Forced full asset/package database refresh via MCP.
- **API Deprecation Cleanup (CS0618):**
  - Updated deprecated `Object.FindFirstObjectByType<T>()` to `Object.FindAnyObjectByType<T>()` in `PlayerTestDriver.cs` and `CameraTestHarness.cs`.

### Verified
- `Packages/manifest.json` clean with only project dependencies.
- Unity Editor console: 0 errors, 0 warnings.
- Git status: clean.

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
