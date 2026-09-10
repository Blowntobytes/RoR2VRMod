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
        private static readonly List<BarrelInteraction> trackedBarrels = new List<BarrelInteraction>();
        private static readonly List<MultiShopController> trackedShops = new List<MultiShopController>();
        private static bool hooked;

        internal static void Init()
        {
            // Hooks are installed once and the feature is gated by the config value at runtime.
            if (hooked) return;
            hooked = true;

            On.RoR2.PurchaseInteraction.Awake += OnPurchaseAwake;
            On.RoR2.PurchaseInteraction.SetAvailable += OnSetAvailable;
            // Money pods (barrels) are not purchases; they have their own interaction class.
            On.RoR2.BarrelInteraction.Start += (orig, self) =>
            {
                orig(self);
                if (!ModConfig.HighlightChests.Value) return;
                trackedBarrels.Add(self);
                if (!self.opened) ApplyHighlight(self.gameObject);
            };
            // Multishops: the controller's own synced 'available' flag is the reliable signal that
            // the whole shop has closed; the terminals' flags can lag or (on some paths) never flip.
            On.RoR2.MultiShopController.Start += (orig, self) =>
            {
                orig(self);
                if (!trackedShops.Contains(self)) trackedShops.Add(self);
            };
            RoR2Application.onUpdate += Poll;
        }

        /// <summary>Is this purchase still worth glowing? Handles multishop terminals too.</summary>
        private static bool IsUsable(PurchaseInteraction p)
        {
            if (!p.available || !p.isActiveAndEnabled) return false;
            ShopTerminalBehavior terminal = p.GetComponent<ShopTerminalBehavior>();
            if (terminal)
            {
                if (terminal.hasBeenPurchased) return false;
                // A closed multishop terminal has its pickup cleared (synced to clients).
                if (terminal.hasStarted && terminal.CurrentPickupIndex() == PickupIndex.none) return false;
                if (terminal.serverMultiShopController && !terminal.serverMultiShopController.available) return false;
                for (int i = 0; i < trackedShops.Count; i++)
                {
                    MultiShopController shop = trackedShops[i];
                    if (!shop || shop.available || shop.terminalGameObjects == null) continue;
                    for (int t = 0; t < shop.terminalGameObjects.Length; t++)
                        if (shop.terminalGameObjects[t] == p.gameObject) return false;
                }
            }
            return true;
        }

        private static float nextPoll;

        /// <summary>
        /// SetAvailable only runs on the host; on clients the flag arrives as a synced variable
        /// and no hook fires, so a chest opened by another player (or a whole multishop closing
        /// after one terminal is bought) kept glowing. Poll the synced flag a few times a second
        /// and keep every outline in step with it.
        /// </summary>
        private static void Poll()
        {
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.25f;
            if (!ModConfig.HighlightChests.Value || (tracked.Count == 0 && trackedBarrels.Count == 0)) return;
            trackedShops.RemoveAll(x => !x);

            for (int i = trackedBarrels.Count - 1; i >= 0; i--)
            {
                BarrelInteraction b = trackedBarrels[i];
                if (!b) { trackedBarrels.RemoveAt(i); continue; }
                Highlight hl = b.GetComponent<Highlight>();
                bool wantOn = !b.opened && b.isActiveAndEnabled;
                if (wantOn) { if (!hl || !hl.isOn) ApplyHighlight(b.gameObject); }
                else if (hl && hl.isOn) hl.isOn = false;
            }

            for (int i = tracked.Count - 1; i >= 0; i--)
            {
                PurchaseInteraction p = tracked[i];
                if (!p) { tracked.RemoveAt(i); continue; }
                Highlight hl = p.GetComponent<Highlight>();
                bool wantOn = IsUsable(p);
                if (wantOn)
                {
                    if (!hl || !hl.isOn) ApplyHighlight(p);
                }
                else if (hl && hl.isOn)
                {
                    hl.isOn = false;
                }
            }
        }

        // NOTE: the lists are deliberately NOT cleared on Stage.Start. The scene director can
        // populate the stage before Stage.Start runs, so clearing there threw away everything the
        // director had just spawned (multishop terminals included) - they kept the glow applied in
        // Awake but were no longer polled, so nothing ever switched it off. Destroyed objects turn
        // into nulls on the next stage and are pruned in Poll instead.


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
            ApplyHighlight(purchase.gameObject);
        }

        private static void ApplyHighlight(GameObject go)
        {
            if (!go) return;
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
                trackedBarrels.RemoveAll(x => !x);
                foreach (var b in trackedBarrels)
                {
                    if (!b.opened) ApplyHighlight(b.gameObject);
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
                trackedBarrels.RemoveAll(x => !x);
                foreach (var b in trackedBarrels)
                {
                    Highlight hl = b.GetComponent<Highlight>();
                    if (hl) hl.isOn = false;
                }
            }
        }
    }
}
