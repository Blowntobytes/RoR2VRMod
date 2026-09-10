using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Marker placed on an item display instance that the mod has hidden for the local VR player.
    /// The runtime hand baker skips anything underneath one of these.
    /// </summary>
    internal class VRHiddenItemDisplay : MonoBehaviour { }

    /// <summary>
    /// Hides selected item displays on the LOCAL player's character model in VR. Some displays
    /// (e.g. Kinetic Dampener) sit right in the first-person view or on the hands and only get in
    /// the way. The list is configurable ([VR Settings] "Hidden item displays", comma separated,
    /// matched case-insensitively against the item's in-game name and its internal name).
    /// Item display prefabs are loaded asynchronously and re-enabled by the game on every
    /// inventory change, so this polls rather than hooking a single call.
    /// </summary>
    internal class ItemDisplayHider : MonoBehaviour
    {
        private CharacterModel model;
        private float nextCheck;
        private static readonly Dictionary<ItemIndex, bool> verdictCache = new Dictionary<ItemIndex, bool>();
        private static readonly HashSet<string> loggedHidden = new HashSet<string>();
        private static string cachedListSource;
        private static string[] cachedList = new string[0];

        private static float nextGlobalCheck;

        internal static void Init()
        {
            // Attach to the local player's model whenever it (re)appears - new stage, respawn,
            // vehicle exit - rather than relying on catching one particular call.
            RoR2Application.onUpdate += () =>
            {
                if (Time.unscaledTime < nextGlobalCheck) return;
                nextGlobalCheck = Time.unscaledTime + 0.5f;
                try
                {
                    CharacterBody body = Utils.localBody;
                    if (!body || !body.modelLocator || !body.modelLocator.modelTransform) return;
                    CharacterModel cm = body.modelLocator.modelTransform.GetComponent<CharacterModel>();
                    if (!cm) return;
                    ItemDisplayHider h = cm.GetComponent<ItemDisplayHider>();
                    if (!h) { h = cm.gameObject.AddComponent<ItemDisplayHider>(); h.model = cm; }
                    else if (h.model != cm) h.model = cm;
                }
                catch (Exception e) { VRMod.StaticLogger.LogWarning("[VR items] hider attach: " + e.Message); }
            };
        }

        private static string[] HiddenNames()
        {
            string src = ModConfig.HiddenItemDisplays != null ? ModConfig.HiddenItemDisplays.Value : "";
            if (src != cachedListSource)
            {
                cachedListSource = src;
                verdictCache.Clear();
                List<string> list = new List<string>();
                foreach (string s in src.Split(','))
                {
                    string t = s.Trim();
                    if (t.Length > 0) list.Add(t.ToLowerInvariant());
                }
                cachedList = list.ToArray();
            }
            return cachedList;
        }

        internal static bool IsHiddenItem(ItemIndex index)
        {
            string[] names = HiddenNames();
            if (names.Length == 0 || index == ItemIndex.None) return false;
            bool verdict;
            if (verdictCache.TryGetValue(index, out verdict)) return verdict;

            verdict = false;
            ItemDef def = ItemCatalog.GetItemDef(index);
            if (def)
            {
                string display = "";
                try { display = Language.GetString(def.nameToken) ?? ""; } catch { }
                string internalName = def.name ?? "";
                string a = display.ToLowerInvariant().Replace("'", "").Replace("-", " ");
                string b = internalName.ToLowerInvariant();
                foreach (string n in names)
                {
                    string nn = n.Replace("'", "").Replace("-", " ");
                    if ((a.Length > 0 && a == nn) || (b.Length > 0 && b == n) || (a.Length > 0 && a.Contains(nn)))
                    {
                        verdict = true;
                        break;
                    }
                }
            }
            verdictCache[index] = verdict;
            return verdict;
        }

        /// <summary>True when <paramref name="t"/> is inside an item display instance that is on the hidden list.</summary>
        internal static bool ShouldHide(CharacterModel charModel, Transform t)
        {
            if (!charModel || !t) return false;
            if (t.GetComponentInParent<VRHiddenItemDisplay>()) return true;
            if (HiddenNames().Length == 0) return false;

            List<CharacterModel.ParentedPrefabDisplay> displays = charModel.parentedPrefabDisplays;
            if (displays == null) return false;
            for (int i = 0; i < displays.Count; i++)
            {
                GameObject inst = displays[i].instance;
                if (!inst || displays[i].itemIndex == ItemIndex.None) continue;
                if (t == inst.transform || t.IsChildOf(inst.transform))
                    return IsHiddenItem(displays[i].itemIndex);
            }
            return false;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            if (!model) { Destroy(this); return; }
            if (HiddenNames().Length == 0) return;

            List<CharacterModel.ParentedPrefabDisplay> displays = model.parentedPrefabDisplays;
            if (displays == null) return;
            for (int i = 0; i < displays.Count; i++)
            {
                GameObject inst = displays[i].instance;
                if (!inst || displays[i].itemIndex == ItemIndex.None) continue;
                if (!IsHiddenItem(displays[i].itemIndex)) continue;

                if (!inst.GetComponent<VRHiddenItemDisplay>()) inst.AddComponent<VRHiddenItemDisplay>();
                if (inst.activeSelf)
                {
                    inst.SetActive(false);
                    ItemDef def = ItemCatalog.GetItemDef(displays[i].itemIndex);
                    string key = def ? def.name : displays[i].itemIndex.ToString();
                    if (loggedHidden.Add(key))
                        VRMod.StaticLogger.LogInfo($"[VR items] Hiding item display '{inst.name}' ({key}) on the local player.");
                }
            }
        }
    }
}
