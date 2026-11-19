using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 10. Окно при входе в газовый тоннель (FirstScene).</summary>
    public static class KoksArrivalBuilder
    {
        [MenuItem("Koks/Build UI/10. Окно при входе в газовый тоннель (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
                return;
            }

            var tp = Object.FindFirstObjectByType<FadeTeleport>(FindObjectsInactive.Include);
            if (tp == null)
            {
                Debug.LogError("[Koks UI] Не найден FadeTeleport. Откройте FirstScene.");
                return;
            }
            var so = new SerializedObject(tp);
            var prop = so.FindProperty("teleportLoc3");
            if (prop == null)
            {
                Debug.LogError("[Koks UI] В FadeTeleport нет поля teleportLoc3.");
                return;
            }
            Vector3 loc = prop.vector3Value;

            KoksUIBuilder.PrepareAssets();
            var heading = KoksUIBuilder.LoadFont("RobotoCondensed-ExtraBold");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            if (heading == null || body == null || medium == null)
            {
                Debug.LogError("[Koks UI] Нет шрифтов TMP.");
                return;
            }

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.parent == null && t.name == "TunnelArrival") Object.DestroyImmediate(t.gameObject);

            var win = KoksStepBuilder.CreateWindow("CanvasTunnel", loc + new Vector3(0f, 1.5f, 0f), heading, body, medium);
            win.stepText = "Газовый тоннель";
            win.title = "Вы в газовом тоннеле";
            win.description = "Здесь нужно осмотреть оборудование. Идите к синим кругам: у каждого будет информация о том, что и как осматривать.";
            win.checksHeader = "Как действовать";
            win.checks = new[]
            {
                "Подойдите к синему кругу",
                "Прочитайте, что нужно осмотреть",
                "Наведите луч на объект и держите 2 секунды"
            };
            win.buttonText = "Понятно";
            win.Apply();
            win.gameObject.SetActive(false);

            var go = new GameObject("TunnelArrival");
            var tr = go.AddComponent<ArrivalTrigger>();
            tr.window = win;
            tr.position = loc;
            EditorUtility.SetDirty(tr);

            Selection.activeGameObject = win.gameObject;
            var scene = go.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Окно входа в тоннель собрано: CanvasTunnel, точка входа " + loc + ". Тексты — в Inspector, компонент Step Window.");
        }
    }
}
