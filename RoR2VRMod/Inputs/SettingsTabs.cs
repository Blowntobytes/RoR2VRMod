using RoR2.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRMod
{
    /// <summary>
    /// Switches the tabs of the settings panel (GamePlay / Controller / Audio / Video / VR ...) with
    /// the grips.
    ///
    /// Every attempt to do this by binding a Rewired action failed because nothing in the game's code
    /// polls an action for it: the tab buttons are driven by HGGamepadInputEvent components whose
    /// action NAME is configured inside the prefab, so it cannot be read from the assemblies and
    /// guessing it (UITabLeft, UISubmenuLeft, UIPageLeft...) kept missing. This skips the guessing
    /// entirely and calls the very methods those prefab events call - MoveHeaderLeft / MoveHeaderRight
    /// on the panel's HGHeaderNavigationController - when a grip is squeezed. The buttons are read
    /// straight off the controllers, the same way the recenter click is, so no binding is involved.
    /// </summary>
    internal static class SettingsTabs
    {
        private static bool leftWasPressed;

        private static bool rightWasPressed;

        private static bool loggedOnce;

        /// <summary>Called every input frame from Controllers.UpdateVRInputs.</summary>
        internal static void Poll()
        {
            bool left = GripPressed(XRNode.LeftHand);
            bool right = GripPressed(XRNode.RightHand);

            bool leftEdge = left && !leftWasPressed;
            bool rightEdge = right && !rightWasPressed;

            leftWasPressed = left;
            rightWasPressed = right;

            if (!leftEdge && !rightEdge) return;

            try
            {
                HGHeaderNavigationController tabs = FindActiveTabs();
                if (!tabs) return;

                if (leftEdge)
                    tabs.MoveHeaderLeft();
                else
                    tabs.MoveHeaderRight();

                if (!loggedOnce)
                {
                    loggedOnce = true;
                    string headerNames = tabs.headers != null
                        ? string.Join(", ", tabs.headers.ConvertAll(h => h.headerName).ToArray())
                        : "?";
                    VRMod.StaticLogger.LogInfo($"[VR input] Settings tab moved {(leftEdge ? "left" : "right")} with the grip; now on index {tabs.currentHeaderIndex} of [{headerNames}].");
                }
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogWarning($"[VR input] Settings tab switch failed: {e.Message}");
            }
        }

        /// <summary>
        /// The settings panel currently on screen, or null. The character select screen is excluded so
        /// the grips stay inert there, and a controller with a single header is ignored because there
        /// is nothing to switch between.
        /// </summary>
        private static HGHeaderNavigationController FindActiveTabs()
        {
            if (Object.FindObjectsOfType<CharacterSelectController>().Any(c => c && c.isActiveAndEnabled))
                return null;

            return Object.FindObjectsOfType<HGHeaderNavigationController>()
                .FirstOrDefault(c => c && c.isActiveAndEnabled && c.headers != null && c.headers.Count > 1);
        }

        private static bool GripPressed(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            bool value;
            return device.isValid && device.TryGetFeatureValue(CommonUsages.gripButton, out value) && value;
        }
    }
}
