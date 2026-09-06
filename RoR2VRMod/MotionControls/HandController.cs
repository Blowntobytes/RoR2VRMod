using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRMod
{
    public class HandController : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer ray;

        [SerializeField]
        private Hand pointerHand;

        [SerializeField]
        internal HandHUD smallHud;

        [SerializeField]
        internal HandHUD watchHud;

        public Animator animator => currentHand.animator;

        public Ray aimRay
        {
            get
            {
                return new Ray(muzzle.position, muzzle.forward);
            }
        }

        public CharacterModel.RendererInfo[] rendererInfos => currentHand.rendererInfos;

        internal Ray uiRay
        {
            get
            {
                return new Ray(uiHand.currentMuzzle.transform.position, uiHand.currentMuzzle.transform.forward);
            }
        }

        public Transform muzzle => currentHand.currentMuzzle.transform;

        internal Hand currentHand { get; private set; }

        internal XRNode xrNode;

        internal HandController oppositeHand;

        internal bool hasAimableEquipment;

        internal bool hasAimableHeresySkill;

        internal bool stabilisePosition;

        private Hand uiHand;

        private bool _uiMode;

        private Vector3 lastPosition;

        private Quaternion lastRotation;

        // In UI mode the ray is never shown (menus are gamepad-driven, no laser pointer).
        private bool rayActive => uiMode ? false : (currentHand.useRay || hasAimableHeresySkill || (hasAimableEquipment && MotionControls.currentBody?.equipmentSlot?.stock > 0));

        internal bool uiMode
        {
            get { return _uiMode; }
            set
            {
                if (_uiMode == value) return;

                _uiMode = value;

                VRMod.StaticLogger.LogInfo($"[VR input] {(xrNode == XRNode.LeftHand ? "Left" : "Right")} hand UI-pointer mode = {_uiMode} (paused={RoR2.PauseManager.isPaused}).");

                if (_uiMode)
                {
                    // Menus are gamepad-driven now: hide the gameplay hand while in UI, and do NOT
                    // show the UI pointer hand or its ray (the pointer model is what kept spawning
                    // far away, and point-and-click is no longer used).
                    ray.gameObject.layer = LayerIndex.ui.intVal;
                    ray.sortingOrder = 999;
                    Utils.SetLayerRecursive(currentHand.gameObject, LayerIndex.noDraw.intVal);
                    uiHand.gameObject.SetActive(false);
                    ray.gameObject.SetActive(false);
                }
                else
                {
                    ray.gameObject.layer = 0;
                    ray.sortingOrder = 0;
                    Utils.SetLayerRecursive(currentHand.gameObject, 0);
                    uiHand.gameObject.SetActive(false);
                    UpdateRayColor();
                }
            }
        }

        private List<GameObject> handPrefabs;

        private InputDevice inputDevice;

        /// <summary>
        /// Corrective rotation that reconciles the OpenXR grip pose (which the TrackedPoseDriver
        /// feeds this object) with the orientation the SteamVR-era hand models were authored for.
        /// Without it the hands/aim point ~90 degrees up. Applied in the tracked (parent) frame so
        /// it rotates the model and its muzzle together, keeping the aim ray aligned with the
        /// visible hand. Tunable via ModConfig.ControllerAngleOffset.
        /// </summary>
        internal static Quaternion PoseCorrection => Quaternion.Euler(ModConfig.ControllerAngleOffset.Value, 0f, 0f);

        private void Awake()
        {
            SetCurrentHand(pointerHand);

            uiHand = Object.Instantiate(pointerHand.gameObject).GetComponent<Hand>();
            uiHand.gameObject.SetLayerRecursive(LayerIndex.ui.intVal);
            uiHand.useRay = false;
            uiHand.gameObject.SetActive(false);

            ray.material.color = ModConfig.RayColor;

            lastPosition = Vector3.zero;
            lastRotation = Quaternion.identity;
        }

        private void OnEnable()
        {
            if (uiMode && uiHand && !uiHand.isActiveAndEnabled)
                uiHand.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (uiHand && uiHand.isActiveAndEnabled)
                uiHand.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (uiHand)
                uiHand.gameObject.name = string.Format("UI Hand ({0})", xrNode == XRNode.LeftHand ? "Left" : "Right");
        }

        private void OnDestroy()
        {
            if(uiHand)
                Destroy(uiHand.gameObject);
        }

        private void Update()
        {
            uiMode = Utils.isUsingUI;

            // The camera's parent lives inside the VR rig, which is SCALED to the character. The
            // pose driver writes this hand's LOCAL transform every frame, so the hand must be placed
            // into that parent in local space (worldPositionStays=false). A world-preserving SetParent
            // multiplied a stale world offset by the rig's scale/rotation, which is what put the hands
            // far behind you / in the sky after a recenter or menu change.
            if (transform.parent != Camera.main.transform.parent)
                transform.SetParent(Camera.main.transform.parent, false);

            // HandControllers are spawned with no parent, so local == world here. Apply the same
            // pose correction as the gameplay hand so the UI pointer ray also points forward.
            uiHand.transform.position = transform.position;
            uiHand.transform.rotation = transform.rotation * PoseCorrection;
        }

        private void LateUpdate()
        {
            if (!currentHand) return;

            bool active = rayActive;

            if (ray.gameObject.activeSelf != active)
                ray.gameObject.SetActive(active);

            if (ray.gameObject.activeSelf)
            {
                ray.transform.position = (uiMode ? uiHand.currentMuzzle.transform : muzzle).position;
                ray.SetPosition(1, ray.transform.InverseTransformPoint(GetRayHitPosition()));
            }
        }

        /// <summary>
        /// Returns the muzzle transform at the specified index.
        /// </summary>
        /// <param name="index">The index of the muzzle.</param>
        /// <returns>The chosen muzzle's transform.</returns>
        public Transform GetMuzzleByIndex(uint index)
        {
            return currentHand.muzzles[index].transform;
        }

        internal void SetCurrentHand(string bodyName)
        {
            foreach (GameObject handPrefab in handPrefabs)
            {
                Hand hand = handPrefab.GetComponent<Hand>();

                if (!hand)
                    continue;

                if (hand.bodyName == bodyName)
                {
                    Hand newHand = Instantiate(handPrefab, transform).GetComponent<Hand>();
                    SetCurrentHand(newHand);

                    return;
                }
            }

            // 2.20.0 EXPERIMENTAL: no authored hand prefab for this body - try building one at
            // runtime from the character's own arm mesh. Fully additive: on any failure the old
            // behavior (default pointer + warning) is kept.
            if (ModConfig.RuntimeHands.Value)
            {
                Hand runtimeHand = RuntimeHands.TryBuildHand(this, bodyName);

                if (runtimeHand)
                {
                    SetCurrentHand(runtimeHand);
                    return;
                }

                // The model's meshes may simply not be loaded yet (skins apply async) - retry
                // for a few seconds before settling for the default pointer.
                RuntimeHands.ScheduleRetry(this, bodyName);
            }

            VRMod.StaticLogger.LogWarning($"Could not find hand with name \'{bodyName}\'. This character is likely not VR supported and some abilities might not work as intended. Using default pointer.");
        }

        internal void SetPrefabs(List<GameObject> prefabs)
        {
            if (prefabs == null)
                return;

            handPrefabs = prefabs;
        }

        private void SetCurrentHand(Hand hand)
        {
            if (currentHand)
                Destroy(currentHand.gameObject);

            currentHand = hand;
            currentHand.gameObject.SetActive(true);

            // Reorient the model within the tracked hand so it (and its muzzle) points forward
            // instead of ~90 degrees up under OpenXR.
            currentHand.transform.localRotation = PoseCorrection * currentHand.transform.localRotation;

            ray.gameObject.SetActive(rayActive);
        }

        private Vector3 GetRayHitPosition()
        {
            if (!currentHand)
                return Vector3.zero;

            Ray ray = uiMode ? uiRay : aimRay;

            LayerMask mask = uiMode ? LayerIndex.ui.mask : LayerIndex.ragdoll.mask;

            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, 300, mask))
            {
                return hitInfo.point;
            }

            return ray.origin + (ray.direction * 300);
        }

        internal void UpdateRayColor()
        {
            ray.material.color = ModConfig.RayColor;
        }
    }
}
