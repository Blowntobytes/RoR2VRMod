using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

namespace VRMod.Inputs
{
    /// <summary>
    /// Runtime diagnostics for the VR controller input pipeline. This exists because the OpenXR
    /// rewrite has to line up four independent things at runtime (OpenXR interaction profiles ->
    /// legacy XR InputDevices at the hand nodes -> the mod's BaseInput readers -> a Rewired
    /// CustomController on the local player), and when any one of them is missing the symptom is
    /// identical: no menu / gameplay input at all. These logs make it obvious which stage failed.
    ///
    /// Everything here is best-effort and swallows its own exceptions - diagnostics must never be
    /// able to break the input path they are reporting on.
    /// </summary>
    internal static class VRInputDiagnostics
    {
        private static float nextDeviceScanTime;
        private static bool loggedDevicesPresent;
        private static bool loggedNoDevicesWarning;
        private static bool loggedFirstButtonActivity;
        private static bool loggedControllerAdded;
        private static int frameCount;

        internal static void LogInteractionProfiles(string context)
        {
            try
            {
                var settings = UnityEngine.XR.OpenXR.OpenXRSettings.Instance;
                if (settings == null)
                {
                    VRMod.StaticLogger.LogWarning($"[VR input] {context}: OpenXRSettings.Instance is null.");
                    return;
                }

                // Which OpenXR runtime is actually driving the headset. The mod is runtime-agnostic -
                // SteamVR (Index, Vive, Bigscreen Beyond), the Meta/Oculus runtime (Rift, Quest Link)
                // and VDXR (Virtual Desktop) all work - so when a headset misbehaves this line is the
                // first thing to check: it names the runtime Windows had set as active at launch.
                try
                {
                    VRMod.StaticLogger.LogInfo($"[VR input] OpenXR runtime: '{UnityEngine.XR.OpenXR.OpenXRRuntime.name}' version {UnityEngine.XR.OpenXR.OpenXRRuntime.version} (OpenXR API {UnityEngine.XR.OpenXR.OpenXRRuntime.apiVersion}, plugin {UnityEngine.XR.OpenXR.OpenXRRuntime.pluginVersion}).");
                }
                catch (System.Exception runtimeEx)
                {
                    VRMod.StaticLogger.LogWarning($"[VR input] Could not read the OpenXR runtime name: {runtimeEx.Message}");
                }

                var features = settings.features;
                int count = features == null ? 0 : features.Length;
                VRMod.StaticLogger.LogInfo($"[VR input] {context}: {count} OpenXR feature(s) registered.");

                if (features != null)
                {
                    foreach (var f in features)
                    {
                        if (f == null) continue;
                        VRMod.StaticLogger.LogInfo($"[VR input]   feature '{f.GetType().Name}' enabled={f.enabled}");
                    }
                }
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogWarning($"[VR input] LogInteractionProfiles failed: {e.Message}");
            }
        }

        /// <summary>Dump every XR input device the runtime currently exposes, with its node/characteristics.</summary>
        internal static void DumpDevices(string context)
        {
            try
            {
                List<InputDevice> devices = new List<InputDevice>();
                InputDevices.GetDevices(devices);

                if (devices.Count == 0)
                {
                    VRMod.StaticLogger.LogWarning($"[VR input] {context}: no XR input devices reported by the runtime yet.");
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[VR input] {context}: {devices.Count} XR device(s):");
                foreach (InputDevice d in devices)
                    sb.AppendLine($"    '{d.name}' valid={d.isValid} characteristics=({d.characteristics})");

                InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                sb.AppendLine($"    LeftHand node valid={left.isValid} name='{left.name}'");
                sb.AppendLine($"    RightHand node valid={right.isValid} name='{right.name}'");

                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogWarning($"[VR input] DumpDevices failed: {e.Message}");
            }
        }

        internal static void OnControllerAddedToPlayer(string playerName)
        {
            if (loggedControllerAdded) return;
            loggedControllerAdded = true;
            VRMod.StaticLogger.LogInfo($"[VR input] VR Rewired controller attached to player '{playerName}'. Maps should now be live.");
        }

        /// <summary>
        /// Called once per Rewired input-source update. Rate-limited so it never spams the log:
        /// it reports hand-node presence a few times right after startup, then goes quiet until it
        /// sees the first real button/axis activity (which confirms the whole path end to end).
        /// </summary>
        internal static void Tick(IEnumerable<BaseInput> inputs)
        {
            try
            {
                frameCount++;

                float now = Time.unscaledTime;
                if (now >= nextDeviceScanTime)
                {
                    nextDeviceScanTime = now + 1f;

                    InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                    InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                    bool bothValid = left.isValid && right.isValid;

                    if (bothValid && !loggedDevicesPresent)
                    {
                        loggedDevicesPresent = true;
                        VRMod.StaticLogger.LogInfo("[VR input] Both hand controllers are now visible to the legacy XR input system. " +
                            "If menu input still does nothing, the break is downstream (Rewired maps / action IDs).");
                    }

                    // ~10 s in with still no controllers -> the OpenXR interaction profiles almost certainly
                    // did not register. That is the single most useful thing to know from a log.
                    if (!bothValid && !loggedNoDevicesWarning && now > 10f)
                    {
                        loggedNoDevicesWarning = true;
                        VRMod.StaticLogger.LogWarning("[VR input] After 10s the runtime still reports no controller at one or both hand nodes " +
                            $"(left valid={left.isValid}, right valid={right.isValid}). This usually means the OpenXR interaction " +
                            "profiles did not register, so no controller input can reach the game. Check the 'SetupControllerProfiles' " +
                            "lines earlier in this log.");
                        DumpDevices("no-device timeout");
                    }
                }

                if (!loggedFirstButtonActivity && inputs != null)
                {
                    InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                    InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                    if (AnyActivity(left) || AnyActivity(right))
                    {
                        loggedFirstButtonActivity = true;
                        VRMod.StaticLogger.LogInfo("[VR input] First controller button/stick activity detected - the XR read path is working. " +
                            "If the game still does not respond, the Rewired action IDs no longer match this build of the game.");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (frameCount < 5)
                    VRMod.StaticLogger.LogWarning($"[VR input] diagnostics tick failed: {e.Message}");
            }
        }

        private static bool AnyActivity(InputDevice device)
        {
            if (!device.isValid) return false;

            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary) return true;
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary) return true;
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger) return true;
            if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip) return true;
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick) && stickClick) return true;
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick) && stick.sqrMagnitude > 0.25f) return true;

            return false;
        }
    }
}
