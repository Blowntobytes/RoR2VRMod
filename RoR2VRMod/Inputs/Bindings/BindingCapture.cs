using System;
using UnityEngine;

namespace VRMod.Inputs.Bindings
{
    /// <summary>
    /// "Click the row, then press what you want." Waits for every control to be released (the A
    /// that selected the row is still down), takes the next press as the button and anything held
    /// at that moment as the modifier, then decides the mode from the release: a stick/pad flick
    /// is a press; a button let go within 0.6 s is a press - or a TAP when another action already
    /// uses that bare button as a hold; held longer than 0.6 s is a hold. Times out after 8 s.
    /// </summary>
    internal static class BindingCapture
    {
        private const float HoldDecision = 0.6f, Timeout = 8f;
        private enum Phase { Idle, WaitRelease, WaitPress, WaitButtonRelease }

        private static Phase phase;
        private static BindingDef target;
        private static PhysicalInput button, modifier;
        private static float started, pressedAt;

        internal static bool Listening => phase != Phase.Idle;
        internal static BindingDef Target => target;
        internal static event Action<BindingDef> Finished;

        internal static string Status
        {
            get
            {
                switch (phase)
                {
                    case Phase.WaitRelease: return "release all buttons…";
                    case Phase.WaitPress: return "press a button (hold another first for a combo)…";
                    case Phase.WaitButtonRelease: return $"{(modifier != PhysicalInput.None ? PhysicalState.Label(modifier) + " + " : "")}{PhysicalState.Label(button)} … keep holding for a hold binding";
                }
                return "";
            }
        }

        internal static void Begin(BindingDef def)
        {
            target = def; phase = Phase.WaitRelease; started = Time.unscaledTime;
            button = modifier = PhysicalInput.None;
            VRMod.StaticLogger.LogInfo($"[VR bindings] Listening for '{def.Name}'.");
        }

        internal static void Cancel()
        {
            BindingDef t = target;
            phase = Phase.Idle; target = null;
            if (t != null) Finished?.Invoke(t);
        }

        /// <summary>Called by the resolver each frame while listening (PhysicalState already updated).</summary>
        internal static void Tick()
        {
            float now = Time.unscaledTime;
            if (now - started > Timeout) { VRMod.StaticLogger.LogInfo("[VR bindings] Timed out."); Cancel(); return; }

            switch (phase)
            {
                case Phase.WaitRelease:
                    if (!PhysicalState.AnyDown) phase = Phase.WaitPress;
                    break;

                case Phase.WaitPress:
                    for (int i = 1; i < PhysicalState.Count; i++)
                    {
                        PhysicalInput p = (PhysicalInput)i;
                        if (!PhysicalState.JustPressed(p)) continue;
                        button = p; pressedAt = now; modifier = PhysicalInput.None;
                        for (int j = 1; j < PhysicalState.Count; j++)
                        {
                            PhysicalInput m = (PhysicalInput)j;
                            if (j != i && PhysicalState.Down(m) && !PhysicalState.JustPressed(m) && !PhysicalState.IsDirectional(m)) { modifier = m; break; }
                        }
                        if (PhysicalState.IsDirectional(button)) { Commit(ChordMode.Press); return; }
                        phase = Phase.WaitButtonRelease;
                        return;
                    }
                    break;

                case Phase.WaitButtonRelease:
                    if (now - pressedAt >= HoldDecision) { Commit(ChordMode.Hold); return; }
                    if (!PhysicalState.Down(button))
                    {
                        ChordMode mode = ChordMode.Press;
                        if (modifier == PhysicalInput.None)
                            foreach (BindingDef o in BindingActions.All)
                                if (o != target && !o.Current.HasModifier && o.Current.Button == button && o.Current.Mode == ChordMode.Hold) { mode = ChordMode.Tap; break; }
                        Commit(mode);
                    }
                    break;
            }
        }

        private static void Commit(ChordMode mode)
        {
            Chord c = new Chord(button, mode, modifier);
            BindingDef t = target;
            phase = Phase.Idle; target = null;
            BindingActions.Set(t, c);
            VRMod.StaticLogger.LogInfo($"[VR bindings] '{t.Name}' = {c}.");
            Finished?.Invoke(t);
        }
    }
}
