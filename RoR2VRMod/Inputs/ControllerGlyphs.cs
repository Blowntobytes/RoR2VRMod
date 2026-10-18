using Rewired;
using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using System.Linq;
using TMPro;
using UnityEngine.XR;

namespace VRMod
{
    internal static class ControllerGlyphs
    {
        internal static readonly string[] standardGlyphs = new string[]
        {
            "<sprite name=\"texVRGlyphs_LStick\">",          //MoveX
            "<sprite name=\"texVRGlyphs_LStick\">",          //MoveY
            "<sprite name=\"texVRGlyphs_RStick\">",          //LookX
            "<sprite name=\"texVRGlyphs_RStick\">",          //LookY
            "<sprite name=\"texVRGlyphs_LStick\">",          //NavigateX
            "<sprite name=\"texVRGlyphs_LStick\">",          //NavigateY
            "<sprite name=\"texVRGlyphs_RPrimary\">",        //Interact
            "<sprite name=\"texVRGlyphs_RSecondary\">",      //Jump
            "<sprite name=\"texVRGlyphs_RTrigger\">",        //PrimarySkill
            "<sprite name=\"texVRGlyphs_LTrigger\">",        //SecondarySkill
            "<sprite name=\"texVRGlyphs_LGrip\">",           //UtilitySkill
            "<sprite name=\"texVRGlyphs_RGrip\">",           //SpecialSkill
            "<sprite name=\"texVRGlyphs_LPrimary\">",        //Equipment
            "<sprite name=\"texVRGlyphs_LStickPress\">",     //Sprint 
            "<sprite name=\"texVRGlyphs_RStickPress\">",     //Ping
            "<sprite name=\"texVRGlyphs_LSecondaryHold\">",  //Info
            "<sprite name=\"texVRGlyphs_LSecondaryHold\">",  //HoldInfo
            "<sprite name=\"texVRGlyphs_RPrimary\">",        //Submit
            "<sprite name=\"texVRGlyphs_RSecondary\">",      //Cancel 
            "<sprite name=\"texVRGlyphs_LPrimary\">",        //Ready
            "<sprite name=\"texVRGlyphs_LTrigger\">",        //TabLeft
            "<sprite name=\"texVRGlyphs_RTrigger\">",        //TabRight
            "<sprite name=\"texVRGlyphs_LGrip\">",           //SubmenuLeft
            "<sprite name=\"texVRGlyphs_RGrip\">",           //SubmenuRight
            "<sprite name=\"texVRGlyphs_LSecondary\">",      //Pause 
            "<sprite name=\"texVRGlyphs_RStickPress\">",     //RecenterHMD
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill1
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill2
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill3
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill4
            "<sprite name=\"texVRGlyphs_Unknown\">",         //PushToTalk
            "<sprite name=\"texVRGlyphs_Unknown\">",         //BuySkill
            "<sprite name=\"texVRGlyphs_LSecondary\">",        //Load (Y in lobby)
            "<sprite name=\"texVRGlyphs_LPrimary\">+<sprite name=\"texVRGlyphs_RPrimary\">",   //UseKeyItem (hold left X + interact button),
            "<sprite name=\"texVRGlyphs_LPrimary\">+<sprite name=\"texVRGlyphs_RStick\">",   //KeyItemNextElement (hold left X + flick right stick),
            "<sprite name=\"texVRGlyphs_LPrimary\">+<sprite name=\"texVRGlyphs_RStick\">"    //KeyItemPrevElement
        };

        internal static readonly string[] viveGlyphs = new string[]
        {
            "<sprite name=\"texVRGlyphs_LTouch\">",          //MoveX
            "<sprite name=\"texVRGlyphs_LTouch\">",          //MoveY
            "<sprite name=\"texVRGlyphs_RTouchHor\">",       //LookX
            "<sprite name=\"texVRGlyphs_RTouchHor\">",       //LookY
            "<sprite name=\"texVRGlyphs_LTouch\">",          //NavigateX
            "<sprite name=\"texVRGlyphs_LTouch\">",          //NavigateY
            "<sprite name=\"texVRGlyphs_RTouchDown\">",      //Interact
            "<sprite name=\"texVRGlyphs_RTouchUp\">",        //Jump
            "<sprite name=\"texVRGlyphs_RTrigger\">",        //PrimarySkill
            "<sprite name=\"texVRGlyphs_LTrigger\">",        //SecondarySkill
            "<sprite name=\"texVRGlyphs_LGrip\">",           //UtilitySkill
            "<sprite name=\"texVRGlyphs_RGrip\">",           //SpecialSkill
            "<sprite name=\"texVRGlyphs_LTouchPress2\">",    //Equipment
            "<sprite name=\"texVRGlyphs_LTouchPress\">",     //Sprint 
            "<sprite name=\"texVRGlyphs_RMenu\">",           //Ping
            "<sprite name=\"texVRGlyphs_LMenuHold\">",       //Info
            "<sprite name=\"texVRGlyphs_LMenuHold\">",       //HoldInfo
            "<sprite name=\"texVRGlyphs_RTouchDown\">",      //Submit
            "<sprite name=\"texVRGlyphs_RTouchUp\">",        //Cancel 
            "<sprite name=\"texVRGlyphs_LTouchPress2\">",    //Ready
            "<sprite name=\"texVRGlyphs_LTrigger\">",        //TabLeft
            "<sprite name=\"texVRGlyphs_RTrigger\">",        //TabRight
            "<sprite name=\"texVRGlyphs_LGrip\">",           //SubmenuLeft
            "<sprite name=\"texVRGlyphs_RGrip\">",           //SubmenuRight
            "<sprite name=\"texVRGlyphs_LMenu\">",           //Pause 
            "<sprite name=\"texVRGlyphs_RMenu\">",           //RecenterHMD
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill1
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill2
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill3
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill4
            "<sprite name=\"texVRGlyphs_Unknown\">",         //PushToTalk
            "<sprite name=\"texVRGlyphs_Unknown\">",         //BuySkill
            "<sprite name=\"texVRGlyphs_LMenu\">",             //Load (Y in lobby)
            "<sprite name=\"texVRGlyphs_LTouchPress2\">+<sprite name=\"texVRGlyphs_RTouchDown\">",   //UseKeyItem (hold left pad up + interact),
            "<sprite name=\"texVRGlyphs_LTouchPress2\">+<sprite name=\"texVRGlyphs_RTouchHor\">",   //KeyItemNextElement (hold left pad up + pad),
            "<sprite name=\"texVRGlyphs_LTouchPress2\">+<sprite name=\"texVRGlyphs_RTouchHor\">"    //KeyItemPrevElement
        };

        internal static readonly string[] wmrGlyphs = new string[]
        {
            "<sprite name=\"texVRGlyphs_LTouch\">",          //MoveX
            "<sprite name=\"texVRGlyphs_LTouch\">",          //MoveY
            "<sprite name=\"texVRGlyphs_RStick\">",          //LookX
            "<sprite name=\"texVRGlyphs_RStick\">",          //LookY
            "<sprite name=\"texVRGlyphs_LStick\">",          //NavigateX
            "<sprite name=\"texVRGlyphs_LStick\">",          //NavigateY
            "<sprite name=\"texVRGlyphs_RTouchDown\">",      //Interact
            "<sprite name=\"texVRGlyphs_RTouchUp\">",        //Jump
            "<sprite name=\"texVRGlyphs_RTrigger\">",        //PrimarySkill
            "<sprite name=\"texVRGlyphs_LTrigger\">",        //SecondarySkill
            "<sprite name=\"texVRGlyphs_LGrip\">",           //UtilitySkill
            "<sprite name=\"texVRGlyphs_RGrip\">",           //SpecialSkill
            "<sprite name=\"texVRGlyphs_LTouchPress2\">",    //Equipment
            "<sprite name=\"texVRGlyphs_LTouchPress\">",     //Sprint 
            "<sprite name=\"texVRGlyphs_RMenu\">",           //Ping
            "<sprite name=\"texVRGlyphs_LMenuHold\">",       //Info
            "<sprite name=\"texVRGlyphs_LMenuHold\">",       //HoldInfo
            "<sprite name=\"texVRGlyphs_RTouchDown\">",      //Submit
            "<sprite name=\"texVRGlyphs_RTouchUp\">",        //Cancel 
            "<sprite name=\"texVRGlyphs_LTouchPress2\">",    //Ready
            "<sprite name=\"texVRGlyphs_LTrigger\">",        //TabLeft
            "<sprite name=\"texVRGlyphs_RTrigger\">",        //TabRight
            "<sprite name=\"texVRGlyphs_LGrip\">",           //SubmenuLeft
            "<sprite name=\"texVRGlyphs_RGrip\">",           //SubmenuRight
            "<sprite name=\"texVRGlyphs_LMenu\">",           //Pause 
            "<sprite name=\"texVRGlyphs_RMenu\">",           //RecenterHMD
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill1
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill2
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill3
            "<sprite name=\"texVRGlyphs_Unknown\">",         //ExtraSkill4
            "<sprite name=\"texVRGlyphs_Unknown\">",         //PushToTalk
            "<sprite name=\"texVRGlyphs_Unknown\">",         //BuySkill
            "<sprite name=\"texVRGlyphs_LMenu\">",             //Load (Y in lobby)
            "<sprite name=\"texVRGlyphs_LTouchPress2\">+<sprite name=\"texVRGlyphs_RTouchDown\">",   //UseKeyItem (hold left pad up + interact),
            "<sprite name=\"texVRGlyphs_LTouchPress2\">+<sprite name=\"texVRGlyphs_RStick\">",   //KeyItemNextElement (hold left pad up + stick),
            "<sprite name=\"texVRGlyphs_LTouchPress2\">+<sprite name=\"texVRGlyphs_RStick\">"    //KeyItemPrevElement
        };

        private static TMP_SpriteAsset glyphsSpriteAsset;

        private static string[] currentGlyphs;

        private static bool isUsingMotionControls;

        internal static void Init()
        {
            On.RoR2.Glyphs.GetGlyphString_MPEventSystem_string_AxisRange_InputSource_bool += GetCustomGlyphString;

            On.RoR2.InputBindingDisplayController.Awake += ApplyInputDisplaySpriteAsset;

            On.RoR2.UI.ContextManager.Awake += ApplyContextSpriteAsset;

            On.RoR2.UI.MPEventSystem.OnLastActiveControllerChanged += ChangedToCustom;

            glyphsSpriteAsset = VRMod.VRAssetBundle.LoadAsset<TMP_SpriteAsset>("sprVRGlyphs");

            RoR2Application.onUpdate += FindControllerType;

            // Rebindable actions: their prompt glyphs follow the player's chords.
            Inputs.Bindings.BindingActions.Changed += ApplyBindingGlyphs;
            ApplyBindingGlyphs();
        }

        /// <summary>Writes the glyph of every rebindable action into all three layout tables.</summary>
        internal static void ApplyBindingGlyphs()
        {
            foreach (Inputs.Bindings.BindingDef d in Inputs.Bindings.BindingActions.All)
            {
                if (d.Element < 0 || d.Element >= standardGlyphs.Length) continue;
                standardGlyphs[d.Element] = ChordGlyph(d.Current, false, false);
                if (d.Element < viveGlyphs.Length) viveGlyphs[d.Element] = ChordGlyph(d.Current, true, false);
                if (d.Element < wmrGlyphs.Length) wmrGlyphs[d.Element] = ChordGlyph(d.Current, false, true);
            }
        }

        private static string ChordGlyph(Inputs.Bindings.Chord c, bool vive, bool wmr)
        {
            if (c.IsNone) return "<sprite name=\"texVRGlyphs_Unknown\">";
            // The authored "hold" art for the default scoreboard button.
            if (!c.HasModifier && c.Mode == Inputs.Bindings.ChordMode.Hold)
            {
                if (!vive && !wmr && c.Button == Inputs.Bindings.PhysicalInput.LeftY) return "<sprite name=\"texVRGlyphs_LSecondaryHold\">";
                if ((vive || wmr) && (c.Button == Inputs.Bindings.PhysicalInput.LeftMenu || c.Button == Inputs.Bindings.PhysicalInput.LeftY)) return "<sprite name=\"texVRGlyphs_LMenuHold\">";
            }
            string g = Inputs.Bindings.PhysicalState.Glyph(c.Button, vive, wmr);
            if (c.HasModifier) g = Inputs.Bindings.PhysicalState.Glyph(c.Modifier, vive, wmr) + "+" + g;
            if (c.Mode == Inputs.Bindings.ChordMode.Hold) g += "<size=70%>hold</size>";
            return g;
        }

        private static void FindControllerType()
        {
            InputDevice controllerDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (controllerDevice == null || controllerDevice.name == null)
                return;

            string controllerName = controllerDevice.name.ToLower();

            if (controllerName == "")
                return;

            if (controllerName.Contains("htc vive controller"))
                currentGlyphs = viveGlyphs;
            else if (controllerName.Contains("windows mr controller"))
                currentGlyphs = wmrGlyphs;
            else
                currentGlyphs = standardGlyphs;

            RoR2Application.onUpdate -= FindControllerType;
        }

        /// <summary>
        /// Action id -> VR controller element, taken from the mod's own Rewired maps. Used when a
        /// prompt asks for a glyph before a Rewired player exists for the event system (the lobby
        /// footer's Ready prompt is built that early), so it still shows the right VR button
        /// instead of nothing.
        /// </summary>
        private static readonly Dictionary<int, int> fallbackActionToElement = new Dictionary<int, int>();

        internal static void RegisterFallbackBindings(IEnumerable<ActionElementMap> maps)
        {
            foreach (ActionElementMap m in maps)
                if (m != null && !fallbackActionToElement.ContainsKey(m.actionId))
                    fallbackActionToElement[m.actionId] = m.elementIdentifierId;
        }

        private static string FallbackGlyph(string actionName)
        {
            if (currentGlyphs == null || string.IsNullOrEmpty(actionName) || !ReInput.isReady) return null;
            int actionId = ReInput.mapping.GetActionId(actionName);
            int element;
            if (actionId < 0 || !fallbackActionToElement.TryGetValue(actionId, out element)) return null;
            return element >= 0 && element < currentGlyphs.Length ? currentGlyphs[element] : null;
        }

        private static string GetCustomGlyphString(On.RoR2.Glyphs.orig_GetGlyphString_MPEventSystem_string_AxisRange_InputSource_bool orig, MPEventSystem eventSystem, string actionName, AxisRange axisRange, MPEventSystem.InputSource currentInputSource, bool useLastActiveControllerOnly)
        {
            if (!eventSystem)
            {
                return "???";
            }
            if (eventSystem.player == null)
            {
                string fallback = FallbackGlyph(actionName);
                if (!string.IsNullOrEmpty(fallback)) return fallback;
            }
            if (isUsingMotionControls && eventSystem.player != null && currentGlyphs != null)
            {
                Glyphs.resultsList.Clear();
                eventSystem.player.controllers.maps.GetElementMapsWithAction(ControllerType.Custom, Controllers.ControllerID, actionName, false, Glyphs.resultsList);

                if (Glyphs.resultsList.Count() > 0)
                {
                    ActionElementMap displayedMap = Glyphs.resultsList.First();
                    if (displayedMap.elementIdentifierId > 0 && displayedMap.elementIdentifierId < currentGlyphs.Length)
                    {
                        return currentGlyphs[displayedMap.elementIdentifierId];
                    }
                    LogUnresolvedOnce(actionName, $"bound to VR element {displayedMap.elementIdentifierId}, which has no glyph");
                }
                else LogUnresolvedOnce(actionName, "no VR binding");
            }

            return orig(eventSystem, actionName, axisRange, currentInputSource, useLastActiveControllerOnly);
        }

        private static readonly HashSet<string> unresolvedLogged = new HashSet<string>();

        /// <summary>2.23.28: names any on-screen prompt the VR glyph table cannot answer (it then
        /// falls back to the game's own lookup, which shows "unmapped").</summary>
        private static void LogUnresolvedOnce(string actionName, string why)
        {
            if (string.IsNullOrEmpty(actionName) || !unresolvedLogged.Add(actionName)) return;
            VRMod.StaticLogger.LogInfo($"[VR glyph] prompt for action '{actionName}': {why} - showing the game's default.");
        }

        private static void ChangedToCustom(On.RoR2.UI.MPEventSystem.orig_OnLastActiveControllerChanged orig, MPEventSystem self, Player player, Controller controller)
        {
            if (controller != null && controller.type == ControllerType.Custom)
            {
                isUsingMotionControls = true;
                // GAMEPAD-style menus: the left stick moves the selection, A / right trigger confirm
                // (UISubmit), B cancels. No laser pointer / cursor is involved, so the menus work the
                // same everywhere - title screen, settings, and the mid-run pause menu.
                self.currentInputSource = MPEventSystem.InputSource.Gamepad;
                return;
            }
            isUsingMotionControls = false;

            orig(self, player, controller);
        }

        private static void ApplyContextSpriteAsset(On.RoR2.UI.ContextManager.orig_Awake orig, ContextManager self)
        {
            orig(self);

            if (self.glyphTMP && glyphsSpriteAsset)
            {
                ApplySpriteAsset(self.glyphTMP);
            }
        }

        private static void ApplyInputDisplaySpriteAsset(On.RoR2.InputBindingDisplayController.orig_Awake orig, InputBindingDisplayController self)
        {
            orig(self);

            if (glyphsSpriteAsset)
            {
                if (self.guiLabel)
                    ApplySpriteAsset(self.guiLabel);

                if (self.label)
                    ApplySpriteAsset(self.label);
            }
        }

        internal static void ApplySpriteAsset(TextMeshPro tmp)
        {
            tmp.spriteAsset = glyphsSpriteAsset;
        }

        internal static void ApplySpriteAsset(TextMeshProUGUI tmp)
        {
            tmp.spriteAsset = glyphsSpriteAsset;
        }

        internal static string GetGlyph(uint index)
        {
            if (currentGlyphs == null || index >= currentGlyphs.Length) return "";

            return currentGlyphs[index];
        }
    }
}
