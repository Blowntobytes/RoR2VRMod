using System.IO;
using System.Reflection;

namespace VRPatcher.Properties
{
    /// <summary>
    /// Embedded resources are stored as raw manifest resources (no .resources container) so the
    /// project can be built with a bare C# compiler and no resgen step.
    /// </summary>
    internal static class Resources
    {
        private static byte[] Load(string name)
        {
            Assembly asm = typeof(Resources).Assembly;
            using (Stream s = asm.GetManifestResourceStream(name))
            {
                if (s == null)
                    throw new FileNotFoundException("Embedded resource not found: " + name);
                using (MemoryStream ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        internal static byte[] openxr_loader => Load("VRPatcher.Plugins.openxr_loader.dll");
        internal static byte[] UnityOpenXR => Load("VRPatcher.Plugins.UnityOpenXR.dll");
        internal static byte[] UnitySubsystemsManifest => Load("VRPatcher.Dependencies.UnitySubsystemsManifest.json");
    }
}
