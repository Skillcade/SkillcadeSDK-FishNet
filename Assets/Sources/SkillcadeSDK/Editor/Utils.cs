using System.IO;
using SkillcadeSDK.Connection;
using SkillcadeSDK.DI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkillcadeSDK.Editor
{
    public static class Utils
    {
        public const string BootstrapScenePath = "Assets/Scenes/BootstrapScene.unity";
        
        public static bool VerifyBootrstapSceneExists()
        {
            if (File.Exists(BootstrapScenePath))
                return true;
            
            Debug.LogError($"BootstrapScene not found at: {BootstrapScenePath}");
            EditorUtility.DisplayDialog("Error", "BootstrapScene not found", "OK");
            return false;
        }
        
        public static void SaveCurrentSceneIfDirty()
        {
            if (!SceneManager.GetActiveScene().isDirty)
                return;
            
            if (EditorUtility.DisplayDialog("Save Scene", "Current scene is dirty. Save?", "Yes", "No"))
                EditorSceneManager.SaveOpenScenes();
        }
        
        public static bool TryLoadBootstrapSceneAndGetScope(out Scene scene, out GameScopeWithAdditionalScenes gameScope)
        {
            scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var rootObjects = scene.GetRootGameObjects();
            gameScope = null;

            foreach (var rootObject in rootObjects)
            {
                gameScope = rootObject.GetComponent<GameScopeWithAdditionalScenes>();
                if (gameScope != null)
                    break;
            }

            if (gameScope == null)
            {
                Debug.LogError("GameScopeWithAdditionalScenes not found in BootstrapScene");
                return false;
            }

            return true;
        }

        public static void ApplyConnectionConfigToGameScope(ConnectionConfig connectionConfig, SerializedObject so)
        {
            if (connectionConfig != null)
            {
                so.FindProperty("_connectionConfig").objectReferenceValue = connectionConfig;
                Debug.Log($"Applied ConnectionConfig: {connectionConfig.name}");
            }
            else
            {
                Debug.LogWarning("BuildConfiguration has no ConnectionConfig assigned.");
            }
        }
    }
}