using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Highlights purchasable interactables (chests, drones, shrines, etc.) with a coloured outline
    /// so they are easier to spot in VR. Activated from the VR settings menu.
    /// Inspired by QoLChests (Faustvii) but implemented natively so it works well in VR.
    /// </summary>
    internal static class ChestHighlighter
    {
        private static readonly List<PurchaseInteraction> tracked = new List<PurchaseInteraction>();
        private static bool hooked;

        internal static void Init()
        {
            // Hooks are installed once and the feature is gated by the config value at runtime.
            if (hooked) return;
            hooked = true;

            On.RoR2.PurchaseInteraction.Awake += OnPurchaseAwake;
            On.RoR2.PurchaseInteraction.SetAvailable += OnSetAvailable;
            On.RoR2.Stage.Start += OnStageStart;
        }

        private static IEnumerator OnStageStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            tracked.Clear();
            return orig(self);
        }

        private static void OnPurchaseAwake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            orig(self);

            if (!ModConfig.HighlightChests.Value) return;

            tracked.Add(self);
            ApplyHighlight(self);
        }

        private static void OnSetAvailable(On.RoR2.PurchaseInteraction.orig_SetAvailable orig, PurchaseInteraction self, bool newAvailable)
        {
            orig(self, newAvailable);

            if (!ModConfig.HighlightChests.Value) return;

            if (!newAvailable)
            {
                RemoveHighlight(self);
            }
        }

        private static void ApplyHighlight(PurchaseInteraction purchase)
        {
            if (!purchase || !purchase.available) return;

            GameObject go = purchase.gameObject;
            Highlight existing = go.GetComponent<Highlight>();
            if (existing && existing.isOn) return;

            Highlight hl = existing ?? go.AddComponent<Highlight>();
            hl.highlightColor = Highlight.HighlightColor.interactive;
            hl.strength = 1f;
            hl.isOn = true;

            // Find the best renderer for the outline. PurchaseInteraction objects often have the
            // mesh on a child rather than the root.
            if (!hl.targetRenderer)
            {
                Renderer r = go.GetComponentInChildren<MeshRenderer>();
                if (!r) r = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (r) hl.targetRenderer = r;
            }
        }

        private static void RemoveHighlight(PurchaseInteraction purchase)
        {
            if (!purchase) return;

            Highlight hl = purchase.gameObject.GetComponent<Highlight>();
            if (hl)
            {
                hl.isOn = false;
            }
        }

        /// <summary>
        /// Called when the setting is toggled at runtime — applies or removes highlights on all
        /// currently tracked interactables.
        /// </summary>
        internal static void OnSettingChanged(object sender, EventArgs e)
        {
            if (ModConfig.HighlightChests.Value)
            {
                // Apply to everything currently alive.
                tracked.RemoveAll(x => !x);
                foreach (var p in tracked)
                {
                    if (p.available) ApplyHighlight(p);
                }
            }
            else
            {
                // Remove all highlights.
                tracked.RemoveAll(x => !x);
                foreach (var p in tracked)
                {
                    RemoveHighlight(p);
                }
            }
        }
    }
}
