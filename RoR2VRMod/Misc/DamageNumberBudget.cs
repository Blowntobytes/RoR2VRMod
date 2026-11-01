using System.Collections.Generic;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Part of "VR performance mode" since 2.26.7 (2.27.0 had it as a separate option).
    ///
    /// Every damage/heal number is one particle emitted by RoR2.DamageNumberManager.SpawnDamageNumber.
    /// Each call also reads the custom data of ALL live number particles
    /// (ParticleSystem.GetCustomParticleData) and writes it all back (SetCustomParticleData), so the
    /// cost of one new number grows with how many are already on screen - with many bleed/burn
    /// stacks ticking it scales roughly with the square of the numbers per second.
    /// With the option on, numbers are shown normally up to MaxPerSecond; past that, further
    /// numbers in the same second are not spawned. Visual only, local only.
    /// </summary>
    internal static class DamageNumberBudget
    {
        private const int MaxPerSecond = 30;
        private const float ReportInterval = 10f;

        private static readonly Queue<float> shownTimes = new Queue<float>(MaxPerSecond + 1);
        private static int skipped, shown;
        private static float nextReport;

        internal static void Init()
        {
            On.RoR2.DamageNumberManager.SpawnDamageNumber += (orig, self, amount, position, crit, teamIndex, damageColorIndex) =>
            {
                if (!ModConfig.ReduceEffectsUnderLoad.Value) { orig(self, amount, position, crit, teamIndex, damageColorIndex); return; }

                float now = Time.unscaledTime;
                while (shownTimes.Count > 0 && now - shownTimes.Peek() >= 1f) shownTimes.Dequeue();
                if (shownTimes.Count < MaxPerSecond)
                {
                    shownTimes.Enqueue(now);
                    shown++;
                    orig(self, amount, position, crit, teamIndex, damageColorIndex);
                }
                else skipped++;

                if (now >= nextReport)
                {
                    if (skipped > 0)
                        VRMod.StaticLogger.LogInfo($"[VR damage numbers] last {ReportInterval:0}s: {skipped} damage number(s) not shown by 'VR performance mode', {shown} shown.");
                    skipped = shown = 0;
                    nextReport = now + ReportInterval;
                }
            };
        }
    }
}
