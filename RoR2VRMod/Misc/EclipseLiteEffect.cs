using RoR2;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Eclipse Lite (DLC3 BarrierOnCooldown) plays a full-body flash effect every time any skill
    /// comes off cooldown. In VR that flash causes graphical problems, so the effect is never
    /// spawned locally. The barrier itself, and the normal barrier bubble, are untouched, and the
    /// effect is still transmitted to other players so they see it as before.
    /// </summary>
    internal static class EclipseLiteEffect
    {
        internal static void Init()
        {
            On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool += (orig, effectIndex, effectData, transmit) =>
            {
                if (IsEclipseLiteProc(effectIndex))
                {
                    if (transmit) EffectManager.TransmitEffect(effectIndex, effectData);
                    return;
                }
                orig(effectIndex, effectData, transmit);
            };
        }

        private static EffectIndex cachedIndex = EffectIndex.Invalid;

        private static bool IsEclipseLiteProc(EffectIndex index)
        {
            if (index == EffectIndex.Invalid) return false;
            if (cachedIndex == EffectIndex.Invalid)
            {
                GameObject prefab = CharacterBody.CommonAssets.barrierOnCooldownPrefab;
                if (!prefab) return false;
                cachedIndex = EffectCatalog.FindEffectIndexFromPrefab(prefab);
                if (cachedIndex == EffectIndex.Invalid) return false;
                VRMod.StaticLogger.LogInfo("[VR effects] Eclipse Lite proc flash is disabled in VR.");
            }
            return index == cachedIndex;
        }
    }
}
