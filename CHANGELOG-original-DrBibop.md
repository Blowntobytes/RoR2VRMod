### 1.0.0
- Initial release of the mod.

### 1.1.0
- All menus are now visible in VR.
- Enemy health bars are now correctly positioned above enemies.
- Ping icons have been pushed further away from the camera.
- Added a config setting to disable VR.

### 1.1.1
- Fixed some indicator icons that were too large.

### 1.1.2
- Fixed yet another oversized indicator.
- Removed R2API dependency.
- Lowered the top part of the HUD (was reverted due to the anniversary update).
- Fixed a bug that caused the game to launch in VR after disabling or uninstalling the mod.
- Removed the need for launch options (this causes the game to launch in SteamVR by default).
- Added a config setting to launch in Oculus mode.
- Removed the "Enable VR" setting.

### 1.2.0
- Added a bindable key to recenter the HMD (Default: RCtrl/Dpad-Up).
- Added HUD config settings for UI scale and anchor placements.
- Fixed a bug that caused the map name to display too high up.
- Added MMHOOK Standalone as dependency (was previously included with the mod).

### 1.2.1
- Icons no longer have a fixed distance.
- Fixed a bug that caused icons to not correctly appear above targets.
- Fixed targeting indicator placements (Huntress primary, Engineer missile launcher, recycler, capacitor, etc.).

### 1.3.0
- Added first person config setting.
- Added snap turn and snap turn angle config settings.
- Added camera pitch lock config setting.
- Removed camera recoil effects.
- The pause menu now follows the camera rotation.
- Changed MMHOOK dependency to HookGenPatcher.

### 2.0.0
- Added motion controls support.
- Added a vignette during high-mobility abilities to reduce motion sickness (can be disabled).
- A dialog box now opens in the main menu telling the player how to recenter the HMD.
- The sprint icon on the bottom right turns yellow while sprinting to compensate for the lack of visual cues like the crosshair.
- The HUD should now appear at the same size no matter your resolution/FOV.
- Added HUD width and height config settings (HUD anchor settings need to be reset to default if you have downloaded a previous version).
- Fixed a bug that caused some targeting indicators to not face the camera properly.
- Fixed a bug that caused the dialog box in the pause menu to not follow the menu rotation.

### 2.0.1
- Reduced the size of multiple muzzle flashes and effects.
- New setting: "Hide broken decal textures".
- Added "Survivor Settings" config category:
	- New setting: "Commando: Dual wield".
	- New setting: "Bandit: Weapon grip snap angle".
	- New setting: "Mercenary: Swing speed threshold".
	- New setting: "Loader: Swing speed threshold".
	- New setting: "Acrid: Swing speed threshold".
- The pop-up appearing when selecting a lobby in multiplayer now correctly appears in the headset.
- The black transition screens now correctly appear in the headset.
- The credits now appear in the headset (it is currently stuck on the headset but this will change soon).
- Fixed a bug that caused inputs to not register when no profiles are selected.
- Fixed a bug that caused the profile creation pop-up to be uninteractable.

### 2.1.0
- Added a wrist HUD setting that attaches the health bar, money display and skills to the wrist.
- Added a watch HUD setting that attaches the inventory, chat, difficulty, objective and allies to a watch-like HUD.
- Added a smooth HUD setting that adds smoothing to the camera HUD when moving the headset.
- Added a spectator screen that appears in front of you when spectating players.
- Added "Ray color" and "Ray opacity" settings to customize the aim ray.
- Added more detailed models for Loader's hands, Bandit's shotgun and Bandit's revolver.
- Removed "UI scale" setting as it already exists in-game.
- Removed the center smoke effect on Bandit's stealth ability for improved visibility.
- Possibly fixed a bug that caused the Heretic wings to appear on the wrong player which would break some abilities.
- Fixed a bug that caused Heretic's primary skill projectiles to not appear from the hand after transforming.
- Fixed a bug that caused Vive Cosmos controllers to use the standard Vive controller binds.

### 2.1.1
- The credits no longer stick to the camera.
- The spectator screen is now fully opaque.
- Loader's aim rays have been aligned better with the mech arms.
- Fixed a bug that caused the spectator screen to not appear in multiplayer for non-host players.
- Fixed a bug that caused the shield effect to appear abnormally large on Bandit's new weapon models.
- Fixed a bug that caused MUL-T's left hand animations to break when activating power mode right before transport mode.

### 2.2.0
- Compatibility with the new VR API which adds the possibility of VR compatible mods such as custom characters.
- New hand models for all survivors.
- Equipments, items and body effects that were obstructing vision are now hidden for better visibility.
- Fixed a bug that made bullets and projectiles no longer appear from weapon muzzles after reviving.
- Fixed a bug that made bullets no longer appear from the main weapon's muzzle on Bandit when disabling Commando's dual wield setting.

### 2.2.1
- Fixed a bug that caused the Smooth HUD config to be ineffective.
- Fixed a bug that caused floating equipments to re-appear when teleporting to a new stage.

### 2.3.0
- The scoreboard and the profile menu can now be accessed by holding the menu button.
- Snap turns will now repeat when holding a direction.
- Added a "Snap Turn Hold Delay" setting.
- Added a "Camera Health Bar" setting which puts the health bar at the bottom-middle of the camera HUD for better visibility.
- Added a new EXPERIMENTAL "Roomscale Tracking Space" setting.
- Added a new EXPERIMENTAL "Player Height" setting.
- The aim ray will now activate on the appropriate hand when you have an aimable equipment or heresy skills.
- The Soulbound Catalyst and the Frost Relic no longer appear around the player.
- Removed "Hide broken decal textures" config.
- Fixed a bug that caused decals to only render on the left eye.
- Fixed a bug that caused the camera HUD to freeze in place while paused.
- Fixed a bug that caused parts of the multiplayer menu to not render properly creating an offset.

### 2.4.0
- You can now freely bind your controls with SteamVR.
	- Every single action can be bound to separate inputs.
	- This new system removes the need to download binds for Index Knuckles and Reverb G2 controllers.
	- A few extra bindable actions have been added for these mods: ExtraSkillSlots, VoiceChat, SkillsPlusPlus and ProperSave.
- Added haptic feedback (controller rumble). The intensity can be adjusted with the in-game gamepad setting.
- In-game "cutscenes" that take control of the camera have been improved to reduce risks of nausea and clipping.
- Fixed a bug that caused inputs to break after selecting a profile.
- Fixed a bug that placed and scaled enemy health bars and some indicators incorrectly when using the roomscale tracking setting.

### 2.5.0
- Added the LIV SDK for XR capture support.
	- Only available with SteamVR and with the "Roomscale tracking space" setting enabled.
	- A setting has been added to display the classic HUD on the XR camera.
- All mod configs can now be edited with the in-game settings menu. Some will only be applied on the next stage or after restarting.
- Elements in the intro cutscene and the escape cutscene have been scaled and placed in a more realistic way.
- The grip sensitivity has been reduced on the default Index Knuckles bindings.
- The "Roomscale tracking space" setting is no longer in an experimental stage and is now enabled by default.
- The default value of Loader's melee swing speed threshold has been slightly reduced.
- Reduced the size of the charging Nano-Bomb effect on Artificer's hand for better visibility.
- Fixed a bug that prevented cutscene subtitles to display correctly in VR.
- Fixed a bug that caused the camera to be placed too high in menus when using roomscale tracking.
- Fixed a bug that caused the Visions on Heresy skill to be activated by swinging your controller when equipped on melee survivors.

### 2.5.1
- The pickup notification and the spectator label have been moved up above the central health bar to prevent them from being hidden behind status icons.
- Fixed a bug that caused some setting sliders to parse decimals incorrectly when using certain languages.

### 2.5.2
- Fixed a bug that caused the credits to appear during the escape cutscene.
- Fixed a bug that prevented the LIV plugin from being correctly copied into the game directory during the patching process.

### 2.6.0
- The UI navigation system has been revamped to use your dominant hand as a pointer instead of using gamepad controls.
- The player and character height can now also scale the camera view with the roomscale tracking space setting disabled.
- The mouse can no longer move the camera when using motion controls.
- Fixed a bug that prevented the default Vive Cosmos controller binds from loading correctly which made the Vive Cosmos controllers unusable.
- Fixed a bug that would sometimes break inputs with the Oculus Mode setting enabled.
- Fixed a bug that caused the SteamVR overlay to pause the game which created sync issues in multiplayer.
- Fixed a bug that caused corruption of some menu backgrounds when using LIV XR capture.
- Fixed a bug that caused the hand tracking to be slightly inaccurate when using SteamVR.
- Possibly fixed a bug that prevented the spectator screen from appearing in some occasions.

### 2.6.1
- Fixed a bug that prevented the spectator screen from appearing.
- The spectator screen will now always render on the foreground.
- Fixed a bug that broke some hand animations after using the command or scrapper panel.
- Fixed a bug that prevented the kick message from displaying correctly.
- Fixed a bug that made the buttons in the lobby details panel unclickable.
- Fixed a bug that broke all inputs when no profile has been created.

### 2.6.2
- Fixed a bug that caused some menus to have unreachable buttons near the edges when playing with a lower resolution per eye.
- Fixed a bug that prevented the watch HUD from appearing or disappearing while paused.
- Fixed a bug that caused the spectator camera to have the wrong field of view.
- Fixed a bug that caused Bandit's revolver animation to cancel by mistake when other Bandit players in the lobby would start sprinting.

### 2.6.3
- The momentum direction is now controlled by the non-dominant hand when using Loader's grapple hook.
- A warning now appears when the game window loses focus.
- Fixed a bug that prevented the use of the triggers or the X button to interact with menus on Oculus Touch controllers when Oculus mode is enabled.
- Fixed a bug that showed the wrong control glyphs when playing with Vive or WMR controllers.

### 2.6.4
- VR settings that depend on other settings will no longer get forcibly switched. For example:
	- Turning off first person deactivates motion controls, but the motion controls setting will stay intact so it stays enabled when re-enabling first person.
	- Turning off motion controls means the wrist and watch HUDs cannot be used, but the settings will now stay enabled.
- Fixed a bug that broke controller inputs when the controllers are no longer detected with Oculus mode on.

### 2.6.5
- Fixed compatibility with the Survivor of the Void update.
- Players spectating a VR player should now properly see towards the direction they're looking at.
- The smooth HUD now follows the camera slightly faster.
- The top and bottom faded black bars in the character selection menu have been removed.
- Fixed a bug that prevented SteamVR from initializing the first time the mod was loaded.

### 2.6.6
- Fixed a bug that would crash the game if attempting to start a run with snap turn disabled.
- Fixed a harmless bug that would spam the console with errors.

### 2.6.7
- Depth of field effects have been completely removed in all locations.
- The round cursor is now hidden when playing with a gamepad.
- Fixed a bug that caused the camera to tilt in some situations.
- Fixed a bug that caused the spectating camera to point towards the wrong direction.
- Fixed a bug that caused the vanilla aim-assist to reduce smooth turn speed when looking towards an enemy.
- Fixed a bug that caused motion controllers to rumble when not using them.
- Fixed a harmless bug that would spam the console with errors in the main menu.

### 2.7.0
- Railgunner and Void Fiend are now fully VR supported.
- Three settings have been added for Railgunner:
	- "Railgunner: Weapon grip snap angle".
	- "Railgunner: Zoom multiplier".
	- "Railgunner: Hide ray while scoping".
- Added aim stabilisation to improve overall accuracy.
	- Rotational stabilisation is applied everywhere, but positional stabilisation is also applied when holding two-handed weapons.
	- The amount of stabilisation can be changed with the new "Aim stabiliser amount" setting.
- Sprinting and scoping no longer affect the smooth turn speed.
- The shield generator, plasma shrimp and shaped glass overlay effects on the hands are now smaller and more transparent.
- Fixed a bug that made the melee swing threshold settings affect only one hand on Loader and Acrid on future runs after changing it.
- Possibly fixed a bug that sometimes prevented some animations from playing.
- Possibly fixed a bug that sometimes prevented binding overrides from applying correctly on Loader.
- Fixed more bugs spamming errors in the console.

### 2.7.1
- Fixed a bug that broke the UI pointer when no profile was loaded.
- Fixed a bug that prevented players from controlling their vertical flying direction with Milky Chrysalis when using controller direction for movement.
- Fixed a bug that broke the command/scrapper UI when closing it.

### 2.8.0
- Added custom skins support through the VRAPI.
- Added alternate models on Bandit's and Void Fiend's mastery skin.
- The FixPluginTypesSerialization mod is now a dependency in order to support custom skins.
- Aim rays for equipments now only appear when the equipment is off cooldown.
- Fixed a bug that prevented the game report screen from appearing after the credits.
- Fixed a bug that prevented the spectator camera from rotating vertically when the "Locked camera pitch" setting was on.
- Fixed a bug that prevented the equipment aim ray or heresy item aim ray from appearing when first loading into a stage.
- Fixed a bug that prevented the difficulty icon, stage count, enemy level and simulacrum waves from appearing on the right watch HUD.

### 2.8.1
- Replaced the "Player height in meters" setting with a simpler "Height multiplier" setting. Increasing the new slider will make you feel taller.
- Replaced the "Roomscale tracking space" setting with a "Seated mode" setting to better communicate its functionality.
- Fixed a bug that forced the physics update rate to match the headset's refresh rate when using SteamVR which impacted performance.

### 2.8.2
- Removed FixPluginTypesSerialization dependency as it is now included with BepInEx.
- Railgunner's scope crosshair texture is no longer affected by graphic settings.
- Fixed some animations on Bandit's revolver when using the Bandit Tweaks mod.

### 2.9.0
- Added haptic feedback for Bhaptics and Shockwave suits.
	- Features directional feedback and other stimulating patterns.
	- A new setting has been added to select which haptic suit is being used. Scroll to the bottom of the VR settings menu to select your suit then restart the game.
- Fixed a bug where kick messages couldn't be interacted with in the main menu.

### 2.9.1
- Fixed the VR patching system that was incomplete in the last update.

### 2.9.2
- Fixed a bug that caused tracking issues with Reverb G2 controllers.
- Reduced default swing speed threshold on all melee survivors.

### 2.10.0 (unofficial rebuild)
- Rebuilt DrBibop's unreleased OpenXR branch (GitHub main, Jan 2026) against the current Steam build of the game (Unity 2021.3.33f1, August 2026 content).
- Reworked the stereo projection fix: the hidden reference camera now stays enabled so the XR system keeps feeding it real per-eye projection matrices; the scene camera copies `GetStereoProjectionMatrix(eye)` from it every pre-cull/pre-render and also mirrors both eyes through `SetStereoProjectionMatrix`.
- Loader grapple aim-ray hook adapted to the runtime MMHOOK signature (private nested state type).
- XR Interaction Toolkit dependency removed (only `InputHelpers` was used; it is now vendored into the mod).
- Unity.InputSystem 1.6.3, Unity.XR.Management 4.4.0, Unity.XR.CoreUtils 2.1.1 and Unity.XR.OpenXR 1.9.1 are compiled from the official package sources and shipped alongside the mod.
- Embedded resources no longer require a .resx/resgen step (plain manifest resources).

### 2.10.1 (unofficial rebuild)
- Restored the full gamepad-style menu binding set that had been left commented out (UI submit/cancel, ready/continue, tab left/right, submenu left/right, pause) so the controllers drive menus the same way the original release did. Right-stick click still recenters; the left Y button still pauses/skips.
- Added HP Reverb G2 and Meta Quest Touch Pro OpenXR interaction profiles alongside the existing Oculus Touch / Index / Vive / WMR / KHR-Simple set.
- Hardened controller init: a failure to hook the new Input System no longer silently kills the entire controller pipeline.
- Added staged input diagnostics to the log (interaction profiles registered, XR devices at each hand node, controller attached to the Rewired player, first real button/stick activity) so a single run pinpoints where input breaks.

### 2.10.2 (unofficial rebuild)
- FIXED: no controller input. `BaseInput.CheckDevice` compared the `InputDevice` struct to null, which is always false, so the controller handle was never fetched and every button/stick reader polled an empty device. It now checks `isValid` (and re-acquires the device if a controller sleeps and wakes with a new id).
- FIXED: bogus "Failed to start OpenXR" error at startup. The loader is already initialised and started by XR Management; the redundant second `Initialize()` legitimately returns false. Startup now checks the display subsystem instead.
- FIXED: `NullReferenceException` in the intro-cutscene canvas fix on the current game build, which aborted the rest of the intro setup and left the black fade panel hanging in front of the camera. All lookups are now null-safe and the two intro fix steps are isolated from each other.

### 2.11.0 (unofficial rebuild)
- FIXED: VR hands/aim pointed ~90 degrees up. The hands are driven by the OpenXR grip pose, which is pitched ~90 degrees from the pose the hand models were authored for. A corrective pitch is now applied to both the gameplay hand and the UI pointer so they face forward and the aim ray lines up with the hand. Tunable with the new "Controller angle offset" setting (default 90; try 45/60 if hands point down, -90 if they point up/back).
- FIXED: camera sat at ground level instead of the character's eye level. Seated Mode now defaults ON, which uses a device-relative tracking origin and places the rig at the character's eye level (recenter re-zeros you there). Turn Seated Mode off in the VR settings for standing/roomscale play where your real floor height drives the camera height.

### 2.12.0 (unofficial rebuild)
- Controller aim tilted up ~15 degrees (the "Controller angle offset" default is now 75; lower aims higher, higher aims lower).
- Recenter (right stick click) now works everywhere - title screen, every menu, paused, and during a run. It is polled directly off the controller instead of relying solely on the Rewired action, and the old "menus only" restriction is removed.
- Fixed motion controllers not being able to navigate the pause menu opened during a run: the hands now switch to UI-pointer mode whenever the game is paused.
- Rotated the opening spaceship cutscene camera 180 degrees so the incoming ship is in front of you instead of behind. Tunable with the new "Intro camera yaw" config value (default 180; set 0 for the old orientation).

### 2.13.0 (unofficial rebuild)
- Camera height is now forced to the character's eye level with a device-relative tracking origin, regardless of the Seated Mode config. This is what caused the "camera at ground" regression: with Seated Mode off, a floor-relative origin dropped the view to the character's feet.
- Recenter: because the tracking origin is now always device-relative, right-stick-click recenter works reliably in every menu and in-run. A one-time "[VR input] Recenter triggered" log line confirms it fires.
- Intro cutscene camera yaw is now applied every frame while the intro scene is active (the previous one-time rotation was overwritten by the cutscene each frame), so the ship faces you. Still tunable via "Intro camera yaw" (default 180).
- The controller aim-pitch config key was renamed to "Controller aim pitch v2" so the new default of 75 (aim ~15 up) actually applies even if you already had the old key saved at 90.
- Added "[VR config]" and hand UI-mode log lines to make the remaining input/menu behavior diagnosable from a single log.

### 2.13.1 (unofficial rebuild)
- Intro cutscene: actually rotate the view to face the incoming ship. The intro camera is driven by a head-tracking pose driver that overwrites the camera's own rotation each frame, so rotating the camera state did nothing; the base orientation comes from the camera's parent, which is now rotated by "Intro camera yaw" (default 180) instead.
- Demoted the harmless "Failed to start OpenXR" startup line to an informational note (the display subsystem simply finishes starting a frame later; VR was running fine).
- Log confirms 2.13.0 was working: build loaded, device origin + eye level forced, aim pitch 75, and recenter fires and is accepted by the runtime.

### 2.14.0 (unofficial rebuild)
- Removed the black "focus lost" overlay that covered the centre of the view. It was the game window's out-of-focus screen, which is triggered constantly while you're in the headset (the desktop window isn't focused). It no longer draws in VR.
- Pause menu now opens with a single press of Y (was a release-triggered input that felt like two presses).
- Skip the intro with the Y button (left secondary), driven directly off the controller.
- UI pointer hand is now parented to the tracked hand, so in menus it appears at the correct size and distance instead of floating far away.
- Recenter now also does a manual yaw recenter (rotates the view so your current facing becomes forward), since some OpenXR runtimes accept TryRecenter() without actually moving the origin. Works in menus; logged once as "manual yaw recenter applied".
- VR settings tab: made the builder resilient and added "[VR settings]" diagnostics. If the tab is still missing, the log now says exactly which settings-panel path changed in this game build so it can be fixed precisely.

### 2.14.1 (unofficial rebuild)
- FIXED the recenter regression from 2.14.0. It read the head yaw as a 0-360 value, so a small angle became a ~356 degree spin, and it rotated the camera rig that the motion-control hands share - so every right-stick click spun the view and threw the hands around (and could leave them pointing the wrong way in a run). Recenter now normalizes the angle and only does the manual menu recenter when NOT in a run; in a run it leaves the hands/rig alone and just calls the runtime recenter. Gameplay hand orientation is back to normal.
- The intro "black box" is the game's full-screen fade image, which renders as a black panel in the middle of the VR view. It is now disabled during the intro instead of being re-homed in front of the camera. (This had nothing to do with window focus - my mistake last build.)

### 2.14.2 (unofficial rebuild)
- Intro black box: the intro's 2D UI canvas was being re-homed to a large panel 12.35 units in front of the camera, and that panel is the black box. It is now disabled entirely during the intro (the 3D ship scene is unaffected). Disabling only the "Fade" child in 2.14.1 was not enough - that was my mistake.
- Added a one-time canvas dump ("[VR intro] Active canvases") so if any black panel remains, the log names the exact object.

### 2.14.3 (unofficial rebuild)
- FIXED the hands pointing down. Regression from 2.14.0: the UI pointer hand was cloned from the pointer hand AFTER the pose correction had already been applied to it, and then the correction was applied a second time - so the hand was double-rotated (~150 degrees) and pointed straight down. The duplicate application is removed; the hand now uses the single, correct correction (aim pitch 75 by default). Gameplay hands were always single-corrected and unchanged.

### 2.15.0 (unofficial rebuild)
- Intro music and typed dialogue restored. The canvas I disabled in 2.14.2 ("SpashScreenCanvas") is the intro STORY canvas - it holds the typed text and drives the music. It is kept alive again (world-spaced in front of the camera as in the original mod) and only its opaque full-screen panel images are hidden, so no black box but text + music work.
- Recenter finally fixed properly. Previous versions read the head's yaw relative to the parent and added its negative to the parent each click - but head tracking rewrites the camera's local rotation every frame, so that yaw never changed and each click re-applied the same offset: continuous rotation. Recenter now sets the parent's rotation ABSOLUTELY (base rotation + the single offset that makes your current facing "forward"), so it snaps to forward and a second click while facing forward is a no-op.
- In-run pause menu navigation: the VR controller is now presented to the game as a GAMEPAD input source instead of mouse+keyboard. The left stick moves the selection and A / right trigger confirms, B cancels, in every menu including the mid-run pause menu - no laser pointer required. The pointer still works as a bonus wherever it is visible.

### 2.15.1 (unofficial rebuild)
- FIXED hands spawning far behind you / in the sky after using a menu. The hands re-parent under the camera's parent every frame with a world-position-preserving SetParent. That was harmless while that parent never moved, but the (now working) recenter rotates it - so the hands' stale world offset got baked into their local transform and they drifted away. They now re-parent in local space and rotate with the recenter, staying in your hands.
- FIXED A / right trigger not activating menu items in a run. The game's gamepad-style menus listen for the UISubmit action (14), but the VR UI map only bound the right trigger to the mouse-click action (20, which needs a hovering cursor). UISubmit is now bound to both the right trigger and the A button; B remains Cancel. The click binding is kept so the laser pointer still works wherever it's visible.
- Intro typed text not rendering in the headset: tabled per your request (music is back; the text only shows on the flat screen). Will revisit later.

### 2.16.0 (unofficial rebuild)
- Hands: HandController reverted to the exact code from 2.13.1 (the last version where the hands were right), plus ONE change: the hand is placed into the camera's parent in local space. Root cause of the "far behind me / in the sky" hands: the camera's parent lives inside the VR rig, which is scaled to the character, and a world-preserving re-parent multiplied a stale world offset by that scale and rotation every time the (now working) recenter rotated the rig. Every hand patch since 2.14.0 was fighting that symptom instead of the cause; those patches are gone.
- Point-and-click menus restored. The input source is back to MouseAndKeyboard (as in the original mod) so the laser pointer's hit position drives the cursor and the right trigger clicks. The real UISubmit binding on A / right trigger is kept, so stick navigation + A still works as a fallback in every menu.
- NEW: "Auto Sprint" option in the VR settings tab (and VRMod.cfg). When on, pushing the left stick sprints automatically - no stick click needed. Abilities that normally cancel sprinting still do.

### 2.17.0 (unofficial rebuild)
- Menus are now purely gamepad-style, using the motion controllers: left stick moves the selection, A or right trigger confirms, B goes back, grips switch tabs, Y opens/closes the pause menu. This works identically on the title screen, in settings, and in the mid-run pause menu.
- Removed the laser-pointer point-and-click system and the tracked hand models in menus. The pointer overwrote the cursor position every frame from the hand's aim ray; with the hands mispositioned the cursor landed nowhere, which made menus un-clickable and overrode the stick navigation - that is why there was no input in the in-run menu. Gameplay hands (weapons) are unaffected.
- Auto Sprint option (from 2.16.0) remains in the VR settings tab.

### 2.17.0 packaging note
- The package is now published under the author name **Blowntobytes** (unofficial fork of DrBibop's mod, MIT). Re-import the zip in r2modman with Author set to `Blowntobytes` after uninstalling any older VRMod entry. The plugin ID stays `com.DrBibop.VRMod` for compatibility with mods that depend on VRMod/VRAPI.
- Added `author`/`website_url` for the fork to `manifest.json`, refreshed `manifestV2.json`, rewrote `README.md` for this build (controls table, OpenXR requirements, known issues), and added `icon.png` to the repository so the Thunderstore package can be assembled from the source tree.

### 2.18.0 (unofficial rebuild)
- FIXED the missing VR settings tab. The tab was added only when the main-menu settings prefab was named exactly "SettingsPanel"; this game build uses a different prefab name, so the check silently failed and no tab appeared (and no error was logged). The mod now detects the settings screen by its structure - a header navigator plus a "SafeArea/SubPanelArea" - so the VR tab is added regardless of the prefab's name, on the title screen and in the in-run pause menu alike.
- The custom key/controller binding rows at the bottom of the VR tab are now added defensively: if their templates moved in this build, only those rows are skipped and the VR options (Auto Sprint, aim pitch, intro yaw, seated mode, and the rest) still appear, instead of the whole tab being dropped.
- Added clear "[VR settings]" log lines: one when the tab is successfully added (with the resulting header list), one when the in-run pause settings open, and a one-time hierarchy dump if a path is ever missing, so any future settings-panel change can be pinpointed from a single log.

### 2.18.1 (unofficial rebuild)
- FIXED: on the character select screen one press of A both picked the survivor and readied up (starting the run). Every menu button polls the same "submit" press in its own Update; picking a survivor rebuilds the screen, the selection jumps to the Ready button as the fallback, and that button then saw the SAME press still held and clicked itself. A guard now lets one press activate exactly one button: A picks the survivor, the Ready button becomes highlighted, and a second A (or stick navigation to it) readies up. A real second press is never affected.
- The "alternate submit" action (the game only uses it for the reset-controller-binds prompt in settings and the hold-to-confirm on the profile screen) was also mapped to A; with the gamepad-style input source that could open the reset-to-defaults dialog from an ordinary A press. It now lives on X, matching the gamepad's X button.
- Log line "[VR input] Ignored a second menu activation from the same press" shows when the guard intervenes, naming both buttons.

### 2.18.2 (unofficial rebuild)
- Character select: the grips now jump between the two areas of the screen. Right grip selects the rules column on the right (Difficulty, Artifacts, Expansions / DLC; press again to step down to the next category), left grip returns to the survivor grid. The stick could not find a path between the two areas, which made the right-hand side unreachable with motion controllers.
- Recenter now re-centres the menu layer too. Menus that open during a run (pause menu, dialogs, the run report) are parked at a fixed spot in front of where you were looking when they opened; after a recenter the head's direction changes and they stayed put. Every such menu is now re-parked in front of the new forward direction two frames after the recenter, once the runtime has applied it.
- The title-screen/menu recenter itself is evaluated two frames after the request instead of immediately, so it reads the head direction AFTER the runtime's own recenter. This removes the case where the two stacked and over-rotated.
- Every recenter logs one "[VR input] Recenter:" line with the head yaw before/after, so the log shows exactly what each click did.

### 2.18.3 (unofficial rebuild)
- FIXED recenter not moving the menu layer - the real cause this time, confirmed by the 2.18.2 log. The mod recenters by rotating the "Camera Offset" node that the scene camera and the hands hang from; the UI camera, which renders every menu, sat OUTSIDE that node as a sibling, so it never turned. It is now a child of the same node, so the environment and the menus turn together. The log also showed that the headset runtime's own recenter call is accepted but never moves the head pose on this setup, so the mod's recenter is now applied in runs as well as menus, as a local yaw offset (correct even while turning with the stick).
- Character select right side: the 2.18.2 lookup searched for the game's "rule category" objects and found none in this game build (the log said so on every press). The right grip now takes every active, interactable control on the right half of the lobby screen - whatever it is - ordered top to bottom, and steps through them on each press. Left grip still returns to the survivor grid. The first press logs an inventory of what it found, so the order can be tuned from a single log if needed.

### 2.18.4 (unofficial rebuild)
- Code is exactly 2.18.1 plus ONE addition. Everything 2.18.2 and 2.18.3 changed (recenter rework, UI camera re-parenting, menu re-parking) is reverted; 2.18.3 broke the scale of the view and neither build delivered.
- Character select, right half of the screen (expansions / DLC, artifacts, ready): the choice buttons there have no navigation links, so the stick could never reach them, and the game's event system drops any selection of such a button while a gamepad is the input source - which is why 2.18.3's jump "selected" a DLC button that never showed. Those controls are now given automatic navigation, the right grip jumps onto the first of them (press again to step to the next), the stick moves between them, and the left grip returns to the picked survivor.

### 2.18.5 (unofficial rebuild)
- FIXED the right trigger doing three things at once. It was bound to Submit, the mouse click AND "next sub-menu" simultaneously; Submit won, so in the character select screen a pull clicked the selected survivor and threw you back to the grid instead of moving to the next panel - exactly what the screen's own footer prompt says the triggers should do. Submit is now the A button alone, and the triggers are left free to be previous / next sub-menu as the game advertises.
- Lobby grip jump (from 2.18.4) is unchanged and confirmed working in the log: it steps through the DLC and artifact buttons one press at a time. Because the highlight was not visible on screen, this build adds a one-line check written a frame after the jump (which event system holds the selection, how many exist, whether the button is interactable), so the reason it does not show can be pinned down instead of guessed at.
- The first grip press also lists the screen's own gamepad shortcuts and the action each listens for, so the VR bindings can be matched to the on-screen prompts.

### 2.18.6 (unofficial rebuild)
- FIXED the root cause of the confusing menu prompts: the on-screen glyphs and the actual buttons were wired to different things. The mod ships VR button artwork indexed by controller element (ControllerGlyphs.standardGlyphs), and the element names agree with it - 17 "Submit", 19 "Ready", 20/21 "TabLeft/Right", 22/23 "SubmenuLeft/Right" - but the OpenXR port bound 17 to the right trigger, 19 to A, and swapped the triggers with the grips. So a prompt drawn with a trigger glyph was fired by a grip, and Submit was drawn as A while it lived on the trigger. Those elements are now bound exactly as the glyphs describe: A = Submit, B = Cancel, X = Ready/alt, triggers = previous/next tab, grips = previous/next sub-menu, Y = pause, right stick click = recenter. Every prompt in every menu now names the button that actually performs it - the controllers simply behave as the gamepad the game thinks it is talking to.
- Because the grips are now genuinely the game's sub-menu navigation, the character select screen should reach its right-hand panels (expansions / DLC, artifacts) natively. The mod's own grip jump is kept only as a fallback: it waits a frame and acts solely when a grip press left the selection untouched, so the game's navigation always wins, and it logs which of the two handled the press.

### 2.18.7 (unofficial rebuild)
- FOUND why the character select screen's right-hand panels were unreachable, whichever button the triggers were on. The screen's own prompts are driven by components that poll Rewired BY NAME for "UIPageLeft" and "UIPageRight" - and no such names exist in the generated RewiredConsts the mod binds against (it has UITabLeft / UITabRight / UISubmenuLeft / UISubmenuRight instead). The VR controller therefore had nothing bound to the actions this screen listens for, so the triggers did nothing at all in 2.18.6. Those two actions are now looked up by name at runtime and bound to the left and right triggers - exactly the buttons the on-screen prompt draws for them - so the page navigation works as the screen advertises.
- Every Rewired action id and name is written to the log once, so any other prompt that turns out to poll by name can be matched to a binding immediately.
- The mod's fallback jump no longer selects controls the player cannot see. It now uses Unity's full interactable test (which honours a CanvasGroup up the hierarchy) and skips anything faded out or scaled away - which is what the earlier builds were doing: the selection genuinely moved, but to buttons sitting on a page that was not on screen, so nothing appeared to happen and only the selection sound was audible.

### 2.18.8 (unofficial rebuild)
- Character select navigation is now consistent and on the triggers alone: RIGHT TRIGGER moves to the panels on the right (expansions / DLC, artifacts), LEFT TRIGGER goes back to the survivor grid, and the stick moves within whichever page is showing.
- FIXED the phantom clicking. The triggers were carrying two actions at once: the page actions (UIPageLeft 41 / UIPageRight 42, which the screen actually polls) plus UITabLeft 29 / UITabRight 30. Those last two are not menu tabs in this game build - ModelPanel.Update reads them to spin the character preview - so every trigger pull fired two unrelated things. They are no longer bound to anything.
- The GRIPS are now completely unbound in menus: the sub-menu actions were removed from them and the mod no longer reads them at all, so squeezing a grip while navigating does nothing. Their gameplay roles (utility and special skill) are untouched, since those live in a separate map.
- The mod's lobby helper no longer handles any buttons. It only gives the right-hand choice buttons - which ship with navigation mode "None" - automatic navigation, which is what lets the stick walk the DLC and artifact rows once the page is open.
- 2.18.7 is tagged in the repository as `known-good-2.18.7` and kept in the build folder as KNOWN-GOOD-Blowntobytes-VRMod-2.18.7.zip, as a restore point to fall back to.

### 2.18.9 (unofficial rebuild)
- Restored grip tab-switching in the settings panel. 2.18.8 unbound the grips wholesale to stop them interfering on the character select screen, which also took away the one thing they did right. The two settings-tab actions (UISubmenuLeft / UISubmenuRight) are back on the left and right grips - the buttons the glyph table already draws for them - and nothing else is: the character select screen listens only for the page actions, Pause, UICancel, Info and UISubmitAlt, so a grip squeeze there still does nothing, and the mod itself reads no grip input at all.

### 2.19.0 (unofficial rebuild)
- Settings tabs switch with the grips again, this time without depending on a Rewired binding at all. Nothing in the game's code polls an action for tab switching: the tab buttons are driven by HGGamepadInputEvent components whose action NAME lives inside the prefab, so it cannot be read from the assemblies - which is why binding UITabLeft, then UISubmenuLeft, then UIPageLeft all missed. The mod now calls the very methods those prefab events call (MoveHeaderLeft / MoveHeaderRight on the panel's header controller) when a grip is squeezed, reading the buttons straight off the controllers like the recenter click does.
- The submenu actions were removed from the grips at the same time, so a squeeze can only ever move the tab once.
- The grips still do nothing on the character select screen: the tab handler ignores that screen outright.

### 2.19.1 (unofficial rebuild)
- FIXED Auto Sprint, which was built on a wrong assumption about how the game reads the sprint button. For ordinary (non-flying) characters RoR2 does not treat sprint as a hold: PlayerCharacterMasterController records GetButtonDown and PollButtonInput TOGGLES the sprint state on each press. Auto Sprint held the button down while the stick was pushed, which produced exactly one press - so sprint engaged once and never returned after anything cancelled it (firing a skill, taking a hit), because no fresh press could happen while the stick stayed pushed. Releasing and re-pushing the stick sent a second press, which toggled sprint back OFF; that is why it felt like you had to stop and start moving again to get it to engage.
- Auto Sprint now sends a fresh press only while you are moving AND not already sprinting, one frame at a time with a short gap so each press is a clean edge, and stops as soon as sprint is on - so it engages immediately, never toggles itself off, and comes back by itself after a cancellation. Flying characters take the game's other code path, which genuinely is a hold, and are handled that way.


### 2.19.2 (unofficial rebuild)
- Works on every PC VR headset, not just Quest over Virtual Desktop. The mod already talks to OpenXR and registers the Oculus Touch, Quest Touch Pro, Valve Index, HTC Vive, HP Reverb G2, Windows Mixed Reality and KHR Simple controller profiles, so Rift CV1 (Oculus PC runtime), Index and Bigscreen Beyond (SteamVR) use the same code path as Quest. All seven profiles were verified to publish a GripButton control, so the grips work on Index and Beyond even though their controllers report a squeeze VALUE rather than a click.
- REMOVED the "Use Oculus mode" trap. That option still ran the pre-OpenXR code path, which scanned Unity's LEGACY joystick names for "left" / "right" and aborted the whole VR input pump when none matched - which is always under OpenXR. Turning it on silently killed every controller input. The option is now inert and its description says so; which runtime is used is decided by Windows' active OpenXR runtime, not by the mod.
- The log now names the active OpenXR runtime, its version, the OpenXR API version and the plugin version, so a headset-specific problem can be identified from one line.

### 2.19.3 (unofficial rebuild)
- Fixed a hang at ~99% loading on headsets without haptics-suit hardware (e.g. Oculus Rift CV1). A TypeLoadException from ShockwaveManager.dll was killing the initialisation coroutine before the game could finish loading. Haptics-suit type references are now isolated so they are never resolved unless the DLL is actually present.

### 2.19.4 (unofficial rebuild)
- ProperSave compatibility: the Y button on the character select screen now triggers Load (ProperSave's save-game prompt) instead of opening the pause menu. Outside the lobby, Y still opens pause as usual.
- Added the correct VR controller glyph for the Load action across all supported controller types.

### 2.19.5 (unofficial rebuild)
- Fixed the Y button triggering both the Load prompt and the Info/Scoreboard overlay in the lobby. Y drives two input elements (Pause and Info hold) - both are now suppressed when remapping to Load.

### 2.19.6 (unofficial rebuild)
- Constrained joystick navigation within lobby panels using per-panel Explicit navigation (superseded by 2.19.8).

### 2.19.7 (unofficial rebuild)
- Withdrawn: this build failed to start VR. Do not use; superseded by 2.19.8.

### 2.19.8 (unofficial rebuild)
- The left stick is now fully confined to one side of the character select screen at a time. Every control is classified as left-side (survivor grid and the survivor's characteristics) or right-side (difficulty, expansions, artifacts), and stick navigation is wired only between controls on the same side - the highlight can never drift across the screen or get lost.
- Switching sides is done ONLY with the triggers, exactly as the on-screen prompts say: RIGHT TRIGGER moves to the right-hand panels, LEFT TRIGGER returns to the survivor grid.
- The Ready button (X) and Load (Y with ProperSave) are untouched and work exactly as before; the footer bar keeps its own navigation.

### 2.19.9 (unofficial rebuild)
- Fixed the stick walking the highlight off the right edge of the character select screen. The right-hand panels (difficulty, expansions, artifacts) keep some selectables laid out beyond the visible canvas; those are no longer wired into navigation, so pushing right simply stops at the edge of the visible canvas instead of going off-screen.
- Renamed the mod to "Resurrected VRMod" (r2modman display name); the plugin GUID is unchanged for compatibility.

### 2.19.10 (unofficial rebuild)
- The 2.19.9 off-screen check used the canvas rect, which is not the real visible boundary (the character-select canvas is nested inside a larger root canvas, so nothing was actually excluded). Navigation is now built from controls that project inside the visible screen through the UI camera, so the left stick physically cannot move the highlight past the right edge of the visible character-select panel.

### 2.19.11 (unofficial rebuild)
- Character-select off-screen fix, third attempt and this time root-caused: the left/right classification anchor (the survivor grid) was being excluded by the 2.19.10 screen-projection check, so every sweep collapsed to a centerline split (boundary x=0 in the log) and the right-hand panel was still escapable. Navigation is now built from controls that lie inside the character-select canvas's own world-space bounds (the canvas UIFixes converts and sizes), so the survivor grid is kept as the anchor AND the off-canvas rules-panel controls can no longer pull the highlight off-screen.
- Removed the right-stick-click recenter. The right stick click was driving BOTH Ping and Recenter, so pinging mid-run also recentered the view. Right stick click now only pings; recenter remains available on the keyboard (Right Ctrl) / D-pad up binding.

### 2.19.12 (unofficial rebuild)
- Character-select off-screen fix v4. The canvas bounds check in 2.19.11 was still too wide — every control, including the ones that pull the highlight off the visible screen, sits inside the character-select canvas, so nothing was excluded (the log said "skipped 0 off-canvas"). The stick can only move the highlight to controls that actually project inside the UI camera's view frustum now, so pushing right stops at the edge of what is actually visible in the headset. The survivor-grid anchor is computed independently of the visibility check, so it can never collapse the way 2.19.10 did.

### 2.19.13 (unofficial rebuild)
- Diagnostic build. After several failed attempts to bound the character-select stick to the visible screen, this build logs the actual lobby layout so the root cause can be seen instead of guessed: the canvas identity and render mode, the UI camera that renders it (position, rotation, FOV), and every selectable's name, world/local/screen position and frustum result. The layout is printed once when the survivor grid is showing and again the first time the rules panel opens (the state that reproduces the off-screen stick). Please run this build, open the character select, open the right-hand panels, and send the log.

### 2.19.14 (unofficial rebuild)
- Character-select off-screen fix, now root-caused from the diagnostic log. The 1920x1080 character-select canvas becomes 9.6m wide at 5m in world space, and the game lays the rules-panel controls (difficulty, expansions, artifacts), the Lobby button and the chat box OUTSIDE the canvas's own bounds - the Lobby button sits at canvas-local (912, 943) which is above the canvas top, and the chat box below its bottom. Pushing the left stick right walks the highlight into those off-canvas controls, which is why it disappears. Navigation is now built only from controls that lie inside the canvas's own local rect (the visible screen), with the survivor-grid anchor still computed independently so the left/right split can never collapse. The log line reports how many controls were skipped as off-canvas.

### 2.19.15 (unofficial rebuild)
- Reverted the 2.19.14 canvas-rect filter: the diagnostic log proved it does nothing (in world space every control - including the Lobby button and the rules panel - is inside the canvas rect, so nothing was ever excluded; the log showed "skipped 0 off-canvas"). The real problem is that the whole character-select canvas is simply too WIDE: 1920x1080 at scale 0.005 = 9.6m at 5m (~87 degrees), which pushes the rules panel and Lobby button out of the comfortable view. This build instead shrinks the character-select canvas to scale 0.0026 (~5m wide at 5m, about 53 degrees - the same apparent size as the other menus), so the survivor grid AND the rules panels are all comfortably in view and the stick can no longer walk off-screen. The lobby layout diagnostic logging remains, so the next log confirms the new geometry.

### 2.19.17 (unofficial rebuild)
- Diagnostic build: trace the actual selection. Several geometry-based fixes (canvas rect, frustum, shrinking the canvas) all failed to change the behavior, which proves the problem is NOT that controls are laid out off-screen. This build reverts the canvas shrink and instead logs every selection change the game makes while the character select screen is open: each "Lobby select:" line names the exact control the highlight moves to (with its canvas-local position and interactable state). Run this, open the character select, push the left stick right on the Difficulty/Expansions/Artifacts panel until the highlight disappears, and send the log - the "Lobby select:" lines will name the control it landed on, which is the definitive answer to what is happening.

### 2.20.0 (unofficial rebuild)
- EXPERIMENTAL runtime hands for characters without authored VR hand models (the Seekers of the Storm survivors: Seeker, Chef, False Son - and any other body the asset bundle does not cover). Instead of the bare default pointer, the mod now bakes the character's own right-arm mesh (found by bone name, triangles selected by skin weight) into a rigid hand model attached to the tracked controller, with the character's real materials. The left controller mirrors it exactly like the authored hands. A muzzle is registered so aim rays and abilities fire from the controller.
- Per-character live tuning: config section "RuntimeHands (experimental)" gets RotX/RotY/RotZ, PosX/PosY/PosZ and Scale entries per body. With "Debug mode" on, the config file is re-read every 3 seconds, so the offsets can be edited with the headset on and the hand moves on save. Debug mode also draws RGB axis lines at the hand origin (blue = aim direction) and logs every entity state the local character enters, to map exactly which states each DLC skill uses for proper per-skill support later.
- Fully additive: characters with authored hand prefabs are untouched, and any failure in the runtime build falls back to the previous default-pointer behavior. "Enable runtime hands" in the config turns the whole feature off.

### 2.20.1 (unofficial rebuild)
- Fixed runtime hands finding zero triangles ("no triangles skinned to 'R_Hand'", seen on Seeker). The hand bone found by the global name search can be a different skeleton instance than the one each SkinnedMeshRenderer is actually skinned to (ragdoll/IK duplicates share bone names). The hand bone is now matched inside each renderer's own bone array by name and that instance is used for both the bone-subtree test and the mesh space, so bone weights always line up.
- Lowered the triangle keep threshold from 0.4 to 0.3 average hand weight.
- Debug mode now logs one line per renderer (bone count, whether the hand bone is in its skeleton, bones in set, kept vertices, max hand weight) so any future zero-triangle case is diagnosable from the log alone.

### 2.20.2 (unofficial rebuild)
- Runtime hands now also capture WEAPONS: any rigid (non-skinned) mesh parented under the hand bone chain - a held cleaver, club, gun or prop - is cloned into the runtime hand at its exact grip position, with its materials. This matches the original mod's authored hand models, which are hand + arm + weapon and nothing else. Skinned weapon parts were already captured via bone weights; body parts are still excluded by the skin-weight filter.

### 2.20.3 (unofficial rebuild)
- Root-caused the missing runtime hand on Seeker: the game applies character skins ASYNCHRONOUSLY, so when the hand pair is applied at spawn the model's SkinnedMeshRenderers still have null meshes - the bake ran once, too early, found nothing and gave up. The build is now retried every half second (up to 10 s) until the meshes exist, then the hand pair is reapplied so the muzzles and aim origin are wired exactly like the normal path.
- Debug mode now logs the model's renderer counts and calls out null (not-yet-applied) meshes explicitly, so timing issues are visible in the log.

### 2.20.4 (unofficial rebuild)
- Fixed the runtime hand dying at the finish line: the 2.20.3 log shows the bake SUCCEEDED on Seeker (886 hand vertices captured, weight 1.00) and then the build was aborted by the debug axis lines - RoR2 replaces Shader.Find with a catalog lookup that throws on unknown shader names ("No GUID record is available for shader name") instead of returning null. Shader lookups are now individually guarded with fallbacks, and the axis-line step can never abort the hand build again (worst case: no axes, hand still appears).

### 2.20.5 (unofficial rebuild)
- Runtime hands no longer point ~90 degrees down: the HandController applies a SteamVR-era pose correction (ControllerAngleOffset, for hand models authored in the old rig) to every hand root - the runtime-baked mesh is not authored for that pose, so the correction is now cancelled inside the runtime hand and the auto-alignment (fingers along the controller's forward axis) holds. Per-character Rot/Pos offsets still apply on top.
- Debug axis lines now borrow the material from the controller's own aim-ray LineRenderer (always present, known-good in this game) instead of Shader.Find, which is a throwing catalog lookup in RoR2 and had no usable entries - this is why no RGB lines appeared.

### 2.20.6 (unofficial rebuild)
- Hand mesh and aim/laser are now tuned INDEPENDENTLY: RotX/Y/Z + PosX/Y/Z move only the hand mesh, and new AimRotX/Y/Z entries rotate only the aim/laser. Previously both hung off one transform, so fixing one broke the other (hands up + laser forward, or hands forward + laser down).
- "Include forearm" now takes effect immediately: changing it re-bakes the hand pair in place (previously the mesh was baked once at spawn, so the toggle appeared to do nothing). It also captures the whole forearm subtree including twist/roll bones instead of just the single parent bone.

### 2.20.7 (unofficial rebuild)
- Fixed the magenta/untextured runtime hands. RoR2's character materials (hgstandard) only render correctly when they go through the game's own CharacterModel.UpdateRendererMaterials pipeline, which the mod already drives for hands via MotionControls.UpdateHandMaterials - but that pipeline only touches renderers listed in Hand.rendererInfos, and the runtime hand shipped with that list EMPTY, so its renderers were never processed and fell back to an untextured material. The runtime hand now registers a RendererInfo per baked renderer, using the default material asset from the model's own baseRendererInfos (matched to the source renderer) rather than the runtime instance, and copies each source renderer's MaterialPropertyBlock so per-renderer shader data (elite index, dither/fade, tints) carries over.
- Runtime hands also opt in to copyMaterialsFromCharacterModelIfNoSkin, so skin changes apply to them like they do to the authored hands.

### 2.20.8 (unofficial rebuild)
- Runtime hands now cover ALL Seekers of the Storm survivors out of the box - Chef (ChefBody) and False Son (FalseSonBody) as well as Seeker. The system was always body-agnostic (it runs for any character with no authored VR hand), but new characters started from zeroed offsets; they now start from a shared set of "Default: ..." angles pre-set to the values verified on Seeker, since the DLC survivors share rig conventions. Each character still gets its own <BodyName>_* entries to override, so Chef and False Son can be adjusted independently without disturbing Seeker.
- "Include forearm" now defaults to ON, matching the verified setup.
- The log now prints the offsets each character starts with, so the tuning starting point is visible without opening the config.

### 2.20.9 (unofficial rebuild)
- Fixed Chef getting no runtime hands and no config entries. The hand-bone search required a bone whose name contains "hand", and Chef's rig has none: his chain is Shoulder_R -> Elbow_R -> ElbowPart2_R -> Wrist_R -> MiddleFinger1_R / ThumbFinger1_R. The search now falls back to "wrist" bones on the correct side, and finally to a fully rig-agnostic heuristic: the bone with the most finger-like children (finger/thumb/index/middle/pinky/ring/digit) on that side. The log names the bone it picked and how it was identified.
- Because the per-character config entries are only created once a hand is successfully built, ChefBody_* entries were never written - they now appear on his first spawn, seeded from the shared "Default: ..." values like every other character.

### 2.20.10 (unofficial rebuild)
- Each controller now builds its hand from its OWN arm instead of always baking the right arm and mirroring it. Characters that carry a different weapon in each hand are now correct - Chef's cleaver (axe_jnt, under Wrist_L) stays on the left and his pizza cutter (pizzaCuter_jnt/blade_jnt, under Wrist_R) on the right, instead of a pizza cutter appearing in both hands.
- The left HandController carries a negative X scale to mirror the authored right-arm hand models; a natively-left arm cancels that mirror so it renders the correct way round, and the shared per-character offsets are reflected across the YZ plane for that hand so both hands still roll symmetrically from one set of values.
- New config option "Mirror one arm to both hands" (default OFF) restores the old right-arm-mirrored behaviour for any character whose left arm comes out wrong.
- If a rig only names one side recognizably, the other arm is mirrored for that hand rather than losing the hand entirely, and the log says so.

### 2.20.11 (unofficial rebuild)
- Added optional per-character LEFT HAND mesh rotation: <BodyName>_UseSeparateLeftHandRotation (default false) plus <BodyName>_LeftRotX/LeftRotY/LeftRotZ. With the toggle off the left hand keeps mirroring the main Rot values, which is correct for symmetric hands; with it on, the left hand mesh uses the Left* angles directly, so a character whose left hand holds a different weapon (Chef's cleaver vs the right-hand pizza cutter) can be aligned independently. The aim/laser is deliberately NOT split - it stays mirrored from the AimRot values for both hands.

### 2.20.12 (unofficial rebuild)
- Reworked the runtime hand config around independent per-hand tuning, as the shared approach did not hold up across the DLC2 survivors:
  - REMOVED the universal "Default: Hand/Aim ..." entries. Starting angles are now compile-time defaults per character, so they add no clutter to the config file.
  - REMOVED the "UseSeparateLeftHandRotation" toggle and the "Mirror one arm to both hands" option. The left hand is never mirrored from the right on these characters.
  - Each hand now has its own mesh entries: <Body>_LeftRotX/Y/Z, <Body>_LeftPosX/Y/Z and <Body>_RightRotX/Y/Z, <Body>_RightPosX/Y/Z. Neither side derives from the other.
  - The aim/laser stays universal per character (<Body>_AimRotX/Y/Z applies to both hands), as does <Body>_Scale.
- All three DLC2 survivors (SeekerBody, ChefBody, FalseSonBody) are registered at startup, so every entry is present in VRMod.cfg without having to play each character first.
- The left controller's built-in mirror is now cancelled on the hand MESH only rather than the whole hand root, so the shared aim values behave identically on both hands while each mesh is posed independently.

### 2.20.13 (unofficial rebuild)
- Fixed runtime hands vanishing after a stage change (e.g. entering the Bazaar), leaving only the attached item displays floating where the hands should be. A stage change builds a brand new character model, and the hand baked from the previous - now destroyed - model kept being used, so its mesh was gone. Each runtime hand now remembers which model instance it was baked from and re-bakes itself when it sees the model change or its renderers disappear.
- Added "Item display scale on hands" (default 0.5). Item pickups the game attaches to the VR hands and forearms - razor wire above all - are modelled for the third-person character and swallow the view in first person at full size; they are now scaled down, with the original size of each display remembered so the factor never compounds.

### 2.20.14 (unofficial rebuild)
- Fixed the hand bone search picking a bone out of an ITEM DISPLAY instead of the character. Chef's left hand was being built from a bone literally named 'hand.l' belonging to an attached item model (3 bones, no arm) rather than his own Wrist_L, because item displays are parented into the character model and carry their own rigs. The search is now restricted to bones that actually drive the character's own skinned meshes, so accessory rigs can never be mistaken for an arm.
- Fixed the item display scale doing nothing. The scale pass looked for ItemDisplay components under the hand controller, but items attached to the hand are BAKED INTO the runtime hand mesh as plain copies, so there was nothing for it to find. Baked parts that came from an item display are now marked at bake time and scaled by "Item display scale on hands" directly, with each part's authored size captured once so the factor never compounds.
- Runtime hands now re-bake as soon as the items on that hand change, so a newly picked-up item appears on the VR hand immediately instead of at the next stage.
- The finger-direction estimate used to align the hand now ignores item displays hanging off the hand bone, so picking up items no longer shifts the hand's orientation.

### 2.20.15 (unofficial rebuild)
- Runtime hands now re-bake when the INVENTORY changes rather than when item displays appear on one specific bone. The previous check only noticed items whose model attaches to the hand bone itself, so most pickups never triggered a rebuild; watching the inventory catches every acquisition regardless of where the item's model hangs.
- Added a visibility repair pass. A runtime hand that built successfully could still be invisible - most often left parked on the no-draw layer by a UI transition that never handed it back (the log shows the UI-pointer mode going ON repeatedly and never OFF). Every second each hand now re-enables disabled renderers, re-activates deactivated objects, and moves anything off the no-draw layer while not in UI mode, reporting in the log when it had to fix something.
- Debug mode now reports the actual render state of each hand every 5 seconds - renderer count, enabled/active flags, layer, UI mode, material name, whether the renderer is considered visible, and world scale - so a hand that is still invisible can be diagnosed from one log line instead of guesswork.

### 2.20.16 (unofficial rebuild)
- Fixed hands and forearms never appearing when loading a save. The debug render report gave it away: the hands' materials were 'matFireRing' and 'matDoubleMag' - item materials, not Chef's skin. Loading a save attaches the carried items to the hand bones long before the character's own skin finishes applying, so the bake ran while meshChef, meshChefPizzaCutter and meshlChefCleaver were all still null, captured nothing but the item displays (rings, magazines, tonic caps), and returned a non-empty container. That counted as SUCCESS, so the retry that waits for the meshes never ran, and the result was a pair of hands made entirely of jewellery.
- A bake that captured no part of the character's own body is now treated as "not ready yet" instead of success: the container is discarded and the retry keeps waiting for the real meshes. The log says so plainly ("Only item displays are loaded ... waiting for the skin before baking the hand").
- The retry window is extended from 10 to 30 seconds, since loading a save takes considerably longer than a normal stage transition.

### 2.21.0 (unofficial rebuild)
- **Auto sprint fix:** When auto sprint is enabled and an attack is performed (trigger or grip held), sprint is now suppressed for the duration of the attack. Sprint re-engages automatically when the attack button is released and the stick is still pushed forward. Previously, auto sprint would re-engage mid-attack and cancel it on many survivors.
- **New accessibility setting: Highlight chests.** Adds a coloured outline to purchasable interactables (chests, drones, shrines, etc.) so they are easier to spot in VR. The highlight is removed when the interactable is purchased or becomes unavailable. Toggle it from the VR settings tab. Off by default.
- **New accessibility setting: Item hints.** Shows the full item description (the detailed stat text from the logbook) instead of the short pickup flavour text in pickup notifications and item tooltips. Pinging a dropped item also prints its description in chat. Toggle it from the VR settings tab. Off by default.

### 2.22.0 (unofficial rebuild)
- **Seeker meditation overlay fix:** The golden meditation petal arc is repositioned above the health bar so it no longer overlaps the HP display in VR.
- **Item hints: taller notification box.** The pickup notification text area is expanded so more of the full item description is visible without being clipped.
- **Item hints: longer display time.** Notifications stay on screen 5 seconds longer when item hints are enabled, giving time to read the full stat text.

### 2.22.1 (unofficial rebuild)
- **Item hints: Command picker descriptions.** When the Command artifact picker panel is open, a text box below the item grid shows the name and full description of the currently highlighted item.

### 2.22.2 (unofficial rebuild)
- **Command picker layout fix.** The grid is widened from 5 to 6 columns to leave more room for the description area at the bottom.

### 2.22.3 (unofficial rebuild)
- **Command picker description text restored.** The item description text below the grid was not visible due to clipping; fixed by reparenting it to the panel root and positioning it inside the panel bounds.

### 2.22.4 (unofficial rebuild)
- **Command picker text centered inside panel.** Reparented description text back to the picker dialog (not the full-screen overlay) and centered it at the bottom of the item grid.
- **Wrist notification box 50% taller.** Increased the extra height added to item pickup notifications from 80px to 120px so the full description text is visible without clipping at the top.

### 2.22.5 (unofficial rebuild)
- **Permanent wrist notifications.** (reverted in 2.22.6)
- **Seeker meditation overlay raised.** (reworked in 2.22.6)

### 2.22.6 (unofficial rebuild)
- **Notification duration set to 30 seconds.** Reverted the permanent notification from 2.22.5. Notifications now display for 30 seconds total and fade normally after that.
- **Seeker meditation overlay fix.** Removed the WristHUD guard from the overlay repositioning hook — the overlay now always moves above the health bar regardless of HUD settings. Added diagnostic logging (`[VR Seeker]`) to confirm the hook fires.

### 2.22.7 (unofficial rebuild)
- **Seeker meditation overlay actually raised above health bar.** Fixed root cause: the health bar uses stretch anchors so `sizeDelta.y` was always 0, making every previous repositioning attempt a no-op. Now uses `rect.height` (the actual rendered height) to calculate the offset.

### 2.22.8 (unofficial rebuild)
- **Seeker overlay hook fix.** The `On.SeekerController.OnOverlayInstanceAdded` hook never fired because the game creates the callback delegate via `ldftn` in `OnEnable`, bypassing MonoMod's method detour. Switched to hooking `OnEnable` instead and subscribing directly to the overlay controller's `onInstanceAdded` event via reflection.

### 2.22.9 (unofficial rebuild)
- **Seeker overlay deferred repositioning.** The overlay's `rect.height` is 0 on the same frame it's created (layout hasn't run yet), so the previous offset was only 42px. Now defers the reposition to the next frame via `onNextUpdate` and shifts the overlay up by the health bar's actual rendered height without re-parenting it.

### 2.22.10 - 2.22.16 (unofficial rebuild)
- Seeker meditation overlay: diagnostic and iteration builds. Root cause finally found in 2.22.16: `SetParent()` with the default `worldPositionStays=true` shrank the overlay's `localScale` to ~0 to compensate for the wrist-canvas -> main-HUD scale difference, so it was reparented correctly but invisible. Fixed with `SetParent(target, false)` + explicit `localScale = 1`.

### 2.22.17 - 2.22.20 (unofficial rebuild)
- Removed the `[VR Seeker]` diagnostic log spam from the lotus overlay fix.
- Discovered that the Seeker HUD is TWO overlays: the lotus petals (created by `SeekerController` when the character spawns) and the meditation arch + arrow mini-game (created by the `Meditate` entity state only when meditation starts). Earlier builds only ever moved the petals.

### 2.22.21 - 2.22.22 (unofficial rebuild)
- **Seeker meditation mini-game raised above the health bar.** The `Meditate` state's overlay is now hooked the same way as the lotus (its callback is wired with `ldftn`, bypassing MonoMod, so `OnEnter` is hooked and the private overlay controller is reached by reflection). It keeps its on-screen composition and is lifted with the lotus so the arch, lotus and arrow inputs all clear the health bar.
- **Arrow inputs point the right way in VR.** The game sets the arrow icons' rotation in world space, which is only correct on a flat screen; on a world-space VR canvas the arrows pointed wrong depending on where you faced. They now rotate in the canvas plane.
- New config entries `[HUD] Seeker lotus height` (210) and `[HUD] Seeker meditation height` (100) so the layout can be tuned without a rebuild.

### 2.22.23 (unofficial rebuild)
- **Notification duration back to the game's own.** The 30-second override that item hints applied to every pickup notification since 2.22.6 is removed; the taller notification box for the longer description text is kept.

### 2.22.24 - 2.22.25 (unofficial rebuild)
- **Single splash logo.** After the logo screen is moved to a world-space canvas, both the UI camera and the scene camera were drawing it at different sizes; the scene camera no longer renders the UI layer during the splash.
- **Intro cutscene text in the headset.** The intro fix used `GameObject.Find("Fade")`, which found the still-loaded splash scene's Fade and converted the logo canvas instead of the story canvas. The intro's own canvas (typed "UES 'Safe Travels'" text, crew subtitles, Skip prompt) is now found by scene and brought into world space, with the fade and letterbox panels hidden.
- **Unfocused-window warning restored** (removed in 2.14.0). A small dark panel with amber text that appears only after the game window has been unfocused for 2 seconds. `[HUD] Show unfocused warning` turns it off.

### 2.22.26 - 2.22.30 (unofficial rebuild)
- **Seated height during the intro cutscene.** Seated mode uses a device (eye-level) tracking origin, so the head sat at the cutscene rig's origin - inside the floor geometry in the cabin shot. The tracked scene camera is now lifted by `[Camera] Intro cutscene seated height (metres)` (default 1.45, as if you stood up that much). Gameplay height is untouched (it goes through `HeightMultiplier` and the character capsule).
- **Unfocused warning moved out of the way.** It now floats closer and 25 degrees below the line of sight, so it can never cover the intro text, menus or the HUD.
- Fixed a regression from 2.22.26-2.22.29 where the intro text disappeared again: the height lift briefly wrapped the intro's UI Camera in a holder object created while the splash scene was still the active scene, which moved the camera into the splash scene and destroyed it when that scene unloaded. Only the scene camera is lifted now, and the helper always creates its holder in the target's own scene.
