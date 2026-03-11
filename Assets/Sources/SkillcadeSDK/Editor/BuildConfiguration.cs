using SkillcadeSDK.Connection;
using UnityEditor;
using UnityEngine;

namespace SkillcadeSDK.Editor
{
    [CreateAssetMenu(fileName = "BuildConfiguration", menuName = "Configs/Build Configuration")]
    public class BuildConfiguration : ScriptableObject
    {
        [Header("Runtime Configuration")]
        [Tooltip("The connection configuration to use for this build.")]
        public ConnectionConfig ConnectionConfig;

        [Tooltip("Scenes to be loaded additively by GameScope (Runtime).")]
        public string[] SceneNames;

        [Header("Build Settings")]
        [Tooltip("Name of the built executable file (e.g., 'Server.exe' or 'Client').")]
        public string BuildFileName;

        [Tooltip("Name of the output folder (e.g., 'WindowsServer').")]
        public string BuildFolderName;

        [Tooltip("The build target platform.")]
        public BuildTarget BuildTarget;

        [Tooltip("The build subtarget (e.g., Server or Player).")]
        public StandaloneBuildSubtarget BuildSubtarget;

        [Tooltip("Additional scenes to include in the build but not load automatically.")]
        public string[] ExtraBuildScenes;

        [Tooltip("Add SKILLCADE_DEBUG to scripting defines")]
        public bool UseSkillcadeDebug;
        
        [Tooltip("If true, the build will be a development build.")]
        public bool DevelopmentBuild;
    }
}
