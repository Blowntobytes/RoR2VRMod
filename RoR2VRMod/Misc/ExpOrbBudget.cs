using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Part of "VR performance mode" (2.26.46, user request; replaces the 2.26.42 gold orb cap, which
    /// targeted the wrong orbs): the bright blue-white experience orbs that fly into you on kills. At
    /// higher levels each kill's experience is split into many orbs (ExperienceManager.CalculateDenominations)
    /// and they become blinding. They are pure visuals created on each client
    /// (ExperienceManager.HandleCreateExpEffectInternal); experience itself is awarded separately by the
    /// server (ExperienceManager.AwardExperience), so hiding an orb never costs experience.
    /// In performance mode at most MaxPerSecond orbs are drawn per second; the rest fly invisibly
    /// (renderers, trail and light off) and end without their arrival flash and sound.
    /// </summary>
    internal static class ExpOrbBudget
    {
        private const int MaxPerSecond = 10;
        private const float ReportInterval = 10f;

        private static readonly Queue<float> times = new Queue<float>(MaxPerSecond + 1);
        private static readonly HashSet<ExperienceOrbBehavior> hiddenOrbs = new HashSet<ExperienceOrbBehavior>();
        private static int skipped, shown;
        private static float nextReport;

        internal static void Init()
        {
            // Called for new and pooled (reused) orbs alike, so visibility is decided every time.
            On.RoR2.ExperienceOrbBehavior.SetupStartingValues += (orig, self) =>
            {
                orig(self);
                bool show = true;
                if (ModConfig.ReduceEffectsUnderLoad.Value)
                {
                    float t = Time.unscaledTime;
                    while (times.Count > 0 && t - times.Peek() >= 1f) times.Dequeue();
                    if (times.Count < MaxPerSecond) { times.Enqueue(t); shown++; }
                    else { show = false; skipped++; }
                    Report(t);
                }
                SetVisible(self, show);
            };
            On.RoR2.ExperienceOrbBehavior.OnHitTarget += (orig, self) =>
            {
                if (hiddenOrbs.Contains(self))
                {
                    hiddenOrbs.Remove(self);
                    self.consumed = true;
                    self.HandleEnd(); // no arrival flash / sound for a hidden orb
                    return;
                }
                orig(self);
            };
        }

        private static void SetVisible(ExperienceOrbBehavior orb, bool visible)
        {
            foreach (Renderer r in orb.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            foreach (Light l in orb.GetComponentsInChildren<Light>(true)) l.enabled = visible;
            if (visible) hiddenOrbs.Remove(orb); else hiddenOrbs.Add(orb);
        }

        private static void Report(float now)
        {
            if (now < nextReport) return;
            if (skipped > 0)
                VRMod.StaticLogger.LogInfo($"[VR perf] last {ReportInterval:0}s: {skipped} experience orb visual(s) hidden by the {MaxPerSecond}/s cap, {shown} shown (experience is still awarded).");
            skipped = shown = 0;
            hiddenOrbs.RemoveWhere(o => !o);
            nextReport = now + ReportInterval;
        }
    }
}
