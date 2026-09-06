using RoR2.UI;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// One controller press must activate exactly ONE menu button.
    ///
    /// Every MPButton polls the UISubmit action (14) in its own Update and clicks itself when it is
    /// the currently selected object. When a click rebuilds the screen and the selection moves to
    /// another button within the same frame - the character select does exactly this: picking a
    /// survivor deselects everything and the Ready button re-selects itself as the fallback - that
    /// second button sees the SAME press still "down" and clicks too. That is how one press of A
    /// both picked the survivor and readied up / started the run.
    ///
    /// This guard remembers the frame of the last submit-driven click and refuses a second
    /// submit-driven click from the same press (same or next frame). A real second press, which is
    /// always many frames later, is unaffected.
    /// </summary>
    internal static class MenuSubmitGuard
    {
        private const int UISubmitActionId = 14;

        private static int lastSubmitClickFrame = -1000;

        private static string lastSubmitClickName = "";

        internal static void Init()
        {
            On.RoR2.UI.MPButton.Update += GuardUpdate;
        }

        private static void GuardUpdate(On.RoR2.UI.MPButton.orig_Update orig, MPButton self)
        {
            if (WouldSubmitClick(self))
            {
                int frame = Time.frameCount;

                if (frame - lastSubmitClickFrame <= 1)
                {
                    VRMod.StaticLogger.LogInfo($"[VR input] Ignored a second menu activation from the same press: '{self.name}' (that press already activated '{lastSubmitClickName}').");
                    return;
                }

                lastSubmitClickFrame = frame;
                lastSubmitClickName = self.name;
            }

            orig(self);
        }

        /// <summary>
        /// Mirrors the exact condition MPButton.Update uses before calling InvokeClick for a
        /// gamepad-style submit, so the guard only ever intervenes where the game would click.
        /// </summary>
        private static bool WouldSubmitClick(MPButton button)
        {
            MPEventSystem eventSystem = button.eventSystem;

            if (!eventSystem || eventSystem.player == null)
                return false;

            if (MPButton.isIgnore || button.disableGamepadClick || button.useBaseGamepadClick)
                return false;

            if (!eventSystem.player.GetButtonDown(UISubmitActionId))
                return false;

            return eventSystem.currentSelectedGameObject == button.gameObject;
        }
    }
}
