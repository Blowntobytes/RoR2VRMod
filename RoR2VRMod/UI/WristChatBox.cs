using RoR2.UI;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// Wrist chat on DrBibop's original placement (UIFixes). On top of that:
    ///  * 2.24.16: taller - the game's StandardRect (the rect the message scroll view always lives in
    ///    while not typing) gets the same anchors as the game's ExpandedRect (0..1.5 of the chat box),
    ///    so it grows upwards by half. Nothing is re-parented (2.24.14/15 moved the scroll view into
    ///    ExpandedRect and the chat disappeared).
    ///  * no manual scrolling - scroll sensitivity 0, scrollbars non-interactable; the game's own
    ///    ScrollToBottom on each message still applies.
    ///  * "Wrist chat always visible": fade group kept at full alpha.
    ///  * 2.24.18: the game's dummy ScrollRect detached from the shared scrollbar and disabled (wrist flip scrolled the history).
    ///  * one-time "[VR HUD] Wrist chat state" dump 3 s after setup.
    /// </summary>
    internal class WristChatBox : MonoBehaviour
    {
        private ChatBox chatBox;
        private RectTransform root;
        private float dumpAt = -1f;

        internal static void Attach(RectTransform chatCluster, HUD hud)
        {
            if (!chatCluster) return;
            WristChatBox w = chatCluster.GetComponent<WristChatBox>() ?? chatCluster.gameObject.AddComponent<WristChatBox>();
            w.root = chatCluster;
        }

        private void LateUpdate()
        {
            // The chat prefab is instantiated into ChatBoxRoot some frames after the HUD exists.
            if (!chatBox && root)
            {
                chatBox = root.GetComponentInChildren<ChatBox>(true);
                if (chatBox) Setup();
            }
            if (!chatBox) return;
            try
            {
                if (ModConfig.WristChatNoFade.Value && chatBox.fadeGroup && chatBox.fadeGroup.alpha < 1f) chatBox.fadeGroup.alpha = 1f;
            }
            catch { }
            if (dumpAt > 0f && Time.unscaledTime >= dumpAt) { dumpAt = -1f; Dump(); }
        }

        private void Setup()
        {
            try
            {
                if (chatBox.standardChatboxRect)
                {
                    RectTransform s = chatBox.standardChatboxRect;
                    RectTransform e = chatBox.expandedChatboxRect;
                    s.anchorMax = new Vector2(s.anchorMax.x, e ? e.anchorMax.y : 1.5f);
                }
                if (chatBox.messagesText)
                {
                    chatBox.messagesText.scrollSensitivity = 0f;
                    if (chatBox.messagesText.verticalScrollbar) chatBox.messagesText.verticalScrollbar.interactable = false;
                }
                if (chatBox.scrollRect)
                {
                    // 2.24.18: the ScrollRect's Content is an empty dummy, but it shares the messages field's
                    // vertical scrollbar. When the watch canvas scales to 0 (wrist turned away) its bounds go
                    // degenerate and it writes a garbage value into that scrollbar, which scrolls the text to a
                    // random point of the history. Detach it and switch it off; the TMP field keeps its own
                    // link to the scrollbar, so the game's ScrollToBottom on new messages still works.
                    chatBox.scrollRect.verticalScrollbar = null;
                    chatBox.scrollRect.horizontalScrollbar = null;
                    chatBox.scrollRect.enabled = false;
                }
                chatBox.ScrollToBottom();
                dumpAt = Time.unscaledTime + 3f;
                VRMod.StaticLogger.LogInfo("[VR HUD] Wrist chat: standard rect raised to the expanded height, manual scrolling disabled.");
            }
            catch (System.Exception ex) { VRMod.StaticLogger.LogWarning("[VR HUD] Wrist chat setup failed: " + ex.Message); }
        }

        private void Dump()
        {
            try
            {
                StringBuilder sb = new StringBuilder("[VR HUD] Wrist chat state:\n");
                Walk(root, 0, sb);
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch { }
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            RectTransform r = t as RectTransform;
            sb.Append(' ', depth * 2).Append(t.name).Append(t.gameObject.activeInHierarchy ? "" : " (INACTIVE)");
            if (r) sb.Append($" rect={r.rect.size} anchors={r.anchorMin}-{r.anchorMax} pivot={r.pivot} pos={r.anchoredPosition} scale={r.localScale} world={r.position}");
            CanvasGroup cg = t.GetComponent<CanvasGroup>(); if (cg) sb.Append($" groupAlpha={cg.alpha}");
            Graphic g = t.GetComponent<Graphic>(); if (g) sb.Append($" color={g.color} enabled={g.enabled}");
            sb.Append('\n');
            if (depth < 7) foreach (Transform ch in t) Walk(ch, depth + 1, sb);
        }
    }
}
