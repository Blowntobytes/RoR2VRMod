using BepInEx;
using System.Security;
using System.Security.Permissions;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using System;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace VRMod
{
    [BepInPlugin("com.DrBibop.VRMod", "Resurrected VRMod", "1.0.2")]
    [BepInDependency("com.Moffein.BanditTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    public class VRMod : BaseUnityPlugin
    {
        internal static ManualLogSource StaticLogger;

        internal static AssetBundle VRAssetBundle;

        private static XRManagerSettings activeManagerSettings;
        private static bool xrShutDown;

        private void Awake()
        {
            StaticLogger = Logger;

            VRAssetBundle = AssetBundle.LoadFromMemory(Properties.Resources.vrmodassets);

            ModConfig.Init();
            ActionAddons.Init();
            SettingsAddon.Init();
            UIFixes.Init();
            ChestHighlighter.Init();
            ItemHints.Init();
            MenuSubmitGuard.Init();
            LobbyNavigation.Init();
            CameraFixes.Init();
            CutsceneFixes.Init();
            FocusChecker.Init();
            if (ModConfig.InitialMotionControlsValue)
            {
                RoR2.RoR2Application.isModded = true;
                MotionControls.Init();
                MotionControlledAbilities.Init();
                RuntimeHands.Init();
                EntityStateAnimationParameter.Init();
            }

            RoR2.RoR2Application.onLoad += () =>
            {
                InitVR();
                RecenterController.Init();
                UIPointer.Init();
                try { Haptics.HapticsManager.Init(); }
                catch (System.Exception ex) { UnityEngine.Debug.LogWarning("[VRMod] Haptics init failed (missing library?): " + ex.Message); }
                RoR2.RoR2Application.onNextUpdate += InitControllers;
            };
        }

        private void InitControllers()
        {
            Controllers.Init();
            ControllerGlyphs.Init();
        }

        private void InitVR()
        {
            var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();

            generalSettings.Manager = managerSettings;

            ((List<XRLoader>)managerSettings.activeLoaders).Clear();

            XRLoader xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();

            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;

            SetupControllerProfiles();

            managerSettings.m_Loaders.Add(xrLoader);

            managerSettings.InitializeLoaderSync();

            if (managerSettings.activeLoader == null)
            {
                StaticLogger.LogError("Failed to initialize OpenXR Loader. Is the VR headset ready?");
                return;
            }

            // InitializeLoaderSync() already ran the loader's Initialize(); StartSubsystems() runs
            // Start(). Calling Initialize() again on a started loader returns false by design, which
            // previously produced a bogus "Failed to start OpenXR" error even though VR was running.
            managerSettings.StartSubsystems();
            activeManagerSettings = managerSettings;

            // The manager settings are created by hand, so Unity's XR Management never registers its
            // own quit handler for them. Without one the OpenXR session is still running while the
            // engine tears down, which hangs or crashes the process on exit. Stop and deinitialise
            // the loader ourselves as soon as the application starts quitting.
            Application.quitting += ShutdownXR;

            List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displaySubsystems);
            bool displayRunning = displaySubsystems.Exists(d => d.running);

            List<XRInputSubsystem> xrSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(xrSubsystems);

            foreach (XRInputSubsystem xrSubsystem in xrSubsystems)
            {
                // Device (seated) origin unconditionally: it keeps the camera at eye level and makes
                // TryRecenter behave predictably. Floor origin is what dropped the view to the ground
                // and made recenter ineffective when the config had Seated Mode off.
                xrSubsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
            }

            if (!displayRunning)
            {
                // The display subsystem often finishes starting a frame or two later, so this is not
                // necessarily an error - VR usually comes up fine. Only worry if the headset stays black.
                StaticLogger.LogInfo("InitVR: XR display subsystem not running yet (it usually starts a moment later). If the headset stays black, check that the OpenXR runtime is active.");
            }

            StaticLogger.LogInfo($"InitVR: OpenXR display running={displayRunning}, {xrSubsystems.Count} XR input subsystem(s).");
            StaticLogger.LogInfo($"[VR config] SeatedMode(config)={ModConfig.SeatedMode.Value} -> forcing device origin + eye level; ControllerAngleOffset={ModConfig.ControllerAngleOffset.Value}; IntroCameraYaw={ModConfig.IntroCameraYaw.Value}; MotionControls={ModConfig.InitialMotionControlsValue}; FirstPerson={ModConfig.InitialFirstPersonValue}");
            global::VRMod.Inputs.VRInputDiagnostics.LogInteractionProfiles("after InitVR");
        }

        private static void ShutdownXR()
        {
            if (xrShutDown) return;
            xrShutDown = true;
            // Application.quitting fires after OnApplicationQuit, so by now the game has saved its
            // convars and user profiles and unloaded Steamworks. What remains is Unity's own scene
            // teardown, and with an OpenXR session attached that teardown is what hangs or crashes
            // the process (the BepInEx log always ends mid-line right after this point). So: stop
            // the XR session cleanly, flush the log, and end the process ourselves.
            try
            {
                if (activeManagerSettings != null && activeManagerSettings.activeLoader != null)
                {
                    StaticLogger.LogInfo("[VR shutdown] Stopping OpenXR subsystems.");
                    activeManagerSettings.StopSubsystems();
                    StaticLogger.LogInfo("[VR shutdown] Deinitialising the OpenXR loader.");
                    activeManagerSettings.DeinitializeLoader();
                    StaticLogger.LogInfo("[VR shutdown] OpenXR loader deinitialised.");
                }
            }
            catch (Exception e)
            {
                StaticLogger.LogWarning("[VR shutdown] " + e.Message);
            }
            finally
            {
                StaticLogger.LogInfo("[VR shutdown] Ending process.");
                FlushBepInExLog();
                try { System.Diagnostics.Process.GetCurrentProcess().Kill(); }
                catch (Exception e) { StaticLogger.LogWarning("[VR shutdown] Kill failed: " + e.Message); }
            }
        }

        private static void FlushBepInExLog()
        {
            try
            {
                foreach (ILogListener listener in BepInEx.Logging.Logger.Listeners)
                {
                    if (listener is DiskLogListener disk && disk.LogWriter != null)
                        disk.LogWriter.Flush();
                }
            }
            catch { }
        }

        private void SetupControllerProfiles()
        {
            StaticLogger.LogInfo("SetupControllerProfiles: Setting up OpenXR controller profiles...");
            try
            {
                var openXRSettings = OpenXRSettings.Instance;

                if (openXRSettings.features.Length > 0) 
                {
                    StaticLogger.LogInfo($"SetupControllerProfiles: Features already configured ({openXRSettings.features.Length} features)");
                    return;
                }

                // Try to create controller profiles
                var features = new List<OpenXRFeature>();

                Type[] profileTypes = {
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile),
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchProControllerProfile),
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.ValveIndexControllerProfile),
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.HTCViveControllerProfile),
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.HPReverbG2ControllerProfile),
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.MicrosoftMotionControllerProfile),
                    typeof(UnityEngine.XR.OpenXR.Features.Interactions.KHRSimpleControllerProfile)
                };

                foreach (var profileType in profileTypes)
                {
                    try
                    {
                        var profile = (OpenXRFeature)ScriptableObject.CreateInstance(profileType);
                        profile.enabled = true;
                        features.Add(profile);
                        StaticLogger.LogInfo($"  Created profile: {profileType.Name}");
                    }
                    catch (Exception e)
                    {
                        StaticLogger.LogWarning($"  Failed to create {profileType.Name}: {e.Message}");
                    }
                }

                if (features.Count > 0)
                {
                    openXRSettings.features = features.ToArray();
                    StaticLogger.LogInfo($"SetupControllerProfiles: Added {features.Count} controller profiles");
                }
                else
                {
                    StaticLogger.LogWarning("SetupControllerProfiles: No controller profiles could be created");
                }
            }
            catch (Exception e)
            {
                StaticLogger.LogError($"SetupControllerProfiles failed: {e}");
            }
        }
    }
}