using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>Logs the bindings sub-panel's scroll structure once, the first time it is shown.</summary>
    internal class BindingsPanelDiagnostics : MonoBehaviour
    {
        private int frames;
        private void Update()
        {
            if (++frames != 3) return;
            try
            {
                StringBuilder sb = new StringBuilder("[VR bindings] Panel structure (frame 3 after show):\n");
                Walk(transform, 0, sb, 4);
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch { }
            Destroy(this);
        }

        private static void Walk(Transform t, int depth, StringBuilder sb, int maxDepth)
        {
            RectTransform r = t as RectTransform;
            sb.Append(' ', depth * 2).Append(t.name).Append(t.gameObject.activeSelf ? "" : " (inactive)");
            if (r) sb.Append($" rect={r.rect.size} anchors={r.anchorMin}-{r.anchorMax} pivot={r.pivot} pos={r.anchoredPosition}");
            sb.Append(" [");
            foreach (Component c in t.GetComponents<Component>())
            {
                if (!c || c is Transform || c is CanvasRenderer) continue;
                sb.Append(c.GetType().Name);
                if (c is ScrollRect sr) sb.Append($"(content={(sr.content ? sr.content.name : "null")}, vbar={(bool)sr.verticalScrollbar}, viewport={(sr.viewport ? sr.viewport.name : "null")})");
                sb.Append(' ');
            }
            sb.Append("]\n");
            if (depth < maxDepth) { int i = 0; foreach (Transform ch in t) { if (i++ > 3 && depth >= 3) { sb.Append(' ', (depth + 1) * 2).Append("…\n"); break; } Walk(ch, depth + 1, sb, maxDepth); } }
        }
    }
}
