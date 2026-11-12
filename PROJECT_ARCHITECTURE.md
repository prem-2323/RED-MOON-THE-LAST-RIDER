# RED MOON: THE LAST RIDER — Project Architecture

**Document status:** Phase 5.1 complete (Environment Foundation, Mountain Terrain & Road Spline, Player/Horse Integration)  
**Engine:** Unity 6.6 (`6000.6.8f1`)  
**Pipeline:** URP `17.6.0`  
**Input:** Unity Input System `1.20.1`  
**Target:** Windows x64 (Mono development builds)

---

## 1. Architectural Principles

1. **Domain separation.** Gameplay systems live in dedicated assemblies. A
   system may depend on `RedMoon.Core`, but domain assemblies must not depend on
   each other without an explicit architectural decision.
2. **One input source of truth.** All bindings live in a single Input Actions
   asset. Gameplay reads input through `RedMoonInput`, never directly from
   `Keyboard.current`, `Gamepad.current` or legacy `Input`.
3. **Data over hard references.** Prefer ScriptableObjects and prefabs for
   shared configuration. Avoid scene-to-scene direct references.
4. **Composition over inheritance.** Use small components and interfaces rather
   than deep MonoBehaviour hierarchies.
5. **Everything version-controlled.** Assets, `.meta` files, project settings
   and documentation are committed together. Generated folders are ignored.
6. **No hidden magic.** Every automated action must be reproducible, documented
   and verified in the Unity console.

---

## 2. Folder Architecture

```
Assets/
  DefaultVolumeProfile.asset                  URP-generated default volume profile
  UniversalRenderPipelineGlobalSettings.asset URP-generated global settings
  _Project/
    Art/
      Characters/
      Creatures/
      Environment/
      Props/
      Materials/
      Textures/
      Animations/
      VFX/
    Audio/
      Music/
      Ambient/
      SFX/
      Voice/
    Prefabs/
      Characters/
      Enemies/
      Environment/
      Props/
      Effects/
      UI/
    Scenes/
      Bootstrap/
      MainMenu/
      World/
      Levels/
        Level01/ ... Level15/
    Scripts/
      Core/
        Input/
      Player/
      Horse/
      Camera/
      Combat/
      Enemies/
      AI/
      Missions/
      Inventory/
      Progression/
      World/
      Audio/
      UI/
      SaveSystem/
      Utilities/
      Editor/
    Settings/
      URP/
        REDMOON_Renderer.asset
        REDMOON_URP.asset
        REDMOON_URP_Low.asset
        REDMOON_URP_Medium.asset
        REDMOON_URP_Ultra.asset
      Input/
        REDMOON_InputActions.inputactions
      Quality/
      Audio/
        REDMOON_AudioMixer.mixer
      Camera/
        Camera_Config.asset
    UI/
      Menus/
      HUD/
      Inventory/
      Map/
      Missions/
```

### Folder responsibilities

| Folder | Responsibility |
|---|---|
| `Art/Characters` | Player and NPC character art, rigs, materials and textures. |
| `Art/Creatures` | Horror creature art and supporting assets. |
| `Art/Environment` | Terrain layers, rocks, foliage, architecture and set dressing. |
| `Art/Props` | Interactive and decorative props. |
| `Art/Materials` | Shared materials and material variants. |
| `Art/Textures` | Shared texture atlases, masks and detail maps. |
| `Art/Animations` | Animation clips, controllers and timeline assets. |
| `Art/VFX` | Particle systems, shaders, VFX graphs and textures. |
| `Audio/*` | Music, ambience, sound effects and voice lines. |
| `Prefabs/*` | Reusable runtime objects grouped by purpose. |
| `Scenes/Bootstrap` | Startup and persistent-system scenes. |
| `Scenes/MainMenu` | Front-end and menu scenes. |
| `Scenes/World` | Open-world streaming and persistent world scenes. |
| `Scenes/Levels/LevelNN` | Individual mission or level scenes. |
| `Scripts/Core` | Shared primitives, input, service interfaces and utilities. |
| `Scripts/Editor` | Editor-only tools and asset builders. |
| `Settings/URP` | Render pipeline, renderer and quality-tier pipeline assets. |
| `Settings/Input` | Input Actions and control-scheme assets. |
| `Settings/Quality` | Reserved for future quality presets beyond the URP assets. |
| `Settings/Audio` | Audio mixer and future mixer snapshots. |
| `Settings/Camera` | Camera configuration and presets. |
| `UI/*` | UI documents, stylesheets, sprites and prefabs. |

The two URP-generated assets at the `Assets/` root are intentionally left in
their generated locations. They are referenced by GUID and should not be moved
without revalidating the pipeline and a player build.

---

## 3. Assembly Definition Strategy

Each gameplay domain owns an assembly definition. This keeps compile times
predictable and prevents accidental coupling.

| Assembly | Folder | References | Platform |
|---|---|---|---|
| `RedMoon.Core` | `Scripts/Core` | `Unity.InputSystem` | All |
| `RedMoon.Player` | `Scripts/Player` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Horse` | `Scripts/Horse` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Camera` | `Scripts/Camera` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Combat` | `Scripts/Combat` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Enemies` | `Scripts/Enemies` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.AI` | `Scripts/AI` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Missions` | `Scripts/Missions` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Inventory` | `Scripts/Inventory` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Progression` | `Scripts/Progression` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.World` | `Scripts/World` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Audio` | `Scripts/Audio` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.UI` | `Scripts/UI` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.SaveSystem` | `Scripts/SaveSystem` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Utilities` | `Scripts/Utilities` | `RedMoon.Core`, `Unity.InputSystem` | All |
| `RedMoon.Editor` | `Scripts/Editor` | `RedMoon.Core`, `Unity.InputSystem`, URP runtime | **Editor only** |

### Dependency rules

- `RedMoon.Core` must never reference a domain assembly.
- Domain assemblies may reference `RedMoon.Core` and the Input System.
- Cross-domain communication should use events, interfaces or a service
  registered in `RedMoon.Core`.
- Editor assemblies are excluded from player builds.
- Do not place runtime scripts in `Scripts/Editor`.
- Do not add `UnityEditor` references to runtime assemblies.

### Verified state

- 16 unique assembly names.
- No circular dependencies.
- No runtime assembly references `UnityEditor`.
- No runtime assembly references `RedMoon.Editor`.
- `RedMoon.Editor` uses `includePlatforms: ["Editor"]`.
- The Phase 1.4 build initially exposed a malformed editor `includePlatforms`
  string; it was corrected to an array and the player build then succeeded.
- Domain assemblies are empty scaffolding and intentionally produce build
  warnings until their first scripts are added.

---

## 4. Tags, Layers and Physics

### Tags

Built-in tag `Player` is used for the player. Custom tags:

- `Horse`
- `Enemy`
- `Interactable`
- `Environment`

Tags identify gameplay objects. Layers control physics, rendering and raycast
filtering; do not substitute one for the other.

### Layers

| Index | Layer | Purpose |
|---:|---|---|
| 0 | `Default` | Unity default. |
| 1 | `TransparentFX` | Unity built-in. |
| 2 | `Ignore Raycast` | Unity built-in. |
| 3 | — | Reserved / unused. |
| 4 | `Water` | Unity built-in. |
| 5 | `UI` | Unity built-in. |
| 6–7 | — | Reserved / unused. |
| 8 | `Player` | Player character colliders and root. |
| 9 | `Enemy` | Hostile characters and creatures. |
| 10 | `Horse` | Horse colliders and mount. |
| 11 | `Interactable` | Doors, pickups, triggers and props. |
| 12 | `Environment` | Static world geometry and props. |
| 13 | `Ground` | Terrain and walkable ground. |
| 14 | `Projectile` | Arrows, thrown objects and future projectiles. |
| 15–31 | — | Reserved for future systems. |

### Collision policy

The full physics matrix is preserved by default. Only three self-collision
pairs are disabled:

| Pair | State | Reason |
|---|---|---|
| `Interactable` ↔ `Interactable` | Disabled | Prevents overlapping trigger-only interactables from generating unnecessary contacts. |
| `Ground` ↔ `Ground` | Disabled | Terrain and ground pieces do not need to collide with each other. |
| `Projectile` ↔ `Projectile` | Disabled | Projectiles should not collide with other projectiles. |

All important character, horse, enemy, environment, interactable and projectile
cross-collisions remain enabled. Owner-based projectile filtering must be
implemented in gameplay code; the collision matrix is only a broad phase.

Physics baseline:

- Gravity: `(0, -9.81, 0)`.
- Simulation: `FixedUpdate`.
- Auto-sync transforms: off.
- Queries hit triggers: on.
- Default contact offset: `0.01`.
- Solver iterations: 6 / 1 velocity iteration.

---

## 5. Input Architecture

### Asset

`Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions`

Verified contents:

- 5 action maps.
- 31 actions.
- 80 bindings.
- 2 control schemes.

### Control schemes

- `KeyboardMouse`
- `Gamepad`

### Action maps and actions

| Map | Actions |
|---|---|
| `Player` | Move, Look, Jump, Sprint, Mount, Interact, Dodge, Attack, Block, Magic, ToggleInventory, ToggleMap, Whistle |
| `Horse` | Move, Look, Jump, Sprint, Dismount |
| `Combat` | Attack, Block, Dodge, Magic |
| `Camera` | Look, Zoom, SwitchShoulder |
| `UI` | Navigate, Submit, Cancel, ToggleInventory, ToggleMap, Pause |

### Runtime access

`RedMoon.Core.Input.RedMoonInput` wraps the asset and exposes typed
`InputAction` properties plus `ReadMove()`, `ReadLook()` and `ReadHorseMove()`.
It also controls map enabling:

- `EnableGameplay()` — Player, Horse, Combat and Camera maps.
- `DisableGameplay()` — disables the gameplay maps.
- `EnableUI()` / `DisableUI()` — controls the UI map.
- `Dispose()` — disables everything.

The class contains no gameplay logic. Player, horse, camera, combat and UI
systems are responsible for interpreting input.

### Adding a new action

1. Open the Input Actions asset.
2. Add the action to the correct map.
3. Add keyboard/mouse and gamepad bindings with the matching scheme group.
4. Add a typed property to `RedMoonInput` if gameplay must consume it.
5. Do not read devices directly from gameplay code.

---

## 5b. Camera System Architecture

### Design Philosophy
- **Dedicated Custom C# Motor:** Standardized custom third-person orbital camera motor (`CameraController.cs` in `RedMoon.Camera` assembly). No external Cinemachine dependencies.
- **World-Space Independence:** Operates independently from character rotation in world space, eliminating orientation feedback loops. Injects its transform into `PlayerController.SetCamera(transform)` for camera-relative steering.
- **Execution Timing:** Updates strictly in `LateUpdate()` to ensure player locomotion and root motion updates are finalized before camera placement.

### Configuration (`CameraConfig.cs`)
Tunable ScriptableObject parameters at `Assets/_Project/Settings/Camera/Camera_Config.asset`:
- **Framing & Offset:** Focus offset `(0, 1.4, 0)`, right shoulder `+0.45m`, left shoulder `-0.45m`, shoulder damping `4.0 m/s`.
- **Distance & Zoom:** Default resting distance `3.2m`, min distance `1.5m`, max distance `5.0m`, zoom speed `2.0 m/s`.
- **Orbit & Sensitivity:** Yaw sensitivity `120°/s`, pitch sensitivity `100°/s`, pitch clamping `[-35°, 65°]`.
- **Damping:** Position follow smoothing `0.08s`, rotation smoothing `0.04s`.
- **Collision & Obstruction:** SphereCast radius `0.2m`, safety padding `0.15m`, layers `Environment` (12) and `Ground` (13).
- **Dynamic FOV:** Default FOV `60°`, sprint FOV `64°`, blend speed `3.0 /s`.

### Collision & Obstruction Avoidance
- **SphereCast Line-of-Sight Query:** Casts a sphere along the view vector from camera pivot to resting distance against `Environment` (12) and `Ground` (13) layers with `QueryTriggerInteraction.Ignore`.
- **Asymmetric Smoothing:** Rapid retraction (`Mathf.MoveTowards` at 25 m/s) when obstructions are detected to prevent clipping; smooth recovery (`Mathf.SmoothDamp` over 0.2s) when clear.
- **Lateral Wall Safety:** Performs a lateral SphereCast before applying shoulder offsets to ensure camera pivot never clips through walls when standing sideways against obstacles.
- **Zero GC Allocations:** All raycast operations use non-allocating structs and queries.

### Dynamic Features
- **Shoulder Switching:** Driven by canonical `SwitchShoulder` input action (`<Keyboard>/c` and `<Gamepad>/rightStickPress`). Seamlessly transitions between right and left shoulders at 4.0 m/s (225ms transition).
- **Dynamic FOV Modulation:** Reads `PlayerController.CurrentState == PlayerState.Sprinting`. Exponentially blends from `60°` to `64°` during sprints, restoring to `60°` when sprinting ceases.

---

## 5c. Horse System Architecture

### Design Philosophy
- **Independent Gameplay Entity:** Autonomous quadruped locomotion motor (`HorseController.cs` in `RedMoon.Horse` assembly) driven by `CharacterController` physics, independent of player state.
- **Configurable ScriptableObject Core:** Centralized tuning at `Assets/_Project/Settings/Horse/Horse_Config.asset`.
- **4-Tier Locomotion Model:** Walk (2.5 m/s), Trot (5.0 m/s), Gallop (9.0 m/s), Sprint (13.5 m/s) with realistic acceleration curves (8 m/s²), deceleration (12 m/s²), turning (160°/s) and slope projection.
- **Explicit 13-State Machine (`HorseStateMachine.cs`):** Idle, Walking, Trotting, Galloping, Sprinting, Turning, Jumping, Falling, Landing, Mounting, Mounted, Dismounting, Dead.
- **Seamless Mounting & Rider Attachment (`HorseMountController.cs`):**
  - Proximity detection via `HorseInteraction.cs` (2.5m range).
  - Suspends Player CharacterController, PlayerInputReader and PlayerController smoothly without destroying or duplicating the player GameObject.
  - Parents rider to `RiderSocket` on the horse.
  - Automatically redirects camera focus to `RiderFocusPoint`.
  - Enables dedicated `Horse` input action map.
- **Safe Dismount Validation:** Evaluates left and right dismount sockets using obstacle collision SphereCasts and downward ground Raycasts to ensure the player never clips into geometry or falls into voids.
- **Camera Integration:** Seamlessly switches target between player and horse focus without teleportation snaps. Supports mouse look, gamepad look, shoulder switching and dynamic sprint FOV while mounted.
- **Test Architecture:** Includes `HorseTestHarness.cs` validating all 29 automated QA criteria and immediate-mode debug UI (`HorseDebugUI.cs`).

---

## 5d. Environment & World Architecture

### Design Philosophy
- **Dark Mountain Wilderness:** Atmospheric dark fantasy mountain valley designed for continuous horse riding and exploration, sculpted with multi-octave Perlin and ridge noise.
- **Target Hardware Optimization (RTX 3050 4GB VRAM):**
  - Terrain dimensions: $500\text{m} \times 120\text{m} \times 500\text{m}$.
  - Heightmap resolution: $513 \times 513$ (cell size $\approx 0.98\text{m}$).
  - Splatmap alphamap resolution: $512 \times 512$.
  - 3 URP TerrainLayers with $1024 \times 1024$ Albedo and Normal textures, low memory footprint.
  - Shadow distance: 55m, single directional moonlight + dark slate ambient trilight.
- **Winding Mountain Pass Road (`RoadSpline.cs`):**
  - Continuous Catmull-Rom spline with 10 waypoints spanning $507.1\text{m}$ from South-West overlook $(70, 16, 70)$ to North-East pass $(420, 18.5, 430)$.
  - Road width: $5.0\text{m}$ with $3.0\text{m}$ soft shoulder transition.
  - Procedural road grading: Terrain under road centerline ($d \le 2.5\text{m}$) is mathematically graded to road spline elevation with gentle $0.05\text{m}$ crown. Shoulders ($2.5\text{m} < d \le 5.5\text{m}$) use Hermite cubic blending. Guaranteed zero floating road sections and zero clipping cliffs.
  - Maximum slope anywhere on the road is $2.2^\circ$ (well within horse $45^\circ$ and player $50^\circ$ CharacterController limits).
  - Splatmap paints 100% road texture along corridor, softly feathering into soil and cliff rock.
- **Splatmap & Terrain Layer Strategy:**
  - **Layer 0 (`TL_DarkSoil_DeadGrass`):** Valley floor soil and dead grass ($12\text{m}\times 12\text{m}$ tiling).
  - **Layer 1 (`TL_MountainCliff_Rock`):** Fractured slate/basalt cliff rock ($16\text{m}\times 16\text{m}$ tiling) auto-blended on slopes $> 22^\circ$ and summits $> 55\text{m}$.
  - **Layer 2 (`TL_MountainRoad_Dirt`):** Packed road earth with pebble gravel and cart wheel ruts ($8\text{m}\times 8\text{m}$ tiling).
- **Environment Blockouts & Boundaries:**
  - Natural perimeter mountain walls ($95\text{m} - 110\text{m}$) form impassable outer boundary barriers.
  - 6 Low-poly silhouette rock spires / crags on Layer 12 (`Environment`) with `MeshCollider` components frame the mountain skyline and valley entrance defiles.
- **Physics & Layer Allocation:**
  - Terrain GameObject on Layer 13 (`Ground`), Tag `Environment`. Attached `TerrainCollider`.
  - RoadSpline GameObject on Layer 13 (`Ground`), Tag `Environment`.
  - Environment Blockouts on Layer 12 (`Environment`), Tag `Environment`.
  - CameraController SphereCast detects both Layer 12 (`Environment`) and Layer 13 (`Ground`) (`collisionLayers: 12288`).
- **Test Architecture:**
  - Includes `EnvironmentTestRunner.cs` in `RedMoon.Editor` validating all 12 QA requirements.
  - Active development scene: `Assets/_Project/Scenes/Development/EnvironmentTest.unity`.

---

## 6. Rendering and Quality

### Baseline

- Color space: Linear.
- HDR: enabled.
- Color grading: HDR.
- Renderer: `REDMOON_Renderer`, Forward rendering, no renderer features.
- Camera depth texture: enabled.
- Camera opaque texture: disabled.
- Additional-light shadows: disabled on all tiers.
- Soft shadows: enabled from Medium upward.
- MSAA: disabled except Ultra.
- Render scale: tier-dependent.
- Additional per-object lights: tier-dependent.
- Quality default for Windows standalone: **High**.

Forward rendering is retained because the current Bootstrap scene has no dense
lighting workload and there is no profiling evidence that Forward+ would be
cheaper or visually necessary. Re-evaluate Forward+ when the first playable
environment blockout is profiled.

### Quality tiers

| Tier | URP asset | Render scale | MSAA | Main shadow | Cascades | Shadow distance | Add. lights | Soft shadows |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Low | `REDMOON_URP_Low` | 0.80 | Off | 1024 | 1 | 30 m | 0 | Off |
| Medium | `REDMOON_URP_Medium` | 0.90 | Off | 2048 | 2 | 40 m | 2 | On |
| High | `REDMOON_URP` | 1.00 | Off | 2048 | 2 | 55 m | 4 | On |
| Ultra | `REDMOON_URP_Ultra` | 1.00 | 4× | 4096 | 4 | 70 m | 8 | On |

The four required tiers use distinct URP pipeline assets sharing the preserved
`REDMOON_Renderer`. `Very Low` and `Very High` are compatibility fallbacks,
mapped to the Low and Ultra assets respectively. All six profiles have distinct
quality settings for shadow distance, pixel light budget, LOD bias,
anisotropic filtering and particle raycast budget.

### Hardware mapping

- Lower hardware: **Low**.
- Entry-level discrete GPU: **Medium**.
- RTX 3050 Laptop 4 GB: **High** (default), with Medium as a fallback after
  profiling.
- Higher hardware: **High** or **Ultra**.

Do not activate expensive rendering features without measuring the impact and
updating this document.

---

## 7. Audio Architecture

### Mixer

`Assets/_Project/Settings/Audio/REDMOON_AudioMixer.mixer`

Routing hierarchy:

```
Master
├── Music
├── Ambience
├── SFX
├── Voice
└── UI
```

No audio clips are imported and no AudioSources exist yet. The mixer is a
foundation for future sound design.

### Spatial conventions

| Content | Bus | Spatial mode | Rolloff |
|---|---|---|---|
| UI clicks, menu music, notifications | `UI` / `Music` | 2D (`spatialBlend = 0`) | None |
| Music beds | `Music` | 2D | None |
| Ambience beds | `Ambience` | 2D | None |
| Positional ambience emitters | `Ambience` | 3D | Logarithmic, large radius |
| World SFX | `SFX` | 3D | Logarithmic, 1–3 m min, 25–60 m max |
| Voice / creature vocalisations | `Voice` | 3D | Logarithmic, 15–25 m max |
| Narration and menu dialogue | `Voice` | 2D | None |

Additional rules:

- Exactly one `AudioListener` belongs on the Main Camera.
- Set `AudioSource.outputAudioMixerGroup` on every source.
- Use `spatialBlend = 1` for world sources and `0` for 2D sources.
- Prefer logarithmic rolloff for natural attenuation. Use linear rolloff only
  for deliberate dialogue or scripted fades.
- Keep world SFX `dopplerLevel` at `0`–`0.5` unless a fast source needs it.
- Use mixer snapshots for tension, muffling underwater/indoors, and horror
  transitions.
- Keep initial group volumes at 0 dB; balance them in context once audio assets
  exist.
- Global audio baseline: 48 kHz, 512 virtual voices, 32 real voices, rolloff
  scale `1`, Doppler factor `1`.

---

## 8. Build Configuration

| Setting | Value |
|---|---|
| Target | `StandaloneWindows64` |
| Architecture | x86_64 |
| Scripting backend | Mono (`Mono2x`) |
| API compatibility | .NET Standard 2.0 |
| Development build | Enabled for current validation |
| Script debugging | Disabled |
| Profiler connection | Disabled |
| Company | `RED MOON` |
| Product | `RED MOON: THE LAST RIDER` |
| Bundle version | `0.1.0` |
| Application identifier | `com.redmoon.lastrider` |
| Startup scene | `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` (index 0) |
| Build output | `Builds/WindowsDev/REDMOON.exe` (ignored) |

Windows IL2CPP support is not installed. Do not switch the project to IL2CPP
until the build module is installed and the build is revalidated.

---

## 9. Naming Conventions

### Scripts and types

- Classes, structs, enums and interfaces: `PascalCase`.
- Interfaces: `I` prefix (`IDamageable`).
- Private fields: `_camelCase`.
- Constants: `PascalCase`.
- One public type per file; the file name matches the public type.
- Namespace mirrors the domain (`RedMoon.Player`, `RedMoon.Combat`).

### Assets

| Asset type | Convention | Example |
|---|---|---|
| Scene | `PascalCase` | `Bootstrap.unity`, `Level01.unity` |
| Prefab | `PascalCase` | `Player_Rider.prefab` |
| Material | `M_` | `M_Mud_Wet` |
| Texture | `T_` + usage | `T_Rock_Albedo` |
| Mesh | `SM_` | `SM_Gate_Old` |
| Animation clip | `A_` | `A_Horse_Gallop` |
| Animator controller | `AC_` | `AC_Player` |
| Audio | `SFX_`, `AMB_`, `MUS_`, `VO_` | `SFX_Wolf_Howl` |
| ScriptableObject | `SO_` or domain suffix | `SO_WeatherProfile` |

### Scenes

- `Bootstrap` — startup and persistent systems.
- `MainMenu` — front-end.
- `World` — persistent open-world scene.
- `Level01` … `Level15` — mission/level scenes.
- Never rely on scene names for gameplay lookup; use stable IDs or GUIDs.

---

## 10. Git and Version Control

- Integration branch: `main`.
- Commit `Assets/`, `ProjectSettings/`, `Packages/manifest.json`,
  `Packages/packages-lock.json`, documentation, `.gitignore`, `.gitattributes`
  and all Unity `.meta` files.
- Ignore `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build/`,
  `Builds/`, `MemoryCaptures/`, generated screenshots and IDE caches.
- Use Git LFS for binary source media:
  `*.psd`, `*.tif`, `*.tiff`, `*.blend`, `*.fbx`, `*.wav`, `*.mp3`, `*.mp4`,
  `*.mov`.
- Do not put Unity scenes, scripts, materials, prefabs or `.meta` files in LFS.
- Never force-push or reset `main` destructively.
- Review `git diff` before every commit.
- Use short, imperative commit messages: `Add input architecture`, not
  `added stuff`.
- The repository includes `.gitattributes` for LFS and a Unity `.gitignore`.

---

## 11. Documentation and Change Control

- Update `DEVELOPMENT_LOG.md` for every implementation session.
- Update this document when architecture or conventions change.
- Update `README.md` when setup or project status changes.
- Every change should be verified in the Unity console and, where relevant,
  through Unity MCP read-only checks.
- Record failed tests and non-blocking warnings honestly; do not claim a test
  passed unless it was executed.
