using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace SkillcadeSDK.FishNetAdapter.Editor
{
    /// <summary>
    /// Automatically detects installed FishNet transports and sets scripting define symbols.
    /// 
    /// - SKILLCADE_UTP: Set when the FishyUnityTransport assembly is present (asset-folder addon).
    /// - SKILLCADE_WEBRTC: Handled by versionDefines in SkillcadeSDK-FishNet-WebRTC.asmdef (auto).
    /// 
    /// This script only manages SKILLCADE_UTP since FishyUnityTransport is not a UPM package
    /// and cannot use versionDefines for auto-detection.
    /// </summary>
    [InitializeOnLoad]
    public static class TransportDefineManager
    {
        private const string UTP_ASSEMBLY_NAME = "FishyUnityTransport";
        private const string UTP_DEFINE = "SKILLCADE_UTP";

        static TransportDefineManager()
        {
            UpdateDefines();
        }

        private static void UpdateDefines()
        {
            bool utpPresent = IsAssemblyPresent(UTP_ASSEMBLY_NAME);

            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentDefines);
            var defines = new List<string>(currentDefines);

            bool changed = false;

            if (utpPresent && !defines.Contains(UTP_DEFINE))
            {
                defines.Add(UTP_DEFINE);
                changed = true;
            }
            else if (!utpPresent && defines.Contains(UTP_DEFINE))
            {
                defines.Remove(UTP_DEFINE);
                changed = true;
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines.ToArray());
            }
        }

        private static bool IsAssemblyPresent(string assemblyName)
        {
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            return assemblies.Any(a => a.name == assemblyName);
        }
    }
}
