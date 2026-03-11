using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SkillcadeSDK.Editor
{
    public static class EditorLaunchHelper
    {
        [MenuItem("Assets/Build Configuration/Apply to Scene", false, 10)]
        public static void ApplyToScene()
        {
            var config = Selection.activeObject as BuildConfiguration;
            if (config == null)
            {
                Debug.LogError("Selected object is not a BuildConfiguration");
                return;
            }
            
            ApplyConfiguration(config);
        }

        [MenuItem("Assets/Build Configuration/Apply to Scene", true)]
        public static bool ApplyToSceneValidation()
        {
            return Selection.activeObject is BuildConfiguration;
        }

        private static void ApplyConfiguration(BuildConfiguration config)
        {
            if (!Utils.VerifyBootrstapSceneExists())
                return;
            
            Utils.SaveCurrentSceneIfDirty();

            if (!Utils.TryLoadBootstrapSceneAndGetScope(out var scene, out var gameScope))
                return;

            var so = new SerializedObject(gameScope);
            
            Utils.ApplyConnectionConfigToGameScope(config.ConnectionConfig, so);

            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(gameScope);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", $"Applied configuration: {config.name}", "OK");
        }
    }
}
