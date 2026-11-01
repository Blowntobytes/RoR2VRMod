using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// 2.26.27: the scrapper picker sometimes had NO selected button (2.26.26 log: 60+ stick
    /// inputs "on none"), so the thumbstick walked the character instead of moving through the
    /// items (the game only blocks movement while a UI control is selected), until the picker was
    /// reopened. Seen right after opening and after the picker rebuilt its buttons (the selected
    /// button was destroyed). While a picker is open and nothing is selected, select the button
    /// closest to where the selection last was (first button if none yet).
    /// </summary>
    internal static class PickerSelectionKeeper
    {
        private static PickupPickerController open;
        private static Vector3 lastLocal;
        private static bool haveLast;

        internal static void Init()
        {
            On.RoR2.PickupPickerController.OnDisplayBegin += (orig, self, prompt, user, rig) =>
            {
                orig(self, prompt, user, rig);
                open = self;
                haveLast = false;
            };
            On.RoR2.PickupPickerController.OnDisplayEnd += (orig, self, prompt, user, rig) =>
            {
                if (open == self) open = null;
                orig(self, prompt, user, rig);
            };
            RoR2Application.onUpdate += Tick;
        }

        private static void Tick()
        {
            if (!open || !open.panelInstance) return;
            LocalUser user = LocalUserManager.GetFirstLocalUser();
            MPEventSystem es = user != null ? user.eventSystem : null;
            if (!es) return;
            Transform panel = open.panelInstance.transform;
            GameObject sel = es.currentSelectedGameObject;
            if (sel && sel.activeInHierarchy)
            {
                if (sel.transform.IsChildOf(panel)) { lastLocal = panel.InverseTransformPoint(sel.transform.position); haveLast = true; }
                return;
            }
            Selectable best = null; float bestD = float.MaxValue;
            foreach (Selectable s in panel.GetComponentsInChildren<Selectable>(false))
            {
                if (!s.IsInteractable() || !s.name.StartsWith("PickupButtonTemplate")) continue;
                float d = haveLast ? (panel.InverseTransformPoint(s.transform.position) - lastLocal).sqrMagnitude : 0f;
                if (!best || d < bestD) { best = s; bestD = d; }
                if (!haveLast) break;
            }
            if (!best) return;
            es.SetSelectedGameObject(best.gameObject);
            VRMod.StaticLogger.LogInfo($"[VR picker] nothing was selected in the open picker - selected '{best.name}' {(haveLast ? "nearest the previous selection" : "(first item)")}.");
        }
    }
}
