using Rewired;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRMod.Inputs
{
    internal class HoldableButtonInput : ButtonInput
    {
        private float holdablePressTime;
        private bool prevHoldableValue;

        internal HoldableButtonInput(XRNode deviceNode, InputHelpers.Button button, int buttonID, ViveOverride viveOverride = ViveOverride.None) : base(deviceNode, button, buttonID, viveOverride) { }

        internal override void UpdateValues(CustomController vrController)
        {
            if (!CheckDevice()) return;

            bool holdableValue = PollButtonState();

            if (holdableValue && !prevHoldableValue)
                holdablePressTime = Time.realtimeSinceStartup;

            vrController.SetButtonValueById(buttonID, holdableValue && Time.realtimeSinceStartup - holdablePressTime > 0.4f);

            prevHoldableValue = holdableValue;
        }
    }
}
