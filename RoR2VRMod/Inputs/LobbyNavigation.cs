using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// Character select (lobby) support. This class does NOT read any buttons.
    ///
    /// Moving between the survivor grid and the panels on the right (expansions / DLC, artifacts) is
    /// the game's own page navigation - "UIPageLeft" / "UIPageRight", bound to the left and right
    /// triggers in RewiredAddons, exactly as the screen's prompt draws it.
    ///
    /// The left stick is confined to one SIDE of the screen at a time. Every selectable is classified
    /// as either left-side (survivor grid and the survivor's characteristics) or right-side
    /// (difficulty, expansions, artifacts), and its navigation links are wired ONLY to selectables on
    /// the same side. The stick therefore cannot cross sides at all - only the triggers, which drive
    /// the game's page switch, move the highlight between the two sides. The footer bar (Ready, back)
    /// is left completely untouched.
    /// </summary>
    internal static class LobbyNavigation
    {
        private static CharacterSelectController activeLobby;

        internal static bool IsInLobby => activeLobby != null && activeLobby.isActiveAndEnabled;

        private static int nextSweepFrame;




        internal static void Init()
        {
            On.RoR2.UI.CharacterSelectController.OnEnable += (orig, self) =>
            {
                orig(self);
                activeLobby = self;
                nextSweepFrame = 0;
            };

            On.RoR2.UI.CharacterSelectController.OnDisable += (orig, self) =>
            {
                orig(self);
                if (activeLobby == self)
                    activeLobby = null;
            };

        }

        /// <summary>
        /// Called every input frame from Controllers.UpdateVRInputs. Reads no input; it just keeps the
        /// lobby controls navigable as pages come and go, a few times a second.
        /// </summary>
        internal static void Poll()
        {
            CharacterSelectController lobby = activeLobby;
            if (!lobby || !lobby.isActiveAndEnabled) return;

            if (Time.frameCount < nextSweepFrame) return;
            nextSweepFrame = Time.frameCount + 15;

            try
            {
                MakeNavigable(lobby);
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogWarning($"[VR input] Lobby navigation upkeep failed: {e.Message}");
            }
        }

        private static bool InsideFrustum(Plane[] frustum, Vector3 worldPos)
        {
            foreach (Plane plane in frustum)
            {
                if (plane.GetDistanceToPoint(worldPos) < -0.5f)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The footer bar (Ready button, back) keeps whatever navigation the game gave it. The X and Y
        /// buttons drive Ready and Load directly, so those must never be rewired. Also excluded: the
        /// lobby control panel (Lobby button) and the chat box, which the game lays out OUTSIDE the
        /// visible rules panel (above and below the canvas). Wiring them into the graph lets the stick
        /// walk the highlight off-screen when pushing right on Difficulty / Expansions / Artifacts.
        /// </summary>
        private static bool IsExcluded(Selectable selectable, Transform canvasRoot)
        {
            Transform t = selectable.transform;
            while (t != null && t != canvasRoot)
            {
                string n = t.name;
                if (n.IndexOf("footer", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("ready", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("lobbycontrolpanel", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("chatbox", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("miscpanel", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static bool IsCategoryHeader(Selectable s)
        {
            return s && s.name.IndexOf("Edit Category", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private struct Entry
        {
            public Selectable selectable;
            public Vector2 pos;
            public bool rightSide;
        }

        /// <summary>
        /// For one entry, finds the closest same-side neighbour in the given direction (canvas-local
        /// space). Off-axis distance is penalized so navigation follows rows and columns naturally.
        /// Returns null when nothing lies that way - the stick simply stops at the side's edge.
        /// </summary>
        private static Selectable FindNeighbor(List<Entry> entries, int self, Vector2 direction)
        {
            Selectable best = null;
            float bestScore = float.MaxValue;
            Vector2 from = entries[self].pos;
            bool side = entries[self].rightSide;

            for (int i = 0; i < entries.Count; i++)
            {
                if (i == self || entries[i].rightSide != side) continue;

                Vector2 delta = entries[i].pos - from;
                float along = Vector2.Dot(delta, direction);
                if (along < 1f) continue;

                float across = Mathf.Abs(delta.x * direction.y) + Mathf.Abs(delta.y * direction.x);
                float score = along + 2f * across;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = entries[i].selectable;
                }
            }
            return best;
        }

        /// <summary>
        /// A sensible control to land on when the lobby has no selection (e.g. after the chat
        /// keyboard closes): the chosen survivor's icon, else the first survivor icon.
        /// </summary>
        internal static Selectable DefaultSelectable()
        {
            CharacterSelectController lobby = activeLobby;
            if (!lobby || !lobby.isActiveAndEnabled) return null;
            Selectable first = null;
            foreach (SurvivorIconController icon in lobby.GetComponentsInChildren<SurvivorIconController>(false))
            {
                Selectable s = icon.GetComponent<Selectable>();
                if (!s || !s.gameObject.activeInHierarchy || !s.IsInteractable()) continue;
                if (icon.isCurrentChoice) return s;
                if (first == null) first = s;
            }
            return first;
        }

        private static void MakeNavigable(CharacterSelectController lobby)
        {
            Canvas canvas = lobby.GetComponentInParent<Canvas>();
            Transform canvasRoot = canvas ? canvas.rootCanvas.transform : lobby.transform;

            Camera uiCam = null;
            if (canvas) uiCam = canvas.worldCamera;
            if (!uiCam) uiCam = Utils.localCameraRig ? Utils.localCameraRig.uiCam : null;

            Plane[] frustum = null;
            if (uiCam)
            {
                try { frustum = GeometryUtility.CalculateFrustumPlanes(uiCam); }
                catch (System.Exception) { frustum = null; }
            }

            var entries = new List<Entry>();
            float gridMaxX = float.MinValue;

            foreach (Selectable selectable in canvasRoot.GetComponentsInChildren<Selectable>(false))
            {
                if (!selectable || !selectable.gameObject.activeInHierarchy) continue;
                if (!selectable.IsInteractable()) continue;
                if (IsExcluded(selectable, canvasRoot)) continue;

                Vector3 pos = selectable.transform.position;
                Vector3 local = canvasRoot.InverseTransformPoint(pos);

                // The survivor grid is the left/right anchor. Update it even when an icon sits just
                // outside the frustum, so the side split can never collapse like it did in 2.19.10.
                if (selectable.GetComponentInParent<SurvivorIconController>() && local.x > gridMaxX)
                    gridMaxX = local.x;

                if (frustum != null && !InsideFrustum(frustum, pos))
                    continue;

                entries.Add(new Entry { selectable = selectable, pos = new Vector2(local.x, local.y) });
            }

            // Everything clearly to the right of the survivor grid's right edge is the rules side
            // (difficulty, expansions, artifacts). Without a grid to anchor on, fall back to the
            // canvas centerline.
            float boundary = gridMaxX > float.MinValue ? gridMaxX + 100f : 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                e.rightSide = e.pos.x > boundary;
                entries[i] = e;
            }

            // The Difficulty / Expansions / Artifacts header buttons sit at the far left of their
            // header bar, but visually the bar spans the whole column. Treat them as centred on the
            // column so up/down walks Difficulty -> Expansions header -> expansions -> Artifacts
            // header -> artifacts, instead of only being reachable by pushing left.
            float rightSum = 0f; int rightN = 0;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].rightSide && !IsCategoryHeader(entries[i].selectable)) { rightSum += entries[i].pos.x; rightN++; }
            if (rightN > 0)
            {
                float centreX = rightSum / rightN;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (!entries[i].rightSide || !IsCategoryHeader(entries[i].selectable)) continue;
                    Entry e = entries[i];
                    e.pos = new Vector2(centreX, e.pos.y);
                    entries[i] = e;
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Navigation nav = entries[i].selectable.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp    = FindNeighbor(entries, i, Vector2.up);
                nav.selectOnDown  = FindNeighbor(entries, i, Vector2.down);
                nav.selectOnLeft  = FindNeighbor(entries, i, Vector2.left);
                nav.selectOnRight = FindNeighbor(entries, i, Vector2.right);
                entries[i].selectable.navigation = nav;
            }

        }
    }
}
