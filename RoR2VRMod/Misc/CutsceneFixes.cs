using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace VRMod
{
    internal static class CutsceneFixes
    {
        private static Transform shipShot;

        internal static void Init()
        {
            On.RoR2.StartEvent.Start += SetupIntro;

            On.RoR2.OutroCutsceneController.OnEnable += SetupOutro;
        }

        private static void SetupOutro(On.RoR2.OutroCutsceneController.orig_OnEnable orig, OutroCutsceneController self)
        {
            GameObject cameraRoot = GameObject.Find("CutsceneEnabledObjects");

            if (cameraRoot)
            {
                Transform cameraTransform = cameraRoot.transform.Find("Camera Matcher/Menu Main Camera");

                if (cameraTransform)
                {
                    CameraRigController cameraRig = cameraTransform.GetComponent<CameraRigController>();

                    GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

                    FixOutroCanvas(allGameObjects, cameraRig);
                    AdjustOutroElements(allGameObjects, cameraRig);
                }
            }

            orig(self);
        }

        private static void AdjustOutroElements(GameObject[] allGameObjects, CameraRigController cameraRig)
        {
            GameObject spaceShot = allGameObjects.First(x => x.name == "Set 2 - Space");

            if (spaceShot)
            {
                spaceShot.transform.Translate(20, 0, 0);

                Transform dropShipParent = spaceShot.transform.Find("Dropship, Space");

                if (dropShipParent)
                {
                    dropShipParent.localScale = Vector3.one;
                }
            }
        }

        private static void FixOutroCanvas(GameObject[] allGameObjects, CameraRigController cameraRig)
        {
            GameObject subtitlesCanvas = allGameObjects.First(x => x.name == "Set 5 - Canvas");
            
            if (subtitlesCanvas)
            {
                Canvas canvas = subtitlesCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform rectTransform = subtitlesCanvas.transform as RectTransform;
                rectTransform.SetParent(cameraRig.sceneCam.transform);
                rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                
                rectTransform.sizeDelta = new Vector2(6000, 5000);
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localPosition = new Vector3(0, 0, 15f);

                RectTransform textCanvas = subtitlesCanvas.GetComponentInChildren<RoR2.UI.OutroFlavorTextController>().transform as RectTransform;
                textCanvas.anchorMin = new Vector2(0.42f, 0.42f);
                textCanvas.anchorMax = new Vector2(0.58f, 0.58f);
                textCanvas.gameObject.AddComponent<SmoothHUD>().Init(canvas.worldCamera.transform);
            }
        }

        private static bool introSkipPrevPressed;

        private static void SetupIntro(On.RoR2.StartEvent.orig_Start orig, StartEvent self)
        {
            orig(self);

            IntroCutsceneController introController = self.GetComponent<IntroCutsceneController>();

            if (introController != null)
            {
                // Skip the intro with the Y button (left secondary). The game's own skip trigger
                // isn't in an assembly we can rebind, so drive the skip directly off the controller.
                introSkipPrevPressed = false;
                System.Action skipPoll = null;
                skipPoll = () =>
                {
                    if (!introController)
                    {
                        RoR2Application.onUpdate -= skipPoll;
                        return;
                    }

                    UnityEngine.XR.InputDevice leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                    bool pressed = false;
                    if (leftHand.isValid)
                        leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out pressed);

                    if (pressed && !introSkipPrevPressed)
                    {
                        IntroCutsceneController.shouldSkip = true;
                        RoR2Application.onUpdate -= skipPoll;
                    }

                    introSkipPrevPressed = pressed;
                };
                RoR2Application.onUpdate += skipPoll;
            }

            if (introController != null)
            {
                RoR2Application.onNextUpdate += () =>
                {
                    GameObject cameraRigObject = GameObject.Find("Menu Main Camera");

                    if (cameraRigObject)
                    {
                        cameraRigObject.transform.localScale /= 6;

                        CameraRigController cameraRig = cameraRigObject.GetComponent<CameraRigController>();

                        Vector3 offset = new Vector3(-0.3387751f, 0.1797891f, -0.5321155f) * 5 / 6;

                        if (ModConfig.InitialRoomscaleValue) offset.y -= 0.3f;

                        cameraRig.desiredCameraState = new CameraState
                        {
                            position = offset,
                            rotation = Quaternion.Euler(3.899f, -180f, 0)
                        };

                        cameraRig.currentCameraState = cameraRig.desiredCameraState;

                        GameObject.Destroy(cameraRig.sceneCam.GetComponent<MatchCamera>());

                        Transform cameraParent = cameraRig.sceneCam.transform.parent;
                        GameObject newSceneCamObject = GameObject.Instantiate(cameraRig.sceneCam.gameObject, cameraParent.position, cameraParent.rotation, cameraParent);
                        GameObject.Destroy(cameraRig.sceneCam.gameObject);

                        Camera newCam = newSceneCamObject.GetComponent<Camera>();
                        newCam.cullingMask = newCam.cullingMask & ~(1 << LayerIndex.ui.intVal);
                        cameraRig.sceneCam = newCam;

                        // The UI camera is a child of the scene camera, so the clone above carries
                        // its own copy and the original dies with the old scene camera at the end of
                        // this frame. Re-point the rig at the clone's UI camera; otherwise everything
                        // placed relative to cameraRig.uiCam during the intro (the story text, the
                        // unfocused warning) is anchored to a destroyed object and never shows.
                        VRMod.StaticLogger.LogInfo($"[VR intro] scene cam '{newCam.name}' parent='{newCam.transform.parent?.name}' (parent scale {newCam.transform.parent?.lossyScale}); ui cam -> '{(cameraRig.uiCam ? cameraRig.uiCam.name : "null")}' parent='{(cameraRig.uiCam ? cameraRig.uiCam.transform.parent?.name : "-")}'");
                        cameraRig.sprintingParticleSystem = newSceneCamObject.GetComponentInChildren<ParticleSystem>();

                        // The intro camera is driven by a head-tracking pose driver, which overwrites
                        // the camera's own rotation every frame - so rotating the camera state does
                        // nothing. The view's base orientation comes from the camera's PARENT, so
                        // rotate that to turn the player to face the incoming ship.
                        if (cameraParent)
                            cameraParent.rotation = Quaternion.Euler(0f, ModConfig.IntroCameraYaw.Value, 0f) * cameraParent.rotation;

                        // Seated mode uses a device (eye-level) tracking origin, so the head sits
                        // exactly at the rig origin, which is floor level for this shot. Like the yaw
                        // above, the camera state position is not what places the view here - the
                        // tracked cameras are - so lift them by inserting an offset parent. The rig
                        // is 1/6 scale, so the value is in physical metres (as if you stood up).
                        if (!ModConfig.InitialRoomscaleValue && ModConfig.IntroSeatedHeightOffset.Value != 0f)
                        {
                            Vector3 lift = new Vector3(0f, ModConfig.IntroSeatedHeightOffset.Value, 0f);
                            // Scene camera only. The UI camera is a root object of the intro scene in
                            // its own unscaled space (HUD-style canvases follow it), so it needs no lift -
                            // and wrapping it in a holder created while the splash scene is still the
                            // active scene moves it INTO the splash scene, where it is destroyed when
                            // that scene unloads, taking every UI canvas with it.
                            InsertOffsetParent(newCam.transform, lift, "VR Intro Height (scene)");
                            VRMod.StaticLogger.LogInfo($"[VR intro] seated lift {ModConfig.IntroSeatedHeightOffset.Value} m applied (scene cam world y now {newCam.transform.position.y:F3}).");
                        }

                        if (CameraFixes.liv) CameraFixes.liv.HMDCamera = newCam;

                        try { FixIntroCanvas(cameraRig); }
                        catch (System.Exception e) { VRMod.StaticLogger.LogWarning($"FixIntroCanvas: intro canvas layout differs from expected, partial fix applied: {e.Message}"); }

                        try { AdjustIntroElements(); }
                        catch (System.Exception e) { VRMod.StaticLogger.LogWarning($"AdjustIntroElements: intro scene layout differs from expected, partial fix applied: {e.Message}"); }
                    }
                };
            }
        }

        private static void InsertOffsetParent(Transform target, Vector3 localOffset, string name)
        {
            Transform oldParent = target.parent;
            GameObject holder = new GameObject(name);
            // A new GameObject lands in the ACTIVE scene, which can be a different (soon
            // unloaded) scene than the target's; parenting fixes that for a non-root target,
            // and for safety move it explicitly first.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, target.gameObject.scene);
            holder.transform.SetParent(oldParent, false);
            holder.transform.localPosition = localOffset;
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = Vector3.one;
            target.SetParent(holder.transform, false);
        }

        private static void AdjustIntroElements()
        {
            GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            GameObject shipShotObject = allGameObjects.FirstOrDefault(x => x.name == "cutscene intro");
            GameObject shipBackground = allGameObjects.FirstOrDefault(x => x.name == "Set 1 - Space");

            if (shipShotObject && shipBackground)
            {
                shipShot = shipShotObject.transform;
                RoR2Application.onLateUpdate += UpdateShipShot;
                shipBackground.transform.Rotate(Vector3.up, -60);
            }

            GameObject captainShot = allGameObjects.FirstOrDefault(x => x.name == "Set 2 - Cabin");

            if (captainShot)
            {
                Transform cabin = captainShot.transform.Find("CabinPosition");

                if (cabin) cabin.localScale = Vector3.one * 20;
            }

            GameObject spaceShot = allGameObjects.FirstOrDefault(x => x.name == "Set 3 - Space, Small Planet");

            if (spaceShot)
            {
                spaceShot.transform.Rotate(Vector3.up, -60);
            }

            GameObject cargoShot = allGameObjects.FirstOrDefault(x => x.name == "Set 4 - Cargo");

            if (cargoShot)
            {
                Transform pp = cargoShot.transform.Find("PP");

                PostProcessVolume ppVolume = pp ? pp.GetComponent<PostProcessVolume>() : null;
                if (ppVolume && ppVolume.profile && ppVolume.profile.HasSettings<DepthOfField>())
                    ppVolume.profile.RemoveSettings<DepthOfField>();
            }
        }

        private static void UpdateShipShot()
        {
            GameObject shipShotObject = GameObject.Find("cutscene intro");
            if (shipShotObject && shipShotObject.transform != shipShot)
                shipShot = shipShotObject.transform;

            if (shipShot)
            {
                if (shipShot.eulerAngles.y != 210)
                    shipShot.transform.eulerAngles = new Vector3(0, 210, 0);

                if (shipShot.localScale.x <= 1)
                    shipShot.transform.localScale = Vector3.one * 50;
            }
            else
            {
                RoR2Application.onLateUpdate -= UpdateShipShot;
            }
        }

        private static void FixIntroCanvas(CameraRigController cameraRig)
        {
            // The intro's story canvas (typed flavour text, subtitles, skip prompt) is a
            // screen-space overlay named plain "Canvas" in the "intro" scene. Screen-space
            // overlays never render in the headset, so bring it into world space in front of
            // the UI camera. NOTE: the splash scene is still loaded at this point and also has
            // a "Fade" object, which is why a GameObject.Find("Fade") lookup used to grab the
            // wrong canvas (the logo screen) and leave the text invisible in VR.
            Canvas canvas = null;
            foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!c || !c.isRootCanvas || c.gameObject.scene.name != "intro") continue;
                if (c.transform.Find("MainArea") == null) continue;
                canvas = c;
                break;
            }

            if (!canvas)
            {
                VRMod.StaticLogger.LogWarning("FixIntroCanvas: intro story canvas not found; the typed text will only show on the monitor.");
                DumpCanvases("intro (canvas not found)");
                return;
            }

            GameObject canvasObject = canvas.gameObject;
            canvasObject.SetActive(true);

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cameraRig.uiCam;
            RectTransform rectTransform = canvasObject.transform as RectTransform;
            if (rectTransform)
            {
                rectTransform.sizeDelta = new Vector2(1920, 1080);
                rectTransform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.rotation = cameraRig.uiCam.transform.rotation;
                rectTransform.position = cameraRig.uiCam.transform.position + cameraRig.uiCam.transform.forward * 12.35f;
            }
            if (!canvasObject.GetComponent<SmoothHUD>())
                canvasObject.AddComponent<SmoothHUD>().Init(cameraRig.uiCam.transform);

            // Hide the opaque full-screen images only: the fade and the letterbox bars would
            // render as black rectangles floating in front of the player. Text and the skip
            // prompt are left alone.
            int hidden = 0;
            foreach (Image img in canvasObject.GetComponentsInChildren<Image>(true))
            {
                string n = img.gameObject.name;
                bool isKnownPanel = n == "Fade" || n.Contains("BlackBar") || n.Contains("Letterbox");
                bool isSolidFill = img.sprite == null && img.color.a > 0.01f;
                if (isKnownPanel || isSolidFill)
                {
                    img.enabled = false;
                    hidden++;
                }
            }

            VRMod.StaticLogger.LogInfo($"FixIntroCanvas: intro story canvas moved to world space (typed text + subtitles visible in VR), hid {hidden} panel image(s).");

            // The skip prompt only appears once the game sees some input; in VR nobody knows
            // which button that is, so show it from the start with the VR glyph for Y.
            Transform skipGroup = canvasObject.transform.Find("SkipVoteOverlay/CanvasGroup");
            if (skipGroup)
            {
                introSkipGroup = skipGroup;
                RoR2Application.onUpdate += KeepSkipPromptVisible;
            }
        }

        private static Transform introSkipGroup;
        private static TMPro.TMP_SpriteAsset introGlyphSheet;

        private static void KeepSkipPromptVisible()
        {
            if (!introSkipGroup)
            {
                RoR2Application.onUpdate -= KeepSkipPromptVisible;
                return;
            }
            try
            {
                Transform overlay = introSkipGroup.parent;
                if (overlay && !overlay.gameObject.activeSelf) overlay.gameObject.SetActive(true);
                if (!introSkipGroup.gameObject.activeSelf) introSkipGroup.gameObject.SetActive(true);
                CanvasGroup cg = introSkipGroup.GetComponent<CanvasGroup>();
                if (cg && cg.alpha < 1f) cg.alpha = 1f;
                Transform button = introSkipGroup.Find("NakedButton");
                if (button && !button.gameObject.activeSelf) button.gameObject.SetActive(true);

                // The skip is wired entirely through UnityEvents in the scene: the button's onClick
                // and HGGamepadInputEvents (Submit/Cancel = A/B) both call IntroCutsceneController.
                // In VR the mod's own poll handles the skip on Y, so silence the game's handlers
                // across the whole overlay (not just the button - the input events can sit higher
                // up). The button components themselves stay enabled so the HGButton keeps drawing
                // its normal transparent background; only their persistent listeners are turned off.
                Transform overlayRoot = overlay ? overlay : introSkipGroup;
                foreach (Behaviour b in overlayRoot.GetComponentsInChildren<Behaviour>(true))
                {
                    if (!b) continue;
                    string tn = b.GetType().Name;
                    // InputResponse is the one that actually fires the skip: it polls its Rewired
                    // action list (Submit/Cancel, i.e. A/B) every frame and invokes the scene event.
                    if (b.enabled && (tn == "HGGamepadInputEvent" || tn == "InputBindingDisplayController" || tn == "InputResponse"))
                        b.enabled = false;
                    if (b is UnityEngine.UI.Button btn)
                    {
                        UnityEngine.Events.UnityEvent click = btn.onClick;
                        for (int i = 0; i < click.GetPersistentEventCount(); i++)
                            if (click.GetPersistentListenerState(i) != UnityEngine.Events.UnityEventCallState.Off)
                                click.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    }
                }

                // Every glyph label in the overlay shows Y. The VR glyph sprite sheet is normally
                // attached by ControllerGlyphs, which initialises after the intro has started -
                // attach it here directly. Y = the "Pause" glyph slot (LSecondary / LMenu on Vive).
                if (!introGlyphSheet) introGlyphSheet = VRMod.VRAssetBundle.LoadAsset<TMPro.TMP_SpriteAsset>("sprVRGlyphs");
                // Element 24 = Pause (Y press). 18 was Cancel, i.e. the B glyph.
                string yGlyph = ControllerGlyphs.GetGlyph(24);
                if (string.IsNullOrEmpty(yGlyph)) yGlyph = "<sprite name=\"texVRGlyphs_LSecondary\">";
                foreach (TMPro.TMP_Text glyphText in overlayRoot.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (!glyphText || glyphText.gameObject.name.IndexOf("Glyph", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (introGlyphSheet && glyphText.spriteAsset != introGlyphSheet) glyphText.spriteAsset = introGlyphSheet;
                    if (!glyphText.richText) glyphText.richText = true;
                    if (glyphText.text != yGlyph) glyphText.text = yGlyph;
                }
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogWarning($"[VR intro] skip prompt: {e.Message}");
                RoR2Application.onUpdate -= KeepSkipPromptVisible;
            }
        }

        internal static void DumpCanvases(string context)
        {
            try
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"[VR ui-dump] {context}: xrActive={UnityEngine.XR.XRSettings.isDeviceActive} mainCam={(Camera.main ? Camera.main.name : "none")}");
                foreach (Canvas c in canvases)
                {
                    if (!c || c.gameObject.scene.name == null) continue; // skip prefab assets
                    if (!c.isRootCanvas) continue;
                    sb.AppendLine($"  CANVAS '{GetPath(c.transform)}' active={c.isActiveAndEnabled} mode={c.renderMode} cam={(c.worldCamera ? c.worldCamera.name : "none")} scene={c.gameObject.scene.name} sort={c.sortingOrder} layer={LayerMask.LayerToName(c.gameObject.layer)} pos={c.transform.position} scale={c.transform.lossyScale} parent={(c.transform.parent ? c.transform.parent.name : "-")}");
                    foreach (Graphic g in c.GetComponentsInChildren<Graphic>(true))
                    {
                        string detail = "";
                        if (g is Image img) detail = $"sprite={(img.sprite ? img.sprite.name : "null")} color={img.color}";
                        else if (g is RawImage raw) detail = $"tex={(raw.texture ? raw.texture.name : "null")}";
                        else if (g is TMPro.TMP_Text tmp) detail = $"text=\"{Truncate(tmp.text)}\"";
                        else if (g is Text ut) detail = $"text=\"{Truncate(ut.text)}\"";
                        RectTransform rt = g.transform as RectTransform;
                        sb.AppendLine($"    {g.GetType().Name} '{GetPath(g.transform, c.transform)}' on={g.enabled} activeInHier={g.gameObject.activeInHierarchy} rect={rt.rect.size} worldPos={rt.position} {detail}");
                    }
                }
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogWarning($"DumpCanvases failed: {e}");
            }
        }

        private static string Truncate(string t)
        {
            if (string.IsNullOrEmpty(t)) return "";
            t = t.Replace("\n", "\\n");
            return t.Length > 60 ? t.Substring(0, 60) + "..." : t;
        }

        private static string GetPath(Transform t, Transform stopAt = null)
        {
            string path = t.name;
            while (t.parent != null && t.parent != stopAt) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        // Re-dump a few times after a scene starts so we catch UI that appears later
        // (logo sequences, typed text) and see whether VR has come up in the meantime.
        internal static void ScheduleDumps(string context)
        {
            float start = Time.realtimeSinceStartup;
            int done = 0;
            float[] at = { 1f, 3f, 6f, 10f };
            System.Action tick = null;
            tick = () =>
            {
                if (done >= at.Length) { RoR2Application.onUpdate -= tick; return; }
                if (Time.realtimeSinceStartup - start >= at[done])
                {
                    DumpCanvases($"{context} +{at[done]}s");
                    done++;
                }
            };
            RoR2Application.onUpdate += tick;
        }
    }
}
