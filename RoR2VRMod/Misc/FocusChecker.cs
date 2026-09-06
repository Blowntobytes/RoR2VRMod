using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// Shows a small warning in the headset when the game window on the monitor is not the
    /// focused window. Keyboard/mouse input goes to whatever window IS focused, so this tells
    /// the player why nothing responds. The warning only appears once the window has been
    /// unfocused for a short while (so alt-tabbing through, or a runtime overlay grabbing
    /// focus for a moment, does not flash it) and it can be turned off in the config.
    /// </summary>
    internal class FocusChecker : MonoBehaviour
    {
        internal static FocusChecker instance;

        private const float ShowDelaySeconds = 2f;

        private Canvas focusCanvas;
        private float unfocusedSince = -1f;
        private bool focused = true;

        internal static void Init()
        {
            RoR2.RoR2Application.onLoad += () =>
            {
                RoR2.RoR2Application.instance.gameObject.AddComponent<FocusChecker>();
            };
        }

        private void Awake()
        {
            instance = this;
            focused = Application.isFocused;
            if (!focused) unfocusedSince = Time.unscaledTime;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (hasFocus)
            {
                unfocusedSince = -1f;
                if (focusCanvas) focusCanvas.gameObject.SetActive(false);
            }
            else
            {
                unfocusedSince = Time.unscaledTime;
            }
        }

        private void Update()
        {
            if (!ModConfig.ShowUnfocusedWarning.Value)
            {
                if (focusCanvas && focusCanvas.gameObject.activeSelf) focusCanvas.gameObject.SetActive(false);
                return;
            }

            if (focused || unfocusedSince < 0f) return;
            if (Time.unscaledTime - unfocusedSince < ShowDelaySeconds) return;

            if (!focusCanvas)
            {
                try { CreateCanvas(); }
                catch (System.Exception e)
                {
                    VRMod.StaticLogger.LogWarning($"[VR focus] Could not build the unfocused warning: {e.Message}");
                    unfocusedSince = -1f; // don't retry every frame
                    return;
                }
            }

            if (focusCanvas && !focusCanvas.gameObject.activeSelf)
                focusCanvas.gameObject.SetActive(true);
        }

        private void CreateCanvas()
        {
            GameObject canvasObject = new GameObject("VRFocusWarningCanvas");
            GameObject.DontDestroyOnLoad(canvasObject);
            canvasObject.layer = RoR2.LayerIndex.ui.intVal;

            focusCanvas = canvasObject.AddComponent<Canvas>();
            focusCanvas.renderMode = RenderMode.WorldSpace;
            focusCanvas.sortingOrder = 30000;

            RectTransform canvasRect = canvasObject.transform as RectTransform;
            canvasRect.sizeDelta = new Vector2(1200, 220);
            canvasRect.localScale = Vector3.one * 0.003f;

            // Dark panel with a warm outline so it reads against any scene.
            GameObject panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            panelObject.layer = canvasObject.layer;
            Image panel = panelObject.AddComponent<Image>();
            panel.color = new Color(0.05f, 0.05f, 0.07f, 0.85f);
            Stretch(panel.rectTransform, Vector2.zero, Vector2.zero);

            GameObject outlineObject = new GameObject("Outline");
            outlineObject.transform.SetParent(panelObject.transform, false);
            outlineObject.layer = canvasObject.layer;
            Image outlineImage = outlineObject.AddComponent<Image>();
            outlineImage.color = new Color(1f, 1f, 1f, 0f);
            Outline outline = outlineObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.4f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            Stretch(outlineImage.rectTransform, Vector2.zero, Vector2.zero);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(panelObject.transform, false);
            textObject.layer = canvasObject.layer;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = "<b>GAME WINDOW NOT FOCUSED</b>\nClick the Risk of Rain 2 window on your monitor to get control back.";
            text.fontSize = 44;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.92f, 0.75f, 1f);
            text.enableWordWrapping = true;
            Stretch(text.rectTransform, new Vector2(30, 20), new Vector2(-30, -20));

            // Borrow the game's own font so the text renders with the same look as the HUD.
            TMP_Text donor = null;
            if (RoR2.RoR2Application.instance && RoR2.RoR2Application.instance.mainCanvas)
                donor = RoR2.RoR2Application.instance.mainCanvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font);
            if (donor == null)
                donor = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.font && t.gameObject.scene.name != null);
            if (donor != null)
            {
                text.font = donor.font;
                if (donor.fontSharedMaterial) text.fontSharedMaterial = donor.fontSharedMaterial;
            }

            follower = canvasObject.AddComponent<HeadFollower>();
            UpdateCameraRig(RoR2.CameraRigController.instancesList.FirstOrDefault());
            VRMod.StaticLogger.LogInfo("[VR focus] Unfocused warning shown (game window lost focus).");
        }

        private HeadFollower follower;

        /// <summary>
        /// Keeps the warning a short distance in front of the view and pitched DOWN, so it
        /// never sits on top of whatever the player is looking at (the intro's typed text,
        /// menus, the HUD all live straight ahead at 12.35 units).
        /// </summary>
        private class HeadFollower : MonoBehaviour
        {
            internal Transform head;
            private const float Distance = 6f;
            private const float PitchDown = 25f;

            private void LateUpdate()
            {
                if (!head) return;
                Quaternion look = Quaternion.LookRotation(head.forward, Vector3.up);
                Quaternion pitched = look * Quaternion.Euler(PitchDown, 0f, 0f);
                transform.position = head.position + pitched * Vector3.forward * Distance;
                transform.rotation = pitched;
            }
        }

        private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        internal void UpdateCameraRig(RoR2.CameraRigController rig)
        {
            if (!rig || !focusCanvas || !rig.uiCam) return;

            focusCanvas.worldCamera = rig.uiCam;
            if (follower) follower.head = rig.uiCam.transform;
        }
    }
}
