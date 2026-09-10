using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// Stick navigation for the multiplayer screens (main / host / server browser).
    ///
    /// Two problems in VR (where the menus run as a gamepad): the server browser's filter column is
    /// hidden for gamepad input, and Unity's automatic navigation cannot get from the left column to
    /// the game list / lobby panel and back (empty list, mismatched navigation modes, big gaps).
    /// The filter column is un-hidden, and every half second the visible selectables of the current
    /// subview are rewired with EXPLICIT links: rows within a column go up/down, neighbours on the
    /// same row go left/right, and the edge of a column crosses to the nearest row of the other one.
    /// </summary>
    internal static class MultiplayerMenuNavigation
    {
        private static MultiplayerMenuController menu;
        private static float nextWire;
        private static int lastSignature;

        internal static void Init()
        {
            On.RoR2.UI.MainMenu.MultiplayerMenuController.OnEnable += (orig, self) => { orig(self); menu = self; lastSignature = 0; };
            On.RoR2.UI.MainMenu.MultiplayerMenuController.OnDisable += (orig, self) => { orig(self); if (menu == self) menu = null; };
            On.RoR2.UI.MainMenu.MultiplayerMenuController.SetSubview += (orig, self, subview) => { orig(self, subview); menu = self; lastSignature = 0; nextWire = 0f; };

            // The filter column of the server browser is hidden whenever the input source is a
            // gamepad (InputSourceFilter with requiredInputSource = MouseAndKeyboard). Show it
            // anyway for the browser's left panel; the toggles work fine with a stick and A.
            On.RoR2.UI.InputSourceFilter.Refresh += (orig, self, force) =>
            {
                try
                {
                    if (self.requiredInputSource == MPEventSystem.InputSource.MouseAndKeyboard && IsInBrowserLeftPanel(self.transform))
                        self.requiredInputSource = MPEventSystem.InputSource.Gamepad;
                }
                catch { }
                orig(self, force);
            };

            // The "Game Info" popup (password box, Refresh, Join Lobby) lives outside the subview
            // roots; while it is open it is the thing to navigate.
            On.RoR2.RemoteGameDetailsPanelController.Awake += (orig, self) => { orig(self); detailsPanel = self; lastSignature = 0; nextWire = 0f; };

            RoR2Application.onUpdate += Tick;
        }

        private static RemoteGameDetailsPanelController detailsPanel;

        private static bool IsInBrowserLeftPanel(Transform t)
        {
            bool seenLeft = false;
            while (t)
            {
                if (t.name == "LeftPanel") seenLeft = true;
                if (t.name == "GameBrowserPanel") return seenLeft;
                t = t.parent;
            }
            return false;
        }

        private static GameObject CurrentRoot()
        {
            if (detailsPanel && detailsPanel.gameObject.activeInHierarchy) return detailsPanel.gameObject;
            if (!menu) return null;
            switch (menu.GetCurrentSubview())
            {
                case MultiplayerMenuController.Subview.FindGame: return menu.JoinGameMenu;
                case MultiplayerMenuController.Subview.HostGame: return menu.HostGameMenu;
                default: return menu.MainMultiplayerMenu;
            }
        }

        private static GameObject lastValidSelection;
        private static float selectionLostAt = -1f;

        private static void Tick()
        {
            if (!menu || !menu.isActiveAndEnabled) return;
            try { KeepSelection(); }
            catch (System.Exception e) { VRMod.StaticLogger.LogWarning("[VR mp-menu] selection: " + e.Message); }
            if (Time.unscaledTime < nextWire) return;
            nextWire = Time.unscaledTime + 0.5f;
            try { Wire(CurrentRoot()); }
            catch (System.Exception e) { VRMod.StaticLogger.LogWarning("[VR mp-menu] wiring: " + e.Message); }
        }

        /// <summary>
        /// The stick can only move a highlight that exists. Refreshing the list, opening the lobby
        /// panel or closing a dropdown leaves the event system with NO selected object, and then
        /// nothing responds. Restore the last valid selection, or pick the first control.
        /// </summary>
        private static void KeepSelection()
        {
            MPEventSystem es = null;
            try { LocalUser lu = LocalUserManager.GetFirstLocalUser(); es = lu != null ? lu.eventSystem : null; } catch { }
            if (!es) return;
            GameObject sel = es.currentSelectedGameObject;
            if (sel && sel.activeInHierarchy)
            {
                Selectable s = sel.GetComponent<Selectable>();
                if (s && s.IsInteractable()) { lastValidSelection = sel; selectionLostAt = -1f; return; }
            }
            if (selectionLostAt < 0f) { selectionLostAt = Time.unscaledTime; return; }
            if (Time.unscaledTime - selectionLostAt < 0.15f) return; // give the game a moment to select something itself
            selectionLostAt = -1f;

            GameObject root = CurrentRoot();
            if (!root) return;
            GameObject target = null;
            if (lastValidSelection && lastValidSelection.activeInHierarchy && lastValidSelection.transform.IsChildOf(root.transform))
            {
                Selectable s = lastValidSelection.GetComponent<Selectable>();
                if (s && s.IsInteractable()) target = lastValidSelection;
            }
            if (!target)
            {
                Selectable first = root.GetComponentsInChildren<Selectable>(false)
                    .Where(Eligible).OrderByDescending(x => x.transform.position.y).ThenBy(x => x.transform.position.x).FirstOrDefault();
                if (first) target = first.gameObject;
            }
            if (target) es.SetSelectedGameObject(target);
        }

        private static bool Eligible(Selectable s)
        {
            if (!s || !s.gameObject.activeInHierarchy || !s.IsInteractable()) return false;
            if (s is Scrollbar) return false; // scrollbars are noise
            // Text boxes are navigated through an invisible proxy button laid over them (see
            // VRKeyboard.EnsureProxy); the box itself is never selected directly.
            if (s is TMPro.TMP_InputField || s is InputField) return false;
            if (s.GetComponent<VRKeyboard.FieldProxy>() && !ModConfig.VRKeyboardEnabled.Value) return false;
            // Leave open dropdown lists and dropdown templates to their own navigation.
            for (Transform t = s.transform; t; t = t.parent)
                if (t.name == "Dropdown List" || t.name == "Template") return false;
            // The Game Info popup's rule/player icon grids are informational; skip them so the
            // stick goes straight between the password box and the buttons.
            if (detailsPanel && (IsUnder(s.transform, detailsPanel.rulesContainer) || IsUnder(s.transform, detailsPanel.playersContainer))) return false;
            // The lobby panel's expand/collapse tab is reached with A on the panel itself - skip.
            return true;
        }

        private static bool IsUnder(Transform t, Transform parent)
        {
            return parent && t && t != parent && t.IsChildOf(parent);
        }

        private class Node { public Selectable s; public Vector3 p; public int col; public int row; }

        private static void Wire(GameObject root)
        {
            if (!root) return;

            // Give every visible text box a proxy first so it takes part in the wiring.
            foreach (Selectable s in root.GetComponentsInChildren<Selectable>(false))
            {
                if ((s is TMPro.TMP_InputField || s is InputField) && s.gameObject.activeInHierarchy && s.IsInteractable())
                {
                    bool skip = false;
                    for (Transform t = s.transform.parent; t && t != root.transform; t = t.parent)
                    {
                        if (t.name == "Dropdown List" || t.name == "Template") skip = true;
                        // A selectable row already wraps this box (host settings): the row is navigated.
                        Selectable wrapper = t.GetComponent<Selectable>();
                        if (wrapper && wrapper.gameObject.activeInHierarchy && wrapper.IsInteractable()) skip = true;
                    }
                    if (!skip) VRKeyboard.EnsureProxy(s);
                }
            }

            List<Node> nodes = new List<Node>();
            foreach (Selectable s in root.GetComponentsInChildren<Selectable>(false))
            {
                if (!Eligible(s)) continue;
                nodes.Add(new Node { s = s, p = s.transform.position });
            }
            // Also include the lobby panel if it lives next to the subview rather than under it.
            if (nodes.Count < 2) return;

            // Only rebuild when the set of selectables changed (cards appear after Refresh, the
            // lobby panel opens/closes, filters expand).
            int sig = 17;
            foreach (Node n in nodes) sig = sig * 31 + n.s.GetInstanceID();
            sig = sig * 31 + root.GetInstanceID();
            if (sig == lastSignature) return;
            lastSignature = sig;

            // Split into two columns at the largest horizontal gap.
            float[] xs = nodes.Select(n => n.p.x).Distinct().OrderBy(x => x).ToArray();
            float splitX = float.NegativeInfinity;
            if (xs.Length >= 2)
            {
                float bestGap = 0f;
                for (int i = 1; i < xs.Length; i++)
                {
                    float gap = xs[i] - xs[i - 1];
                    if (gap > bestGap) { bestGap = gap; splitX = (xs[i] + xs[i - 1]) * 0.5f; }
                }
                // A tiny "gap" means it is really one column; keep everything together then.
                if (bestGap < 1.0f) splitX = float.NegativeInfinity;
            }
            foreach (Node n in nodes) n.col = n.p.x < splitX ? 0 : 1;

            // Rows within each column (tolerance in world units; the menu is ~13 m wide).
            const float rowTol = 0.2f;
            List<List<Node>>[] rowsByCol = new List<List<Node>>[2];
            for (int c = 0; c < 2; c++)
            {
                List<Node> colNodes = nodes.Where(n => n.col == c).OrderByDescending(n => n.p.y).ToList();
                List<List<Node>> rows = new List<List<Node>>();
                foreach (Node n in colNodes)
                {
                    List<Node> row = rows.Count > 0 && Mathf.Abs(rows[rows.Count - 1][0].p.y - n.p.y) <= rowTol ? rows[rows.Count - 1] : null;
                    if (row == null) { row = new List<Node>(); rows.Add(row); }
                    row.Add(n);
                }
                foreach (List<Node> row in rows) row.Sort((a, b) => a.p.x.CompareTo(b.p.x));
                rowsByCol[c] = rows;
            }

            int wired = 0;
            for (int c = 0; c < 2; c++)
            {
                List<List<Node>> rows = rowsByCol[c];
                for (int r = 0; r < rows.Count; r++)
                {
                    List<Node> row = rows[r];
                    for (int i = 0; i < row.Count; i++)
                    {
                        Node n = row[i];
                        Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                        nav.selectOnUp = r > 0 ? NearestByX(rows[r - 1], n.p.x) : null;
                        nav.selectOnDown = r < rows.Count - 1 ? NearestByX(rows[r + 1], n.p.x) : null;
                        nav.selectOnLeft = i > 0 ? row[i - 1].s : (c == 1 ? NearestByY(rowsByCol[0], n.p.y) : null);
                        nav.selectOnRight = i < row.Count - 1 ? row[i + 1].s : (c == 0 ? NearestByY(rowsByCol[1], n.p.y) : null);
                        n.s.navigation = nav;
                        wired++;
                    }
                }
            }

        }

        private static Selectable NearestByX(List<Node> row, float x)
        {
            Node best = null; float bestD = float.MaxValue;
            foreach (Node n in row) { float d = Mathf.Abs(n.p.x - x); if (d < bestD) { bestD = d; best = n; } }
            return best != null ? best.s : null;
        }

        private static Selectable NearestByY(List<List<Node>> rows, float y)
        {
            Node best = null; float bestD = float.MaxValue;
            foreach (List<Node> row in rows)
                foreach (Node n in row) { float d = Mathf.Abs(n.p.y - y); if (d < bestD) { bestD = d; best = n; } }
            return best != null ? best.s : null;
        }
    }
}
