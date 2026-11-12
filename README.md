# RED MOON: THE LAST RIDER

A cinematic, realistic 3D open-world horror adventure built in Unity 6.

> **Status:** Phase 1.5 — project configuration, quality assurance and repository foundation complete. No gameplay content has been implemented yet.  
> **Engine:** Unity 6.6 (`6000.6.8f1`)  
> **Render pipeline:** Universal Render Pipeline (URP) `17.6.0`  
> **Input:** Unity Input System `1.20.1`  
> **Platform:** Windows 11, 64-bit  
> **Automation:** Unity MCP `10.3.0`  
> **Repository:** <https://github.com/prem-2323/RED-MOON-THE-LAST-RIDER>  
> **Branch:** `main`

---

## Game Overview

RED MOON: THE LAST RIDER is a single-player horror adventure set in a vast,
hostile open world. The player travels on horseback through decaying landscapes,
investigates supernatural events, and survives encounters with creatures that
hunt by sound, scent and moonlight.

The project is designed around:

- A grounded, cinematic tone with deliberate pacing.
- A horseback traversal system built for long-distance exploration.
- Systemic horror rather than scripted jump scares.
- Strong visual direction driven by moonlight, fog and negative space.
- A scalable content pipeline suitable for a long-running production.

---

## Technology

| Component | Version |
|---|---|
| Unity Editor | `6000.6.8f1` (Unity 6.6) |
| Universal Render Pipeline | `17.6.0` |
| Input System | `1.20.1` |
| Unity MCP | `10.3.0` |
| Scripting backend | Mono (development) |
| API compatibility | .NET Standard 2.0 |
| Color space | Linear |
| Target platform | Windows x64 |

The project currently uses **Forward** rendering. Forward+ remains an option
for later dense-lighting scenes, but it has not been enabled without profiling
evidence.

---

## Hardware Requirements

### Development target

| Component | Target |
|---|---|
| GPU | NVIDIA RTX 3050 Laptop, 4 GB VRAM |
| CPU | AMD Ryzen 7 7435HS (or equivalent) |
| RAM | 16 GB |
| Storage | 25 GB free for Unity, packages and development builds |
| Display | 1920 × 1080 |
| OS | Windows 11 x64 |

### Player-facing expectations

The current build is a development foundation with no gameplay content, so
frame-rate targets are not yet meaningful. The working quality mapping is:

- **Low** — lower-end hardware and conservative rendering.
- **Medium** — entry-level discrete GPUs.
- **High** — default for the RTX 3050 development target.
- **Ultra** — higher-end GPUs and future visual validation.

`Very Low` and `Very High` remain as compatibility profiles alongside the four
official tiers. See `PROJECT_ARCHITECTURE.md` for exact rendering values.

---

## Installation

### Prerequisites

1. Install **Unity Hub**.
2. Install **Unity 6.6 `6000.6.8f1`** with:
   - Windows Build Support (Mono) — required for current development builds.
   - Microsoft Visual Studio or another C# editor.
3. Install **Git** and **Git LFS** (`git lfs version` should report `3.8.0` or
   newer).
4. Optional: install Unity MCP `10.3.0` if editor automation is required.

Windows IL2CPP build support is **not currently installed**. Development
builds use Mono. Install IL2CPP before producing a release build.

### Clone

```powershell
git clone https://github.com/prem-2323/RED-MOON-THE-LAST-RIDER.git
cd RED-MOON-THE-LAST-RIDER
git lfs install --local
```

### Open

1. Open Unity Hub.
2. Add the cloned project folder.
3. Open it with Unity `6000.6.8f1`.
4. Allow Unity to resolve packages on first import.
5. Open `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`.
6. Press **Play**.

The Bootstrap scene is build index 0 and contains only the Main Camera and
Directional Light. There is no player, terrain, horse, enemy or combat content
yet.

### Build

- Target: **Windows x64**.
- Build Settings scene: `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`
  (index 0, enabled).
- Development build: enabled for current validation.
- Output used during Phase 1.4:
  `Builds/WindowsDev/REDMOON.exe` (ignored by Git).

The verified Phase 1.4 development build completed with **0 errors**. The 14
warnings are expected empty-domain assembly notices; see
`DEVELOPMENT_LOG.md`.

---

## Repository Layout

```
Assets/
  _Project/
    Art/            Characters, creatures, environment, props, materials,
                    textures, animations and VFX.
    Audio/          Music, ambience, sound effects and voice.
    Prefabs/        Characters, enemies, environment, props, effects and UI.
    Scenes/         Bootstrap, main menu, world and level scenes.
    Scripts/        Domain assemblies (Core, Player, Horse, Camera, Combat,
                    Enemies, AI, Missions, Inventory, Progression, World,
                    Audio, UI, SaveSystem, Utilities, Editor).
    Settings/       URP tiers, input actions, quality and audio mixer.
    UI/             Menus, HUD, inventory, map and mission UI.
```

Generated root-level URP assets (`Assets/UniversalRenderPipelineGlobalSettings.asset`
and `Assets/DefaultVolumeProfile.asset`) are intentionally kept at their
URP-generated locations. See `PROJECT_ARCHITECTURE.md` for the full structure,
tags, layers, collision policy and assembly rules.

---

## Input

The canonical asset is:

`Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions`

It contains five maps and two control schemes:

| Map | Purpose |
|---|---|
| `Player` | On-foot movement, interaction, combat and exploration. |
| `Horse` | Mounted movement and dismounting. |
| `Combat` | Shared combat actions. |
| `Camera` | Look, zoom and shoulder switching. |
| `UI` | Navigation, submit, cancel, inventory, map and pause. |

Control schemes: `KeyboardMouse`, `Gamepad`.

Runtime access is centralized in
`Assets/_Project/Scripts/Core/Input/RedMoonInput.cs`. Gameplay code must not
read devices directly. No gameplay behaviour is attached to the asset yet.

---

## Rendering and Quality

The required quality tiers are **Low**, **Medium**, **High** and **Ultra**.
Each uses a distinct URP pipeline asset:

| Tier | URP asset | Render scale | MSAA | Main shadow | Cascades | Additional lights |
|---|---|---:|---:|---:|---:|---:|
| Low | `REDMOON_URP_Low` | 0.80 | Off | 1024 | 1 | 0 |
| Medium | `REDMOON_URP_Medium` | 0.90 | Off | 2048 | 2 | 2 |
| High | `REDMOON_URP` | 1.00 | Off | 2048 | 2 | 4 |
| Ultra | `REDMOON_URP_Ultra` | 1.00 | 4× | 4096 | 4 | 8 |

High is the default for Windows standalone. Soft shadows are enabled from
Medium upward; additional-light shadows are disabled on every tier. Camera
depth texture is enabled; opaque texture is disabled.

`Very Low` and `Very High` are retained as compatibility profiles, mapped to
the Low and Ultra pipeline assets respectively.

---

## Audio

The project includes a mixer foundation at:

`Assets/_Project/Settings/Audio/REDMOON_AudioMixer.mixer`

Routing groups: **Master → Music, Ambience, SFX, Voice, UI**. No audio clips
have been imported and no AudioSources exist yet.

Documented conventions:

- UI and music use 2D audio (`spatialBlend = 0`).
- World SFX and voice use 3D audio (`spatialBlend = 1`).
- World SFX use logarithmic rolloff, 1–3 m minimum distance and 25–60 m
  maximum distance.
- Voice uses logarithmic rolloff with a shorter 15–25 m range; narration and
  menu dialogue use 2D Voice-group sources.
- Ambience can be a 2D bed or a large-radius 3D emitter; horror layers should
  be routed through Ambience and controlled with mixer snapshots.
- Exactly one AudioListener belongs on the Main Camera.
- Use `AudioSource.outputAudioMixerGroup` to route every source.

Full conventions are in `PROJECT_ARCHITECTURE.md`.

---

## Git Workflow

The project uses `main` as the integration branch.

```powershell
git status
git add <reviewed-paths>
git commit -m "Short imperative message"
git pull --rebase origin main
git push origin main
```

Rules:

- Never force-push or reset `main` destructively.
- Review `git diff` before committing.
- Commit `Assets/`, `ProjectSettings/`, `Packages/manifest.json`,
  `Packages/packages-lock.json`, documentation and all `.meta` files.
- Do not commit `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build/`,
  `Builds/`, `MemoryCaptures/`, IDE caches or generated screenshots.
- Binary source-art, audio and video files use Git LFS:
  `*.psd`, `*.tif`, `*.tiff`, `*.blend`, `*.fbx`, `*.wav`, `*.mp3`, `*.mp4`,
  `*.mov`.
- Unity scenes, scripts, materials, prefabs and `.meta` files stay in normal
  Git, not LFS.

---

## Development Status

| Phase | Status |
|---|---|
| 1.1 Environment inspection | Complete |
| 1.2 Project initialization and URP | Complete |
| 1.3 Architecture, folders, assemblies and input | Complete |
| 1.4 Final configuration and QA | Complete |
| 1.5 Git, GitHub, LFS and documentation | Complete |

Phase 1.4 verified:

- 16 assembly definitions, no cycles and no runtime `UnityEditor` references.
- Input asset: 5 maps, 31 actions, 80 bindings, 2 control schemes.
- URP: 4 pipeline assets sharing the preserved `REDMOON_Renderer`.
- Tags and layers for Player, Enemy, Horse, Interactable, Environment, Ground
  and Projectile.
- Audio mixer with six groups.
- Windows x64 development build: 0 errors.
- Play Mode smoke test: clean console.
- Built-player smoke test: process launched and remained stable.

No player controller, horse controller, terrain, enemy AI, combat, missions or
save system has been implemented. Phase 2 requires explicit approval.

---

## Documentation

| Document | Purpose |
|---|---|
| `README.md` | Project overview, setup, build and workflow. |
| `PROJECT_ARCHITECTURE.md` | Final folders, assemblies, tags, layers, rendering and audio conventions. |
| `DEVELOPMENT_LOG.md` | Chronological implementation and verification record. |
