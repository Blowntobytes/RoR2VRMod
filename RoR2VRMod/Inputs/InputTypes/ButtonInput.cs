using Rewired;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRMod.Inputs
{
    internal class ButtonInput : BaseInput
    {
        protected InputHelpers.Button button;
        protected int buttonID;
        private readonly ViveOverride viveOverride;

        internal int ElementId => buttonID;

        internal ButtonInput(XRNode deviceNode, InputHelpers.Button button, int buttonID, ViveOverride viveOverride = ViveOverride.None) : base(deviceNode)
        {
            this.button = button;
            this.buttonID = buttonID;
            this.viveOverride = viveOverride;
        }

        protected virtual bool PollButtonState()
        {
            // HTC Vive / original WMR wands have no A/B/X/Y: remap to trackpad regions / menu button.
            if (viveOverride != ViveOverride.None && ViveControllers.IsTrackpadWand)
                return PollViveState();

            if (device.IsPressed(button, out bool value))
                return value;

            return false;
        }

        private bool PollViveState()
        {
            switch (viveOverride)
            {
                case ViveOverride.MenuButton:
                    return device.IsPressed(InputHelpers.Button.MenuButton, out bool pressed) && pressed;
                case ViveOverride.PadClickUp:
                    return ViveControllers.PadClickUp(device);
                case ViveOverride.PadClickDown:
                    return ViveControllers.PadClickDown(device);
            }
            return false;
        }

        internal override void UpdateValues(CustomController vrController)
        {
            if (!CheckDevice()) return;

            vrController.SetButtonValueById(buttonID, PollButtonState());
        }
    }
}
