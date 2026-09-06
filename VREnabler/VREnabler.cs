using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace VRPatcher
{
    public static class VRDependenciesPatcher
    {
        internal static string VRPatcherPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string ManagedPath => Paths.ManagedPath;
        internal static string PluginsPath => Path.Combine(ManagedPath, "../Plugins/x86_64");
        internal static string SubsystemsPath => Path.Combine(ManagedPath, "../UnitySubsystems");
        internal static string OpenXRSubsystemsPath => Path.Combine(SubsystemsPath, "UnityOpenXR");

        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("VRDependenciesPatcher");
        

        /// <summary>
        /// Called from BepInEx while patching, our entry point for patching.
        /// Do not change the method name as it is identified by BepInEx. Method must remain public.
        /// </summary>
        [Obsolete("Should not be used!", true)]
        public static void Initialize()
        {
            if (!Directory.Exists(SubsystemsPath))
                Directory.CreateDirectory(SubsystemsPath);

            if (!Directory.Exists(OpenXRSubsystemsPath))
                Directory.CreateDirectory(OpenXRSubsystemsPath);

            Logger.LogInfo("Copying subsystems...");
            Logger.LogInfo($"ManagedPath={ManagedPath}");
            Logger.LogInfo($"PluginsPath={PluginsPath}");
            Logger.LogInfo($"SubsystemsPath={SubsystemsPath}");
            Logger.LogInfo($"OpenXRSubsystemsPath={OpenXRSubsystemsPath}");

            string openXRSubsystemPath = Path.Combine(OpenXRSubsystemsPath, "UnitySubsystemsManifest.json");
            byte[] openXRSubsystemFile = Properties.Resources.UnitySubsystemsManifest;

            if (!CopyFile(openXRSubsystemPath, openXRSubsystemFile, true))
            {
                Logger.LogInfo($"UnitySubsystemsManifest.json -> {openXRSubsystemPath}: already present.");
            }
            else
            {
                Logger.LogInfo($"UnitySubsystemsManifest.json -> {openXRSubsystemPath}: copied ({openXRSubsystemFile.Length} bytes).");
            }

            Logger.LogInfo("Copying libraries...");

            string openXRPluginPath = Path.Combine(PluginsPath, "UnityOpenXR.dll");
            byte[] openXRPluginFile = Properties.Resources.UnityOpenXR;

            if (!CopyFile(openXRPluginPath, openXRPluginFile, false))
            {
                Logger.LogInfo($"UnityOpenXR.dll -> {openXRPluginPath}: already present.");
            }
            else
            {
                Logger.LogInfo($"UnityOpenXR.dll -> {openXRPluginPath}: copied ({openXRPluginFile.Length} bytes).");
            }

            string openXRLoaderPath = Path.Combine(PluginsPath, "openxr_loader.dll");
            byte[] openXRLoaderFile = Properties.Resources.openxr_loader;

            if (!CopyFile(openXRLoaderPath, openXRLoaderFile, false))
            {
                Logger.LogInfo($"openxr_loader.dll -> {openXRLoaderPath}: already present.");
            }
            else
            {
                Logger.LogInfo($"openxr_loader.dll -> {openXRLoaderPath}: copied ({openXRLoaderFile.Length} bytes).");
            }

            Logger.LogInfo($"Copy complete. UnityOpenXR.dll present: {File.Exists(openXRPluginPath)}, openxr_loader.dll present: {File.Exists(openXRLoaderPath)}, manifest present: {File.Exists(openXRSubsystemPath)}");

            /*
            if (!VREnabler.EnableVROptions(Path.Combine(VREnabler.ManagedPath, "../globalgamemanagers")))
            {
                return;
            }
            VREnabler.Logger.LogInfo("Checking for VR plugins...");
            string pluginsPath = Path.Combine(VREnabler.PluginsPath, "x86_64");
            if (!Directory.Exists(pluginsPath))
            {
                pluginsPath = VREnabler.PluginsPath;
            }

            string[] plugins = new string[]
            {
                "AudioPluginOculusSpatializer.dll",
                "openvr_api.dll",
                "OVRGamepad.dll",
                "OVRPlugin.dll",
                "LIV_Bridge.dll",
                "ShockWaveIMU.dll"
            };
            string[] managedLibraries = new string[]
            {
                "SteamVR.dll",
                "SteamVR_Actions.dll",
                "ShockwaveManager.dll",
                "Bhaptics.Tact.dll"
            };

            bool copyPluginsResult = CopyFiles(pluginsPath, plugins, "Plugins.");
            bool copyManagedLibrariesResult = CopyFiles(VREnabler.ManagedPath, managedLibraries, "Plugins.");

            if (copyPluginsResult || copyManagedLibrariesResult)
                VREnabler.Logger.LogInfo("Successfully copied VR plugins!");
            else
                VREnabler.Logger.LogInfo("VR plugins already present");

            VREnabler.Logger.LogInfo("Checking for binding files...");


            if (!Directory.Exists(SteamVRPath))
            {
                try
                {
                    Directory.CreateDirectory(SteamVRPath);
                }
                catch (Exception e)
                {
                    VREnabler.Logger.LogError("Could not create SteamVR folder in StreamingAssets: " + e.Message);
                    VREnabler.Logger.LogError(e.StackTrace);
                    return;
                }
            }

            string[] bindingFiles = new string[]
            {
                "actions.json",
                "binding_holographic_hmd.json",
                "binding_index_hmd.json",
                "binding_rift.json",
                "binding_vive.json",
                "binding_vive_cosmos.json",
                "binding_vive_pro.json",
                "binding_vive_tracker_camera.json",
                "bindings_holographic_controller.json",
                "bindings_knuckles.json",
                "bindings_logitech_stylus.json",
                "bindings_oculus_touch.json",
                "bindings_vive_controller.json",
                "bindings_vive_cosmos_controller.json"
            };

            if (CopyFiles(SteamVRPath, bindingFiles, "Binds.", true))
                VREnabler.Logger.LogInfo("Successfully copied binding files!");
            else
                VREnabler.Logger.LogInfo("Binding files already present");*/
        }

        private static bool CopyFile(string destination, byte[] data, bool replaceIfDifferent)
        {
            if (File.Exists(destination))
            {
                if (replaceIfDifferent)
                {
                    SHA256 sha = SHA256.Create();

                    byte[] sourceHash = sha.ComputeHash(data);
                    byte[] destHash = sha.ComputeHash(File.ReadAllBytes(destination));

                    if (sourceHash.SequenceEqual(destHash))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            File.WriteAllBytes(destination, data);

            return true;
        }

        private static bool CopyFiles(string destinationPath, string[] fileNames, string embedFolder, bool replaceIfDifferent = false)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(destinationPath);
            FileInfo[] files = directoryInfo.GetFiles();
            bool flag = false;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string name = executingAssembly.GetName().Name;
            string[] array = fileNames;
            for (int i = 0; i < array.Length; i++)
            {
                string fileName = array[i];
                if (!Array.Exists<FileInfo>(files, (FileInfo file) => fileName == file.Name))
                {
                    flag = true;
                    using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(name + "." + embedFolder + fileName))
                    {
                        using (FileStream fileStream = new FileStream(Path.Combine(directoryInfo.FullName, fileName), FileMode.Create, FileAccess.ReadWrite, FileShare.Delete))
                        {
                            Logger.LogInfo("Copying " + fileName);
                            manifestResourceStream.CopyTo(fileStream);
                        }
                    }
                }
                else if (replaceIfDifferent)
                {
                    string resourceFileContent;
                    using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(name + "." + embedFolder + fileName))
                    {
                        using (StreamReader reader = new StreamReader(manifestResourceStream))
                        {
                            resourceFileContent = reader.ReadToEnd();
                        }
                    }

                    FileInfo installedFile = files.First(file => file.Name == fileName);
                    string installedFileContent = File.ReadAllText(@installedFile.FullName);

                    if (resourceFileContent != installedFileContent)
                    {
                        flag = true;
                        Logger.LogInfo("Overwriting " + fileName);
                        File.WriteAllText(installedFile.FullName, resourceFileContent);
                    }
                }
            }
            return flag;
        }

        /// <summary>
        /// For BepInEx to identify your patcher as a patcher, it must match the patcher contract as outlined in the BepInEx docs:
        /// https://bepinex.github.io/bepinex_docs/v5.0/articles/dev_guide/preloader_patchers.html#patcher-contract
        /// It must contain a list of managed assemblies to patch as a public static <see cref="IEnumerable{T}"/> property named TargetDLLs
        /// </summary>
        [Obsolete("Should not be used!", true)]
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        /// <summary>
        /// For BepInEx to identify your patcher as a patcher, it must match the patcher contract as outlined in the BepInEx docs:
        /// https://bepinex.github.io/bepinex_docs/v5.0/articles/dev_guide/preloader_patchers.html#patcher-contract
        /// It must contain a public static void method named Patch which receives an <see cref="AssemblyDefinition"/> argument,
        /// which patches each of the target assemblies in the TargetDLLs list.
        /// 
        /// We don't actually need to patch any of the managed assemblies, so we are providing an empty method here.
        /// </summary>
        /// <param name="ad"></param>
        [Obsolete("Should not be used!", true)]
        public static void Patch(AssemblyDefinition ad) { }
    }
}
