# CLAUDE.md — VRMod project notes

Persistent notes for AI sessions working on this codebase. Read this before making changes.

## Project identity

- **What:** Unofficial OpenXR rebuild of DrBibop's Risk of Rain 2 VR mod
- **Author:** Blowntobytes (GitHub: `Blowntobytes/RoR2VRMod`)
- **Current version:** 1.0.2 (first public release; version numbers restarted from the 2.22.x pre-releases)
- **Game target:** Risk of Rain 2 on Unity 2021.3.33f1 (current Steam build)
- **Framework:** BepInEx 5.4.x plugin + patcher, MonoMod On./IL. hooks, HookGen (MMHOOK_RoR2.dll)
- **VR stack:** Unity OpenXR 1.9.1, XR Management 4.4.0, XR CoreUtils 2.1.1, InputSystem 1.6.3
- **Plugin ID:** `com.DrBibop.VRMod` (kept for compatibility with mods that depend on it)

## Repository layout

```
RoR2VRMod/                  ← source code (C#)
  VRMod.cs                  ← plugin entry point (BepInPlugin)
  Inputs/
    Controllers.cs           ← VR controller polling loop (UpdateVRInputs), auto-sprint, recenter
    RewiredAddons.cs         ← Rewired CustomController definition, action-to-element maps
    ControllerGlyphs.cs      ← TMP sprite glyphs per controller type (standard/vive/wmr)
    LobbyNavigation.cs       ← character select lobby stick-navigation fix
    ActionAddons.cs           ← adds custom Rewired actions (UISubmitTertiary, etc.)
    SettingsTabs.cs           ← grip-driven settings tab switching
    MenuSubmitGuard.cs        ← one-press-per-activation for menu buttons
    InputTypes/               ← BaseInput subclasses: ButtonInput, HoldableButtonInput, etc.
  Camera/                    ← VR camera, stereo projection fix, recentering, vignette
  MotionControls/            ← hand tracking, per-survivor weapon animations
  Haptics/                   ← Shockwave / bHaptics suit support (optional DLLs)
  UI/                        ← HUD, UI pointer, UI fixes
  Settings/                  ← mod config (ModConfig.cs), settings panel addon
  LIV/                       ← LIV mixed-reality capture SDK (unchanged from original)
```

## Build toolchain

Builds run in a Linux cloud environment. No Visual Studio required.

```bash
# Compile
cd /root/build
/opt/pwsh/pwsh -File /root/tools/csc.ps1 @out/VRMod.dll.rsp

# Copy DLL into Thunderstore package
cp /root/build/out/VRMod.dll /root/build/pkg/plugins/VRMod.dll

# Package zip
cd /root/build/pkg && zip -r /root/build/Blowntobytes-VRMod-<VERSION>.zip .
```

- Response file: `/root/build/out/VRMod.dll.rsp` (all references, defines, resource embeds)
- Asset bundle: embedded as a raw manifest resource via `/resource:` in the .rsp
- Reference assemblies: `/root/build/libs/`, `/root/build/mmhook/`, `/root/build/il/`
- Package template: `/root/build/pkg/` (manifest.json, icon.png, plugins/, patchers/)

## Version release checklist

**Every fix or feature gets its own version number. Never reuse or amend a version that has been built.**

1. **Bump version** in `/root/build/pkg/manifest.json`
2. **Build** the DLL, copy to pkg, create the zip
3. **Deliver zip** to user's machine at `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\VRMod-unofficial-build\`
4. **Commit** the source changes with a message like `2.XX.Y: short description`
5. **Tag** the commit: `git tag v2.XX.Y`
6. **Create git bundle** (incremental from upstream): `git bundle create /root/build/RoR2VRMod-history-<VERSION>.bundle upstream/main..main v2.19.3 v2.19.4 ... v<VERSION>`
7. **Update** `Publish-ToGitHub.ps1` — set `$Version` to the new version
8. **Write release notes** in `release-notes-<VERSION>.md`
9. **Deliver** the bundle, publish script, and release notes to the user's VRMod-unofficial-build folder
10. User runs `Publish-ToGitHub.cmd` on their Windows machine to push to GitHub

**Never amend a commit that has already been tagged or shipped.** Create a new commit and a new version instead.

## Key technical patterns

### Rewired input system

The mod creates a Rewired `CustomController` with 33 elements (0–32). Each element maps to a physical button or axis via `BaseInput` subclasses in the `inputs` array. The input loop in `Controllers.UpdateVRInputs()` calls `UpdateValues()` on each input every frame, which calls `vrControllers.SetButtonValueById(elementId, value)`.

**Critical:** Rewired latches button-down edges the instant `SetButtonValueById` is called with `true`. You cannot set an element to true and then clear it in the same frame — the game will already have seen the press. To suppress an input conditionally, you must **skip the `UpdateValues()` call entirely** for that element, not clear it after the fact.

Action-to-element bindings are in `RewiredAddons.cs` (CreateUIMap / CreateGameplayMap). Glyph sprites are indexed by element ID in `ControllerGlyphs.cs`.

### Element ID reference (important ones)

| Element | Name | Physical button | Action |
|---------|------|----------------|--------|
| 15 | Info | Y (hold) | Scoreboard / profile (HoldableButtonInput) |
| 24 | Pause | Y (press) | Pause menu (ButtonInput) |
| 32 | Load | — (virtual) | UISubmitTertiary / ProperSave Load (lobby remap only) |

Y button (left hand, SecondaryButton) drives **both** elements 15 and 24 via separate input entries. When remapping Y in the lobby, both must be suppressed.

### TypeLoadException isolation

The CLR throws `TypeLoadException` before a method body executes if that method references types from a missing assembly. A try-catch inside the method cannot catch it. To safely reference optional types (e.g. ShockwaveController from ShockwaveManager.dll), isolate the reference into a separate factory method — the JIT only resolves the type when that specific method is called.

```csharp
// WRONG — TypeLoadException kills Init() before try-catch can fire
void Init() { try { new ShockwaveController(); } catch { } }

// RIGHT — JIT only resolves ShockwaveController when CreateShockwave() is called
void Init() { try { CreateShockwave(); } catch { } }
private static GenericHapticsController CreateShockwave() => new ShockwaveController();
```

### Lobby navigation

The game's character select screen ships buttons with `Navigation.Mode = None`, which makes them unreachable by stick when the input source is Gamepad. `LobbyNavigation.cs` periodically sweeps for these and sets them to `Automatic`. The `IsInLobby` property exposes whether the character select screen is active for use by the input remap logic.

### Menu input source

In `ControllerGlyphs.ChangedToCustom`, when the active controller switches to Custom (our VR controller), we set `currentInputSource = Gamepad`. This makes all menus navigable with the left stick and A/B buttons — no laser pointer needed. This applies everywhere: title screen, settings, and pause menu.

## User's environment

- **Headset:** Meta Quest 3 via Virtual Desktop (VDXR OpenXR runtime)
- **Friend's headset:** Oculus Rift CV1 (Meta/Oculus runtime)
- **Game install:** two Steam library paths (E: and C: drives)
- **Mod manager:** r2modman
- **Build output folder:** `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\VRMod-unofficial-build\`
- **r2modman profile:** `BitsySeekers1.1` (at `C:\Users\atyou\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\`)
- **Active mods alongside VRMod:** ProperSave 3.0.7, ProperSave_NoDeathDelete, SeekersPatcher, MiscFixes, RoR2BepInExPack

## Git conventions

- Remote `origin`: `https://github.com/Blowntobytes/RoR2VRMod.git` (user's fork)
- Remote `upstream`: `https://github.com/DrBibop/RoR2VRMod.git` (original mod)
- Branch: `main` only
- Commit message format: `<version>: <short description>` (e.g. `2.19.5: fix Y button firing both Load and Scoreboard in lobby`)
- Non-version commits: `chore: <description>` for cleanup
- Git bundles for GitHub publishing are incremental from `upstream/main` (not from previous versions), keeping them ~100 KB
- User's git identity: `Blowntobytes` / `147319775+Blowntobytes@users.noreply.github.com`

## Version history (2.18.1+)

| Version | Summary |
|---------|---------|
| 2.18.1 | One press = one menu activation; UISubmitAlt moved from A to X |
| 2.18.2 | Lobby grips jump between sides; recenter re-parks open menus |
| 2.18.3 | UI camera on Camera Offset node; lobby right grip geometric select |
| 2.18.4 | Revert to 2.18.1 base; lobby automatic navigation; grips jump sides |
| 2.18.5 | Submit is A only; lobby jump verification and shortcut logging |
| 2.18.6 | Bindings match glyph artwork (A=Submit, triggers=tabs, grips=submenus) |
| 2.18.7 | UIPageLeft/Right on triggers for character select navigation |
| 2.18.8 | Triggers drive page nav; grips unbound in menus; lobby helper nav-mode only |
| 2.18.9 | Settings-tab actions (UISubmenuLeft/Right) back on grips |
| 2.19.0 | Settings tabs via MoveHeaderLeft/Right from grip poll |
| 2.19.1 | Auto Sprint fix — send fresh press edges instead of holding |
| 2.19.2 | Cross-headset OpenXR support (Rift CV1, Index, Bigscreen Beyond) |
| 2.19.3 | Fix TypeLoadException hang when haptics DLLs are missing |
| 2.19.4 | ProperSave compatibility — Y triggers Load in lobby |
| 2.19.5 | Fix Y button flickering (skip elements in loop, don't clear after) |
| 2.19.6 | Constrain joystick navigation in lobby panels (superseded by 2.19.8) |
| 2.19.7 | Withdrawn build — did not start VR |
| 2.19.8 | Confine the left stick to one side of the character select screen |
| 2.19.9 | Stop the stick walking the highlight off the right edge of the character select screen; renamed to "Resurrected VRMod" |
| 2.19.10 | Character-select off-screen fix v2: keep only controls that project inside the visible screen (UI camera) |
| 2.19.11 | Character-select off-screen fix v3 (anchor kept; canvas world-bounds); removed right-stick-click recenter so ping works |
| 2.19.12 | Character-select off-screen fix v4: bound navigation to the UI camera's view frustum |
| 2.19.13 | Diagnostic build: log the lobby canvas/camera/control layout to find the real off-screen cause |
| 2.19.14 | Character-select off-screen fix (root-caused): build navigation only from controls inside the canvas's own local rect |
| 2.19.15 | Reverted the 2.19.14 filter (it did nothing); shrink the character-select canvas to ~53° so the rules panel fits in view |
| 2.19.17 | Diagnostic build: trace the actual selection (Lobby select: lines) - geometry fixes failed, so log exactly which control the highlight lands on |
