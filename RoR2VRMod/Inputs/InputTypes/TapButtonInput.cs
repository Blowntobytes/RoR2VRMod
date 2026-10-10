using Rewired;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRMod.Inputs
{
    /// <summary>
    /// 2.23.25: fires a short pulse on RELEASE, and only when the button was held for less than
    /// HoldThreshold. Paired with HoldableButtonInput on the same physical button (Y): a quick
    /// press-and-release opens the pause menu, holding past the threshold shows the scoreboard
    /// instead - the menu no longer pops open on the way to a hold.
    /// </summary>
    internal class TapButtonInput : ButtonInput
    {
        internal const float HoldThreshold = 0.4f; // must match HoldableButtonInput
        private const float PulseLength = 0.05f;

        private bool prev;
        private float pressTime;
        private float pulseUntil;

        internal TapButtonInput(XRNode deviceNode, InputHelpers.Button button, int buttonID, ViveOverride viveOverride = ViveOverride.None) : base(deviceNode, button, buttonID, viveOverride) { }

        internal override void UpdateValues(CustomController vrController)
        {
            if (!CheckDevice()) return;

            float now = Time.realtimeSinceStartup;
            bool value = PollButtonState();

            if (value && !prev) pressTime = now;
            if (!value && prev && now - pressTime < HoldThreshold) pulseUntil = now + PulseLength;

            vrController.SetButtonValueById(buttonID, now < pulseUntil);
            prev = value;
        }
    }
}
