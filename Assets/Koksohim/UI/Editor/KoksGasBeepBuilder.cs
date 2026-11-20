using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 18. Писк газоанализатора (FirstScene). Можно повторно.</summary>
    public static class KoksGasBeepBuilder
    {
        [MenuItem("Koks/Build UI/18. Писк газоанализатора (тоннель и клапаны)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string back = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.GetActiveScene().path.EndsWith("FirstScene.unity"))
                EditorSceneManager.OpenScene("Assets/Scenes/FirstScene.unity", OpenSceneMode.Single);
            Run();
        }

        static void Run()
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var old = all.FirstOrDefault(t => t.name == "GasAnalyzerBeep");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var clips = new[] { "Assets/Koksohim/Audio/gazoanalizator_sound_1.mp3", "Assets/Koksohim/Audio/gazoanalizator_sound_2.mp3" }
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
            if (clips.Length == 0) { Debug.LogError("[Koks UI] Нет gazoanalizator_sound_*.mp3 в Assets/Koksohim/Audio (Pull)."); return; }
            var arr = Object.FindFirstObjectByType<ArrivalTrigger>(FindObjectsInactive.Include);
            var step2 = all.FirstOrDefault(t => t.name == "PointPosition_Step2");
            var go = new GameObject("GasAnalyzerBeep");
            var b = go.AddComponent<GasAnalyzerBeep>();
            b.clips = clips; b.tunnelTrigger = arr; b.valvesPoint = step2;
            if (arr == null) Debug.LogWarning("[Koks UI] Не найден TunnelArrival: писка не будет. Запустите меню 'Прибытие в тоннель'.");
            if (step2 == null) Debug.LogWarning("[Koks UI] Не найден PointPosition_Step2: писк будет одной громкости.");
            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);
            Debug.Log("[Koks UI] Писк газоанализатора добавлен (" + clips.Length + " зап.). Громкость — в Inspector: Tunnel Volume / Valves Volume.");
        }
    }
}
