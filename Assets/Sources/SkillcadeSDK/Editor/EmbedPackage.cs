using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace SkillcadeSDK.Editor
{
    public static class EmbedPackage
    {
        [MenuItem("Assets/Embed Package", false, 1000000)]
        private static void EmbedPackageMenuItem()
        {
            var selection = Selection.activeObject;
            var packageName = Path.GetFileName(AssetDatabase.GetAssetPath(selection));

            Debug.Log($"[Embed package] Embedding package '{packageName}' into the project.");
            Client.Embed(packageName);
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Embed Package", true)]
        private static bool EmbedPackageValidation()
        {
            var selection = Selection.activeObject;
            if (selection == null)
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(selection);
            var folder = Path.GetDirectoryName(path);
            
            // We only deal with direct folders under Packages/
            return folder == "Packages";
        }
    }
}