using System;

namespace VRMod.Inputs.Bindings
{
    internal enum ChordMode
    {
        Press,  // active while the button is down
        Tap,    // fires on release when held less than HoldTime
        Hold    // active once held for HoldTime
    }

    /// <summary>One binding: optional held modifier + a button + how the button is read.</summary>
    internal struct Chord : IEquatable<Chord>
    {
        internal const float HoldTime = 0.4f;

        internal PhysicalInput Modifier;
        internal PhysicalInput Button;
        internal ChordMode Mode;

        internal Chord(PhysicalInput button, ChordMode mode = ChordMode.Press, PhysicalInput modifier = PhysicalInput.None)
        {
            Button = button; Mode = mode; Modifier = modifier;
        }

        internal bool IsNone => Button == PhysicalInput.None;
        internal bool HasModifier => Modifier != PhysicalInput.None;

        /// <summary>"LeftX + RightA", "LeftY hold", "RightB" - what is stored in VRMod.cfg.</summary>
        public override string ToString()
        {
            if (IsNone) return "None";
            string s = HasModifier ? Modifier + " + " + Button : Button.ToString();
            if (Mode == ChordMode.Tap) s += " tap";
            else if (Mode == ChordMode.Hold) s += " hold";
            return s;
        }

        internal string Label()
        {
            if (IsNone) return "—";
            string s = HasModifier ? PhysicalState.Label(Modifier) + " + " + PhysicalState.Label(Button) : PhysicalState.Label(Button);
            if (Mode == ChordMode.Tap) s += " (tap)";
            else if (Mode == ChordMode.Hold) s += " (hold)";
            return s;
        }

        internal static bool TryParse(string text, out Chord chord)
        {
            chord = default;
            if (string.IsNullOrEmpty(text)) return false;
            string s = text.Trim();
            if (s.Equals("none", StringComparison.OrdinalIgnoreCase)) { chord = default; return true; }

            ChordMode mode = ChordMode.Press;
            string lower = s.ToLowerInvariant();
            if (lower.EndsWith(" hold")) { mode = ChordMode.Hold; s = s.Substring(0, s.Length - 5); }
            else if (lower.EndsWith(" tap")) { mode = ChordMode.Tap; s = s.Substring(0, s.Length - 4); }
            else if (lower.EndsWith(" press")) { s = s.Substring(0, s.Length - 6); }

            PhysicalInput mod = PhysicalInput.None, btn;
            int plus = s.IndexOf('+');
            if (plus >= 0)
            {
                if (!PhysicalState.TryParse(s.Substring(0, plus), out mod)) return false;
                if (!PhysicalState.TryParse(s.Substring(plus + 1), out btn)) return false;
            }
            else if (!PhysicalState.TryParse(s, out btn)) return false;

            if (btn == PhysicalInput.None) return false;
            chord = new Chord(btn, mode, mod);
            return true;
        }

        public bool Equals(Chord o) => Modifier == o.Modifier && Button == o.Button && Mode == o.Mode;
        public override bool Equals(object obj) => obj is Chord c && Equals(c);
        public override int GetHashCode() => ((int)Modifier * 97 + (int)Button) * 7 + (int)Mode;
    }
}
