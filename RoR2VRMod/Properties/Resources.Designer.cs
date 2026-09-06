using System.IO;
using System.Reflection;

namespace VRMod.Properties
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

        private static byte[] _vrmodassets;
        internal static byte[] vrmodassets => _vrmodassets ?? (_vrmodassets = Load("VRMod.Properties.vrmodassets"));
    }
}
