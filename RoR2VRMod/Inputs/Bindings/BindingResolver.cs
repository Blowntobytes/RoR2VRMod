using Rewired;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace VRMod.Inputs.Bindings
{
    /// <summary>
    /// Turns the raw controller state into the game's Rewired elements according to the player's
    /// chords. General rules (the old hard-wired special cases - Y tap/hold, the key item modifier -
    /// are just default chords now):
    ///  * A chord with a modifier fires when its button is pressed while the modifier is held.
    ///    Stick/pad directions fire as a flick (one pulse); buttons stay active while held.
    ///  * A button that serves as a modifier has its own bare action deferred: it fires on release,
    ///    and only if no chord was used during the hold.
    ///  * While a modifier is held, the bare action of any button that pairs with it is blocked, and
    ///    the stick a flick-chord uses stops turning / moving.
    ///  * On one button, "tap" fires on a short release and "hold" after 0.4 s; "press" is plain.
    /// </summary>
    internal static class BindingResolver
    {
        private const float Pulse = 0.12f, IndicatorDelay = 0.15f, Together = 0.2f;

        private static readonly float[] downSince = new float[PhysicalState.Count];
        private static readonly bool[] isModifier = new bool[PhysicalState.Count];
        private static readonly bool[] comboUsed = new bool[PhysicalState.Count];
        private static readonly bool[] holdFired = new bool[PhysicalState.Count];
        private static readonly bool[] bareBlocked = new bool[PhysicalState.Count];
        private static readonly Dictionary<BindAction, float> pulseUntil = new Dictionary<BindAction, float>();
        private static readonly Dictionary<BindAction, bool> active = new Dictionary<BindAction, bool>();
        private static readonly Dictionary<BindAction, bool> prevActive = new Dictionary<BindAction, bool>();

        internal static bool SuppressRightStick, SuppressLeftStick;
        internal static bool Listening => BindingCapture.Listening;

        internal static bool IsActive(BindAction a) => active.TryGetValue(a, out bool v) && v;
        internal static bool JustActivated(BindAction a) => IsActive(a) && !(prevActive.TryGetValue(a, out bool p) && p);

        internal static void Reset()
        {
            pulseUntil.Clear(); active.Clear(); prevActive.Clear();
            System.Array.Clear(comboUsed, 0, comboUsed.Length);
            System.Array.Clear(holdFired, 0, holdFired.Length);
            System.Array.Clear(bareBlocked, 0, bareBlocked.Length);
            KeyItemIndicator.Show(false, null);
        }

        private static bool KeyItemEnabled(out KeyItemSlot slot)
        {
            slot = null;
            try
            {
                CharacterBody body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
                KeyItemSlot s = body ? body.keyItemSlot : null;
                if (s && s.keyItem != KeyItemIndex.None) { slot = s; return true; }
            }
            catch { }
            return false;
        }

        private static bool Enabled(BindingDef d, bool keyItem)
        {
            switch (d.Action)
            {
                case BindAction.UseKeyItem: case BindAction.NextElement: case BindAction.PrevElement: return keyItem;
            }
            return true;
        }

        internal static void Update()
        {
            float now = Time.unscaledTime;
            PhysicalState.Update();
            if (BindingCapture.Listening) { BindingCapture.Tick(); Reset(); return; }

            foreach (var kv in active) prevActive[kv.Key] = kv.Value;

            bool keyItem = KeyItemEnabled(out KeyItemSlot slot);
            List<BindingDef> defs = BindingActions.All;

            // Press timing per physical input.
            for (int i = 1; i < PhysicalState.Count; i++)
            {
                PhysicalInput p = (PhysicalInput)i;
                if (PhysicalState.JustPressed(p)) { downSince[i] = now; comboUsed[i] = false; holdFired[i] = false; }
                if (!PhysicalState.Down(p)) bareBlocked[i] = false;
            }

            // Which inputs act as modifiers right now (only for enabled chords).
            System.Array.Clear(isModifier, 0, isModifier.Length);
            SuppressRightStick = SuppressLeftStick = false;
            foreach (BindingDef d in defs)
            {
                if (!Enabled(d, keyItem) || !d.Current.HasModifier) continue;
                isModifier[(int)d.Current.Modifier] = true;
                if (PhysicalState.Down(d.Current.Modifier))
                {
                    bareBlocked[(int)d.Current.Button] = true;
                    if (PhysicalState.IsDirectional(d.Current.Button))
                    {
                        if (PhysicalState.IsLeft(d.Current.Button)) SuppressLeftStick = true; else SuppressRightStick = true;
                    }
                }
            }

            bool droneGrips = DroneFlight.Active;

            foreach (BindingDef d in defs)
            {
                Chord c = d.Current;
                bool on = false;
                if (!c.IsNone && Enabled(d, keyItem))
                {
                    int b = (int)c.Button;
                    bool down = PhysicalState.Down(c.Button);
                    bool pressed = PhysicalState.JustPressed(c.Button);
                    bool released = PhysicalState.JustReleased(c.Button);
                    float held = down ? now - downSince[b] : 0f;

                    if (c.HasModifier)
                    {
                        bool modDown = PhysicalState.Down(c.Modifier);
                        // Fires when the button is pressed while the modifier is held, OR when the two land
                        // together (button first, modifier within 0.2 s) - "press X and A at once" counts.
                        bool modJustPressed = PhysicalState.JustPressed(c.Modifier);
                        bool pressedUnderMod = (modDown && pressed && downSince[(int)c.Modifier] <= downSince[b])
                                            || (modJustPressed && down && now - downSince[b] < Together);
                        if (pressedUnderMod)
                        {
                            comboUsed[(int)c.Modifier] = true;
                            if (PhysicalState.IsDirectional(c.Button) || c.Mode != ChordMode.Press) pulseUntil[d.Action] = now + Pulse;
                            if (d.Action == BindAction.UseKeyItem) KeyItemIndicator.Flash(slot, "Used");
                            else if (d.Action == BindAction.NextElement) KeyItemIndicator.Flash(slot, "Element  ►");
                            else if (d.Action == BindAction.PrevElement) KeyItemIndicator.Flash(slot, "◄  Element");
                        }
                        if (!PhysicalState.IsDirectional(c.Button) && c.Mode == ChordMode.Press)
                            on = modDown && down && (downSince[(int)c.Modifier] <= downSince[b] || downSince[(int)c.Modifier] - downSince[b] < Together);
                    }
                    else if (isModifier[b])
                    {
                        // Deferred: fires on release when no chord used this button as a modifier, and
                        // no button that pairs with it is still held (that is a combo, not a tap).
                        if (released && !comboUsed[b] && c.Mode != ChordMode.Hold && !PairedButtonDown(c.Button, defs, keyItem)) pulseUntil[d.Action] = now + Pulse;
                        if (c.Mode == ChordMode.Hold) on = down && held >= Chord.HoldTime && !comboUsed[b];
                    }
                    else if (bareBlocked[b])
                    {
                        on = false;
                    }
                    else if (PhysicalState.IsDirectional(c.Button))
                    {
                        if (pressed) pulseUntil[d.Action] = now + Pulse;
                    }
                    else switch (c.Mode)
                    {
                        case ChordMode.Press: on = down; break;
                        case ChordMode.Hold:
                            on = down && held >= Chord.HoldTime;
                            if (on) holdFired[b] = true;
                            break;
                        case ChordMode.Tap:
                            if (released && !holdFired[b] && now - downSince[b] < Chord.HoldTime) pulseUntil[d.Action] = now + Pulse;
                            break;
                    }

                    if (pulseUntil.TryGetValue(d.Action, out float until) && now < until) on = true;

                    if (droneGrips && (c.Button == PhysicalInput.LeftGrip || c.Button == PhysicalInput.RightGrip)) on = false;
                }
                active[d.Action] = on;
            }

            // Mod-level action: switch equipment set (no Rewired element).
            if (JustActivated(BindAction.SwitchEquipment)) SwitchEquipmentSet();

            // Wrist panel while the key item modifier is held.
            Chord use = BindingActions.ChordOf(BindAction.UseKeyItem);
            bool showPanel = keyItem && use.HasModifier && PhysicalState.Down(use.Modifier) && now - downSince[(int)use.Modifier] > IndicatorDelay;
            KeyItemIndicator.Show(showPanel, keyItem ? slot : null);
        }

        private static bool PairedButtonDown(PhysicalInput modifier, List<BindingDef> defs, bool keyItem)
        {
            foreach (BindingDef o in defs)
                if (Enabled(o, keyItem) && o.Current.Modifier == modifier && PhysicalState.Down(o.Current.Button) && !PhysicalState.IsDirectional(o.Current.Button)) return true;
            return false;
        }

        private static void SwitchEquipmentSet()
        {
            try
            {
                CharacterBody body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
                Inventory inv = body ? body.inventory : null;
                if (!inv || inv.accessibleEquipmentSetCount <= 1) return;
                inv.DispatchSwitchToNextEquipmentInSet();
                VRMod.StaticLogger.LogInfo("[VR bindings] Switched equipment set.");
            }
            catch (System.Exception e) { VRMod.StaticLogger.LogWarning("[VR bindings] Switch equipment set: " + e.Message); }
        }

        /// <summary>Writes every action's element. Pause/Scoreboard are skipped in the lobby (Y = Load there).</summary>
        internal static void Apply(CustomController vr, bool lobbyActive)
        {
            bool interact = IsActive(BindAction.Interact), jump = IsActive(BindAction.Jump);
            InteractOrJump.Adjust(ref interact, ref jump);
            foreach (BindingDef d in BindingActions.All)
            {
                if (d.Element < 0) continue;
                if (lobbyActive && (d.Element == 15 || d.Element == 24)) continue;
                bool on = d.Action == BindAction.Interact ? interact : d.Action == BindAction.Jump ? jump : IsActive(d.Action);
                vr.SetButtonValueById(d.Element, on);
            }
            if (SuppressRightStick) { vr.SetAxisValueById(2, 0f); }
            if (SuppressLeftStick) { vr.SetAxisValueById(0, 0f); vr.SetAxisValueById(1, 0f); }
        }

        /// <summary>Hint line for the wrist panel, built from the current chords.</summary>
        internal static string KeyItemHint()
        {
            Chord use = BindingActions.ChordOf(BindAction.UseKeyItem);
            Chord next = BindingActions.ChordOf(BindAction.NextElement);
            Chord prev = BindingActions.ChordOf(BindAction.PrevElement);
            string cycle = next.Button == PhysicalInput.RightStickRight && prev.Button == PhysicalInput.RightStickLeft ? (ViveControllers.IsTrackpadWand ? "R pad left/right" : "R stick left/right")
                : PhysicalState.Label(prev.Button) + " / " + PhysicalState.Label(next.Button);
            if (use.HasModifier)
                return $"hold {PhysicalState.Label(use.Modifier)} + {PhysicalState.Label(use.Button)}: use    {cycle}: element";
            return $"{use.Label()}: use    {cycle}: element";
        }
    }
}
