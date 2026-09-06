using UnityEngine.XR;

namespace VRMod.Inputs
{
    internal abstract class BaseInput
    {
        internal XRNode xrNode;
        protected InputDevice device;

        internal BaseInput(XRNode deviceNode)
        {
            this.xrNode = deviceNode;
        }

        /// <summary>
        /// Makes sure <see cref="device"/> points at a live controller before it is polled.
        ///
        /// NOTE: <see cref="InputDevice"/> is a struct. The previous implementation compared it to
        /// null, which is always false for a struct, so the device was never actually fetched and
        /// every reader polled an empty default device forever - the game received no controller
        /// input at all. Checking <see cref="InputDevice.isValid"/> also transparently handles
        /// controllers that go to sleep and come back with a new device id.
        /// </summary>
        protected bool CheckDevice()
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(xrNode);
                return device.isValid;
            }

            return true;
        }

        internal abstract void UpdateValues(Rewired.CustomController vrControllers);
    }
}
