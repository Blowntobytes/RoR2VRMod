# Changelog

This package continues [DrBibop's RoR2VRMod](https://github.com/DrBibop/RoR2VRMod) (MIT), whose own history ends at 2.9.2; its original changelog is kept in the source repository as `CHANGELOG-original-DrBibop.md`. Version numbers restart at 1.0.x for this package.

### 1.0.3
- README: revised install steps (r2modman profile code for dependencies, then Import local mod), controls and VR settings sections; Thunderstore page text now matches the GitHub README.

### 1.0.2
- README rewritten with step-by-step r2modman instructions (code for the dependencies, then Import local mod).
- Source repository made public.

### 1.0.1 — first public release

(1.0.0 was consumed by a failed upload and was never published.)

**Runs on the current game**
- Ported to the game's current Unity engine and rebuilt on OpenXR (SteamVR, Meta/Oculus PC, Virtual Desktop VDXR and Windows Mixed Reality runtimes). Controller profiles for Oculus/Quest Touch, Touch Pro, Valve Index, HTC Vive, HP Reverb G2, WMR and the generic KHR profile.
- Seated (device) tracking origin with eye-level camera; right stick click recenters anywhere, including menus.
- Clean OpenXR shutdown on exit.

**Survivors**
- VR hands for the Seekers of the Storm and Alloyed Collective survivors (Seeker, Chef, False Son, Drifter, Operator) generated at runtime from the character model, posed from the bind pose so item pickups no longer bend them.
- Operator: gun attached to the left hand with tunable offsets; the docked drone leaves the right claw when thrown and returns when the skill is ready.
- Per-hand position and rotation offsets for every survivor in the config, shipped with in-headset tuned defaults.

**Menus and HUD**
- Every menu is navigated like a gamepad and every prompt shows the real button (A confirm, B back, triggers page left/right, grips switch settings tabs, Y pause).
- Seeker's lotus and meditation mini-game sit above the health bar (heights configurable).
- End-of-run report readable in VR, with configurable width and text size; left/right triggers page through players.
- Confirmation dialogs draw in front of the pause menu; pause menu draws in front of the report.
- Equipment slot icon, countdown and stock (including MUL-T's second slot) and the item strip initialise correctly on the hand HUD.
- Pickup notifications use the game's own duration.
- "Game window not focused" warning restored.

**Intro cutscene**
- Typed story text renders in the headset; seated height during the cutscene is configurable; the Skip prompt is visible from the start and only Y skips.
- Splash logo no longer drawn twice.

**Settings**
- VR tab in the game's settings: Auto Sprint, controller aim pitch, intro camera yaw, left dominant hand, snap turn, first person, HUD options.
- Config file: `BepInEx/config/com.DrBibop.VRMod.cfg` (plugin ID unchanged so mods depending on the VR mod keep working).
