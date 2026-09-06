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

        private static int lastFixedCount;

        private static bool dumpedLayout;
        private static bool dumpedBigLayout;

        private static GameObject lastSelected;
        private static int lastSelectedFrame;
        private static bool tracedSelection;

        internal static void Init()
        {
            On.RoR2.UI.CharacterSelectController.OnEnable += (orig, self) =>
            {
                orig(self);
                activeLobby = self;
                nextSweepFrame = 0;
                lastFixedCount = -1;
                dumpedLayout = false;
                dumpedBigLayout = false;
                lastSelected = null;
                tracedSelection = false;
            };

            On.RoR2.UI.CharacterSelectController.OnDisable += (orig, self) =>
            {
                orig(self);
                if (activeLobby == self)
                    activeLobby = null;
            };

            // TRACE: log every selection change while the character select is open. This answers
            // the one question the layout dump cannot: when the left stick is pushed right on the
            // Difficulty / Expansions / Artifacts panel and the highlight "disappears", WHICH control
            // does the game actually select, and where is it? That tells us the real mechanism
            // (off-canvas control, clipped popout, ScrollRect content, or the game's own wiring)
            // instead of another guess.
            On.RoR2.UI.MPEventSystem.Update += (orig, self) =>
            {
                orig(self);
                if (!IsInLobby) return;

                try
                {
                    GameObject sel = self.currentSelectedGameObject;
                    if (sel == lastSelected) return;

                    // Ignore the very first callback (initial selection at screen open).
                    if (lastSelected == null && Time.frameCount - lastSelectedFrame < 2)
                    {
                        lastSelected = sel;
                        lastSelectedFrame = Time.frameCount;
                        return;
                    }

                    lastSelected = sel;
                    lastSelectedFrame = Time.frameCount;

                    if (sel == null)
                    {
                        VRMod.StaticLogger.LogInfo($"[VR input] Lobby select: <none> (deselected).");
                        return;
                    }

                    Selectable selSel = sel.GetComponent<Selectable>();
                    Transform root = activeLobby ? (activeLobby.GetComponentInParent<Canvas>() ? activeLobby.GetComponentInParent<Canvas>().rootCanvas.transform : null) : null;
                    string path = sel.name;
                    Transform p = sel.transform.parent;
                    int depth = 0;
                    while (p != null && p != root && depth < 6) { path = p.name + "/" + path; p = p.parent; depth++; }
                    string pos = root ? root.InverseTransformPoint(sel.transform.position).ToString("F0") : sel.transform.position.ToString("F2");
                    string grid = sel.GetComponentInParent<SurvivorIconController>() ? "GRID" : "    ";
                    string isSel = selSel ? $"interactable={selSel.IsInteractable()}" : "not-a-Selectable";
                    VRMod.StaticLogger.LogInfo($"[VR input] Lobby select: [{grid}] '{path}' local={pos} {isSel}");
                    tracedSelection = true;
                }
                catch (System.Exception e)
                {
                    VRMod.StaticLogger.LogWarning($"[VR input] Lobby select trace failed: {e.Message}");
                }
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
            int skippedOffCanvas = 0;

            // Diagnostic dump: capture every control's position each sweep, and print the layout on
            // the first sweep AND the first time the rules panel opens (right side grows), because
            // that is exactly when the stick starts walking off the visible screen.
            var dump = new System.Text.StringBuilder();
            foreach (Selectable selectable in canvasRoot.GetComponentsInChildren<Selectable>(false))
            {
                if (!selectable || !selectable.gameObject.activeInHierarchy) continue;
                if (!selectable.IsInteractable()) continue;
                if (IsExcluded(selectable, canvasRoot)) continue;

                Vector3 pos = selectable.transform.position;
                Vector3 local = canvasRoot.InverseTransformPoint(pos);

                if (dump.Length < 80000)
                {
                    string path = selectable.name;
                    Transform p = selectable.transform.parent;
                    int depth = 0;
                    while (p != null && p != canvasRoot && depth < 5) { path = p.name + "/" + path; p = p.parent; depth++; }
                    string screen = "n/a";
                    if (uiCam)
                    {
                        Vector3 sp = uiCam.WorldToScreenPoint(pos);
                        screen = $"({sp.x:F0},{sp.y:F0},z{sp.z:F2})";
                    }
                    string inFrustum = frustum == null ? "n/a" : InsideFrustum(frustum, pos) ? "Y" : "N";
                    string isGrid = selectable.GetComponentInParent<SurvivorIconController>() ? "GRID" : "    ";
                    dump.AppendLine($"    [{isGrid}] '{path}' world=({pos.x:F2},{pos.y:F2},{pos.z:F2}) local=({local.x:F0},{local.y:F0}) screen={screen} frustum={inFrustum}");
                }

                // The survivor grid is the left/right anchor. Update it even when an icon sits just
                // outside the frustum, so the side split can never collapse like it did in 2.19.10.
                if (selectable.GetComponentInParent<SurvivorIconController>() && local.x > gridMaxX)
                    gridMaxX = local.x;

                if (frustum != null && !InsideFrustum(frustum, pos))
                {
                    skippedOffCanvas++;
                    continue;
                }

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

            int rightCount = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].rightSide) rightCount++;

                Navigation nav = entries[i].selectable.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp    = FindNeighbor(entries, i, Vector2.up);
                nav.selectOnDown  = FindNeighbor(entries, i, Vector2.down);
                nav.selectOnLeft  = FindNeighbor(entries, i, Vector2.left);
                nav.selectOnRight = FindNeighbor(entries, i, Vector2.right);
                entries[i].selectable.navigation = nav;
            }

            // Print the captured layout twice per lobby: on the first sweep (survivor grid), and the
            // first time the rules panel is open (right count jumps) - the state that reproduces the
            // off-screen stick. Never more than once each so the log stays readable.
            bool isFirstSweep = lastFixedCount < 0;
            bool shouldDump = !dumpedLayout || (!dumpedBigLayout && rightCount >= 15);
            if (shouldDump)
            {
                if (rightCount >= 15) dumpedBigLayout = true;
                else dumpedLayout = true;

                string camName = uiCam ? uiCam.name : "NONE";
                string camState = uiCam ? $"enabled={uiCam.isActiveAndEnabled} pos=({uiCam.transform.position.x:F2},{uiCam.transform.position.y:F2},{uiCam.transform.position.z:F2}) rot=({uiCam.transform.eulerAngles.x:F0},{uiCam.transform.eulerAngles.y:F0},{uiCam.transform.eulerAngles.z:F0}) fov={uiCam.fieldOfView:F0} near={uiCam.nearClipPlane:F2} far={uiCam.farClipPlane:F0}" : "";
                string canvasState = canvas ? $"name='{canvas.name}' mode={canvas.renderMode} worldCam={(canvas.worldCamera ? canvas.worldCamera.name : "none")} pixelRect={canvas.pixelRect}" : "NO CANVAS";
                VRMod.StaticLogger.LogInfo($"[VR input] Lobby layout: canvas={canvasState}; uiCam='{camName}' {camState}; rootCanvas='{(canvas ? canvas.rootCanvas.name : "none")}'");
                VRMod.StaticLogger.LogInfo($"[VR input] Lobby controls ({dump.Length} chars):\n{dump}");
                VRMod.StaticLogger.LogInfo($"[VR input] Lobby split anchor: gridMaxX={gridMaxX:F0} -> boundary={(gridMaxX > float.MinValue ? gridMaxX + 100f : 0f):F0}; skippedOffCanvas={skippedOffCanvas}; firstSweep={isFirstSweep}");
            }

            if (entries.Count > 0 && entries.Count != lastFixedCount)
            {
                lastFixedCount = entries.Count;
                VRMod.StaticLogger.LogInfo($"[VR input] Lobby: side-confined navigation on {entries.Count} control(s) ({entries.Count - rightCount} left / {rightCount} right, boundary x={boundary:F0}, skipped {skippedOffCanvas} off-canvas).");
            }
        }
    }
}
