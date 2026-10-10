using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// Alternate game mode screens (Eclipse, Simulacrum, Prismatic Trials and mods that reuse them,
    /// such as Scavenger Bingo) load in their own scene. Their Awake/OnEnable hooks in UIFixes run
    /// before that scene's camera rig exists, so CanvasToWorldSpace finds no UI camera and silently
    /// leaves the canvas in screen space: the buttons draw on the desktop window but not in the
    /// headset (only the 3D background is visible). This watcher keeps retrying the conversion while
    /// a mode screen is present, and also converts any other screen-space canvas with buttons in
    /// that scene. It also writes a diagnostic dump of the menu tree to the log.
    /// </summary>
    internal static class ModeMenuFixes
    {
        private const float CheckInterval = 0.5f;
        private static float nextCheck;
        private static int dumpsThisScene;
        private static float sceneLoadedAt;
        private static bool lateDumpDone;
        private static readonly HashSet<int> loggedConversions = new HashSet<int>();

        // Scenes that only hold a mode-select menu (never a stage), so every screen-space menu there belongs in VR.
        private static readonly HashSet<string> ModeSceneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eclipseworld", "infinitetowerworld", "weeklyrun", "weeklyrunworld"
        };

        // Controllers from other mods that take over a mode screen.
        private static readonly HashSet<string> ReplacementControllerNames = new HashSet<string>
        {
            "ScavengerBingoMenuController"
        };

        internal static void Init()
        {
            SceneManager.activeSceneChanged += (oldScene, newScene) =>
            {
                dumpsThisScene = 0;
                lateDumpDone = false;
                sceneLoadedAt = Time.unscaledTime;
                loggedConversions.Clear();
            };
            RoR2Application.onUpdate += Tick;
        }

        private static bool IsModeController(MonoBehaviour mb)
        {
            return mb is EclipseRunScreenController || mb is InfiniteTowerMenuController || mb is WeeklyRunScreenController;
        }

        private static void Tick()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckInterval;

            try
            {
                List<MonoBehaviour> controllers = new List<MonoBehaviour>();
                controllers.AddRange(UnityEngine.Object.FindObjectsOfType<EclipseRunScreenController>());
                controllers.AddRange(UnityEngine.Object.FindObjectsOfType<InfiniteTowerMenuController>());
                controllers.AddRange(UnityEngine.Object.FindObjectsOfType<WeeklyRunScreenController>());

                // Mods can replace a mode screen's controller: Scavenger Bingo destroys the
                // InfiniteTowerMenuController and adds its own ScavengerBingoMenuController.
                // So also recognise the mode scenes by name, and any known replacement controller.
                Scene active = SceneManager.GetActiveScene();
                bool modeScene = ModeSceneNames.Contains(active.name);
                if (controllers.Count == 0 && modeScene) // scan only in mode scenes; never during a run
                {
                    foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                    {
                        if (mb && ReplacementControllerNames.Contains(mb.GetType().Name))
                            controllers.Add(mb);
                    }
                }
                if (controllers.Count == 0 && !modeScene) return;

                string what = controllers.Count > 0 ? controllers[0].GetType().Name : "scene '" + active.name + "'";
                bool haveCam = UIFixes.GetUICamera();
                if (dumpsThisScene == 0)
                    DumpMenuTree("mode screen detected (" + what + "), UI camera " + (haveCam ? "ready" : "NOT ready yet"));

                if (!haveCam) return;

                bool converted = false;

                foreach (MonoBehaviour c in controllers)
                {
                    if (!c) continue;
                    if (!IsModeController(c)) continue; // replacement controllers are handled by the scene sweep below
                    Canvas canvas = c.GetComponent<Canvas>();
                    if (!canvas) canvas = c.GetComponentInParent<Canvas>();
                    if (!canvas) continue;
                    Canvas root = canvas.rootCanvas ? canvas.rootCanvas : canvas;
                    if (root.renderMode == RenderMode.WorldSpace) continue;

                    GameObject target = c.GetComponent<Canvas>() ? c.gameObject : root.gameObject;
                    UIFixes.CanvasToWorldSpace(target, UIFixes.hdResolution, UIFixes.menuPosition, UIFixes.menuScale, true);
                    LogConversion(root, "mode screen " + c.GetType().Name);
                    converted = true;
                }

                // Any other screen-space canvas with buttons in the same scene (e.g. a mod's replacement menu).
                Scene scene = controllers.Count > 0 ? controllers[0].gameObject.scene : active;
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
                {
                    if (!canvas || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                    if (canvas.gameObject.scene != scene) continue;
                    if (RoR2Application.instance && canvas == RoR2Application.instance.mainCanvas) continue;
                    if (canvas.GetComponentInChildren<Selectable>(true) == null) continue;

                    UIFixes.CanvasToWorldSpace(canvas.gameObject, UIFixes.hdResolution, UIFixes.menuPosition, UIFixes.menuScale, true);
                    LogConversion(canvas, "extra screen-space canvas");
                    converted = true;
                }

                if (converted && dumpsThisScene < 3)
                    DumpMenuTree("after world-space conversion");
                else if (!lateDumpDone && Time.unscaledTime - sceneLoadedAt > 4f)
                {
                    lateDumpDone = true;
                    DumpMenuTree("4 s after the mode scene loaded");
                }
            }
            catch (Exception e)
            {
                VRMod.StaticLogger.LogError("[VR mode menu] " + e);
            }
        }

        private static void LogConversion(Canvas canvas, string why)
        {
            if (!canvas || !loggedConversions.Add(canvas.GetInstanceID())) return;
            VRMod.StaticLogger.LogInfo("[VR mode menu] Moved '" + PathOf(canvas.transform) + "' to world space (" + why + "); now " + canvas.renderMode + ".");
        }

        /// <summary>Diagnostic dump: every canvas, selectable and input-source filter in the loaded scenes.</summary>
        internal static void DumpMenuTree(string reason)
        {
            dumpsThisScene++;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[VR menu dump] " + reason + " | active scene '" + SceneManager.GetActiveScene().name + "'");

                Camera uiCam = UIFixes.cachedUICam;
                sb.AppendLine("  UI camera: " + (uiCam ? "'" + uiCam.name + "' enabled=" + uiCam.isActiveAndEnabled + " cullingMask=0x" + uiCam.cullingMask.ToString("X") : "none")
                    + " | camera rig: " + (Utils.localCameraRig ? "'" + Utils.localCameraRig.name + "'" : "none"));

                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                sb.AppendLine("  Canvases:");
                foreach (Canvas c in canvases)
                {
                    if (!c || !c.gameObject.scene.IsValid()) continue; // skip prefabs/assets
                    int layer = c.gameObject.layer;
                    bool inMask = uiCam && (uiCam.cullingMask & (1 << layer)) != 0;
                    sb.AppendLine("    " + PathOf(c.transform)
                        + " | scene=" + c.gameObject.scene.name
                        + " active=" + c.gameObject.activeInHierarchy + " enabled=" + c.enabled
                        + " root=" + c.isRootCanvas
                        + " mode=" + c.renderMode
                        + " cam=" + (c.worldCamera ? c.worldCamera.name : "none")
                        + " overrideSorting=" + c.overrideSorting + " order=" + c.sortingOrder
                        + " layer=" + LayerMask.LayerToName(layer) + "(" + layer + ")" + (uiCam ? (inMask ? " [in UI cam mask]" : " [NOT in UI cam mask]") : "")
                        + " pos=" + c.transform.position + " scale=" + c.transform.lossyScale);
                }

                sb.AppendLine("  Selectables:");
                foreach (Selectable s in Resources.FindObjectsOfTypeAll<Selectable>())
                {
                    if (!s || !s.gameObject.scene.IsValid()) continue;
                    Graphic g = s.targetGraphic;
                    Canvas pc = s.GetComponentInParent<Canvas>();
                    CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
                    sb.AppendLine("    " + s.GetType().Name + " " + PathOf(s.transform)
                        + " | active=" + s.gameObject.activeInHierarchy + " interactable=" + s.IsInteractable()
                        + " graphic=" + (g ? (g.enabled + " a=" + g.color.a.ToString("0.00")) : "none")
                        + " group=" + (cg ? ("a=" + cg.alpha.ToString("0.00")) : "none")
                        + " canvas=" + (pc ? pc.name + "/" + pc.rootCanvas.renderMode : "none"));
                }

                sb.AppendLine("  InputSourceFilters:");
                foreach (InputSourceFilter f in Resources.FindObjectsOfTypeAll<InputSourceFilter>())
                {
                    if (!f || !f.gameObject.scene.IsValid()) continue;
                    StringBuilder objs = new StringBuilder();
                    if (f.objectsToFilter != null)
                        foreach (GameObject o in f.objectsToFilter)
                            objs.Append(o ? o.name + "(" + (o.activeSelf ? "on" : "off") + ") " : "null ");
                    sb.AppendLine("    " + PathOf(f.transform)
                        + " | required=" + f.requiredInputSource
                        + " eventSystem=" + (f.eventSystem ? f.eventSystem.name : "null")
                        + " objects=" + objs);
                }

                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch (Exception e)
            {
                VRMod.StaticLogger.LogError("[VR menu dump] failed: " + e);
            }
        }

        internal static string PathOf(Transform t)
        {
            if (!t) return "null";
            StringBuilder sb = new StringBuilder(t.name);
            Transform p = t.parent;
            while (p)
            {
                sb.Insert(0, p.name + "/");
                p = p.parent;
            }
            return sb.ToString();
        }
    }
}
