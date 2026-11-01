using RoR2;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// VR performance option "VR performance mode" (named "Reduce effects under heavy load" before 2.26.4).
    ///
    /// The game already has an effect budget: every active effect with a VFXAttributes adds its
    /// intensity score (Low 1, Medium 5, High 25) to VFXBudget.totalCost, and EffectManager.SpawnEffect
    /// asks VFXBudget.CanAffordSpawn before spawning. Low-priority effects start being skipped
    /// once the total passes 50, medium-priority ones past 200 (spawn chance = threshold / total);
    /// "Always" effects are never skipped. VR renders every effect twice at a higher frame-rate
    /// target, so those vanilla thresholds kick in far too late.
    ///
    /// Off by default (2.25.1). With the option on, the same rule is used with the thresholds at a tenth (low 5,
    /// medium 20; 2.25.0-2.25.1 used a quarter). "Always" effects are still never skipped. The check runs locally on each
    /// client, so it only changes what this player sees.
    /// </summary>
    internal static class EffectBudget
    {
        private const float ThresholdScale = 0.1f; // 2.25.2: was 0.25 (user: tighten a little)
        private const float ReportInterval = 10f;

        private static int skippedByUs, skippedByVanilla, peakCost, alwaysSpawned, otherSpawned, alwaysSkipped;
        // 2.28.0: ceiling for 'always' effects (never skipped before). Highest rate seen in
        // logs so far was ~44/s (multiplayer Mithrix); set well above so normal fights are untouched.
        private const int AlwaysMaxPerSecond = 80;
        private static readonly System.Collections.Generic.Queue<float> alwaysTimes = new System.Collections.Generic.Queue<float>(AlwaysMaxPerSecond + 1);
        private static float nextReport;

        internal static void Init()
        {
            On.RoR2.VFXBudget.CanAffordSpawn_VFXIntensity_VFXPriority += (orig, intensity, priority) =>
            {
                if (!ModConfig.ReduceEffectsUnderLoad.Value) return orig(intensity, priority);
                if (priority == VFXAttributes.VFXPriority.Always)
                {
                    float t = Time.unscaledTime;
                    while (alwaysTimes.Count > 0 && t - alwaysTimes.Peek() >= 1f) alwaysTimes.Dequeue();
                    if (alwaysTimes.Count >= AlwaysMaxPerSecond) { alwaysSkipped++; Report(); return false; }
                    alwaysTimes.Enqueue(t);
                    alwaysSpawned++;
                    Report();
                    return orig(intensity, priority);
                }

                int real = VFXBudget.totalCost + VFXBudget.GetIntensityScore(intensity) + VFXBudget.particleCostBias.value;
                VFXBudget.realTotalCost = real;
                if (real > peakCost) peakCost = real;

                float vanillaThreshold = priority == VFXAttributes.VFXPriority.Low
                    ? VFXBudget.lowPriorityCostThreshold.value
                    : VFXBudget.mediumPriorityCostThreshold.value;
                float ratio = real > 0 ? (vanillaThreshold * ThresholdScale) / real : 1f;
                float roll = Random.value;
                bool spawn = Mathf.Pow(ratio, VFXBudget.chanceFailurePower) > roll;

                if (spawn) otherSpawned++;
                else
                {
                    // Same roll against the vanilla threshold, only for the diagnostic line.
                    float vanillaRatio = real > 0 ? vanillaThreshold / real : 1f;
                    if (Mathf.Pow(vanillaRatio, VFXBudget.chanceFailurePower) > roll) skippedByUs++;
                    else skippedByVanilla++;
                }
                Report();
                return spawn;
            };
        }

        private static void Report()
        {
            float now = Time.unscaledTime;
            if (now < nextReport) return;
            if (skippedByUs + skippedByVanilla + alwaysSkipped > 0)
            {
                VRMod.StaticLogger.LogInfo($"[VR perf] last {ReportInterval:0}s: {skippedByUs} extra effect(s) skipped by 'VR performance mode' ({skippedByVanilla} the game would have skipped anyway), peak effect cost {peakCost}; spawned: {alwaysSpawned} 'always' ({alwaysSkipped} over the {AlwaysMaxPerSecond}/s ceiling skipped), {otherSpawned} low/medium.");
            }
            skippedByUs = skippedByVanilla = peakCost = alwaysSpawned = otherSpawned = alwaysSkipped = 0;
            nextReport = now + ReportInterval;
        }
    }
}
