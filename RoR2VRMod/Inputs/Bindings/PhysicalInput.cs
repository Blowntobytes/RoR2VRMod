using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRMod.Inputs.Bindings
{
    /// <summary>
    /// Every physical control a chord can name. "Pad" inputs are a trackpad CLICK in a region
    /// (HTC Vive / WMR wands; on stick controllers it is the stick click while pushed that way).
    /// "Stick" inputs are a push past 70 % in that direction, click or not.
    /// </summary>
    internal enum PhysicalInput
    {
        None = 0,
        LeftX, LeftY, RightA, RightB,
        LeftTrigger, RightTrigger,
        LeftGrip, RightGrip,
        LeftStickClick, RightStickClick,
        LeftMenu, RightMenu,
        LeftStickUp, LeftStickDown, LeftStickLeft, LeftStickRight,
        RightStickUp, RightStickDown, RightStickLeft, RightStickRight,
        LeftPadUp, LeftPadDown, LeftPadLeft, LeftPadRight,
        RightPadUp, RightPadDown, RightPadLeft, RightPadRight,
    }

    /// <summary>Reads the raw state of every PhysicalInput once per frame.</summary>
    internal static class PhysicalState
    {
        internal const int Count = (int)PhysicalInput.RightPadRight + 1;
        private const float On = 0.7f, Off = 0.35f, PadRegion = 0.4f;

        private static readonly bool[] down = new bool[Count];
        private static readonly bool[] prev = new bool[Count];
        private static readonly bool[] stickLatch = new bool[Count];

        internal static bool Down(PhysicalInput i) => down[(int)i];
        internal static bool Prev(PhysicalInput i) => prev[(int)i];
        internal static bool JustPressed(PhysicalInput i) => down[(int)i] && !prev[(int)i];
        internal static bool JustReleased(PhysicalInput i) => !down[(int)i] && prev[(int)i];
        internal static bool AnyDown { get { for (int i = 1; i < Count; i++) if (down[i]) return true; return false; } }

        /// <summary>Stick / pad inputs are flicks, not buttons: they never count as "held".</summary>
        internal static bool IsDirectional(PhysicalInput i) => i >= PhysicalInput.LeftStickUp;
        internal static bool IsLeft(PhysicalInput i)
        {
            switch (i)
            {
                case PhysicalInput.LeftX: case PhysicalInput.LeftY: case PhysicalInput.LeftTrigger: case PhysicalInput.LeftGrip:
                case PhysicalInput.LeftStickClick: case PhysicalInput.LeftMenu:
                case PhysicalInput.LeftStickUp: case PhysicalInput.LeftStickDown: case PhysicalInput.LeftStickLeft: case PhysicalInput.LeftStickRight:
                case PhysicalInput.LeftPadUp: case PhysicalInput.LeftPadDown: case PhysicalInput.LeftPadLeft: case PhysicalInput.LeftPadRight:
                    return true;
            }
            return false;
        }

        internal static void Update()
        {
            Array.Copy(down, prev, Count);
            Array.Clear(down, 0, Count);

            InputDevice l = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice r = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            ReadHand(l, true);
            ReadHand(r, false);
        }

        private static void ReadHand(InputDevice d, bool left)
        {
            if (!d.isValid) return;
            bool b; Vector2 v;

            Set(left ? PhysicalInput.LeftX : PhysicalInput.RightA, d.TryGetFeatureValue(CommonUsages.primaryButton, out b) && b);
            Set(left ? PhysicalInput.LeftY : PhysicalInput.RightB, d.TryGetFeatureValue(CommonUsages.secondaryButton, out b) && b);
            Set(left ? PhysicalInput.LeftTrigger : PhysicalInput.RightTrigger, d.TryGetFeatureValue(CommonUsages.triggerButton, out b) && b);
            Set(left ? PhysicalInput.LeftGrip : PhysicalInput.RightGrip, d.TryGetFeatureValue(CommonUsages.gripButton, out b) && b);
            Set(left ? PhysicalInput.LeftMenu : PhysicalInput.RightMenu, d.TryGetFeatureValue(CommonUsages.menuButton, out b) && b);

            // Stick (or the wand's pad): the WMR wand has both - its pad is the secondary axis.
            bool wmr = ViveControllers.IsTrackpadWand && !ViveControllers.IsViveWand;
            Vector2 stick = d.TryGetFeatureValue(CommonUsages.primary2DAxis, out v) ? v : Vector2.zero;
            bool stickClick = d.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out b) && b;
            Vector2 pad = stick; bool padClick = stickClick;
            if (wmr)
            {
                pad = d.TryGetFeatureValue(CommonUsages.secondary2DAxis, out v) ? v : Vector2.zero;
                padClick = d.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out b) && b;
            }

            Set(left ? PhysicalInput.LeftStickClick : PhysicalInput.RightStickClick, stickClick);

            Vector2 flick = ViveControllers.IsTrackpadWand ? pad : stick;
            Latch(left ? PhysicalInput.LeftStickUp : PhysicalInput.RightStickUp, flick.y);
            Latch(left ? PhysicalInput.LeftStickDown : PhysicalInput.RightStickDown, -flick.y);
            Latch(left ? PhysicalInput.LeftStickRight : PhysicalInput.RightStickRight, flick.x);
            Latch(left ? PhysicalInput.LeftStickLeft : PhysicalInput.RightStickLeft, -flick.x);

            Set(left ? PhysicalInput.LeftPadUp : PhysicalInput.RightPadUp, padClick && pad.y >= PadRegion);
            Set(left ? PhysicalInput.LeftPadDown : PhysicalInput.RightPadDown, padClick && pad.y <= -PadRegion);
            Set(left ? PhysicalInput.LeftPadRight : PhysicalInput.RightPadRight, padClick && pad.x >= PadRegion && Mathf.Abs(pad.y) < PadRegion);
            Set(left ? PhysicalInput.LeftPadLeft : PhysicalInput.RightPadLeft, padClick && pad.x <= -PadRegion && Mathf.Abs(pad.y) < PadRegion);
        }

        private static void Set(PhysicalInput i, bool value) => down[(int)i] = value;

        private static void Latch(PhysicalInput i, float amount)
        {
            int k = (int)i;
            if (stickLatch[k]) { if (amount < Off) stickLatch[k] = false; }
            else if (amount > On) stickLatch[k] = true;
            down[k] = stickLatch[k];
        }

        // ---- names --------------------------------------------------------------------------

        private static readonly Dictionary<string, PhysicalInput> byName = new Dictionary<string, PhysicalInput>(StringComparer.OrdinalIgnoreCase);
        static PhysicalState()
        {
            foreach (PhysicalInput p in Enum.GetValues(typeof(PhysicalInput))) byName[p.ToString()] = p;
            // friendly aliases
            byName["LeftA"] = PhysicalInput.LeftX; byName["LeftB"] = PhysicalInput.LeftY; byName["RightX"] = PhysicalInput.RightA; byName["RightY"] = PhysicalInput.RightB;
            byName["X"] = PhysicalInput.LeftX; byName["Y"] = PhysicalInput.LeftY; byName["A"] = PhysicalInput.RightA; byName["B"] = PhysicalInput.RightB;
        }

        internal static bool TryParse(string s, out PhysicalInput p)
        {
            p = PhysicalInput.None;
            if (string.IsNullOrEmpty(s)) return false;
            return byName.TryGetValue(s.Trim().Replace(" ", ""), out p);
        }

        /// <summary>Short human name for the settings rows and wrist panel.</summary>
        internal static string Label(PhysicalInput p)
        {
            bool wand = ViveControllers.IsTrackpadWand;
            switch (p)
            {
                case PhysicalInput.None: return "—";
                case PhysicalInput.LeftX: return wand ? "L pad up" : "X";
                case PhysicalInput.LeftY: return wand ? "L menu" : "Y";
                case PhysicalInput.RightA: return wand ? "R pad down" : "A";
                case PhysicalInput.RightB: return wand ? "R pad up" : "B";
                case PhysicalInput.LeftTrigger: return "L trigger";
                case PhysicalInput.RightTrigger: return "R trigger";
                case PhysicalInput.LeftGrip: return "L grip";
                case PhysicalInput.RightGrip: return "R grip";
                case PhysicalInput.LeftStickClick: return wand ? "L pad click" : "L stick click";
                case PhysicalInput.RightStickClick: return wand ? "R pad click" : "R stick click";
                case PhysicalInput.LeftMenu: return "L menu";
                case PhysicalInput.RightMenu: return "R menu";
                case PhysicalInput.LeftStickUp: return "L stick up"; case PhysicalInput.LeftStickDown: return "L stick down";
                case PhysicalInput.LeftStickLeft: return "L stick left"; case PhysicalInput.LeftStickRight: return "L stick right";
                case PhysicalInput.RightStickUp: return "R stick up"; case PhysicalInput.RightStickDown: return "R stick down";
                case PhysicalInput.RightStickLeft: return "R stick left"; case PhysicalInput.RightStickRight: return "R stick right";
                case PhysicalInput.LeftPadUp: return "L pad up"; case PhysicalInput.LeftPadDown: return "L pad down";
                case PhysicalInput.LeftPadLeft: return "L pad left"; case PhysicalInput.LeftPadRight: return "L pad right";
                case PhysicalInput.RightPadUp: return "R pad up"; case PhysicalInput.RightPadDown: return "R pad down";
                case PhysicalInput.RightPadLeft: return "R pad left"; case PhysicalInput.RightPadRight: return "R pad right";
            }
            return p.ToString();
        }

        /// <summary>Glyph sprite for the game's on-screen prompts, per controller layout.</summary>
        internal static string Glyph(PhysicalInput p, bool vive, bool wmr)
        {
            string n;
            bool wand = vive || wmr;
            switch (p)
            {
                case PhysicalInput.LeftX: n = wand ? "LTouchPress2" : "LPrimary"; break;
                case PhysicalInput.LeftY: n = wand ? "LMenu" : "LSecondary"; break;
                case PhysicalInput.RightA: n = wand ? "RTouchDown" : "RPrimary"; break;
                case PhysicalInput.RightB: n = wand ? "RTouchUp" : "RSecondary"; break;
                case PhysicalInput.LeftTrigger: n = "LTrigger"; break;
                case PhysicalInput.RightTrigger: n = "RTrigger"; break;
                case PhysicalInput.LeftGrip: n = "LGrip"; break;
                case PhysicalInput.RightGrip: n = "RGrip"; break;
                case PhysicalInput.LeftStickClick: n = wand ? "LTouchPress" : "LStickPress"; break;
                case PhysicalInput.RightStickClick: n = wand ? "RTouchPress" : "RStickPress"; break;
                case PhysicalInput.LeftMenu: n = "LMenu"; break;
                case PhysicalInput.RightMenu: n = "RMenu"; break;
                case PhysicalInput.LeftPadUp: n = "LTouchPress2"; break;
                case PhysicalInput.LeftPadDown: n = "LTouchPress"; break;
                case PhysicalInput.RightPadUp: n = "RTouchUp"; break;
                case PhysicalInput.RightPadDown: n = "RTouchDown"; break;
                case PhysicalInput.LeftStickUp: case PhysicalInput.LeftStickDown: case PhysicalInput.LeftStickLeft: case PhysicalInput.LeftStickRight:
                case PhysicalInput.LeftPadLeft: case PhysicalInput.LeftPadRight:
                    n = vive ? "LTouch" : "LStick"; break;
                case PhysicalInput.RightStickUp: case PhysicalInput.RightStickDown: case PhysicalInput.RightStickLeft: case PhysicalInput.RightStickRight:
                case PhysicalInput.RightPadLeft: case PhysicalInput.RightPadRight:
                    n = vive ? "RTouchHor" : "RStick"; break;
                default: n = "Unknown"; break;
            }
            return "<sprite name=\"texVRGlyphs_" + n + "\">";
        }
    }
}
