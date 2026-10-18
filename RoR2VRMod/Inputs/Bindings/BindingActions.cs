using BepInEx.Configuration;
using System;
using System.Collections.Generic;

namespace VRMod.Inputs.Bindings
{
    /// <summary>Everything a player can rebind. Menu navigation and the sticks are not on this list.</summary>
    internal enum BindAction
    {
        Interact, Jump, Equipment, Sprint, Ping,
        Primary, Secondary, Utility, Special,
        Pause, Scoreboard,
        UseKeyItem, NextElement, PrevElement,
        SwitchEquipment,   // mod-level: no Rewired element, calls Inventory.DispatchSwitchToNextEquipmentInSet
    }

    internal sealed class BindingDef
    {
        internal BindAction Action;
        internal int Element;              // Rewired element id the resolver writes
        internal string Name;              // settings row label
        internal string Description;       // settings hover text
        internal Chord Default, WandDefault;
        internal ConfigEntry<string> Entry;
        internal Chord Current;
    }

    /// <summary>The table of bindable actions, their defaults and their config entries.</summary>
    internal static class BindingActions
    {
        internal static readonly List<BindingDef> All = new List<BindingDef>();
        private static readonly Dictionary<BindAction, BindingDef> byAction = new Dictionary<BindAction, BindingDef>();
        internal static event Action Changed;

        internal static BindingDef Get(BindAction a) => byAction[a];
        internal static Chord ChordOf(BindAction a) => byAction[a].Current;

        private static void Define(BindAction a, int element, string name, string desc, Chord def, Chord wand)
        {
            BindingDef d = new BindingDef { Action = a, Element = element, Name = name, Description = desc, Default = def, WandDefault = wand };
            All.Add(d); byAction[a] = d;
        }

        static BindingActions()
        {
            var P = ChordMode.Press; var T = ChordMode.Tap; var H = ChordMode.Hold;
            Define(BindAction.Primary,    8,  "Primary skill",   "Fires the primary skill (right trigger by default). Left/right dominant hand still swaps the triggers and grips on top of this.",
                new Chord(PhysicalInput.RightTrigger), new Chord(PhysicalInput.RightTrigger));
            Define(BindAction.Secondary,  9,  "Secondary skill", "Fires the secondary skill.",
                new Chord(PhysicalInput.LeftTrigger), new Chord(PhysicalInput.LeftTrigger));
            Define(BindAction.Utility,    10, "Utility skill",   "Fires the utility skill.",
                new Chord(PhysicalInput.LeftGrip), new Chord(PhysicalInput.LeftGrip));
            Define(BindAction.Special,    11, "Special skill",   "Fires the special skill.",
                new Chord(PhysicalInput.RightGrip), new Chord(PhysicalInput.RightGrip));
            Define(BindAction.Interact,   6,  "Interact",        "Activates chests, shrines, the teleporter and other interactables.",
                new Chord(PhysicalInput.RightA), new Chord(PhysicalInput.RightPadDown));
            Define(BindAction.Jump,       7,  "Jump",            "Jump.",
                new Chord(PhysicalInput.RightB), new Chord(PhysicalInput.RightPadUp));
            Define(BindAction.Equipment,  12, "Use equipment",   "Uses the equipment item.",
                new Chord(PhysicalInput.LeftX), new Chord(PhysicalInput.LeftPadUp));
            Define(BindAction.Sprint,     13, "Sprint",          "Sprint (or turn on Auto Sprint).",
                new Chord(PhysicalInput.LeftStickClick), new Chord(PhysicalInput.LeftPadDown));
            Define(BindAction.Ping,       14, "Ping",            "Pings what you are looking at.",
                new Chord(PhysicalInput.RightStickClick), new Chord(PhysicalInput.RightMenu));
            Define(BindAction.Pause,      24, "Pause menu",      "Opens the pause menu. A tap binding shares its button with a hold binding (scoreboard by default).",
                new Chord(PhysicalInput.LeftY, T), new Chord(PhysicalInput.LeftMenu, T));
            Define(BindAction.Scoreboard, 15, "Scoreboard",      "Shows the scoreboard / profile while held.",
                new Chord(PhysicalInput.LeftY, H), new Chord(PhysicalInput.LeftMenu, H));
            Define(BindAction.SwitchEquipment, -1, "Switch equipment set", "With an item that gives a second equipment set, switches to the next set (the game's own combo is Interact held + Equipment; this is the same pair, in either order).",
                new Chord(PhysicalInput.RightA, P, PhysicalInput.LeftX), new Chord(PhysicalInput.RightPadDown, P, PhysicalInput.LeftPadUp));
            Define(BindAction.UseKeyItem, 33, "Use key item",    "Uses the carried key item (Hallowed Concepts). A chord with a modifier: hold the first button, press the second. The modifier's own action then fires when it is released without a combo.",
                new Chord(PhysicalInput.RightB, P, PhysicalInput.LeftX), new Chord(PhysicalInput.RightPadUp, P, PhysicalInput.LeftPadUp));
            Define(BindAction.NextElement, 34, "Next element",   "Key item: next element.",
                new Chord(PhysicalInput.RightStickRight, P, PhysicalInput.LeftX), new Chord(PhysicalInput.RightStickRight, P, PhysicalInput.LeftPadUp));
            Define(BindAction.PrevElement, 35, "Previous element", "Key item: previous element.",
                new Chord(PhysicalInput.RightStickLeft, P, PhysicalInput.LeftX), new Chord(PhysicalInput.RightStickLeft, P, PhysicalInput.LeftPadUp));
        }

        internal static Chord DefaultFor(BindingDef d) => ViveControllers.IsTrackpadWand ? d.WandDefault : d.Default;

        /// <summary>Binds one config entry per action in "[Bindings]". Called from ModConfig.Init.</summary>
        internal static void BindConfig(ConfigFile file)
        {
            foreach (BindingDef d in All)
            {
                d.Entry = file.Bind<string>("Bindings", d.Name, "default",
                    d.Description + " Format: Button, Modifier + Button, with an optional 'tap' or 'hold' suffix; 'default' uses the built-in layout for your controller. " +
                    "Buttons: LeftX LeftY RightA RightB LeftTrigger RightTrigger LeftGrip RightGrip LeftStickClick RightStickClick LeftMenu RightMenu " +
                    "LeftStickUp/Down/Left/Right RightStickUp/Down/Left/Right LeftPadUp/Down/Left/Right RightPadUp/Down/Left/Right.");
                d.Entry.SettingChanged += (s, e) => Reload(d);
            }
            ReloadAll();
        }

        internal static void ReloadAll()
        {
            foreach (BindingDef d in All) Resolve(d);
            Changed?.Invoke();
        }

        private static void Reload(BindingDef d)
        {
            Resolve(d);
            Changed?.Invoke();
        }

        private static void Resolve(BindingDef d)
        {
            string text = d.Entry != null ? d.Entry.Value : "default";
            if (string.IsNullOrEmpty(text) || text.Trim().Equals("default", StringComparison.OrdinalIgnoreCase))
                d.Current = DefaultFor(d);
            else if (Chord.TryParse(text, out Chord c))
                d.Current = c;
            else
            {
                VRMod.StaticLogger.LogWarning($"[VR bindings] '{d.Name} = {text}' is not a valid binding; using the default.");
                d.Current = DefaultFor(d);
            }
        }

        internal static void Set(BindingDef d, Chord c)
        {
            d.Current = c;
            if (d.Entry != null) d.Entry.Value = c.Equals(DefaultFor(d)) ? "default" : c.ToString();
            else Changed?.Invoke();
        }

        internal static void ResetAll()
        {
            foreach (BindingDef d in All) { d.Current = DefaultFor(d); if (d.Entry != null) d.Entry.Value = "default"; }
            Changed?.Invoke();
        }

        /// <summary>Other actions that share this chord's bare button with the same modifier and mode.</summary>
        internal static List<BindingDef> ConflictsWith(BindingDef d)
        {
            var list = new List<BindingDef>();
            if (d.Current.IsNone) return list;
            foreach (BindingDef o in All)
                if (o != d && o.Current.Equals(d.Current)) list.Add(o);
            return list;
        }
    }
}
