using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2.ContentManagement;
using System;
using System.Reflection;

namespace VRMod
{
    /// <summary>
    /// Keeps a modded game network-compatible with vanilla players (Oct 2026 "Hallowed Concepts" build).
    ///
    /// Since that build, AddressablesLoadHelper.CanLoadType loads EVERY asset type of a content pack when
    /// RoR2Application.isModded is true, but only the pack's loadOnlyExplicitTypes when it is false. The
    /// "RoR2.Junk" pack (cut content) lists items / equipment / buffs / elites only, so vanilla never loads
    /// its character bodies, skills, projectiles and so on, while any BepInEx game loads all of them. The
    /// body and skill catalogs then differ from vanilla, and every BodyIndex and Loadout sent over the
    /// network is read against the wrong tables: vanilla clients see the modded player under the Ready
    /// button, cannot leave the pod, and both sides log "NetworkReader:ReadByte out of range" in
    /// Loadout.Deserialize. Nothing in this mod uses the junk content, so load exactly what vanilla loads.
    ///
    /// Done by rewriting the call sites inside the load coroutine rather than hooking CanLoadType itself,
    /// because CanLoadType is marked AggressiveInlining and a detour on it could be bypassed by the JIT.
    /// </summary>
    internal static class VanillaContentCompat
    {
        private static ILHook hook;

        internal static void Init()
        {
            try
            {
                Type helper = typeof(AddressablesLoadHelper);
                MethodInfo moveNext = null;
                foreach (Type nested in helper.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (!nested.Name.Contains("AddContentPackLoadOperationWithYields")) continue;
                    moveNext = nested.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (moveNext != null) break;
                }
                if (moveNext == null)
                {
                    VRMod.StaticLogger.LogWarning("[VR content] AddContentPackLoadOperationWithYields coroutine not found - vanilla players may not be able to play with you.");
                    return;
                }
                hook = new ILHook(moveNext, RewriteCanLoadTypeCalls);
            }
            catch (Exception e)
            {
                VRMod.StaticLogger.LogWarning("[VR content] Could not install the vanilla content-load fix: " + e.Message);
            }
        }

        private static void RewriteCanLoadTypeCalls(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            MethodInfo replacement = typeof(VanillaContentCompat).GetMethod(nameof(CanLoadTypeLikeVanilla), BindingFlags.NonPublic | BindingFlags.Static);
            int count = 0;
            while (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<AddressablesLoadHelper>("CanLoadType")))
            {
                c.Remove();
                c.Emit(OpCodes.Call, replacement);
                count++;
            }
            VRMod.StaticLogger.LogInfo($"[VR content] Content packs will load exactly what vanilla loads ({count} load check(s) redirected) so vanilla players stay compatible.");
        }

        // Same signature shape as the instance call it replaces: (this helper, type, pack).
        private static bool CanLoadTypeLikeVanilla(AddressablesLoadHelper helper, string type, ContentPack contentPack)
        {
            if (contentPack != null && contentPack.loadOnlyExplicitTypes != null && contentPack.loadOnlyExplicitTypes.Count >= 1)
                return contentPack.loadOnlyExplicitTypes.Contains(type);
            return true;
        }
    }
}
