using UnityEngine;
using UnityEngine.XR;

namespace VRMod.Inputs
{
    internal enum WandKind { None, Vive, Wmr }

    /// <summary>Which trackpad-wand input replaces a face button on HTC Vive / original WMR wands.</summary>
    internal enum ViveOverride { None, MenuButton, PadClickUp, PadClickDown }

    /// <summary>
    /// Detects HTC Vive wands (trackpad on primary2DAxis) and original Windows Mixed Reality wands
    /// (trackpad on secondary2DAxis, stick on primary) and reads trackpad click regions, reproducing
    /// DrBibop's original SteamVR bindings (right pad up = jump, right pad down = interact, menu = ping/pause/info).
    /// WMR detection also requires a trackpad so HP Reverb G2 controllers (A/B buttons, no pad) are not matched.
    /// </summary>
    internal static class ViveControllers
    {
        private static bool determined;
        private static WandKind kind;

        internal const float RegionThreshold = 0.35f;

        internal static bool IsViveWand
        {
            get { if (!determined) Evaluate(); return kind == WandKind.Vive; }
        }

        internal static bool IsTrackpadWand
        {
            get { if (!determined) Evaluate(); return kind != WandKind.None; }
        }

        private static void Evaluate()
        {
            WandKind k = Classify(InputDevices.GetDeviceAtXRNode(XRNode.RightHand));
            if (k == WandKind.None) k = Classify(InputDevices.GetDeviceAtXRNode(XRNode.LeftHand));

            if (k != WandKind.None)
            {
                kind = k;
                determined = true;
                VRMod.StaticLogger.LogInfo("[VR input] " + (k == WandKind.Vive ? "HTC Vive" : "Windows Mixed Reality")
                    + " wands detected - using the trackpad-region control scheme (jump = right pad up, interact = right pad down, etc.).");
            }
            else if (HasNamedController())
            {
                // A real, named controller that is not a wand: stop checking.
                determined = true;
            }
        }

        private static WandKind Classify(InputDevice device)
        {
            if (!device.isValid || string.IsNullOrEmpty(device.name)) return WandKind.None;
            string n = device.name.ToLowerInvariant();
            if (n.Contains("vive controller")) return WandKind.Vive;
            if ((n.Contains("windows mr controller") || n.Contains("windows mixed reality") || n.Contains("microsoft motion"))
                && HasTrackpad(device))
                return WandKind.Wmr;
            return WandKind.None;
        }

        private static bool HasTrackpad(InputDevice device)
        {
            return device.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool _)
                || device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 _);
        }

        private static bool HasNamedController()
        {
            InputDevice r = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            InputDevice l = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            return (r.isValid && !string.IsNullOrEmpty(r.name)) || (l.isValid && !string.IsNullOrEmpty(l.name));
        }

        internal static bool PadClickUp(InputDevice device) => PadClick(device, out float y) && y >= RegionThreshold;

        internal static bool PadClickDown(InputDevice device) => PadClick(device, out float y) && y <= -RegionThreshold;

        private static bool PadClick(InputDevice device, out float y)
        {
            y = 0f;
            if (!device.isValid) return false;

            if (kind == WandKind.Wmr)
            {
                if (!device.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool wClick) || !wClick) return false;
                if (!device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 wPad)) return false;
                y = wPad.y;
                return true;
            }

            if (!device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool click) || !click) return false;
            if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 pad)) return false;
            y = pad.y;
            return true;
        }
    }
}
