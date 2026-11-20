using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 13. Осмотр кантовочного механизма (шаг 4).
    /// Запускать в FirstScene вне Play, после меню 11. Точки осмотра, окно «запишите в журнал»,
    /// итоговое окно и круг PointPosition_Step5. Позиции кругов не двигает; можно запускать повторно.
    /// </summary>
    public static class KoksStep4Builder
    {
        const string Kantovka = "Кантовочный механизм";

        static readonly string[] Checks =
        {
            "Положение пробок стопорных кранов",
            "Соединения рычагов кантовочных кранов со штангой кантовочной лебёдки",
            "Положение крышек устройства обезграфичивания",
            "Внешняя поверхность распределительных газопроводов. Результат записать в эксплуатационный журнал",
            "Состояние штанг кантовочного механизма",
            "Состояние канатов и жимков канатов кантовочного механизма"
        };

        static readonly KoksInspectBuilder.Def[] Defs =
        {
            new KoksInspectBuilder.Def("Cylinder.341", "Кантовочные краны", "Пробки стопорных кранов",
                "Произвести осмотр положений пробок стопорных кранов."),
            new KoksInspectBuilder.Def("Cylinder.372", "Кантовочные краны", "Рычаги кранов",
                "Произвести осмотр соединений рычагов кантовочных кранов со штангой кантовочной лебёдки."),
            new KoksInspectBuilder.Def("Cylinder.161", "Обезграфичивание", "Крышки устройства",
                "Произвести осмотр положений крышек устройства обезграфичивания."),
            new KoksInspectBuilder.Def("Pipe_2", "Газопроводы", "Газопровод 1",
                "Произвести осмотр внешней поверхности распределительных газопроводов. Результаты осмотра зафиксировать в эксплуатационном журнале."),
            new KoksInspectBuilder.Def("Pipe_3", "Газопроводы", "Газопровод 2",
                "Произвести осмотр внешней поверхности распределительных газопроводов. Результаты осмотра зафиксировать в эксплуатационном журнале."),
            new KoksInspectBuilder.Def("Cylinder.334", Kantovka, "Штанги",
                "Произвести осмотр состояния штанг кантовочного механизма."),
            new KoksInspectBuilder.Def("Cube.053", Kantovka, "Канаты и жимки",
                "Произвести осмотр состояния канатов и жимков канатов кантовочного механизма."),
            new KoksInspectBuilder.Def("Roller1.003", Kantovka, "Канаты и жимки",
                "Произвести осмотр состояния канатов и жимков канатов кантовочного механизма."),
            new KoksInspectBuilder.Def("Roller1.001", Kantovka, "Канаты и жимки",
                "Произвести осмотр состояния канатов и жимков канатов кантовочного механизма."),
        };

        static Transform FindT(string name, bool rootOnly = false)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t != null && t.name == name && (!rootOnly || t.parent == null));
        }

        [MenuItem("Koks/Build UI/13. Осмотр кантовочного механизма — шаг 4 (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
                return;
            }

            var circle3 = FindT("PointPosition_Step3");
            var circle4 = FindT("PointPosition_Step4");
            if (circle3 == null || circle4 == null)
            {
                Debug.LogError("[Koks UI] Нет PointPosition_Step3 / PointPosition_Step4. Сначала запустите меню 11.");
                return;
            }
            var m4 = circle4.GetComponent<StepMarker>();
            if (m4 == null) m4 = circle4.gameObject.AddComponent<StepMarker>();

            KoksUIBuilder.PrepareAssets();
            var heading = KoksUIBuilder.LoadFont("RobotoCondensed-ExtraBold");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            if (heading == null || body == null || medium == null) { Debug.LogError("[Koks UI] Нет шрифтов TMP."); return; }

            // ---- точки осмотра (объекты ищем рядом с кругом шага 3)
            foreach (var n in new[] { "CanvasInspect2", "InspectMarkers2" })
            {
                Transform o;
                while ((o = FindT(n, true)) != null) Object.DestroyImmediate(o.gameObject);
            }
            var hud = KoksInspectBuilder.BuildSet(Defs, circle3.position, "CanvasInspect2", "InspectMarkers2", heading, body, medium);
            if (hud == null) return;

            // ---- окно шага 4 (у круга 4): список из шести пунктов, «Приступить к осмотру»
            var w4Tr = FindT("CanvasStep4", true);
            StepWindow w4 = w4Tr != null ? w4Tr.GetComponent<StepWindow>() : null;
            if (w4 == null || w4.backButton == null)
            {
                w4 = KoksStepBuilder.CreateWindow("CanvasStep4", circle4.position + new Vector3(0f, 1.5f, 0f), heading, body, medium, true);
                w4.gameObject.SetActive(false);
            }
            w4.stepText = "Шаг 4";
            w4.title = "Осмотр оборудования";
            w4.description = "Наводите луч на маркеры и держите 2 секунды. Осмотрите оборудование по списку.";
            w4.checksHeader = "Что проверить";
            w4.checks = Checks;
            w4.buttonText = "Приступить к осмотру";
            w4.autoConfirmSeconds = 0f;
            w4.Apply();
            EditorUtility.SetDirty(w4);
            m4.window = w4;
            EditorUtility.SetDirty(m4);

            // ---- окно «запишите в журнал» (после газопроводов)
            var journal = Info("CanvasJournal", circle4.position, heading, body, medium,
                "Журнал", "Запишите результат в журнал",
                "Газопроводы осмотрены. Результаты осмотра нужно зафиксировать в эксплуатационном журнале.",
                "Что дальше", new[] { "Запишите результат осмотра", "Продолжайте осмотр оборудования" }, "Понятно");

            // ---- итоговое окно + круг шага 5
            var circle5Tr = FindT("PointPosition_Step5");
            GameObject circle5;
            if (circle5Tr != null) circle5 = circle5Tr.gameObject;
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Koksohim/Prefabs/PointPosition.prefab");
                if (prefab == null) { Debug.LogError("[Koks UI] Нет префаба PointPosition."); return; }
                circle5 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                circle5.name = "PointPosition_Step5";
                circle5.transform.position = circle4.position + new Vector3(2f, 0f, 0f);
                circle5.transform.rotation = circle4.rotation;
            }

            var done = Info("CanvasStep4Done", circle4.position, heading, body, medium,
                "Шаг 4 выполнен", "Осмотр завершён",
                "Проведите такой же осмотр на другой стороне печи.",
                "Что дальше", new[] { "Пройдите к синему кругу" }, "Понятно");

            var w5Tr = FindT("CanvasStep5", true);
            StepWindow w5 = w5Tr != null ? w5Tr.GetComponent<StepWindow>() : null;
            if (w5 == null)
            {
                w5 = KoksStepBuilder.CreateWindow("CanvasStep5", circle5.transform.position + new Vector3(0f, 1.5f, 0f), heading, body, medium);
                w5.stepText = "Шаг 5";
                w5.title = "Другая сторона печи";
                w5.description = "Здесь будет описание осмотра на другой стороне печи и что в нём нужно осмотреть.";
                w5.checksHeader = "Что проверить";
                w5.buttonText = "Приступить к осмотру";
                w5.Apply();
                w5.gameObject.SetActive(false);
                EditorUtility.SetDirty(w5);
            }
            var m5 = circle5.GetComponent<StepMarker>();
            if (m5 == null) m5 = circle5.AddComponent<StepMarker>();
            m5.window = w5;
            EditorUtility.SetDirty(m5);

            // ---- связи
            hud.startWindow = w4;                          // маркеры появятся после «Приступить к осмотру»
            hud.completionWindow = done;
            hud.activateOnComplete = new[] { circle5 };    // круг шага 5 виден вместе с итоговым окном
            var gw = hud.GetComponent<GroupCompleteWindow>();
            if (gw == null) gw = hud.gameObject.AddComponent<GroupCompleteWindow>();
            gw.hud = hud;
            gw.group = "Газопроводы";
            gw.window = journal;
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(gw);

            // ---- всё последующее скрыто при старте (то, что уже в списке, не теряем)
            var flowTr = FindT("StepFlow", true);
            StepFlow flow = flowTr != null ? flowTr.GetComponent<StepFlow>() : new GameObject("StepFlow").AddComponent<StepFlow>();
            var hide = new List<GameObject>();
            if (flow.hideAtStart != null) hide.AddRange(flow.hideAtStart.Where(g => g != null));
            foreach (var g in new[] { circle5, journal.gameObject, done.gameObject, w5.gameObject, w4.gameObject })
                if (!hide.Contains(g)) hide.Add(g);
            flow.hideAtStart = hide.ToArray();
            EditorUtility.SetDirty(flow);
            circle5.SetActive(false);

            Selection.activeGameObject = circle5;
            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Шаг 4 собран: " + hud.points.Count + " точек осмотра. Круг шага 5: PointPosition_Step5 (" +
                      circle5.transform.position + ") — передвиньте, куда нужно (включите галочку, переставьте, снимите).");
        }

        // информационное окно: закрывается само через 6 секунд
        static StepWindow Info(string name, Vector3 near, TMPro.TMP_FontAsset heading, TMPro.TMP_FontAsset body, TMPro.TMP_FontAsset medium,
                               string step, string title, string description, string header, string[] checks, string button)
        {
            var w = KoksStepBuilder.CreateWindow(name, near + new Vector3(0f, 1.5f, 0f), heading, body, medium);
            w.stepText = step;
            w.title = title;
            w.description = description;
            w.checksHeader = header;
            w.checks = checks;
            w.buttonText = button;
            w.autoConfirmSeconds = 6f;
            w.Apply();
            w.gameObject.SetActive(false);
            EditorUtility.SetDirty(w);
            return w;
        }
    }
}
