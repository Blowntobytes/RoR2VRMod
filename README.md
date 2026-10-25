# Resurrected VRMod — Risk of Rain 2 VR mod, unofficial build by Blowntobytes

Play Risk of Rain 2 in virtual reality with motion controls, on the **current Steam version of the game**.

This is an unofficial, community-maintained rebuild of [DrBibop's RoR2VRMod](https://github.com/DrBibop/RoR2VRMod). The original mod stopped working after the game moved to a newer Unity engine; this build ports it to that engine, switches it to OpenXR, and fixes the things that broke along the way. All credit for the mod itself goes to awesome DrBibop and the original contributors (see Credits). Released under the same MIT license.

## VR Features

Open the in-game *Settings* and pick the **VR** tab (the same options are in `BepInEx/config/VRMod.cfg`, editable from the mod manager's Config editor). Notable features in this build:

- **Auto Sprint** — *Off by default* - sprint automatically whenever you push the left stick, and re-engage on its own after anything cancels the sprint (firing a skill, taking a hit). Also works with a regular gamepad.
- **A: Jump when nothing to interact with** — A interacts as usual and jumps when nothing is in reach.
- **Wrist chat always visible** — *On by default* keeps the chat on your left watch readable instead of fading out.
- **Highlight chests** — Chest, teleporters, shrines will have a yellow glow for easy locating.
- **Item hints** — *Off by default* When pinging an item or scrolling through items in a command run, item name and info will be included on your left wrist or at the bottom of the picker panel.
- **Seated mode** — *On by default* Because who wants to stand when playing this game? You can disable it to switch to standing mode but you'll have to restart the game to apply.
- **Hidden item displays** — items whose 3D model is too distracting to display (default: Kinetic Dampener, Harvesters Scythe). Comma-separated, matched by in-game name; the items still work.
- **Menu selection frame** / **VR keyboard** / **VR chat (left stick click)** — *On by default* the amber selection frame, the on-screen keyboard and the chat shortcut; each can be switched off.
- **Enforcer: Shield opacity** — how solid Enforcer's shield looks while shielding (0 = invisible, 100 = solid).
- **Enforcer** — the shotgun is always in your dominant hand and the shield in the other (right-handed: shotgun right, shield left; left-handed: the mirror image). Triggers and grips use the normal layout: the dominant trigger fires the shotgun, and Protect and Serve (raise the shield) is on the grip of the shield hand. Shots come out of the shotgun. The shield, and any items attached to it, are only drawn while it is raised; its transparency is the **Enforcer: Shield opacity** slider in the VR settings.
- **Left-handed mode** — turn on "Left dominant hand" in the VR settings. The skill buttons swap sides, and the Seekers of the Storm, Alloyed Collective and Hallowed Concepts survivors become an exact mirror image of right-handed mode, aiming with the left hand. Restart the run after switching (switching mid-run can leave weapons see-through or firing from the wrong spot).
- Rebinding — A new tab that gives you the ability to rebind buttons



## Installation 


**With r2modman / Thunderstore Mod Manager (recommended):** install it from Thunderstore with the "Install with Mod Manager" button, then start the game with "Start modded". If you previously imported a pre-release zip of this mod as a local mod (or still have DrBibop's original VRMod installed), uninstall that entry first — two copies of the mod must never be installed together.

**[Manual install]**  Install [BepInExPack](https://thunderstore.io/package/bbepis/BepInExPack/) (which brings RoR2BepInExPack, FixPluginTypesSerialization and BepInEx GUI with it) and [HookGenPatcher](https://thunderstore.io/package/RiskofThunder/HookGenPatcher/). Then download the [Latest zip](https://github.com/Blowntobytes/RoR2VRMod/releases/latest) — on that page, open **Assets** and click `Blowntobytes-Resurrected_VRMod-<version>.zip`. Do not use GitHub's green "Code" button; that gives you the source code, not the mod. Then in r2modman / Thunderstore Mod Manager: Select *Settings → Profile → Import local mod* → select the zip in Downloads → *Import local mod* and start the game with **Start modded**.


- Source code: https://github.com/Blowntobytes/RoR2VRMod
- Original mod: https://github.com/DrBibop/RoR2VRMod
- Found a bug? Open an issue on the GitHub page above and attach your `BepInEx/LogOutput.log`.

## Multiplayer- Compatible with vanilla players!

Only VR players need the mod. You can play with vanilla (non-VR) players.

## Requirements

- Risk of Rain 2 (current Steam build).
- A PC VR headset with an **OpenXR** runtime: SteamVR (any SteamVR headset, Index, Vive, Pico/Quest via Steam Link, Virtual Desktop, ALVR), the Meta/Oculus PC runtime (Quest Link / Air Link), or Windows Mixed Reality.
- Dependencies listed above
- Turn off "Use Desktop Game Theater while SteamVR is active" in the game's Steam properties.
- Do **not** turn on the old "Use Oculus mode" config option — it is a leftover from the pre-OpenXR mod and does nothing in this build.



## Controls

These are the default bindings for Meta (this build does not use the SteamVR binding editor). Every gameplay action can be moved to another button or combo in the **BINDINGS** tab of the in-game Settings (see below). Button names follow the Meta/Quest layout; on Index, Vive and WMR controllers the equivalent buttons are used.

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
| Right stick click | Ping |

**Combos with X (key items, equipment sets)** — X uses your equipment when tapped on its own; held, it works as a modifier for the combos below. A combo also works if you press both buttons within a fraction of a second, in either order.

| Input | Action |
|---|---|
| X (hold) + B | Use the key item (Hallowed Concepts) |
| X (hold) + right stick left / right | Previous / next element |
| X (hold) + A | Switch equipment set (with an item that gives a second equipment set) |

The wrist panel on your left hand shows the key item you are carrying (and N'tormat's current page) and reminds you of the buttons.

**Remote-operated drones** (after you die in multiplayer and take over a drone): the left stick flies along where you look (forward/back) and strafes left/right, the right grip climbs, the left grip descends. 

**HTC Vive / original Windows Mixed Reality wands** — these have no A/B/X/Y, so the trackpad clicks and menu buttons stand in (HP Reverb G2 has real A/B buttons and uses the normal layout) Untested:

| Wand input | Acts as |
|---|---|
| Right trackpad click, lower half | A (interact / confirm) |
| Right trackpad click, upper half | B (jump / back) |
| Left trackpad click, upper half | X (equipment / ready) |
| Left trackpad click, lower half | Sprint (left stick click) |
| Right menu button | Ping (right stick click) |
| Left menu button | Y (tap = pause menu, hold = scoreboard) |

**Steam Frame** — mapped like a Quest/Touch controller: right A = A, right Y = B, left D-pad down = X, left D-pad up = Y, left View button = menu; sticks, triggers and grips are the same.


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
| Y | Open / close the pause menu, skip the intro cutscene (also Y on a gamepad) |

On the character select screen, A picks the highlighted survivor; the **Ready** button then becomes the highlighted control, so press A again (or move to it with the stick) to ready up. One press never does both. Up/down on the rules panel walks Difficulty → Expansions → Artifacts; A on the Expansions or Artifacts header opens that picker.

**Typing (VR keyboard):** move the highlight onto any text box (game browser search and filters, host name, password, tags, the password box on a game's info panel) and press A. A keyboard appears below the menu: left stick moves over the keys, A types, X is backspace, Y is space, B closes and keeps the text, and DONE confirms the box the way Enter does. A bright amber frame always shows which menu control is selected.

**Chat:** click the left stick on the character select screen (or in a run while the game is paused) to open the keyboard on the chat box. B or DONE sends the message; B with nothing typed just closes it. The highlight returns to the menu afterwards.

## Rebinding (BINDINGS tab)

In the in-game *Settings*, the **BINDINGS** tab (to the right of **VR**) lists every gameplay action. Select a row and press A, release everything, then press the button you want:

- **A single button** — just press it.
- **A combo** — hold the first button (the modifier) and press the second.
- **Tap or hold** — release within about half a second for a tap binding, keep holding for a hold binding (Y is tap = pause menu, hold = scoreboard by default).
- A flick of a stick (for example "R stick right") can be bound too.

Conflicts are flagged on the row. **Reset all bindings** restores the defaults. The choices are saved in the `[Bindings]` section of `VRMod.cfg`; `default` means the built-in layout for your controller. The game's own Keyboard + Mouse and Controller binding pages are not used for VR controls.


## Known issues in this build

- Hand models are not shown inside menus (menus are navigated like a gamepad). Hands and weapons work normally in a run.
- Not tested with other mods. If you have any issues, disable all other mods first before reporting and make sure the VR mod works by itself, with the dependencies it requires
- The loading screen can sit at 0 % for about 20 seconds before it starts counting. This comes from the FixPluginTypesSerialization dependency on the current game build, not from this mod; it happens with the VR mod disabled too.
- Character support from [VRAPI](https://thunderstore.io/package/DrBibop/VRAPI/) (custom survivors with VR hands) is included unchanged but untested.

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
**Blowntobytes:** welder, current-game-version port, OpenXR rebuild and maintenance of this fork\
The `ParentCamera.projectionMatrix = _childCamera.GetStereoProjectionMatrix(eye)` stereo fix came from a community report\
Playtesters: JosheM, Void-Citizen, Thanjar(for left-handed mode)
