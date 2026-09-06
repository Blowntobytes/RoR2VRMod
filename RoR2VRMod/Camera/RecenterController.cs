using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace VRMod
{
    internal static class RecenterController
    {
        private static GameObject instance;
        public static void Init()
        {
            instance = new GameObject("VRManager");

            instance.AddComponent<MPEventSystemProvider>().fallBackToMainEventSystem = true;

            InputResponse inputResponse = instance.AddComponent<InputResponse>();
            inputResponse.inputActionNames = new string[] { "RecenterHMD" };
            inputResponse.onPress = new UnityEvent();
            inputResponse.onPress.AddListener(Recenter);

            Object.DontDestroyOnLoad(instance);
        }

        private static void Recenter()
        {
            RecenterNow();
        }

        /// <summary>
        /// Recenters the headset. Works in every menu (including the title screen) and in-run,
        /// paused or not. Called both from the Rewired "RecenterHMD" action and directly from the
        /// controller poll (see Controllers.UpdateVRInputs), so it does not depend on the Rewired
        /// binding path being wired up correctly.
        /// </summary>
        private static bool loggedRecenter;

        internal static void RecenterNow()
        {
            List<XRInputSubsystem> xrSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(xrSubsystems);

            int recentered = 0;
            foreach (XRInputSubsystem xrSubsystem in xrSubsystems)
            {
                if (xrSubsystem.TryRecenter())
                    recentered++;
            }

            // Manual yaw recenter (menus only). See TryManualYawRecenter for why the previous
            // attempts kept rotating instead of snapping to forward.
            bool manual = false;
            float yaw = 0f;
            if (!RoR2.Run.instance)
                manual = TryManualYawRecenter(out yaw);

            if (!loggedRecenter)
            {
                loggedRecenter = true;
                VRMod.StaticLogger.LogInfo($"[VR input] Recenter triggered. {xrSubsystems.Count} input subsystem(s), {recentered} accepted TryRecenter(); manual menu recenter applied={manual} (offset now {yaw:F1} deg). (Only logged once.)");
            }
        }

        /// <summary>
        /// The accumulated yaw offset we have applied to the camera's parent. We track it ourselves
        /// because rotating the parent does NOT change the camera's local yaw (head tracking rewrites
        /// the local rotation every frame), so reading the camera's local yaw back was always the same
        /// value - which is why each click re-applied the same offset and the view kept spinning.
        /// </summary>
        private static float appliedOffsetDeg;
        private static Transform offsetTarget;
        private static Quaternion offsetTargetBaseRotation;

        private static bool TryManualYawRecenter(out float newOffsetDeg)
        {
            newOffsetDeg = appliedOffsetDeg;

            Camera cam = Camera.main;
            if (!cam || !cam.transform.parent)
                return false;

            Transform parent = cam.transform.parent;

            // Re-base if the camera parent changed (scene change etc.): remember its untouched rotation.
            if (offsetTarget != parent)
            {
                offsetTarget = parent;
                offsetTargetBaseRotation = parent.rotation;
                appliedOffsetDeg = 0f;
            }

            // Head yaw relative to the parent, normalized to [-180, 180]. This is the raw tracked
            // value; it is independent of whatever offset we already applied to the parent.
            float headYaw = cam.transform.localEulerAngles.y;
            if (headYaw > 180f) headYaw -= 360f;

            // The parent must end up rotated by exactly -headYaw relative to its BASE rotation so the
            // current head direction becomes forward. Setting it absolutely (base * offset) rather than
            // multiplying onto the current rotation is what stops it accumulating click after click.
            appliedOffsetDeg = -headYaw;
            parent.rotation = Quaternion.AngleAxis(appliedOffsetDeg, Vector3.up) * offsetTargetBaseRotation;

            // Note: the motion-control hands share this parent and their pose driver writes their
            // LOCAL transform every frame, so rotating the parent correctly turns head and hands
            // together. Nothing else to do here.

            newOffsetDeg = appliedOffsetDeg;
            return true;
        }
    }
}
