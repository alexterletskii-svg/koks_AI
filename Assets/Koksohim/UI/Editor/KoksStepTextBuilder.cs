using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 11. Тексты шага 1, переход по лестнице и круг шага 3 (FirstScene).
    /// Ничего не пересобирает: меняет тексты на месте и добавляет недостающее. Положение кругов не трогает.
    /// </summary>
    public static class KoksStepTextBuilder
    {
        [MenuItem("Koks/Build UI/11. Тексты шага 1, лестница и круг шага 3 (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
                return;
            }

            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var points = Object.FindObjectsByType<InspectPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var circle1 = all.FirstOrDefault(t => t.name == "PointPosition" && t.parent == null);
            var circle2 = all.FirstOrDefault(t => t.name == "PointPosition_Step2");
            var hud = Object.FindFirstObjectByType<InspectHud>(FindObjectsInactive.Include);
            if (circle1 == null || circle2 == null || hud == null)
            {
                Debug.LogError("[Koks UI] Нужны PointPosition, PointPosition_Step2 и осмотр клапанов. Сначала запустите меню 8 и 9.");
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

            // 0. лишние точки (раньше было 8, теперь 6): убираем точку и её маркер
            var wanted = KoksInspectBuilder.Defs.Select(d => d.obj).ToList();
            int removed = 0;
            foreach (var p in points)
            {
                if (p == null || wanted.Contains(p.name)) continue;
                if (p.marker != null) Object.DestroyImmediate(p.marker.gameObject);
                Object.DestroyImmediate(p);
                removed++;
            }
            points = Object.FindObjectsByType<InspectPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            hud.points = points.Where(x => wanted.Contains(x.name)).ToList();
            EditorUtility.SetDirty(hud);

            // 1. тексты точек осмотра
            int set = 0;
            foreach (var d in KoksInspectBuilder.Defs)
            {
                var p = points.FirstOrDefault(x => x.name == d.obj);
                if (p == null) { Debug.LogWarning("[Koks UI] Нет точки осмотра на объекте " + d.obj); continue; }
                p.group = d.group;
                p.title = d.title;
                p.text = d.text;
                EditorUtility.SetDirty(p);
                set++;
            }

            // 2. окно шага 1 (у первого круга)
            var m1 = circle1.GetComponent<StepMarker>();
            if (m1 != null && m1.window != null)
            {
                var w = m1.window;
                w.stepText = "Шаг 1";
                w.title = "Газовоздушные клапаны";
                w.description = "Вы у газовоздушных клапанов. Осмотрите оба клапана: наводите луч на отмеченные маркеры и держите 2 секунды.";
                w.checksHeader = "Что проверить";
                w.checks = new[]
                {
                    "Открытие клапанов (положение пластин)",
                    "Положение штоков тарелок",
                    "Целостность узлов клапанов"
                };
                w.buttonText = "Приступить к осмотру";
                w.Apply();
                EditorUtility.SetDirty(w);
                // маркеры осмотра появятся только после «Приступить к осмотру» в этом окне
                hud.startWindow = w;
                EditorUtility.SetDirty(hud);
            }

            // окно «Клапаны осмотрены»: текст под шесть точек
            if (hud.completionWindow != null)
            {
                hud.completionWindow.description = "Вы осмотрели оба клапана: пластины, тарелки и узлы. Дальше — следующий пункт обхода.";
                hud.completionWindow.autoConfirmSeconds = 6f;   // информационное окно: закрывается само
                hud.completionWindow.Apply();
                EditorUtility.SetDirty(hud.completionWindow);
            }

            // 3. окно у круга шага 2: подняться по лестнице
            var m2 = circle2.GetComponent<StepMarker>();
            if (m2 == null || m2.window == null)
            {
                Debug.LogError("[Koks UI] У PointPosition_Step2 нет Step Marker с окном. Запустите меню 9.");
                return;
            }
            var w2 = m2.window;
            w2.stepText = "Переход";
            w2.title = "Поднимитесь по лестнице";
            w2.description = "Клапаны осмотрены. Поднимитесь по лестнице к следующему шагу: синий круг ждёт вас наверху.";
            w2.checksHeader = "Что дальше";
            w2.checks = new[] { "Поднимитесь по лестнице", "Подойдите к синему кругу", "" };
            w2.buttonText = "Понятно";
            w2.autoConfirmSeconds = 6f;   // информационное окно: закрывается само и включает круг шага 3
            w2.Apply();
            EditorUtility.SetDirty(w2);

            // 4. круг шага 3 и его окно (позицию круга пользователь выставляет сам)
            all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var circle3Tr = all.FirstOrDefault(t => t.name == "PointPosition_Step3");
            GameObject circle3;
            if (circle3Tr != null) circle3 = circle3Tr.gameObject;
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Koksohim/Prefabs/PointPosition.prefab");
                if (prefab == null) { Debug.LogError("[Koks UI] Нет префаба PointPosition."); return; }
                circle3 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                circle3.name = "PointPosition_Step3";
                circle3.transform.position = circle2.position + new Vector3(2f, 0f, 0f);
                circle3.transform.rotation = circle2.rotation;
            }

            // окно шага 3: шесть пунктов на двух страницах (пересоздаём, если его собрали без страниц)
            var w3Tr = all.FirstOrDefault(t => t.name == "CanvasStep3" && t.parent == null);
            StepWindow w3 = w3Tr != null ? w3Tr.GetComponent<StepWindow>() : null;
            if (w3 == null || w3.backButton == null)
            {
                w3 = KoksStepBuilder.CreateWindow("CanvasStep3", circle3.transform.position + new Vector3(0f, 1.5f, 0f), heading, body, medium, true);
                w3.stepText = "Шаг 3";
                w3.title = "Кантовочный механизм и газопроводы";
                w3.description = "Вы у кантовочного механизма. Осмотрите оборудование по списку.";
                w3.checksHeader = "Что проверить";
                w3.checks = new[]
                {
                    "Положение пробок стопорных кранов",
                    "Соединения рычагов кантовочных кранов со штангой кантовочной лебёдки",
                    "Положение крышек устройства обезграфичивания",
                    "Внешняя поверхность распределительных газопроводов. Результат записать в эксплуатационный журнал",
                    "Состояние штанг кантовочного механизма",
                    "Состояние канатов и жимков канатов кантовочного механизма"
                };
                w3.buttonText = "Приступить к осмотру";
                w3.Apply();
                w3.gameObject.SetActive(false);
                EditorUtility.SetDirty(w3);
            }

            // у окна шага 3 последняя кнопка — «Понятно»: осмотр начнётся у круга шага 4
            w3.buttonText = "Понятно";
            w3.Apply();
            EditorUtility.SetDirty(w3);

            var m3 = circle3.GetComponent<StepMarker>();
            if (m3 == null) m3 = circle3.AddComponent<StepMarker>();
            m3.window = w3;
            EditorUtility.SetDirty(m3);

            all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // круг шага 4 и его окно-заглушка (позицию круга пользователь выставляет сам)
            var circle4Tr = all.FirstOrDefault(t => t.name == "PointPosition_Step4");
            GameObject circle4;
            if (circle4Tr != null) circle4 = circle4Tr.gameObject;
            else
            {
                var prefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Koksohim/Prefabs/PointPosition.prefab");
                circle4 = (GameObject)PrefabUtility.InstantiatePrefab(prefab4);
                circle4.name = "PointPosition_Step4";
                circle4.transform.position = circle3.transform.position + new Vector3(2f, 0f, 0f);
                circle4.transform.rotation = circle3.transform.rotation;
            }
            var w4Tr = all.FirstOrDefault(t => t.name == "CanvasStep4" && t.parent == null);
            StepWindow w4 = w4Tr != null ? w4Tr.GetComponent<StepWindow>() : null;
            if (w4 == null)
            {
                w4 = KoksStepBuilder.CreateWindow("CanvasStep4", circle4.transform.position + new Vector3(0f, 1.5f, 0f), heading, body, medium);
                w4.stepText = "Шаг 4";
                w4.title = "Следующее оборудование";
                w4.description = "Здесь будет описание следующего оборудования и что в нём нужно осмотреть.";
                w4.checksHeader = "Что проверить";
                w4.buttonText = "Приступить к осмотру";
                w4.Apply();
                w4.gameObject.SetActive(false);
                EditorUtility.SetDirty(w4);
            }
            var m4 = circle4.GetComponent<StepMarker>();
            if (m4 == null) m4 = circle4.AddComponent<StepMarker>();
            m4.window = w4;
            EditorUtility.SetDirty(m4);
            // пока у шага 3 нет точек осмотра, круг 4 включается после «Приступить к осмотру»
            m3.activateOnConfirm = new[] { circle4 };
            EditorUtility.SetDirty(m3);

            // круг шага 2 включает круг шага 3 после «Понятно»
            m2.activateOnEnter = new[] { circle3 };     // круг шага 3 виден сразу вместе с окном «Поднимитесь по лестнице»
            m2.activateOnConfirm = new[] { circle3 };
            EditorUtility.SetDirty(m2);

            // 5. страховка: всё последующее скрыто при старте
            var flowTr = all.FirstOrDefault(t => t.name == "StepFlow" && t.parent == null);
            StepFlow flow = flowTr != null ? flowTr.GetComponent<StepFlow>() : null;
            if (flow == null) flow = new GameObject("StepFlow").AddComponent<StepFlow>();
            flow.hideAtStart = new[] { circle3, circle4, circle2.gameObject, w3.gameObject, w4.gameObject, w2.gameObject };
            EditorUtility.SetDirty(flow);

            circle3.SetActive(false);
            circle4.SetActive(false);

            Selection.activeGameObject = circle4;
            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Точки осмотра: " + set + " из " + KoksInspectBuilder.Defs.Length + ", убрано лишних: " + removed +
                      ". Круги: PointPosition_Step3 (" + circle3.transform.position + "), PointPosition_Step4 (" + circle4.transform.position + ") — передвиньте, куда нужно.");
        }
    }
}
