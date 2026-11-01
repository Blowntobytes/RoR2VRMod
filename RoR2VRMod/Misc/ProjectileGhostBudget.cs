using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Part of "VR performance mode" (2.28.0): at most MaxVisible projectile visuals of the
    /// PLAYER team are drawn at once (ATG missiles, Pocket I.C.B.M., fireworks, drone and turret
    /// shots...). A projectile's visible part is its "ghost" (ProjectileGhostController), a
    /// purely visual object that follows the real projectile; the projectile itself - flight,
    /// hits, damage - is untouched. The game has no cap on projectiles at all
    /// (ProjectileController.projectileCount is only counted, never checked).
    /// When a new player projectile appears while MaxVisible are already shown, the OLDEST shown
    /// one is hidden (its renderers and lights switched off), so the count stays at MaxVisible and
    /// what you see is always the most recent missiles - the ones leaving you - while the oldest,
    /// about to hit anyway, vanish first. Hidden ghosts are restored when the game disables them
    /// (projectile gone / ghost returned to its pool). Enemy projectiles are never hidden.
    /// </summary>
    internal static class ProjectileGhostBudget
    {
        private const int MaxVisible = 150;
        // 2.26.42 (user): Electric Boomerang (red item, StunAndPierceBoomerang) visuals are heavy and
        // stack up; in performance mode at most this many are drawn at once (they still fly, stun, hit).
        private const int MaxBoomerangsVisible = 8;
        private const float ReportInterval = 10f;

        private class Hidden
        {
            public readonly List<Renderer> renderers = new List<Renderer>();
            public readonly List<Light> lights = new List<Light>();
        }

        private static readonly LinkedList<ProjectileGhostController> visible = new LinkedList<ProjectileGhostController>();
        private static readonly LinkedList<ProjectileGhostController> boomerangs = new LinkedList<ProjectileGhostController>();
        private static readonly HashSet<ProjectileGhostController> isBoomerang = new HashSet<ProjectileGhostController>();
        private static int hiddenBoomerangs;
        private static readonly Dictionary<ProjectileGhostController, LinkedListNode<ProjectileGhostController>> nodes = new Dictionary<ProjectileGhostController, LinkedListNode<ProjectileGhostController>>();
        private static readonly Dictionary<ProjectileGhostController, Hidden> hidden = new Dictionary<ProjectileGhostController, Hidden>();
        private static int hiddenCount, peakAlive;
        private static float nextReport;

        internal static void Init()
        {
            On.RoR2.Projectile.ProjectileController.Start += (orig, self) =>
            {
                orig(self);
                try { OnProjectileStarted(self); } catch (System.Exception e) { VRMod.StaticLogger.LogWarning("[VR perf] projectile visual cap: " + e.Message); }
            };
            On.RoR2.Projectile.ProjectileGhostController.OnDisable += (orig, self) =>
            {
                Release(self);
                orig(self);
            };
        }

        private static void OnProjectileStarted(ProjectileController projectile)
        {
            if (!ModConfig.ReduceEffectsUnderLoad.Value) return;
            ProjectileGhostController ghost = projectile.ghost;
            if (!ghost || nodes.ContainsKey(ghost) || hidden.ContainsKey(ghost)) return;
            TeamFilter team = projectile.teamFilter;
            if (!team || team.teamIndex != TeamIndex.Player) return;

            BoomerangProjectile boom = projectile.GetComponent<BoomerangProjectile>();
            if (boom && boom.isStunAndPierce)
            {
                isBoomerang.Add(ghost);
                nodes[ghost] = boomerangs.AddLast(ghost);
                while (boomerangs.Count > MaxBoomerangsVisible)
                {
                    ProjectileGhostController oldestB = boomerangs.First.Value;
                    boomerangs.RemoveFirst();
                    nodes.Remove(oldestB);
                    isBoomerang.Remove(oldestB);
                    if (oldestB) { Hide(oldestB); hiddenBoomerangs++; }
                }
                Report();
                return;
            }

            nodes[ghost] = visible.AddLast(ghost);
            while (visible.Count > MaxVisible)
            {
                ProjectileGhostController oldest = visible.First.Value;
                visible.RemoveFirst();
                nodes.Remove(oldest);
                if (oldest) Hide(oldest);
            }
            int alive = visible.Count + hidden.Count;
            if (alive > peakAlive) peakAlive = alive;
            Report();
        }

        private static void Hide(ProjectileGhostController ghost)
        {
            var h = new Hidden();
            foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; h.renderers.Add(r); }
            foreach (Light l in ghost.GetComponentsInChildren<Light>())
                if (l.enabled) { l.enabled = false; h.lights.Add(l); }
            hidden[ghost] = h;
            hiddenCount++;
        }

        private static void Release(ProjectileGhostController ghost)
        {
            if (nodes.TryGetValue(ghost, out var node))
            {
                if (isBoomerang.Remove(ghost)) boomerangs.Remove(node); else visible.Remove(node);
                nodes.Remove(ghost);
            }
            if (hidden.TryGetValue(ghost, out Hidden h))
            {
                foreach (Renderer r in h.renderers) if (r) r.enabled = true;
                foreach (Light l in h.lights) if (l) l.enabled = true;
                hidden.Remove(ghost);
            }
        }

        private static void Report()
        {
            float now = Time.unscaledTime;
            if (now < nextReport) return;
            if (hiddenBoomerangs > 0)
                VRMod.StaticLogger.LogInfo($"[VR perf] last {ReportInterval:0}s: {hiddenBoomerangs} Electric Boomerang visual(s) hidden by the {MaxBoomerangsVisible}-at-once cap (they still fly, stun and hit).");
            hiddenBoomerangs = 0;
            if (hiddenCount > 0)
                VRMod.StaticLogger.LogInfo($"[VR perf] last {ReportInterval:0}s: {hiddenCount} player projectile visual(s) hidden by the {MaxVisible}-at-once cap (peak {peakAlive} player projectiles alive; they still fly and hit).");
            // drop entries whose ghost was destroyed without OnDisable
            if (visible.Count > 0)
            {
                var node = visible.First;
                while (node != null) { var next = node.Next; if (!node.Value) { nodes.Remove(node.Value); visible.Remove(node); } node = next; }
            }
            if (boomerangs.Count > 0)
            {
                var node = boomerangs.First;
                while (node != null) { var next = node.Next; if (!node.Value) { nodes.Remove(node.Value); isBoomerang.Remove(node.Value); boomerangs.Remove(node); } node = next; }
            }
            hiddenCount = peakAlive = 0;
            nextReport = now + ReportInterval;
        }
    }
}
