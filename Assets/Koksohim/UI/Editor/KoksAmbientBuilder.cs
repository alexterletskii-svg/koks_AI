using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 15. Звуковое окружение. Запускать в FirstScene вне Play. Можно повторно.</summary>
    public static class KoksAmbientBuilder
    {
        [MenuItem("Koks/Build UI/15. Звуковое окружение (zavod_sound)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз."); return; }

            var guids = AssetDatabase.FindAssets("zavod_sound t:AudioClip", new[] { "Assets/Koksohim/Audio" });
            var clips = guids.Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                             .Where(c => c != null).OrderBy(c => c.name).ToList();
            if (clips.Count == 0) { Debug.LogError("[Koks UI] Не найдены zavod_sound*.mp3 в Assets/Koksohim/Audio."); return; }

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
                if (t != null && t.parent == null && t.name == "AmbientSound") Object.DestroyImmediate(t.gameObject);

            var go = new GameObject("AmbientSound");
            var amb = go.AddComponent<AmbientSound>();
            // длинные записи — фон по кругу, короткие (до 30 с) — отдельные звуки поверх
            amb.playlist = clips.Where(c => c.length >= 30f).ToArray();
            amb.accents = clips.Where(c => c.length < 30f).ToArray();
            EditorUtility.SetDirty(amb);

            var scene = go.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
            Debug.Log("[Koks UI] Звуковое окружение собрано: фон " + amb.playlist.Length + " шт., короткие " + amb.accents.Length +
                      " шт. Громкость, перекрытие и паузы — в Inspector у объекта AmbientSound.");
        }
    }
}
