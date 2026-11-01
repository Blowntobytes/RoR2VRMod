using RoR2;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// Enforcer's shield dash (the boost that rams enemies) goes the way you MOVE, not where the aiming
    /// hand points (user request).
    /// 2.26.51 hooked ShieldDashOverlapHeavy, but the log showed the dash actually used is
    /// BaseShieldDashPrepCharge -> ProxyShieldDash (Light/Heavy). Those pick the direction from the
    /// AIM: the prep state turns the character to GetAimRay().direction on enter and keeps aim mode on,
    /// and ProxyShieldDash reads characterDirection.forward and inputBank.aimDirection on enter. In VR the
    /// aim is the aiming hand. 2.26.52: while the local VR Enforcer's body state is one of those dash
    /// states, GetAimRay's direction and inputBank.aimDirection report the movement reference instead -
    /// the head, or the non-dominant controller when "Use controller direction for movement" is on (the
    /// same source the left stick uses). Outside the dash, aim is untouched. Steering during the dash
    /// still uses the left stick as in vanilla.
    /// </summary>
    internal static class ShieldDashDirection
    {
        private static bool logged;
        private static CharacterBody cachedBody;
        private static EntityStateMachine cachedBodyMachine;

        internal static void Init()
        {
            On.EntityStates.BaseState.GetAimRay += (orig, self) =>
            {
                Ray ray = orig(self);
                if (self.characterBody && self.isAuthority && InDash(self.characterBody))
                {
                    Vector3 dir = MoveForward();
                    if (dir.sqrMagnitude > 0.0001f) { Log(); return new Ray(ray.origin, dir); }
                }
                return ray;
            };
            // inputBank.aimDirection has no hookable getter in this build: overwrite it after the
            // player controller's Update (LateUpdate) while the dash states run, so the states' next
            // FixedUpdate (aim mode turns the character toward aimDirection) sees the movement direction.
            var go = new GameObject("VRMod ShieldDashDirection");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<LateWriter>();
        }

        private class LateWriter : MonoBehaviour
        {
            private void LateUpdate()
            {
                CharacterBody body = MotionControls.currentBody;
                if (!body || !body.inputBank || !InDash(body)) return;
                Vector3 dir = MoveForward();
                if (dir.sqrMagnitude < 0.0001f) return;
                body.inputBank.aimDirection = dir;
                Log();
            }
        }

        private static bool InDash(CharacterBody body)
        {
            if (body != MotionControls.currentBody || !body.hasEffectiveAuthority) return false;
            if (cachedBody != body)
            {
                cachedBody = body;
                cachedBodyMachine = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
            }
            if (!cachedBodyMachine || cachedBodyMachine.state == null) return false;
            object st = cachedBodyMachine.state;
            if (st is EntityStates.Enforcer.BaseShieldDashPrepCharge) return true;
            string n = st.GetType().Name;
            return n.StartsWith("ProxyShieldDash") || n.StartsWith("ShieldDashOverlap");
        }

        private static void Log()
        {
            if (logged) return;
            logged = true;
            VRMod.StaticLogger.LogInfo("[VR enforcer] Shield dash follows the movement direction (" + (ModConfig.ControllerMovementDirection.Value && MotionControls.HandsReady ? "controller" : "head") + ").");
        }

        private static Vector3 MoveForward()
        {
            if (ModConfig.ControllerMovementDirection.Value && MotionControls.HandsReady)
            {
                HandController hand = MotionControls.GetHandByDominance(false);
                if (hand && hand.muzzle) return hand.muzzle.transform.forward;
            }
            CameraRigController rig = Utils.localCameraRig;
            if (rig && rig.sceneCam) return rig.sceneCam.transform.forward;
            return Vector3.zero;
        }
    }
}
