using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 16. Музыка в MenuScene. Запускать вне Play: сцена меню открывается, в неё добавляется
    /// объект MenuMusic, сцена сохраняется и открывается прежняя сцена. Можно повторно.
    /// </summary>
    public static class KoksMenuMusicBuilder
    {
        [MenuItem("Koks/Build UI/16. Музыка в сцене меню (menuscenes_sound)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз."); return; }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Koksohim/Audio/menuscenes_sound.mp3");
            if (clip == null) { Debug.LogError("[Koks UI] Не найден Assets/Koksohim/Audio/menuscenes_sound.mp3."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string back = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity", OpenSceneMode.Single);

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
                if (t != null && t.parent == null && t.name == "MenuMusic") Object.DestroyImmediate(t.gameObject);

            var go = new GameObject("MenuMusic");
            go.AddComponent<AudioSource>();
            var m = go.AddComponent<SceneMusic>();
            m.clip = clip;
            m.volume = 0.12f;
            EditorUtility.SetDirty(m);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Музыка добавлена в MenuScene (громкость 12 %, правится в Inspector у MenuMusic).");

            if (!string.IsNullOrEmpty(back) && back != scene.path) EditorSceneManager.OpenScene(back, OpenSceneMode.Single);
        }
    }
}
