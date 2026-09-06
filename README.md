# Resurrected VRMod — Risk of Rain 2 VR mod, unofficial build by Blowntobytes

Play Risk of Rain 2 in virtual reality with motion controls, on the **current Steam version of the game**.

This is an unofficial, community-maintained rebuild of [DrBibop's RoR2VRMod](https://github.com/DrBibop/RoR2VRMod). The original mod stopped working after the game moved to a newer Unity engine; this build ports it to that engine, switches it to OpenXR, and fixes the things that broke along the way. All credit for the mod itself goes to DrBibop and the original contributors (see Credits). Released under the same MIT license.

## Download and Manual Install

**[Download the latest release zip](https://github.com/Blowntobytes/RoR2VRMod/releases/latest)** — on that page, open **Assets** and click `Blowntobytes-Resurrected_VRMod-<version>.zip`. Do not use GitHub's green "Code" button; that gives you the source code, not the mod.

Then in r2modman / Thunderstore Mod Manager: Select Risk of Rain 2 → Steam platform. At *Profile selection* page → click on *Import/Update* at the bottom and *From code* Copy and paste this code in there '01a076cc-d897-138e-a2d2-fdaed4aa561b' → *Continue* → *Import* → *Import new profile* → *Create* That will install all of the dependencies. Then select that profile → *Settings → Profile → Import local mod* → select the zip in Downloads → *Import local mod* and start the game with **Start modded**. 

**With r2modman / Thunderstore Mod Manager (recommended):** install it from Thunderstore with the "Install with Mod Manager" button, then start the game with "Start modded". If you previously imported a pre-release zip of this mod as a local mod (or still have DrBibop's original VRMod installed), uninstall that entry first — two copies of the mod must never be installed together.

**Manual:** install BepInEx and HookGenPatcher first, then copy the `plugins` and `patchers` folders from the zip into your `BepInEx` folder.

The first launch copies the OpenXR native files into place; if the headset stays black the very first time, simply launch again. Pick up both controllers before the title screen so the runtime wakes them.


- Source code: https://github.com/Blowntobytes/RoR2VRMod
- Original mod: https://github.com/DrBibop/RoR2VRMod
- Found a bug? Open an issue on the GitHub page above and attach your `BepInEx/LogOutput.log`.

## Multiplayer

Only VR players need the mod. You can play with vanilla (non-VR) players.

## Requirements

- Risk of Rain 2 (current Steam build).
- A PC VR headset with an **OpenXR** runtime: SteamVR (any SteamVR headset, Index, Vive, Pico/Quest via Steam Link, Virtual Desktop, ALVR), the Meta/Oculus PC runtime (Quest Link / Air Link), or Windows Mixed Reality.
- [BepInExPack](https://thunderstore.io/package/bbepis/BepInExPack/) and [HookGenPatcher](https://thunderstore.io/package/RiskofThunder/HookGenPatcher/) (the mod manager installs these for you).

### Which headsets work, and the one setting that matters

The mod does not care which headset you own — it asks Windows for whatever OpenXR runtime is currently active. So the only thing to get right is that the runtime matching your headset is the active one:

| Headset | Set this as the active OpenXR runtime |
|---|---|
| Quest 2/3/Pro over Virtual Desktop | Virtual Desktop streamer → *Streaming* → OpenXR runtime **VDXR** |
| Quest over Link / Air Link, Rift CV1, Rift S | Oculus (Meta) PC app → *Settings > General* → **Set Oculus as active OpenXR runtime** |
| Valve Index, HTC Vive, Bigscreen Beyond, other SteamVR headsets | SteamVR → *Settings > OpenXR* → **Set SteamVR as OpenXR runtime** |
| Windows Mixed Reality, HP Reverb G2 | Mixed Reality Portal / OpenXR Tools for WMR → set WMR as the runtime |

Only one runtime can be active at a time, so switching headsets means flipping this setting. Controller bindings are registered for Oculus Touch, Quest Touch Pro, Valve Index, HTC Vive, HP Reverb G2, WMR and the generic KHR profile, so the button layout below applies on all of them (Index/Beyond knuckles: the "grip" is the squeeze).

Turn off "Use Desktop Game Theatre while SteamVR is active" in the game's Steam properties.

Do **not** turn on the old "Use Oculus mode" config option — it is a leftover from the pre-OpenXR mod and does nothing in this build.

## Controls

Bindings are built in (this build does not use the SteamVR binding editor). Button names follow the Oculus/Quest layout; on Index, Vive and WMR controllers the equivalent buttons are used.

**Gameplay** (right-handed; turn on "Left dominant hand" in the VR settings to swap the skill buttons)

| Input | Action |
|---|---|
| Left stick | Move |
| Right stick | Turn / look |
| Right trigger | Primary skill |
| Left trigger | Secondary skill |
| Left grip | Utility skill |
| Right grip | Special skill |
| A | Interact |
| B | Jump |
| X | Use equipment |
| Y (tap) | Pause menu |
| Y (hold) | Scoreboard / profile |
| Left stick click | Sprint (or turn on **Auto Sprint** in the VR settings to sprint automatically) |
| Right stick click | Ping — and **recenter** the view (works everywhere, including menus) |

**Menus** — the controllers act as a gamepad, and every on-screen prompt names the button that really does it (A is A, the trigger glyph means the trigger):

| Input | Action |
|---|---|
| Left stick | Move the selection |
| A | Confirm |
| B | Back / cancel |
| X | Ready / alternate confirm |
| Left trigger | Back to the survivor grid (previous page) |
| Right trigger | Forward to the panels on the right — expansions / DLC, artifacts (next page) |
| Left grip / right grip | Previous / next tab in the settings panel. Nothing anywhere else — they cannot trigger anything by accident on the character select screen |
| Y | Open / close the pause menu, skip the intro cutscene |
| Right stick click | Recenter the view |

On the character select screen, A picks the highlighted survivor; the **Ready** button then becomes the highlighted control, so press A again (or move to it with the stick) to ready up. One press never does both.

## VR settings

Open the in-game *Settings* and pick the **VR** tab (the same options are in `BepInEx/config/VRMod.cfg`, editable from the mod manager's Config editor). Notable options in this build:

- **Auto Sprint** — sprint automatically whenever you push the left stick, and re-engage on its own after anything cancels the sprint (firing a skill, taking a hit).
- **Controller aim pitch** — tilts the hands/aim up or down if your controllers report a different grip angle.
- **Intro camera yaw** — where the opening cutscene faces (default 180 so the ship arrives in front of you).
- **Left dominant hand**, snap turning, first person / motion controls, HUD scale and placement, and the other original options.

The camera always sits at the character's eye level with a seated (device) tracking origin; click the right stick to recenter after you sit down or turn your chair.

## Known issues in this build

- Hand models are not shown inside menus (menus are navigated like a gamepad). Hands and weapons work normally in a run.
- The hands for the Seekers of the Storm and Alloyed Collective survivors are generated from the character model at runtime rather than hand-made, so they are functional rather than pretty. Their position and rotation can be tuned per survivor in the config.
- Bhaptics / Shockwave haptics suit support is still compiled in but has not been tested with this build.
- Character support from [VRAPI](https://thunderstore.io/package/DrBibop/VRAPI/) (custom survivors with VR hands) is included unchanged and untested.

The mod's internal plugin ID is still `com.DrBibop.VRMod`, so other mods that depend on the VR mod or VRAPI keep working.

## Building from source

The original Visual Studio project files are kept, but this build was produced with a self-contained, offline toolchain that needs only PowerShell 7 — see [`build-tools/README.md`](build-tools/README.md) for the reference-assembly setup and the exact build commands. The packaged zip layout (`manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `plugins/`, `patchers/`) is the standard Thunderstore format.

## Credits

**DrBibop:** original mod and patcher programmer, animator\
**MrPurple6411:** patcher programmer\
**dotflare:** 3D artist\
**Ncognito:** 3D artist\
**eliotttate:** original creator, code assist\
**HutchyBen:** code assist\
**AmadeusMop:** code assist\
**Daerst:** fixed the biggest and oldest bug of the original mod\
**laila, HutchyBen, Skarl1n, Geb, Popzix, Terrorcotta211:** controller support for the original mod\
**Blowntobytes:** current-game-version port, OpenXR rebuild and maintenance of this fork\
The `ParentCamera.projectionMatrix = _childCamera.GetStereoProjectionMatrix(eye)` stereo fix came from a community report.
