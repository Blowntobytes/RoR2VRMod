using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// A bright frame drawn around whatever menu control is currently selected. The game's own
    /// selection tint is nearly invisible on the world-space VR menus, so people lose the cursor.
    /// The frame is a child of the selected element (stretched to its rect) so it follows layout,
    /// scrolling and destruction automatically.
    /// </summary>
    internal static class SelectionFrame
    {
        private static GameObject frame;
        private static GameObject lastTarget;
        private static Sprite frameSprite;

        internal static void Init()
        {
            RoR2Application.onUpdate += Tick;
        }

        private static void Tick()
        {
            if (!ModConfig.MenuSelectionFrame.Value) { if (frame) frame.SetActive(false); return; }

            GameObject target = null;
            try
            {
                LocalUser lu = LocalUserManager.GetFirstLocalUser();
                MPEventSystem es = lu != null ? lu.eventSystem : null;
                if (es) target = es.currentSelectedGameObject;
            }
            catch { }

            // Only decorate UI elements; never in-world objects.
            if (target && !(target.transform is RectTransform)) target = null;
            if (target && target.GetComponent<Selectable>() == null) target = null;

            if (!target)
            {
                if (frame) frame.SetActive(false);
                lastTarget = null;
                return;
            }
            if (target == lastTarget && frame && frame.activeSelf && frame.transform.parent == target.transform) return;
            lastTarget = target;

            if (!frame)
            {
                frame = new GameObject("VRSelectionFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Image img = frame.GetComponent<Image>();
                img.sprite = GetFrameSprite();
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 0.5f; // 6-unit border on the 1920-wide menu canvas
                img.color = new Color(1f, 0.82f, 0.2f, 0.95f);
                img.raycastTarget = false;
                frame.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
                frame.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            RectTransform rt = (RectTransform)frame.transform;
            rt.SetParent(target.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-4f, -4f);
            rt.offsetMax = new Vector2(4f, 4f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling();
            frame.layer = target.layer;
            frame.SetActive(true);
        }

        /// <summary>A 16x16 hollow square (3 px border) used as a 9-sliced frame.</summary>
        private static Sprite GetFrameSprite()
        {
            if (frameSprite) return frameSprite;
            const int size = 16, border = 3;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < border || y < border || x >= size - border || y >= size - border;
                    px[y * size + x] = edge ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            frameSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return frameSprite;
        }
    }
}
