# Changelog

This package continues [DrBibop's RoR2VRMod](https://github.com/DrBibop/RoR2VRMod) (MIT), whose own history ends at 2.9.2; its original changelog is kept in the source repository as `CHANGELOG-original-DrBibop.md`. Version numbers restart at 1.0.x for this package.

### 1.0.5

**New**
- Updated for the October 2026 game update (Hallowed Concepts): rebuilt against the new game code, HUD, wrist panels, scoreboard and character select work again, and the bundled OpenXR/Input System libraries no longer spam errors.
- VR hands for the new survivors **Enforcer** and **Acolyte**, built from the characters' own arms, with their held weapons. Enforcer's shield only appears while you are shielding.
- Enforcer: triggers and grips are swapped left/right (shotgun on the left, shield on the right). New setting `[Survivor Settings] Enforcer: Shield opacity` (0-100 %, 0 = invisible), also in the in-game VR settings.
- Transformations: Acolyte's Reaper form and the Shrine of Compulsion Lemurian get their own hands and a first-person view scaled to the form's size, using the game's own camera height.
- Key items: hold the left grip and press A to use the key item; hold the left grip and flick the right stick left/right to change element. A wrist panel shows the key item and what the buttons do.
- Y button: tap for the pause menu, hold for the scoreboard.
- HTC Vive and original Windows Mixed Reality wand controls (DrBibop's SteamVR layout: right pad up = jump, right pad down = interact), including the VR keyboard on trackpad wands.
- Steam Frame controller support.
- All runtime hand, aim and weapon positions for Seekers of the Storm, Alloyed Collective and Hallowed Concepts survivors are tuned and built in.

**Fixed**
- Eclipse, Simulacrum, Prismatic Trials and Scavenger Bingo menus showed only the background in VR; their buttons are visible and usable again.
- Character select: the left trigger switches to the survivor grid again after the game update; the sprint icon is found again in the new HUD.
- The intro "skip" prompt is moved into view instead of being cut off at the edge.
- Key item button prompts no longer show as "unmapped".
- A temporary body (Shrine of Compulsion polymorph) no longer bakes the whole creature onto your controllers.
- A recurring hitch while playing, caused by the config file being re-read too often.
- Changing a VR setting in the in-run pause menu could leave the character unable to move afterwards: the stick could carry the menu selection onto a HUD control (the key item slot), and a selected control keeps the game in menu mode. HUD controls are no longer reachable by stick navigation, and a leftover selection is cleared when the menu closes.

**Changed**
- The per-character hand, aim and weapon settings and the `RuntimeHands (experimental)` section are removed from `VRMod.cfg`; their values are built into the mod and players can no longer adjust them. Old entries are cleaned out of existing config files automatically.
- Dependencies: BepInExPack 5.4.2122 (which installs RoR2BepInExPack, FixPluginTypesSerialization and BepInEx GUI) and HookGenPatcher.

**Known issue**
- The loading screen can wait at 0 % for about 20 seconds on the current game build. This is caused by the FixPluginTypesSerialization dependency and also happens with this mod disabled.

### 1.0.4

**New**
- VR keyboard: press A on any text box in the multiplayer menus (game browser search and filters, host name, password, tags, and the password box on a game's info panel) to type with the controllers. Left stick moves, A types, X backspace, Y space, B closes, DONE confirms. Setting: `[VR Settings] VR keyboard`.
- Chat from the controllers: click the left stick on the character select screen, or in a run while paused, to type a chat message; B or DONE sends it, B with nothing typed closes. The menu highlight is restored afterwards and the closing press never reaches the lobby. Setting: `[VR Settings] VR chat (left stick click)`.
- Bright amber selection frame around the currently selected menu control (`[VR Settings] Menu selection frame`).
- `[VR Settings] Hidden item displays`: hides chosen items' 3D models on your own character in VR (default: Kinetic Dampener).
- Per-survivor forearm switch for the runtime hands (`<Body>_Forearm`; off for False Son, replaces the old global "Include forearm").
- Runtime hand, aim and weapon offsets in the config are re-read within 3 seconds of saving the file while in game (no restart, no Debug mode needed).
- Retuned aim/laser defaults for Seeker, Chef, Operator and Drifter, and False Son's club offset.

**Fixed**
- Highlight chests: the glow now goes out when a chest, money pod or multishop terminal is used up by any player (host or client), including the other two terminals of a tri-shop; money pods are highlighted too.
- Auto Sprint no longer interrupts skills that keep playing after the button is released (Bandit's Lights Out wind-up and similar); it waits for the weapon states to go idle.
- Multiplayer menus are fully navigable with the stick: the server browser's filter column is shown in VR and reachable, right/left crosses between the filters and the game list, the Host screen's lobby panel (Invite / Copy / Leave) is reachable, and the highlight never disappears after a refresh or panel change.
- Character select: up/down on the rules panel passes through the Expansions and Artifacts headers, so their pickers can be opened without pushing left.
- Kinetic Dampener display no longer reappears after a stage change.
- False Son's forearms are no longer baked into his VR hands.
- Obsolete settings (`Include forearm`, `*_IncludeForearm`, `Intro cutscene seated height`) are removed from the config file automatically.
- Menu glyph lookup at the title screen no longer throws a NullReference, and the Ready prompt (X) on the character select screen no longer disappears in multiplayer.
- Game info panel: its password box and details are reachable and typable.
- Health bar repositioning no longer spams NullReference errors in multiplayer when a body has no model yet.

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
