using RoR2;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRMod
{
    /// <summary>
    /// 2.23.27 - tools for mapping runtime hands on short-lived forms.
    ///
    /// Always on:
    ///   * Model-swap watcher: in-place transformations (Acolyte's Reaper form) swap the visible
    ///     model on the SAME body. When the local body's model changes, the hand pair is rebuilt,
    ///     which gives the form its own "Body_Form" config section (see RuntimeHands.FormConfigName).
    ///
    /// Only while RuntimeHandsDebug is on:
    ///   * Freeze: hold BOTH grips and click the LEFT stick to toggle game time to zero (solo only).
    ///     Enemies, projectiles and transformation timers stop; head and hand tracking, rendering and
    ///     the 3-second live config reload keep running, so hand offsets can be tuned in peace.
    ///   * Infinite soul: turns on the game's own "acolyte_infinite_soul" console variable so
    ///     Acolyte's transformed form does not time out while mapping.
    ///   * Polymorph test (2.23.28): hold BOTH grips and click the RIGHT stick to turn into the
    ///     Shrine of Compulsion's PolymorphLemurianBody and back (solo), so its hands can be mapped
    ///     without finding the shrine. Uses the game's own CharacterMaster.TransformBody.
    /// </summary>
    internal static class MappingMode
    {
        private static Transform lastModel;
        private static bool lastReaper;
        private static bool handRebuildPending;

        private static bool InTransformState(CharacterBody body)
        {
            foreach (EntityStateMachine m in body.GetComponents<EntityStateMachine>())
            {
                if (m == null || m.customName != "Body" || m.state == null) continue;
                string n = m.state.GetType().Name;
                return n.Contains("SpawnState") || n.Contains("BodySwap") || n == "Return" || n == "FinishReturn";
            }
            return false;
        }
        private static CharacterBody lastBody;
        private static bool frozen, prevChord, prevPolyChord;
        private static string polymorphReturnBody;
        private const string PolymorphBody = "PolymorphLemurianBody";
        private static bool infiniteSoulSet;

        internal static bool IsFrozen => frozen;

        internal static void Init()
        {
            RoR2Application.onUpdate += Tick;
        }

        private static void Tick()
        {
            try { WatchModel(); } catch (System.Exception e) { VRMod.StaticLogger.LogWarning($"[VR mapping] model watcher: {e.Message}"); }

            bool debug = ModConfig.RuntimeHandsDebug.Value;
            if (!debug)
            {
                if (frozen) SetFrozen(false, "RuntimeHandsDebug turned off");
                return;
            }

            try { PollFreezeChord(); } catch (System.Exception e) { VRMod.StaticLogger.LogWarning($"[VR mapping] freeze: {e.Message}"); }
            if (frozen && Time.timeScale != 0f) Time.timeScale = 0f; // something else (pause menu closing) restored it

            if (!infiniteSoulSet) TryInfiniteSoul();
        }

        private static void WatchModel()
        {
            CharacterBody body = MotionControls.currentBody;
            if (!body || !body.modelLocator) { lastBody = null; lastModel = null; return; }

            Transform model = body.modelLocator.modelTransform;
            AcolyteBodySwapController swap = body.GetComponent<AcolyteBodySwapController>();
            bool reaper = swap && swap.isReaperForm;
            if (body != lastBody) { lastBody = body; lastModel = model; lastReaper = reaper; return; }

            // 2.23.29: the Reaper form toggles child objects under the SAME model, so watch the flag.
            if (reaper != lastReaper)
            {
                lastReaper = reaper;
                VRMod.StaticLogger.LogInfo($"[VR mapping] '{body.name}' {(reaper ? "entered" : "left")} its Reaper form - rebuilding hands and view.");
                CameraFixes.RequestReanchor(reaper ? "Reaper form" : "normal form");
                handRebuildPending = true;
            }
            // 2.23.40: the game's own transformation (both grips) swaps the body INSIDE
            // LilReaper.SpawnState, which keeps the character model hidden while it plays; hands
            // baked then stayed invisible (only the gizmo showed). Wait until the Body state
            // machine has left its spawn / body-swap state, then rebuild.
            if (handRebuildPending)
            {
                if (InTransformState(body)) return;
                handRebuildPending = false;
                VRMod.StaticLogger.LogInfo($"[VR mapping] '{body.name}' transformation finished - rebuilding hands.");
                MotionControls.SetHandPair(body);
                return;
            }
            if (!model || model == lastModel) return;

            VRMod.StaticLogger.LogInfo($"[VR mapping] '{body.name}' swapped model '{(lastModel ? lastModel.name : "none")}' -> '{model.name}' - rebuilding hands for this form.");
            lastModel = model;
            MotionControls.SetHandPair(body);
        }

        private static void PollFreezeChord()
        {
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool lg = left.isValid && left.IsPressed(InputHelpers.Button.GripButton, out bool a) && a;
            bool rg = right.isValid && right.IsPressed(InputHelpers.Button.GripButton, out bool b) && b;
            bool ls = left.isValid && left.IsPressed(InputHelpers.Button.Primary2DAxisClick, out bool c) && c;
            bool chord = lg && rg && ls;

            bool rs = right.isValid && right.IsPressed(InputHelpers.Button.Primary2DAxisClick, out bool d) && d;
            bool polyChord = lg && rg && rs;
            if (polyChord && !prevPolyChord) TogglePolymorph();
            prevPolyChord = polyChord;

            if (chord && !prevChord)
            {
                if (!frozen && !IsSolo())
                    VRMod.StaticLogger.LogInfo("[VR mapping] Freeze is only available when you are the only player.");
                else
                    SetFrozen(!frozen, "both grips + left stick click");
            }
            prevChord = chord;
        }

        private static void TogglePolymorph()
        {
            if (!IsSolo()) { VRMod.StaticLogger.LogInfo("[VR mapping] Polymorph test is only available when you are the only player."); return; }
            CharacterBody body = MotionControls.currentBody;
            CharacterMaster master = body ? body.master : null;
            if (!master || !UnityEngine.Networking.NetworkServer.active) return;

            // 2.23.30: on Acolyte the same combo toggles her Reaper form instead, using the game's
            // own swap (visuals, camera pivot, stats) - for checking the form's hands and view scale.
            AcolyteBodySwapController swap = body.GetComponent<AcolyteBodySwapController>();
            if (swap)
            {
                if (swap.isReaperForm)
                {
                    VRMod.StaticLogger.LogInfo("[VR mapping] Reaper test: back to normal Acolyte.");
                    swap.UnswapBody();
                }
                else
                {
                    VRMod.StaticLogger.LogInfo("[VR mapping] Reaper test: Acolyte -> Reaper form. Both grips + right stick click again to change back.");
                    swap.SwapBody();
                }
                return;
            }

            string current = body.name.Replace("(Clone)", "").Trim();
            if (current != PolymorphBody)
            {
                polymorphReturnBody = current;
                VRMod.StaticLogger.LogInfo($"[VR mapping] Polymorph test: '{current}' -> '{PolymorphBody}'. Both grips + right stick click again to change back.");
                master.TransformBody(PolymorphBody);
            }
            else if (!string.IsNullOrEmpty(polymorphReturnBody))
            {
                VRMod.StaticLogger.LogInfo($"[VR mapping] Polymorph test: back to '{polymorphReturnBody}'.");
                master.TransformBody(polymorphReturnBody);
            }
        }

        private static bool IsSolo()
        {
            return NetworkUser.readOnlyInstancesList.Count <= 1;
        }

        private static void SetFrozen(bool value, string why)
        {
            frozen = value;
            if (frozen)
            {
                Time.timeScale = 0f;
                Inputs.KeyItemIndicator.SetStatus("<b>FROZEN</b> (mapping)\n<size=80%>Both grips + left stick: resume</size>");
            }
            else
            {
                if (!PauseManager.isPaused) Time.timeScale = 1f;
                Inputs.KeyItemIndicator.SetStatus(null);
            }
            VRMod.StaticLogger.LogInfo($"[VR mapping] {(frozen ? "FROZEN" : "resumed")} ({why}). Config edits still apply every 3 s.");
        }

        private static void TryInfiniteSoul()
        {
            CharacterBody body = MotionControls.currentBody;
            if (!body || !body.name.StartsWith("AcolyteBody")) return;
            infiniteSoulSet = true;
            try
            {
                var field = typeof(AcolyteFormController).GetField("cvInfiniteSoul",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                var cv = field != null ? field.GetValue(null) as RoR2.ConVar.BoolConVar : null;
                if (cv == null) { VRMod.StaticLogger.LogWarning("[VR mapping] acolyte_infinite_soul not found in this game version."); return; }
                cv.SetBool(true);
                VRMod.StaticLogger.LogInfo("[VR mapping] acolyte_infinite_soul ON (RuntimeHandsDebug) - Acolyte's transformation will not time out.");
            }
            catch (System.Exception e) { VRMod.StaticLogger.LogWarning($"[VR mapping] could not set acolyte_infinite_soul: {e.Message}"); }
        }
    }
}
