using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VRMod
{
    public static class ModConfig
    {
        private const string CONFIG_FILE_NAME = "VRMod.cfg";

        private static readonly ConfigFile configFile = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, CONFIG_FILE_NAME), true);

        internal static readonly Dictionary<string, ConfigSetting> settings = new Dictionary<string, ConfigSetting>();

        internal static ConfigEntry<bool> OculusMode { get; private set; }
        internal static ConfigEntry<bool> FirstPerson { get; private set; }
        internal static ConfigEntry<bool> UseConfortVignette { get; private set; }
        internal static ConfigEntry<bool> SeatedMode { get; private set; }
        internal static ConfigEntry<float> HeightMultiplier { get; private set; }
        internal static ConfigEntry<float> ControllerAngleOffset { get; private set; }
        internal static ConfigEntry<float> IntroCameraYaw { get; private set; }
        internal static ConfigEntry<bool> AutoSprint { get; private set; }
        internal static ConfigEntry<string> HapticsSuit { get; private set; }

        internal static ConfigEntry<string> RayColorHex { get; private set; }
        internal static ConfigEntry<float> RayOpacity { get; private set; }
        internal static ConfigEntry<bool> CommandoDualWield { get; private set; }
        internal static ConfigEntry<float> BanditWeaponGripSnapAngle { get; private set; }
        internal static ConfigEntry<float> MercSwingSpeedThreshold { get; private set; }
        internal static ConfigEntry<float> LoaderSwingSpeedThreshold { get; private set; }
        internal static ConfigEntry<float> AcridSwingSpeedThreshold { get; private set; }
        internal static ConfigEntry<float> RailgunnerWeaponGripSnapAngle { get; private set; }
        internal static ConfigEntry<float> RailgunnerZoomMultiplier { get; private set; }
        internal static ConfigEntry<bool> RailgunnerDisableScopeRay { get; private set; }
        internal static Color RayColor = Color.white;

        internal static ConfigEntry<bool> WristHUD { get; private set; }
        internal static ConfigEntry<bool> WatchHUD { get; private set; }
        internal static ConfigEntry<bool> BetterHealthBar { get; private set; }
        internal static ConfigEntry<bool> UseSmoothHUD { get; private set; }
        internal static ConfigEntry<int> HUDWidth { get; private set; }
        internal static ConfigEntry<int> HUDHeight { get; private set; }
        internal static ConfigEntry<float> BottomAnchor { get; private set; }
        internal static ConfigEntry<float> TopAnchor { get; private set; }
        internal static ConfigEntry<float> LeftAnchor { get; private set; }
        internal static ConfigEntry<float> RightAnchor { get; private set; }
        internal static ConfigEntry<bool> LIVHUD { get; private set; }
        internal static Vector2 AnchorMin { get; private set; }
        internal static Vector2 AnchorMax { get; private set; }

        internal static ConfigEntry<bool> SnapTurn { get; private set; }
        internal static ConfigEntry<int> SnapTurnAngle { get; private set; }
        internal static ConfigEntry<float> SnapTurnHoldDelay { get; private set; }
        internal static ConfigEntry<bool> LockedCameraPitch { get; private set; }
        internal static ConfigEntry<bool> UseMotionControls { get; private set; }
        internal static ConfigEntry<bool> LeftDominantHand { get; private set; }
        internal static ConfigEntry<bool> ControllerMovementDirection { get; private set; }
        internal static ConfigEntry<float> AimStabiliserAmount { get; private set; }

        internal static bool InitialOculusModeValue { get; private set; }
        internal static bool InitialFirstPersonValue { get; private set; }
        internal static bool InitialRoomscaleValue { get; private set; }
        internal static bool InitialMotionControlsValue { get; private set; }
        internal static bool TempWristHUDValue { get; private set; }
        internal static bool TempWatchHUDValue { get; private set; }
        internal static bool TempLockedCameraPitchValue { get; private set; }

        public static bool MotionControlsEnabled => InitialMotionControlsValue;
        public static bool LeftHanded => LeftDominantHand.Value;

        private static bool prevLeftHandedValue;

        internal static ConfigEntry<bool> HighlightChests { get; private set; }
        internal static ConfigEntry<bool> ItemHints { get; private set; }

        internal static ConfigEntry<bool> RuntimeHands { get; private set; }

        internal static ConfigEntry<bool> RuntimeHandsDebug { get; private set; }
        internal static ConfigEntry<bool> RuntimeHandsRay { get; private set; }
        internal static ConfigEntry<bool> RuntimeHandsForearm { get; private set; }
        internal static ConfigEntry<float> RuntimeHandsItemScale { get; private set; }
        internal static ConfigEntry<float> IntroSeatedHeightOffset { get; private set; }
        internal static ConfigEntry<bool> ShowUnfocusedWarning { get; private set; }
        internal static ConfigEntry<float> ReportTextZoom { get; private set; }
        internal static ConfigEntry<float> ReportWidthMeters { get; private set; }
        internal static ConfigEntry<float> SeekerLotusRaise { get; private set; }
        internal static ConfigEntry<float> SeekerMeditateRaise { get; private set; }

        /// <summary>Late-bound per-character runtime hand offset entries (2.20.0 experimental).</summary>
        internal static ConfigEntry<T> BindRuntimeHand<T>(string bodyName, string key, T defaultValue, string description)
        {
            return configFile.Bind<T>("RuntimeHands (experimental)", $"{bodyName}_{key}", defaultValue, description);
        }

        /// <summary>Re-reads the config file so runtime hand offsets can be tuned live in debug mode.</summary>
        internal static void ReloadRuntimeHandConfig()
        {
            try { configFile.Reload(); } catch { }
        }

        internal static void Init()
        {
            OculusMode = configFile.Bind<bool>(
                "VR Settings",
                "Use Oculus mode",
                false,
                "OBSOLETE - leave this off. It is a leftover from the SteamVR version of the mod. This build runs on OpenXR and simply uses whichever OpenXR runtime Windows has set as active (SteamVR, Oculus/Meta, VDXR or WMR), so there is nothing for it to switch. Choose your headset by setting that runtime active in its own software instead."
            );
            FirstPerson = configFile.Bind<bool>(
                "VR Settings",
                "First Person",
                true,
                "Experience the game in a first person POV."
            );
            UseConfortVignette = configFile.Bind<bool>(
                "VR Settings",
                "Confort Vignette",
                true,
                "Adds a black vignette during high-mobility abilities to reduce motion sickness."
            );
            SeatedMode = configFile.Bind<bool>(
                "VR Settings",
                "Seated Mode",
                true,
                "Enable this setting for a better seated experience. Recentering your HMD will place the camera at eye level of the in-game character. Disable it for standing/roomscale play where your real floor height sets the camera height. Keep in mind that seated play is not compatible with LIV XR Capture."
            );
            HeightMultiplier = configFile.Bind<float>(
                "VR Settings",
                "Character height multiplier",
                1.0f,
                "Used for scaling the view. This multiplier will be applied to the character's height. Increase this value to feel taller or decrease it to feel smaller."
            );
            // NOTE: the config KEY name is intentionally versioned. BepInEx never rewrites an
            // existing value, so bumping the key name is the only way to push a new default (75)
            // to users who already have the old key saved at 90.
            ControllerAngleOffset = configFile.Bind<float>(
                "VR Settings",
                "Controller aim pitch v2",
                75f,
                "Pitch (in degrees) applied to the VR hands/aim to compensate for the difference between the OpenXR grip pose and how the hand models were authored. Lower values aim higher, higher values aim lower (90 aims straight along the grip; 75 tilts the aim up ~15 degrees). Applies on the next stage."
            );
            IntroCameraYaw = configFile.Bind<float>(
                "VR Settings",
                "Intro camera yaw",
                180f,
                "Extra yaw (in degrees) applied to the camera during the opening spaceship cutscene. 180 faces the incoming ship; set to 0 for the old orientation, or nudge it if the ship isn't centered."
            );
            AutoSprint = configFile.Bind<bool>(
                "VR Settings",
                "Auto Sprint",
                false,
                "Sprint automatically whenever you push the left stick, so you never have to click it in. Abilities that normally interrupt sprinting still do."
            );
            HighlightChests = configFile.Bind<bool>(
                "VR Settings",
                "Highlight chests",
                false,
                "Highlights purchasable interactables (chests, drones, shrines, etc.) with a coloured outline so they are easier to spot in VR."
            );
            ItemHints = configFile.Bind<bool>(
                "VR Settings",
                "Item hints",
                false,
                "Shows the full item description (stat details from the logbook) in pickup notifications and item tooltips instead of the short flavour text. Pinging a dropped item also prints its description in chat."
            );
            HapticsSuit = configFile.Bind<string>(
                "VR Settings",
                "Haptics Suit",
                "None",
                "The type of haptics suit you're wearing. The mod currently supports the Shockwave suit. The bHaptics suit may also come in the future."
            );

            RayColorHex = configFile.Bind<string>(
                "Survivor Settings",
                "General: Aim ray color",
                "FFFFFF",
                "Changes the color of aim rays."
            );
            RayOpacity = configFile.Bind<float>(
                "Survivor Settings",
                "General: Aim ray opacity",
                0.6f,
                "Sets the aim ray opacity between 0 (invisible) and 1 (opaque)."
            );
            CommandoDualWield = configFile.Bind<bool>(
                "Survivor Settings",
                "Commando: Dual wield",
                true,
                "TRUE: Double Tap and Phase Blast alternate between the left and right pistol for each bullet. FALSE: Double Tap only uses the dominant pistol while Phase Blast uses the non-dominant pistol."
            );
            BanditWeaponGripSnapAngle = configFile.Bind<float>(
                "Survivor Settings",
                "Bandit: Weapon grip snap angle",
                40,
                "Angle in which the non-dominant hand can grip the weapon. Set to 0 to completely disable two-handed gripping. Set to 180 for constant grip."
            );
            MercSwingSpeedThreshold = configFile.Bind<float>(
                "Survivor Settings",
                "Mercenary: Swing speed threshold",
                20,
                "The sword tip speed required to trigger an attack."
            );
            LoaderSwingSpeedThreshold = configFile.Bind<float>(
                "Survivor Settings",
                "Loader: Swing speed threshold",
                12,
                "The mech fist speed required to trigger an attack."
            );
            AcridSwingSpeedThreshold = configFile.Bind<float>(
                "Survivor Settings",
                "Acrid: Swing speed threshold",
                10,
                "The claw tip speed required to trigger an attack."
            );
            RailgunnerWeaponGripSnapAngle = configFile.Bind<float>(
                "Survivor Settings",
                "Railgunner: Weapon grip snap angle",
                50,
                "Angle in which the non-dominant hand can grip the weapon. Set to 0 to completely disable two-handed gripping. Set to 180 for constant grip."
            );
            RailgunnerZoomMultiplier = configFile.Bind<float>(
                "Survivor Settings",
                "Railgunner: Zoom multiplier",
                3f,
                "Changes the zoom multiplier of the scope."
            );
            RailgunnerDisableScopeRay = configFile.Bind<bool>(
                "Survivor Settings",
                "Railgunner: Hide ray while scoping",
                false,
                "Disables the aim ray while using the scope. Enable this setting if you find it obstructs the scope too much... or if you're a gamer and think no scopes should be harder."
            );

            RuntimeHands = configFile.Bind<bool>(
                "RuntimeHands (experimental)",
                "Enable runtime hands",
                true,
                "EXPERIMENTAL: for characters without an authored VR hand (e.g. Seekers of the Storm survivors), build a hand model at runtime from the character's own arm mesh. Characters with real VR hands are unaffected."
            );
            RuntimeHandsDebug = configFile.Bind<bool>(
                "RuntimeHands (experimental)",
                "Debug mode",
                false,
                "Draws RGB axis lines on runtime hands (blue = aim direction), logs every entity state the local character enters, and re-reads this config file every 3 seconds so the per-character offsets below can be tuned live while in game."
            );
            RuntimeHandsRay = configFile.Bind<bool>(
                "RuntimeHands (experimental)",
                "Show aim ray",
                true,
                "Shows the aim ray on runtime hands so aiming is usable before the offsets are tuned."
            );
            RuntimeHandsForearm = configFile.Bind<bool>(
                "RuntimeHands (experimental)",
                "Include forearm",
                true,
                "Also bakes the forearm into the runtime hand mesh (rigid - it will not bend at the elbow)."
            );

            RuntimeHandsItemScale = configFile.Bind<float>(
                "RuntimeHands (experimental)",
                "Item display scale on hands",
                1f,
                "Scales item pickups that the game attaches to the runtime VR hands and forearms (razor wire, etc.). They are modelled for the third-person character, so at 1.0 they fill the view in first person. 0.5 is half size; 1.0 is the game's own size."
            );

            IntroSeatedHeightOffset = configFile.Bind<float>(
                "Camera",
                "Intro cutscene seated height (metres)",
                1.45f,
                "Seated mode only. How much higher (in metres, as if you stood up that much) the view sits during the opening cutscene. 0 = head exactly at the cutscene camera position, which is too low for most seated players."
            );

            ShowUnfocusedWarning = configFile.Bind<bool>(
                "HUD",
                "Show unfocused warning",
                true,
                "Shows a small warning in the headset when the game window on the monitor is not the focused window (keyboard/mouse input then goes elsewhere). It appears after the window has been unfocused for a couple of seconds."
            );

            ReportTextZoom = configFile.Bind<float>(
                "HUD",
                "End-of-run report text size",
                4f,
                "How much larger the text and icons on the end-of-run stats report are drawn relative to its panels (1 = the flat-screen layout). Higher values make the print bigger and the panels denser. 1 to 4."
            );

            ReportWidthMeters = configFile.Bind<float>(
                "HUD",
                "End-of-run report width",
                6f,
                "Overall width of the end-of-run stats report in metres (it sits 15 m in front of you). Smaller = the whole screen fits in view without turning your head; larger = bigger but you have to look left and right. 4 to 24."
            );

            SeekerLotusRaise = configFile.Bind<float>(
                "HUD",
                "Seeker lotus height",
                210f,
                "How far above the health bar the Seeker's lotus petals are drawn (HUD units). Higher = further up. Applies next time the Seeker spawns."
            );

            SeekerMeditateRaise = configFile.Bind<float>(
                "HUD",
                "Seeker meditation height",
                100f,
                "How far the Seeker meditation mini-game (arch + arrow inputs) is raised from where the game puts it (HUD units). Higher = further up. Applies next time you start meditating."
            );

            // Per-character runtime hand entries for the DLC2 survivors are created up front so
            // all three are present in this file without having to play each one first.
            global::VRMod.RuntimeHands.PreRegisterKnownBodies();

            RayColor = HexToColor(RayColorHex.Value);

            WristHUD = configFile.Bind<bool>(
                "HUD Settings",
                "Wrist HUD",
                true,
                "TRUE: Attaches the healthbar, money and cooldowns to the wrists. FALSE: Attaches the healthbar, money and cooldowns to the camera."
            );
            WatchHUD = configFile.Bind<bool>(
                "HUD Settings",
                "Watch HUD",
                true,
                "TRUE: Attaches the inventory, chat, objective and allies to a watch that appears when looking at it. FALSE: Attaches the inventory, chat, objective and allies to the camera."
            );
            BetterHealthBar = configFile.Bind<bool>(
                "HUD Settings",
                "Camera Health Bar",
                true,
                "TRUE: Makes the health bar more visible by placing it on the bottom-middle of the camera HUD. FALSE: Places the healthbar on the left wrist HUD (if the wrist HUD setting is enabled)."
            );
            UseSmoothHUD = configFile.Bind<bool>(
                "HUD Settings",
                "Smooth HUD",
                true,
                "TRUE: The camera HUD will follow the camera smoothly making it lag behind when moving the headset. FALSE: The camera HUD will follow the camera directly without smoothing."
            );
            HUDWidth = configFile.Bind<int>(
                "HUD Settings",
                "HUD Width",
                1200,
                "Width of the camera HUD."
            );
            HUDHeight = configFile.Bind<int>(
                "HUD Settings",
                "HUD Height",
                1000,
                "Height of the camera HUD."
            );
            BottomAnchor = configFile.Bind<float>(
                "HUD Settings",
                "Bottom anchor",
                1f,
                "Position of the bottom HUD anchor between 0 and 1 (Middle to bottom edge of the screen)."
            );
            TopAnchor = configFile.Bind<float>(
                "HUD Settings",
                "Top anchor",
                0.7f,
                "Position of the top HUD anchor between 0 and 1 (Middle to top edge of the screen)."
            );
            LeftAnchor = configFile.Bind<float>(
                "HUD Settings",
                "Left anchor",
                1f,
                "Position of the left HUD anchor between 0 and 1 (Middle to left edge of the screen)."
            );
            RightAnchor = configFile.Bind<float>(
                "HUD Settings",
                "Right anchor",
                1f,
                "Position of the right HUD anchor between 0 and 1 (Middle to right edge of the screen)."
            );
            LIVHUD = configFile.Bind<bool>(
                "HUD Settings",
                "Display HUD on LIV camera",
                true,
                "The classic RoR2 HUD will display on the LIV camera when using the LIV XR capture. Since the camera cannot render any UI, this is a good way for viewers to see the HUD."
            );

            AnchorMin = new Vector2((1 - Mathf.Clamp(LeftAnchor.Value, 0, 1)) / 2, (1 - Mathf.Clamp(BottomAnchor.Value, 0, 1)) / 2);
            AnchorMax = new Vector2((1 + Mathf.Clamp(RightAnchor.Value, 0, 1)) / 2, (1 + Mathf.Clamp(TopAnchor.Value, 0, 1)) / 2);

            SnapTurn = configFile.Bind<bool>(
                "Controls",
                "Snap turn",
                true,
                "TRUE: Rotate the camera in increments (unavailable in third person).  FALSE: Smooth camera rotation."
            );
            SnapTurnAngle = configFile.Bind<int>(
                "Controls",
                "Snap turn angle",
                45,
                "Rotation in degrees of each snap turn. If you're trying to change the smooth turn speed, use the gamepad sensitivity setting in-game."
            );
            SnapTurnHoldDelay = configFile.Bind<float>(
                "Controls",
                "Snap turn hold delay",
                0.33f,
                "Time in seconds between each snap turn when holding a direction."
            );

            LockedCameraPitch = configFile.Bind<bool>(
                "Controls",
                "Locked camera pitch",
                true,
                "Prevents the camera from rotating vertically (cannot disable when snap turn is on)."
            );
            UseMotionControls = configFile.Bind<bool>(
                "Controls",
                "Use motion controls",
                true,
                "Enables motion controls for VR controllers. They act as a simple gamepad when set to false."
            );
            LeftDominantHand = configFile.Bind<bool>(
                "Controls",
                "Set left hand as dominant",
                false,
                "Swaps left/right triggers and grips. The aiming hand for each skill is also swapped as well as hand models."
            );
            ControllerMovementDirection = configFile.Bind<bool>(
                "Controls",
                "Use controller direction for movement",
                false,
                "When enabled, pushing forward on the joystick will move the character towards the direction the controller is pointing instead of the head."
            );
            AimStabiliserAmount = configFile.Bind<float>(
                "Controls",
                "Aim stabiliser amount",
                0.5f,
                "The amount of smoothing applied to the hand position and rotation. Set to 0 to completely remove stabilisation."
            );

            UpdateDependantValues();

            prevLeftHandedValue = LeftDominantHand.Value;

            InitialFirstPersonValue = FirstPerson.Value;
            InitialMotionControlsValue = InitialFirstPersonValue ? UseMotionControls.Value : false;
            InitialOculusModeValue = OculusMode.Value;
            InitialRoomscaleValue = !SeatedMode.Value;
            TempWristHUDValue = InitialMotionControlsValue ? WristHUD.Value : false;
            TempWatchHUDValue = InitialMotionControlsValue ? WatchHUD.Value : false;
            TempLockedCameraPitchValue = (SnapTurn.Value || InitialMotionControlsValue) ? true : LockedCameraPitch.Value;

            settings.Add("vr_auto_sprint", new ConfigSetting(AutoSprint, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_highlight_chests", new ConfigSetting(HighlightChests, ConfigSetting.SettingUpdate.Instant, ChestHighlighter.OnSettingChanged));
            settings.Add("vr_item_hints", new ConfigSetting(ItemHints, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_snap_turn", new ConfigSetting(SnapTurn, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_snap_angle", new ConfigSetting(SnapTurnAngle, 30, 180, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_snap_delay", new ConfigSetting(SnapTurnHoldDelay, 0.2f, 1, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_controller_movement", new ConfigSetting(ControllerMovementDirection, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_vignette", new ConfigSetting(UseConfortVignette, ConfigSetting.SettingUpdate.Instant, ChangeVignetteSetting));
            settings.Add("vr_left_handed", new ConfigSetting(LeftDominantHand, ConfigSetting.SettingUpdate.Instant, ChangeHandDominance));
            settings.Add("vr_aim_stabiliser_amount", new ConfigSetting(AimStabiliserAmount, 0f, 1f, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_wrist_hud", new ConfigSetting(WristHUD, ConfigSetting.SettingUpdate.NextStage));
            settings.Add("vr_watch_hud", new ConfigSetting(WatchHUD, ConfigSetting.SettingUpdate.NextStage));
            settings.Add("vr_better_health", new ConfigSetting(BetterHealthBar, ConfigSetting.SettingUpdate.NextStage));
            settings.Add("vr_smooth_hud", new ConfigSetting(UseSmoothHUD, ConfigSetting.SettingUpdate.Instant, ChangeSmoothHUD));
            settings.Add("vr_liv_hud", new ConfigSetting(LIVHUD, ConfigSetting.SettingUpdate.Instant, ChangeLIVHUD));
            settings.Add("vr_seated", new ConfigSetting(SeatedMode, ConfigSetting.SettingUpdate.AfterRestart));
            settings.Add("vr_height", new ConfigSetting(HeightMultiplier, 0.5f, 2.0f, ConfigSetting.SettingUpdate.NextStage));
            settings.Add("vr_controller_angle", new ConfigSetting(ControllerAngleOffset, -90f, 90f, ConfigSetting.SettingUpdate.NextStage));
            settings.Add("vr_ray_color", new ConfigSetting(RayColorHex, ConfigSetting.SettingUpdate.Instant, ChangeRayColor));
            settings.Add("vr_ray_opacity", new ConfigSetting(RayOpacity, 0, 1, ConfigSetting.SettingUpdate.Instant, ChangeRayColor));
            settings.Add("vr_com_dual", new ConfigSetting(CommandoDualWield, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_bandit_angle", new ConfigSetting(BanditWeaponGripSnapAngle, 0, 180, ConfigSetting.SettingUpdate.Instant, MotionControls.UpdateBanditSnapAngle));
            settings.Add("vr_merc_threshold", new ConfigSetting(MercSwingSpeedThreshold, 5, 50, ConfigSetting.SettingUpdate.Instant, MotionControls.UpdateMercMeleeThreshold));
            settings.Add("vr_loader_threshold", new ConfigSetting(LoaderSwingSpeedThreshold, 5, 50, ConfigSetting.SettingUpdate.Instant, MotionControls.UpdateLoaderMeleeThreshold));
            settings.Add("vr_acrid_threshold", new ConfigSetting(AcridSwingSpeedThreshold, 5, 50, ConfigSetting.SettingUpdate.Instant, MotionControls.UpdateAcridMeleeThreshold));
            settings.Add("vr_railgunner_angle", new ConfigSetting(RailgunnerWeaponGripSnapAngle, 0, 180, ConfigSetting.SettingUpdate.Instant, MotionControls.UpdateRailgunnerSnapAngle));
            settings.Add("vr_railgunner_zoom", new ConfigSetting(RailgunnerZoomMultiplier, 2, 4, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_railgunner_scoperay", new ConfigSetting(RailgunnerDisableScopeRay, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_hud_width", new ConfigSetting(HUDWidth, 400, 2400, ConfigSetting.SettingUpdate.Instant, ChangeHUDSize));
            settings.Add("vr_hud_height", new ConfigSetting(HUDHeight, 400, 2400, ConfigSetting.SettingUpdate.Instant, ChangeHUDSize));
            settings.Add("vr_anchor_bottom", new ConfigSetting(BottomAnchor, 0, 1, ConfigSetting.SettingUpdate.Instant, ChangeHUDAnchors));
            settings.Add("vr_anchor_top", new ConfigSetting(TopAnchor, 0, 1, ConfigSetting.SettingUpdate.Instant, ChangeHUDAnchors));
            settings.Add("vr_anchor_left", new ConfigSetting(LeftAnchor, 0, 1, ConfigSetting.SettingUpdate.Instant, ChangeHUDAnchors));
            settings.Add("vr_anchor_right", new ConfigSetting(RightAnchor, 0, 1, ConfigSetting.SettingUpdate.Instant, ChangeHUDAnchors));
            settings.Add("vr_oculus", new ConfigSetting(OculusMode, ConfigSetting.SettingUpdate.AfterRestart));
            settings.Add("vr_first_person", new ConfigSetting(FirstPerson, ConfigSetting.SettingUpdate.AfterRestart));
            settings.Add("vr_locked_camera", new ConfigSetting(LockedCameraPitch, ConfigSetting.SettingUpdate.Instant));
            settings.Add("vr_motion_controls", new ConfigSetting(UseMotionControls, ConfigSetting.SettingUpdate.AfterRestart));
            settings.Add("vr_haptics_suit", new ConfigSetting(HapticsSuit, ConfigSetting.SettingUpdate.AfterRestart));
        }

        private static void ChangeLIVHUD(object sender, EventArgs e)
        {
            if (UIFixes.livHUD != null)
            {
                UIFixes.livHUD.gameObject.SetActive(ModConfig.LIVHUD.Value);
            }
            else if (LIVHUD.Value && CameraFixes.liv && CameraFixes.liv.enabled)
            {
                UIFixes.CreateLIVHUD(CameraFixes.liv.render.cameraInstance);
            }
        }

        private static void ChangeHandDominance(object sender, EventArgs e)
        {
            if (LeftDominantHand.Value == prevLeftHandedValue) return;

            prevLeftHandedValue = LeftDominantHand.Value;

            Controllers.ChangeDominanceDependantMaps();
            MotionControls.UpdateDominance();
        }

        private static void ChangeHUDAnchors(object sender, EventArgs e)
        {
            AnchorMin = new Vector2((1 - Mathf.Clamp(LeftAnchor.Value, 0, 1)) / 2, (1 - Mathf.Clamp(BottomAnchor.Value, 0, 1)) / 2);
            AnchorMax = new Vector2((1 + Mathf.Clamp(RightAnchor.Value, 0, 1)) / 2, (1 + Mathf.Clamp(TopAnchor.Value, 0, 1)) / 2);

            if (Run.instance)
            {
                CameraRigController localCameraRig = LocalUserManager.GetFirstLocalUser().cameraRigController;

                if (localCameraRig && localCameraRig.hud)
                {
                    RoR2.UI.HUD hud = localCameraRig.hud;

                    RectTransform springCanvas = hud.mainUIPanel.transform.Find("SpringCanvas") as RectTransform;
                    springCanvas.anchorMin = ModConfig.AnchorMin;
                    springCanvas.anchorMax = ModConfig.AnchorMax;

                    RectTransform notificationArea = hud.mainContainer.transform.Find("NotificationArea") as RectTransform;
                    notificationArea.anchorMin = new Vector2(0.5f, ModConfig.AnchorMin.y);
                    notificationArea.anchorMax = new Vector2(0.5f, ModConfig.AnchorMin.y);

                    RectTransform mapNameCluster = hud.mainContainer.transform.Find("MapNameCluster") as RectTransform;
                    mapNameCluster.anchorMin = new Vector2(0.5f, (ModConfig.AnchorMax.y - 0.5f) * 0.54f + 0.5f);
                    mapNameCluster.anchorMax = new Vector2(0.5f, (ModConfig.AnchorMax.y - 0.5f) * 0.54f + 0.5f);
                }
            }
        }

        private static void ChangeHUDSize(object sender, EventArgs e)
        {
            if (Run.instance)
            {
                CameraRigController localCameraRig = LocalUserManager.GetFirstLocalUser().cameraRigController;

                if (localCameraRig && localCameraRig.hud)
                {
                    RectTransform rectTransform = localCameraRig.hud.transform as RectTransform;
                    rectTransform.sizeDelta = new Vector2(ModConfig.HUDWidth.Value, ModConfig.HUDHeight.Value);
                }
            }
        }

        private static void ChangeSmoothHUD(object sender, EventArgs e)
        {
            CameraRigController localCameraRig = Utils.localCameraRig;

            if (!localCameraRig || !localCameraRig.hud) return;

            SmoothHUD instance = localCameraRig.hud.GetComponent<SmoothHUD>();

            if (instance)
            {
                instance.enabled = UseSmoothHUD.Value;
            }
            else if (Run.instance && UseSmoothHUD.Value)
            {
                localCameraRig.hud.gameObject.AddComponent<SmoothHUD>().Init(localCameraRig.uiCam.transform);
            }
        }

        private static void ChangeRayColor(object sender, EventArgs e)
        {
            RayColor = HexToColor(RayColorHex.Value);
            if (MotionControls.HandsReady) MotionControls.UpdateRayColor();
        }

        private static void ChangeVignetteSetting(object sender, EventArgs e)
        {
            if (ConfortVignette.instance)
            {
                ConfortVignette.instance.enabled = UseConfortVignette.Value;
            }
            else if (Run.instance && UseConfortVignette.Value)
            {
                CameraRigController localCameraRig = LocalUserManager.GetFirstLocalUser().cameraRigController;

                if (localCameraRig)
                {
                    localCameraRig.uiCam.gameObject.AddComponent<ConfortVignette>();
                }
            }
        }

        private static Color HexToColor(string hexString)
        {
            Color result = Color.white;
            result.a = RayOpacity.Value;

            if (hexString.StartsWith("#") && hexString.Length > 1)
            {
                hexString = hexString.Substring(1);
            }

            if (hexString.Length == 6)
            {
                string[] hexValues = new string[]
                {
                    hexString.Substring(0, 2),
                    hexString.Substring(2, 2),
                    hexString.Substring(4, 2)
                };

                for (int i = 0; i < 3; i++)
                {
                    int colorChannel = 255;
                    if (int.TryParse(hexValues[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out colorChannel))
                    {
                        switch (i)
                        {
                            case 0:
                                result.r = (float)colorChannel / 255f;
                                break;
                            case 1:
                                result.g = (float)colorChannel / 255f;
                                break;
                            case 2:
                                result.b = (float)colorChannel / 255f;
                                break;
                        }
                    }
                }
            }

            return result;
        }

        private static void UpdateDependantValues()
        {
            TempLockedCameraPitchValue = (SnapTurn.Value || InitialMotionControlsValue) ? true : LockedCameraPitch.Value;

            if (InitialMotionControlsValue)
            {
                TempWristHUDValue = WristHUD.Value;
                TempWatchHUDValue = WatchHUD.Value;
            }
        }

        internal static void Save()
        {
            UpdateDependantValues();
            configFile.Save();
        }

        internal class ConfigSetting
        {
            internal readonly ConfigEntryBase entry;
            internal readonly float minValue;
            internal readonly float maxValue;
            internal SettingUpdate settingUpdate;

            internal ConfigSetting(ConfigEntryBase entry, SettingUpdate settingUpdate, EventHandler onChangedCallback = null)
            {
                this.entry = entry;
                this.settingUpdate = settingUpdate;

                if (onChangedCallback == null) return;

                AddCallback(onChangedCallback);
            }

            internal ConfigSetting(ConfigEntryBase entry, float minValue, float maxValue, SettingUpdate settingUpdate, EventHandler onChangedCallback = null)
            {
                this.entry = entry;
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.settingUpdate = settingUpdate;

                if (onChangedCallback == null) return;

                AddCallback(onChangedCallback);
            }

            private void AddCallback(EventHandler callback)
            {
                if (entry.SettingType == typeof(bool))
                {
                    (entry as ConfigEntry<bool>).SettingChanged += callback;
                }
                else if (entry.SettingType == typeof(int))
                {
                    (entry as ConfigEntry<int>).SettingChanged += callback;
                }
                else if (entry.SettingType == typeof(float))
                {
                    (entry as ConfigEntry<float>).SettingChanged += callback;
                }
                else if (entry.SettingType == typeof(string))
                {
                    (entry as ConfigEntry<string>).SettingChanged += callback;
                }
            }

            internal enum SettingUpdate
            {
                Instant,
                NextStage,
                AfterRestart
            }
        }
    }
}
