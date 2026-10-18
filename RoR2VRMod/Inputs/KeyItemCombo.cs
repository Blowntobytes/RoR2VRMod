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

        private static string flashKind;

        internal static void Flash(KeyItemSlot slot, string what)
        {
            flashKind = what;
            flashUntil = Time.unscaledTime + 1.5f;
            Refresh(slot);
        }

        /// <summary>Flash text is rebuilt every frame so it shows the page the game actually selected.</summary>
        private static string BuildFlashText(KeyItemSlot slot)
        {
            string name = ItemName(slot);
            if (flashKind == "Used") return $"<b>Used {name}</b>";
            string page = CurrentElement(slot);
            return $"<b>{name}</b>\n{flashKind}{(page != null ? "   <color=#ffffff>" + page + "</color>" : "")}";
        }

        /// <summary>The selected "element" - for The N'tormat, the current page (spell) and its uses.</summary>
        private static string CurrentElement(KeyItemSlot slot)
        {
            try
            {
                var book = slot ? slot.bodyBehavior as RoR2.Items.CursedBookBodyBehavior : null;
                if (book == null) return null;
                var spell = book.GetCurrentSpellSlot();
                if (spell == null) return "no page";
                CursedBookSpellDef def = CursedBookSpellCatalog.GetCursedBookSpellDef(spell.spellIndex);
                string n = def && !string.IsNullOrEmpty(def.nameToken) ? Language.GetString(def.nameToken) : spell.spellIndex.ToString();
                if (spell.isConsumed) return n + " <color=#ff8080>(used up)</color>";
                return spell.uses > 0 ? $"{n} ×{spell.uses}" : n;
            }
            catch { return null; }
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
                text.text = BuildFlashText(slot);
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

            string header = flashing ? BuildFlashText(slot) : $"<b>{ItemName(slot)}</b>{state}";
            string page = flashing ? null : CurrentElement(slot);
            if (page != null) header += $"\n<size=85%>Page: <color=#ffffff>{page}</color></size>";
            text.text = $"{header}\n<size=80%>{Bindings.BindingResolver.KeyItemHint()}</size>";
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
            rt.sizeDelta = new Vector2(560, 160);
            rt.localPosition = new Vector3(0, 260, 0);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.07f, 0.75f);
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
