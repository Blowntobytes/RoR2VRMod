using RoR2.UI;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRMod.Inputs.Bindings;

namespace VRMod
{
    /// <summary>
    /// One "action = chord" row in the VR settings tab, built from the game's own Bool settings row
    /// (so it looks native). Click it and press what you want (see BindingCapture). The last row is
    /// "Reset all bindings".
    /// </summary>
    internal class BindingRow : MonoBehaviour
    {
        private BindingDef def;
        private bool resetRow;
        private TextMeshProUGUI valueText;
        private static bool dumped;

        internal static BindingRow Build(GameObject template, Transform parent, BindingDef def, LanguageTextMeshController description)
        {
            GameObject row = Instantiate(template, parent);
            row.name = def != null ? "VRModBinding, " + def.Name : "VRModBinding, Reset";
            BindingRow r = row.AddComponent<BindingRow>();
            r.def = def; r.resetRow = def == null;
            r.Setup(description);
            return r;
        }

        private void Setup(LanguageTextMeshController description)
        {
            if (!dumped) { dumped = true; Dump(); }

            // The game's setting control would try to drive a convar: drop it, keep the look.
            // Row root: HGButton + CarouselController (the Bool row IS a carousel); the checkbox is the
            // "CarouselRect" child. Remove the control COMPONENTS only - never the row object itself.
            foreach (BaseSettingsControl c in GetComponentsInChildren<BaseSettingsControl>(true)) Destroy(c);
            Transform carouselRect = transform.Find("CarouselRect");
            if (carouselRect) Destroy(carouselRect.gameObject);
            foreach (Toggle t in GetComponentsInChildren<Toggle>(true)) Destroy(t.gameObject);

            Transform bt = transform.Find("ButtonText");
            TextMeshProUGUI label = bt ? bt.GetComponent<TextMeshProUGUI>() : GetComponentInChildren<TextMeshProUGUI>(true);
            if (label)
            {
                // The row ROOT carries the LanguageTextMeshController that drives ButtonText.
                LanguageTextMeshController ltmc = GetComponent<LanguageTextMeshController>();
                if (ltmc) ltmc.token = resetRow ? "Reset all bindings" : def.Name;
                label.text = resetRow ? "Reset all bindings" : def.Name;

                // Value text: a copy of the label, right-aligned in the row.
                GameObject v = Instantiate(label.gameObject, label.transform.parent);
                v.name = "BindingValue";
                LanguageTextMeshController vl = v.GetComponent<LanguageTextMeshController>();
                if (vl) Destroy(vl);
                valueText = v.GetComponent<TextMeshProUGUI>();
                valueText.alignment = TextAlignmentOptions.MidlineRight;
                valueText.enableAutoSizing = false;
                valueText.fontSize = Mathf.Max(14f, label.fontSize * 0.85f);
                RectTransform vr = v.GetComponent<RectTransform>();
                RectTransform lr = label.GetComponent<RectTransform>();
                vr.anchorMin = new Vector2(0.45f, lr.anchorMin.y);
                vr.anchorMax = new Vector2(1f, lr.anchorMax.y);
                vr.offsetMin = new Vector2(0f, lr.offsetMin.y);
                vr.offsetMax = new Vector2(-24f, lr.offsetMax.y);
                ControllerGlyphs.ApplySpriteAsset(valueText);
            }

            HGButton button = GetComponent<HGButton>();
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
                button.updateTextOnHover = true;
                button.hoverToken = resetRow ? "Puts every binding back to the built-in layout for your controller."
                    : def.Description + "  Press A on this row, then press the button (or hold a modifier and press a button) you want. Hold it longer than half a second for a hold binding.";
                button.hoverLanguageTextMeshController = description;
            }
            descriptionPanel = description;

            BindingActions.Changed += Refresh;
            BindingCapture.Finished += OnFinished;
            Refresh();
        }

        private void OnDestroy()
        {
            BindingActions.Changed -= Refresh;
            BindingCapture.Finished -= OnFinished;
        }

        private float warnUntil;
        private LanguageTextMeshController descriptionPanel;

        private void OnFinished(BindingDef d)
        {
            if (d == def && !resetRow)
            {
                var conflicts = BindingActions.ConflictsWith(def);
                if (conflicts.Count > 0)
                {
                    warnUntil = Time.unscaledTime + 5f;
                    string names = string.Join(", ", conflicts.ConvertAll(c => c.Name).ToArray());
                    if (descriptionPanel) descriptionPanel.token = $"<color=#ff8080>Warning: {def.Current.Label()} is already used by {names}. Both will fire. Rebind one of them.</color>";
                }
            }
            Refresh();
        }

        private void OnClick()
        {
            if (resetRow) { BindingActions.ResetAll(); return; }
            if (BindingCapture.Listening) return;
            BindingCapture.Begin(def);
            Refresh();
        }

        private void Update()
        {
            if (!resetRow && BindingCapture.Listening && BindingCapture.Target == def && valueText)
                valueText.text = "<color=#ffd070>" + BindingCapture.Status + "</color>";
            else if (warnUntil > 0f && Time.unscaledTime > warnUntil) { warnUntil = 0f; Refresh(); }
        }

        private void Refresh()
        {
            if (!valueText) return;
            if (resetRow) { valueText.text = ""; return; }
            string s = def.Current.Label();
            var conflicts = BindingActions.ConflictsWith(def);
            if (conflicts.Count > 0)
            {
                StringBuilder sb = new StringBuilder(s).Append(Time.unscaledTime < warnUntil ? "  <color=#ff4040>⚠ also: " : "  <color=#ff8080>(also: ");
                for (int i = 0; i < conflicts.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(conflicts[i].Name); }
                s = sb.Append(Time.unscaledTime < warnUntil ? "</color>" : ")</color>").ToString();
            }
            valueText.text = s;
        }

        private void Dump()
        {
            try
            {
                StringBuilder sb = new StringBuilder("[VR bindings] Settings row template hierarchy:\n");
                Walk(transform, 0, sb);
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch { }
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            sb.Append(' ', depth * 2).Append(t.name).Append(" [");
            foreach (Component c in t.GetComponents<Component>()) if (c) sb.Append(c.GetType().Name).Append(' ');
            sb.Append("]\n");
            foreach (Transform ch in t) Walk(ch, depth + 1, sb);
        }
    }
}
