using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;

namespace VRMod
{
    /// <summary>
    /// Keeps the game's scene camera ("parent camera") using the projection matrices that the
    /// XR system computed for the headset.
    ///
    /// RoR2 writes Camera.fieldOfView / projection every frame (FOV settings, match camera, sprint
    /// FOV, etc.), which overrides the per-eye matrices OpenXR hands to the camera and produces a
    /// stretched / mis-converged image. To work around that, a hidden child camera is kept alive
    /// and driven by the XR system; because it renders nothing, nothing in the game touches it, so
    /// it always holds the correct stereo projection. Right before the parent camera culls/renders
    /// each eye, its projection is copied from the child:
    ///
    ///     ParentCamera.projectionMatrix = _childCamera.GetStereoProjectionMatrix(eye);
    ///
    /// The child camera MUST stay enabled. The XR subsystem only updates stereo matrices for
    /// cameras that are active and rendering; a disabled camera keeps whatever matrices it had
    /// when it was last enabled (usually the mono fallback), which is why an earlier version of
    /// this component that toggled the reference camera on/off inside OnPreCull did not fix the
    /// image on newer game builds.
    /// </summary>
    internal class StereoProjectionFix : MonoBehaviour
    {
        private Camera ParentCamera;
        private Camera _childCamera;
        private GameObject _childCameraObject;

        private void Awake()
        {
            ParentCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            CreateChildCamera();
        }

        private void OnEnable()
        {
            if (_childCamera)
                _childCamera.enabled = true;
        }

        private void OnDisable()
        {
            if (_childCamera)
                _childCamera.enabled = false;

            if (ParentCamera)
                ParentCamera.ResetProjectionMatrix();
        }

        private void CreateChildCamera()
        {
            if (_childCameraObject)
                return;

            _childCameraObject = new GameObject("VR Stereo Reference Camera");
            _childCameraObject.transform.SetParent(transform, false);
            _childCameraObject.transform.localPosition = Vector3.zero;
            _childCameraObject.transform.localRotation = Quaternion.identity;
            _childCameraObject.transform.localScale = Vector3.one;

            _childCamera = _childCameraObject.AddComponent<Camera>();
            _childCamera.CopyFrom(ParentCamera);

            // Render nothing, as cheaply as possible, but stay enabled so the XR subsystem keeps
            // feeding this camera the headset's per-eye projection matrices.
            _childCamera.cullingMask = 0;
            _childCamera.clearFlags = CameraClearFlags.Nothing;
            _childCamera.depth = -100;
            _childCamera.allowHDR = false;
            _childCamera.allowMSAA = false;
            _childCamera.useOcclusionCulling = false;
            _childCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            _childCamera.targetTexture = null;
            _childCamera.enabled = true;

            // Same pose as the HMD so near/far, aspect and eye offsets line up with the parent.
            TrackedPoseDriver poseDriver = _childCameraObject.AddComponent<TrackedPoseDriver>();
            poseDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Head);
            poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        }

        private void LateUpdate()
        {
            // Keep clip planes in sync with whatever the game set on the parent so the matrices
            // we copy back use the game's near/far values.
            if (!_childCamera || !ParentCamera)
                return;

            if (_childCamera.nearClipPlane != ParentCamera.nearClipPlane)
                _childCamera.nearClipPlane = ParentCamera.nearClipPlane;

            if (_childCamera.farClipPlane != ParentCamera.farClipPlane)
                _childCamera.farClipPlane = ParentCamera.farClipPlane;
        }

        private void OnPreCull()
        {
            ApplyStereoProjection();
        }

        private void OnPreRender()
        {
            ApplyStereoProjection();
        }

        private void ApplyStereoProjection()
        {
            if (!ParentCamera || !_childCamera || !XRSettings.enabled || !XRSettings.isDeviceActive)
                return;

            // In multi-pass the scene camera renders twice per frame; stereoActiveEye tells us which
            // eye this pass is for. Mono (e.g. spectator/screenshot passes) keeps the game's matrix.
            Camera.StereoscopicEye eye;
            switch (ParentCamera.stereoActiveEye)
            {
                case Camera.MonoOrStereoscopicEye.Left:
                    eye = Camera.StereoscopicEye.Left;
                    break;
                case Camera.MonoOrStereoscopicEye.Right:
                    eye = Camera.StereoscopicEye.Right;
                    break;
                default:
                    return;
            }

            // Explicitly set the projection matrix from the VR system (user-reported fix).
            ParentCamera.projectionMatrix = _childCamera.GetStereoProjectionMatrix(eye);

            // Also push both eyes through the stereo API so single-pass / other consumers that read
            // the stereo matrices (post-processing, LIV, UI raycasts) see the same values.
            ParentCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, _childCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left));
            ParentCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, _childCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right));
        }

        private void OnDestroy()
        {
            if (ParentCamera)
                ParentCamera.ResetProjectionMatrix();

            if (_childCameraObject)
                Destroy(_childCameraObject);
        }
    }
}
