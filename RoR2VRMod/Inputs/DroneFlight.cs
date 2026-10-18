using Rewired;
using RoR2;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRMod.Inputs
{
    /// <summary>
    /// Remote-operated drone (a dead player in multiplayer flies a drone until the stage ends).
    /// The game steers fliers with the sticks only: forward follows the camera pitch, nothing is mapped
    /// to pure up / down. Here, while the local player is flying a non-survivor body:
    ///   left stick = along the head's gaze (forward/back) and level strafe (left/right),
    ///   right grip = climb, left grip  = descend (analog squeeze where the controller reports one),
    ///   written over the game's move vector after PlayerCharacterMasterController.Update ran.
    /// The grips' Rewired elements (10 / 11, Utility / Special) are held off meanwhile so squeezing
    /// does not also fire a drone skill.
    /// </summary>
    internal static class DroneFlight
    {
        private const int LeftGripElement = 10, RightGripElement = 11, SprintElement = 13;
        private const float Deadzone = 0.15f;

        private static bool active;
        private static float climb, descend;
        private static Vector2 stick;

        internal static bool Active => active;

        internal static void Init()
        {
            On.RoR2.PlayerCharacterMasterController.Update += (orig, self) =>
            {
                orig(self);
                if (!active || !self.hasEffectiveAuthority) return;
                InputBankTest inputs = self.bodyInputs;
                if (!inputs) return;

                // 2.23.51: the game steers fliers along the CAMERA RIG's forward, which in VR is not
                // where you look (stick forward sent the drone downwards). Rebuild the whole move
                // vector here: left stick forward/back = along the head's gaze (including pitch),
                // left/right = level strafe, grips = straight up/down.
                Vector3 move;
                if (TryGetHeadForward(out Vector3 forward, out Vector3 flatRight))
                    move = forward * stick.y + flatRight * stick.x;
                else
                    move = inputs.moveVector;
                move += Vector3.up * (climb - descend);
                if (move.sqrMagnitude > 1f) move.Normalize();
                inputs.moveVector = move;
            };
        }

        /// <summary>Called once per input tick, before the normal inputs are written.</summary>
        internal static void Poll()
        {
            bool wasActive = active;
            active = false;
            climb = descend = 0f;
            stick = Vector2.zero;

            LocalUser user = LocalUserManager.GetFirstLocalUser();
            PlayerCharacterMasterController controller = user != null ? user.cachedMasterController : null;
            CharacterBody body = user != null ? user.cachedBody : null;
            if (!controller || !body || !controller.bodyIsFlier) return;
            // Survivors never fly in vanilla; a flying body under player control is a remote-op drone.
            if (SurvivorCatalog.GetSurvivorIndexFromBodyIndex(body.bodyIndex) != SurvivorIndex.None) return;

            active = true;
            climb = GripAmount(XRNode.RightHand);
            stick = StickAmount(XRNode.LeftHand);
            descend = GripAmount(XRNode.LeftHand);
            if (!wasActive) { logged = false; VRMod.StaticLogger.LogInfo($"[VR drone] Flying '{body.name}': right grip = up, left grip = down (grips do not fire skills while flying)."); }
        }

        internal static void Apply(CustomController vr)
        {
            if (!active) return;
            vr.SetButtonValueById(LeftGripElement, false);
            vr.SetButtonValueById(RightGripElement, false);
            // 2.24.27: the game's PlayerFlyState sets the move vector's Y to -1 (straight down) whenever
            // Sprint is held - auto-sprint kept it held, so stick-forward always dived. No sprint while flying.
            vr.SetButtonValueById(SprintElement, false);
        }

        /// <summary>
        /// Gaze direction in world space from the HMD pose: the tracked eye camera's transform. The
        /// game's Camera.main / rig transform is NOT the head in VR (the rig carries the game's own
        /// pitch, which is why stick-forward used to push the drone into the ground).
        /// </summary>
        private static bool TryGetHeadForward(out Vector3 forward, out Vector3 flatRight)
        {
            forward = Vector3.forward; flatRight = Vector3.right;
            Transform head = null;
            try
            {
                CameraRigController rig = CameraRigController.readOnlyInstancesList.Count > 0 ? CameraRigController.readOnlyInstancesList[0] : null;
                if (rig && rig.sceneCam) head = rig.sceneCam.transform;   // the eye camera is tracked to the HMD
            }
            catch { }
            if (!head && Camera.main) head = Camera.main.transform;
            if (!head) return false;
            forward = head.forward;
            flatRight = Vector3.ProjectOnPlane(head.right, Vector3.up);
            if (flatRight.sqrMagnitude < 1e-4f) flatRight = Vector3.Cross(Vector3.up, forward);
            flatRight.Normalize();
            if (!logged) { logged = true; VRMod.StaticLogger.LogInfo($"[VR drone] steering from '{head.name}' (pitch {Vector3.SignedAngle(Vector3.ProjectOnPlane(forward, Vector3.up), forward, head.right):F0}°)."); }
            return true;
        }
        private static bool logged;

        private static Vector2 StickAmount(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid || !device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v)) return Vector2.zero;
            float m = v.magnitude;
            if (m <= Deadzone) return Vector2.zero;
            return v / m * Mathf.Clamp01((m - Deadzone) / (1f - Deadzone));
        }

        private static float GripAmount(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return 0f;
            if (device.TryGetFeatureValue(CommonUsages.grip, out float amount) && amount > Deadzone)
                return Mathf.Clamp01((amount - Deadzone) / (1f - Deadzone));
            if (device.IsPressed(InputHelpers.Button.GripButton, out bool pressed) && pressed) return 1f;
            return 0f;
        }
    }
}
