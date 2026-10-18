using RoR2;
using VRMod.Inputs.Bindings;

namespace VRMod.Inputs
{
    /// <summary>
    /// VR setting "A: Jump when nothing to interact with" (2.23.51). On the Interact press edge, if
    /// the game's InteractionDriver has nothing in reach, that press becomes a Jump instead (for as
    /// long as the chord stays active, so a jump can't turn into an interaction mid-air). Decided
    /// BEFORE the elements are written, because Rewired latches a press the instant an element goes
    /// true. Jump's own binding keeps working regardless.
    /// </summary>
    internal static class InteractOrJump
    {
        private static bool asJump;

        internal static void Poll()
        {
            if (!ModConfig.InteractOrJump.Value) { asJump = false; return; }
            if (BindingResolver.JustActivated(BindAction.Interact)) asJump = NothingInReach();
            if (!BindingResolver.IsActive(BindAction.Interact)) asJump = false;
        }

        /// <summary>Lets the resolver swap Interact for Jump on this frame.</summary>
        internal static void Adjust(ref bool interact, ref bool jump)
        {
            if (!asJump) return;
            interact = false;
            jump = true;
        }

        private static bool NothingInReach()
        {
            try
            {
                CharacterBody body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
                if (!body) return false;
                InteractionDriver driver = body.GetComponent<InteractionDriver>();
                if (!driver) return true;
                return driver.FindBestInteractableObject() == null;
            }
            catch { return false; }
        }
    }
}
