using Rewired;
using RoR2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRMod.Inputs
{
    /// <summary>
    /// Key items (Hallowed Concepts update, Oct 2026): "Use Key Item" (Rewired action 44) and
    /// "Key Item Change Element" (action 47, axis: + next / - previous) had no VR binding.
    ///
    /// Combo, only while the local body actually holds a key item:
    ///   hold LEFT GRIP + press RIGHT A (Vive: right pad down)   -> use key item
    ///   hold LEFT GRIP + flick RIGHT STICK left/right (Vive: pad) -> previous / next element
    /// While armed, the left grip's own skill (Utility, or Special when left-handed) fires on
    /// RELEASE instead of on press, and only if no combo was used during the hold - the same
    /// tap-vs-hold idea as Y. Right A does not Interact, and the right stick does not turn, while
    /// the grip is held. With no key item the grip, A and stick behave exactly as before.
    ///
    /// Rewired latches a button-down edge the instant an element is set true, so the combo must
    /// decide BEFORE the normal inputs run: Poll() first, then the loop asks Owns(...) to skip the
    /// elements this class drives, then Apply() writes them.
    /// </summary>
    internal static class KeyItemCombo
    {
        internal const int UseElement = 33, NextElement = 34, PrevElement = 35;
        private const int GripElement = 10, InteractElement = 6, LookXAxis = 2, LookYAxis = 3;
        private const float Pulse = 0.12f;          // long enough for the game's buffered poll to see it
        private const float IndicatorDelay = 0.15f; // quick grip taps for the skill don't flash the panel
        private const float FlickOn = 0.7f, FlickOff = 0.35f;

        private static bool armed, gripPrev, aPrev, comboUsed, flickLatched, interactBlocked;
        private static float gripDownTime, skillPulseUntil, usePulseUntil, nextPulseUntil, prevPulseUntil;
        private static Vector2 rightStick;

        internal static bool OwnsGrip => armed || Time.unscaledTime < skillPulseUntil;
        internal static bool OwnsInteract => interactBlocked;
        internal static bool OwnsLookX => armed && gripPrev;

        internal static bool Owns(BaseInput input)
        {
            if (input is ButtonInput b)
                return (b.ElementId == GripElement && OwnsGrip) || (b.ElementId == InteractElement && OwnsInteract);
            if (input is VectorInput v && v.XAxisId == LookXAxis)
                return OwnsLookX;
            return false;
        }

        internal static KeyItemSlot LocalSlot()
        {
            CharacterBody body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            KeyItemSlot slot = body ? body.keyItemSlot : null;
            return slot && slot.keyItem != KeyItemIndex.None ? slot : null;
        }

        internal static void Poll()
        {
            float now = Time.unscaledTime;
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            bool grip = left.isValid && left.IsPressed(InputHelpers.Button.GripButton, out bool g) && g;
            bool a = ViveControllers.IsTrackpadWand
                ? right.isValid && ViveControllers.PadClickDown(right)
                : right.isValid && right.IsPressed(InputHelpers.Button.PrimaryButton, out bool p) && p;
            if (!right.isValid || !right.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick)) rightStick = Vector2.zero;

            KeyItemSlot slot = null;
            if (grip && !gripPrev)
            {
                slot = LocalSlot();
                armed = slot != null;
                comboUsed = false;
                gripDownTime = now;
                aPrev = a;                                            // A already held doesn't count as a press
                flickLatched = Mathf.Abs(rightStick.x) > FlickOff;    // nor does a stick already pushed
            }

            if (armed && grip)
            {
                if (slot == null) slot = LocalSlot();

                if (a && !aPrev)
                {
                    usePulseUntil = now + Pulse;
                    comboUsed = true;
                    KeyItemIndicator.Flash(slot, "Used");
                }

                if (!flickLatched && Mathf.Abs(rightStick.x) > FlickOn)
                {
                    if (rightStick.x > 0) nextPulseUntil = now + Pulse; else prevPulseUntil = now + Pulse;
                    comboUsed = true;
                    flickLatched = true;
                    KeyItemIndicator.Flash(slot, rightStick.x > 0 ? "Element  ►" : "◄  Element");
                }
                else if (flickLatched && Mathf.Abs(rightStick.x) < FlickOff)
                {
                    flickLatched = false;
                }
            }

            if (!grip && gripPrev && armed)
            {
                if (!comboUsed) skillPulseUntil = now + Pulse; // a plain tap: fire the grip's skill now
                armed = false;
            }

            // Right A stays away from Interact for the whole hold, and until it is let go after a combo.
            interactBlocked = (armed && grip) || (interactBlocked && a);

            KeyItemIndicator.Show(armed && grip && now - gripDownTime > IndicatorDelay, armed ? (slot ?? LocalSlot()) : null);

            gripPrev = grip;
            aPrev = a;
        }

        internal static void Apply(CustomController vr)
        {
            float now = Time.unscaledTime;
            if (OwnsGrip) vr.SetButtonValueById(GripElement, now < skillPulseUntil);
            if (OwnsInteract) vr.SetButtonValueById(InteractElement, false);
            if (OwnsLookX)
            {
                vr.SetAxisValueById(LookXAxis, 0f);
                vr.SetAxisValueById(LookYAxis, rightStick.y);
            }
            vr.SetButtonValueById(UseElement, now < usePulseUntil);
            vr.SetButtonValueById(NextElement, now < nextPulseUntil);
            vr.SetButtonValueById(PrevElement, now < prevPulseUntil);
        }

        /// <summary>Called when the VR keyboard / menus suppress input: drop any half-finished combo.</summary>
        internal static void Reset()
        {
            armed = comboUsed = flickLatched = interactBlocked = false;
            skillPulseUntil = usePulseUntil = nextPulseUntil = prevPulseUntil = 0f;
            KeyItemIndicator.Show(false, null);
        }
    }

    /// <summary>
    /// Small panel on the left wrist while the combo is armed: key item name, ready state, and the
    /// buttons. After an action it briefly shows what happened ("Used ...", "Element ►").
    /// </summary>
    internal static class KeyItemIndicator
    {
        private static GameObject panel;
        private static TextMeshProUGUI text;
        private static Canvas hostCanvas;
        private static float flashUntil;
        private static string flashText;
        private static bool holdVisible;
        private static string statusText;

        /// <summary>Persistent message on the wrist panel (e.g. mapping freeze); null clears it.</summary>
        internal static void SetStatus(string text)
        {
            statusText = text;
            Refresh(null);
        }

        internal static void Flash(KeyItemSlot slot, string what)
        {
            string name = ItemName(slot);
            flashText = what == "Used" ? $"<b>Used {name}</b>" : $"<b>{name}</b>\n{what}";
            flashUntil = Time.unscaledTime + 1.2f;
            Refresh(slot);
        }

        internal static void Show(bool holding, KeyItemSlot slot)
        {
            holdVisible = holding;
            Refresh(slot);
        }

        private static void Refresh(KeyItemSlot slot)
        {
            bool flashing = Time.unscaledTime < flashUntil;
            bool visible = holdVisible || flashing || statusText != null;
            if (!visible)
            {
                if (panel && panel.activeSelf) panel.SetActive(false);
                return;
            }
            if (!Ensure()) return;
            if (!panel.activeSelf) panel.SetActive(true);

            if (statusText != null && !holdVisible && !flashing)
            {
                text.text = statusText;
                return;
            }

            if (flashing && !holdVisible)
            {
                text.text = flashText;
                return;
            }

            string state = "";
            try
            {
                if (slot)
                {
                    if (slot.isDisabled) state = "  <color=#ff8080>disabled</color>";
                    else if (slot.bodyBehavior && !slot.bodyBehavior.IsReadyToExecute()) state = "  <color=#ffd070>recharging</color>";
                    else state = "  <color=#90ff90>ready</color>";
                }
            }
            catch { }

            string use = ViveControllers.IsTrackpadWand ? "Pad ▼" : "A";
            string cycle = ViveControllers.IsTrackpadWand ? "Pad ◄ ►" : "Stick ◄ ►";
            string header = flashing ? flashText : $"<b>{ItemName(slot)}</b>{state}";
            text.text = $"{header}\n<size=80%>{use}: use    {cycle}: element</size>";
        }

        private static string ItemName(KeyItemSlot slot)
        {
            try
            {
                if (slot)
                {
                    KeyItemDef def = KeyItemCatalog.GetKeyItemDef(slot.keyItem);
                    if (def) return Language.GetString(def.nameToken);
                }
            }
            catch { }
            return "Key item";
        }

        private static bool Ensure()
        {
            if (panel && hostCanvas) return true;
            if (!MotionControls.HandsReady) return false;
            HandController leftHand = MotionControls.GetHandBySide(true);
            Canvas canvas = leftHand && leftHand.smallHud ? leftHand.smallHud.Canvas : null;
            if (!canvas) return false;

            if (panel) Object.Destroy(panel);
            hostCanvas = canvas;

            panel = new GameObject("VRKeyItemIndicator", typeof(RectTransform));
            panel.layer = canvas.gameObject.layer;
            RectTransform rt = panel.transform as RectTransform;
            rt.SetParent(canvas.transform, false);
            rt.sizeDelta = new Vector2(520, 130);
            rt.localPosition = new Vector3(0, 260, 0);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.07f, 0.85f);
            bg.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.layer = panel.layer;
            RectTransform trt = textObject.transform as RectTransform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 8); trt.offsetMax = new Vector2(-16, -8);
            text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 30;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.92f, 0.75f, 1f);
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            try
            {
                TMP_Text donor = RoR2Application.instance && RoR2Application.instance.mainCanvas
                    ? RoR2Application.instance.mainCanvas.GetComponentInChildren<TMP_Text>(true) : null;
                if (donor && donor.font) { text.font = donor.font; if (donor.fontSharedMaterial) text.fontSharedMaterial = donor.fontSharedMaterial; }
            }
            catch { }

            panel.SetActive(false);
            VRMod.StaticLogger.LogInfo("[VR key item] wrist indicator created.");
            return true;
        }
    }
}
