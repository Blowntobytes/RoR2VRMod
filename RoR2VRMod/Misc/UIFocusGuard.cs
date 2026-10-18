using RoR2;
using RoR2.UI;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// 2.23.46: the game drops ALL movement input while LocalUser.isUIFocused is true, and that is
    /// true whenever the event system still has a selected UI control. A control inside the pause
    /// menu's settings panel (seen with the VR settings sliders) can stay selected after the menu
    /// closes, leaving the player standing still until some other menu changes the selection.
    /// Once the pause menu is closed and no cursor is shown, a selection that is hidden or that
    /// belongs to a settings/pause panel is cleared.
    /// </summary>
    internal static class UIFocusGuard
    {
        private static float nextCheck;

        internal static void Init()
        {
            RoR2Application.onUpdate += Tick;
        }

        private static void Tick()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.25f;
            if (PauseManager.isPaused || !Run.instance) return;
            try
            {
                LocalUser lu = LocalUserManager.GetFirstLocalUser();
                MPEventSystem es = lu?.eventSystem;
                if (!es || es.isCursorVisible) return;
                GameObject sel = es.currentSelectedGameObject;
                if (!sel) return;
                // 2.23.47: the stick can also land the selection on the in-game HUD (seen: the
                // key item slot's IconPanel under BottomRightCluster/ExtraButtonPanels) - a HUD
                // element must never hold the UI selection while playing.
                // 2.24.28: the drone picker (RemoteOperationDronePickerPanel) and the item/command pickers
                // (PickupPickerPanel) also live under the HUD - clearing their selection every 0.25 s made
                // the highlight jump back to the first entry (gamepad players, multiplayer reports).
                if (sel.activeInHierarchy && (sel.GetComponentInParent<PickupPickerPanel>() || sel.GetComponentInParent<RemoteOperationDronePickerPanel>())) return;
                bool stale = !sel.activeInHierarchy || sel.GetComponentInParent<PauseScreenController>() || IsUnderSettings(sel.transform) || sel.GetComponentInParent<HUD>();
                if (!stale) return;
                es.SetSelectedGameObject(null);
                VRMod.StaticLogger.LogInfo($"[VR focus] Cleared stale UI selection '{sel.name}' after the pause menu closed (it kept the game in UI-focus and blocked movement).");
            }
            catch { }
        }

        private static bool IsUnderSettings(Transform t)
        {
            for (int i = 0; t && i < 12; i++, t = t.parent)
            {
                string n = t.name;
                if (n.StartsWith("SettingsPanel") || n.StartsWith("SettingsSubPanel") || n.StartsWith("VRModSetting") || n.StartsWith("PauseScreen")) return true;
            }
            return false;
        }
    }
}
