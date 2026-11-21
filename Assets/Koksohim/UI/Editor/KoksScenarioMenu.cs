using UnityEditor;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>Создаёт ассет сценария «Осмотр тоннелей» в Resources/Scenarios, чтобы править задания в Inspector.</summary>
    internal static class KoksScenarioMenu
    {
        [MenuItem("Koks/Scenario/Создать ассет «Осмотр тоннелей» (для правки в Inspector)")]
        static void CreateTunnelAsset()
        {
            const string dir = "Assets/Resources/Scenarios";
            const string path = dir + "/TunnelInspection.asset";
            if (AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path) != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
                Debug.Log("[Koks] Ассет сценария уже есть: " + path);
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Resources", "Scenarios");
            var def = ScenarioDefinition.TunnelInspection();
            def.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(def, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = def;
            Debug.Log("[Koks] Создан ассет сценария: " + path + ". Теперь задания правятся в Inspector; без этого файла берётся встроенный сценарий.");
        }
    }
}
