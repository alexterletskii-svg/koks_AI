using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 9. Окно «клапаны осмотрены» и следующий синий круг (FirstScene).</summary>
    public static class KoksStepFlowBuilder
    {
        const string CirclePrefab = "Assets/Koksohim/Prefabs/PointPosition.prefab";

        [MenuItem("Koks/Build UI/9. Окно «клапаны осмотрены» и следующий круг (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
                return;
            }

            var hud = Object.FindFirstObjectByType<InspectHud>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogError("[Koks UI] Нет InspectHud. Сначала запустите меню 8.");
                return;
            }

            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var circle1 = all.FirstOrDefault(t => t.name == "PointPosition" && t.parent == null);
            if (circle1 == null)
            {
                Debug.LogError("[Koks UI] Не найден первый круг PointPosition.");
                return;
            }

            KoksUIBuilder.PrepareAssets();
            var heading = KoksUIBuilder.LoadFont("RobotoCondensed-ExtraBold");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            if (heading == null || body == null || medium == null)
            {
                Debug.LogError("[Koks UI] Нет шрифтов TMP.");
                return;
            }

            // уборка дублей окна шага 1 (раньше меню 7 оставляло копии)
            var step1 = circle1.GetComponent<StepMarker>() != null ? circle1.GetComponent<StepMarker>().window : null;
            foreach (var t in all)
                if (t != null && t.parent == null && t.name == "CanvasStep1" && (step1 == null || t.gameObject != step1.gameObject))
                    Object.DestroyImmediate(t.gameObject);

            // следующий круг: рядом с серединой точек осмотра, на высоте первого круга
            var pts = hud.points.Where(p => p != null).Select(p => p.transform.position).ToList();
            Vector3 centre = pts.Count > 0 ? pts.Aggregate(Vector3.zero, (a, b) => a + b) / pts.Count : circle1.position;
            Vector3 pos = new Vector3(centre.x, circle1.position.y, centre.z);

            foreach (var n in new[] { "PointPosition_Step2", "CanvasStep1Done", "CanvasStep2" })
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t != null && t.parent == null && t.name == n) Object.DestroyImmediate(t.gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CirclePrefab);
            if (prefab == null)
            {
                Debug.LogError("[Koks UI] Нет префаба " + CirclePrefab);
                return;
            }
            var circle2 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            circle2.name = "PointPosition_Step2";
            circle2.transform.position = pos;
            circle2.transform.rotation = circle1.rotation;

            // окно «Шаг 1 выполнен»
            var done = KoksStepBuilder.CreateWindow("CanvasStep1Done", pos + new Vector3(0f, 1.5f, 0f), heading, body, medium);
            done.stepText = "Шаг 1 выполнен";
            done.title = "Клапаны осмотрены";
            done.description = "Вы осмотрели оба клапана: крышки, внутренности, тяги и цепочки. Дальше — следующий пункт обхода.";
            done.checksHeader = "Что дальше";
            done.checks = new[] { "Подойдите к синему кругу", "", "" };
            done.buttonText = "Понятно";
            done.Apply();
            done.gameObject.SetActive(false);

            // окно следующего шага (заглушка, текст заменит автор)
            var step2 = KoksStepBuilder.CreateWindow("CanvasStep2", pos + new Vector3(0f, 1.5f, 0f), heading, body, medium);
            step2.stepText = "Шаг 2";
            step2.title = "Следующее оборудование";
            step2.description = "Здесь будет описание следующего оборудования и что в нём нужно осмотреть.";
            step2.checksHeader = "Что проверить";
            step2.buttonText = "Приступить к осмотру";
            step2.Apply();
            step2.gameObject.SetActive(false);

            var sm = circle2.AddComponent<StepMarker>();
            sm.window = step2;

            hud.completionWindow = done;
            hud.activateOnComplete = new[] { circle2 };
            circle2.SetActive(false);

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(sm);
            Selection.activeGameObject = circle2;
            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Готово. Следующий круг: PointPosition_Step2 (" + pos + ") — передвиньте его, куда нужно. Окна: CanvasStep1Done, CanvasStep2.");
        }
    }
}
