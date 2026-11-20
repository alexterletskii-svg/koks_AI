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
            "Внешняя поверхность газопроводов. Результат записать в журнал",
            "Состояние штанг кантовочного механизма",
            "Состояние канатов и жимков канатов кантовочного механизма"
        };

        static readonly KoksInspectBuilder.Def[] BaseDefs =
        {
            new KoksInspectBuilder.Def("Cylinder.341", "Кантовочные краны", "Пробки стопорных кранов",
                "Произвести осмотр положений пробок стопорных кранов."),
            new KoksInspectBuilder.Def("Cylinder.372", "Кантовочные краны", "Рычаги кранов",
                "Произвести осмотр соединений рычагов кантовочных кранов со штангой кантовочной лебёдки."),
            new KoksInspectBuilder.Def("Cylinder.161", "Обезграфичивание", "Крышки устройства",
                "Произвести осмотр положений крышек устройства обезграфичивания."),
            new KoksInspectBuilder.Def("Cylinder.334", Kantovka, "Штанги",
                "Произвести осмотр состояния штанг кантовочного механизма."),
            new KoksInspectBuilder.Def("Cube.053", Kantovka, "Канаты и жимки",
                "Произвести осмотр состояния канатов и жимков канатов кантовочного механизма."),
            new KoksInspectBuilder.Def("Roller1.003", Kantovka, "Ролики канатов",
                "Произвести осмотр состояния канатов и жимков канатов кантовочного механизма."),
        };

        // одна труба вместо двух: берём ту, что ближе к кругу 3 (Pipe_2 или Pipe_3)
        static KoksInspectBuilder.Def[] MakeDefs(Vector3 near)
        {
            string best = "Pipe_2";
            float bd = float.MaxValue;
            foreach (var n in new[] { "Pipe_2", "Pipe_3" })
            {
                var t = FindT(n);
                var r = t != null ? t.GetComponent<Renderer>() : null;
                if (r == null) continue;
                float d = (r.bounds.ClosestPoint(near) - near).sqrMagnitude;
                if (d < bd) { bd = d; best = n; }
            }
            var list = new List<KoksInspectBuilder.Def>(BaseDefs);
            list.Insert(3, new KoksInspectBuilder.Def(best, "Газопроводы", "Газопровод",
                "Произвести осмотр внешней поверхности распределительных газопроводов. Результаты осмотра зафиксировать в эксплуатационном журнале."));
            Debug.Log("[Koks UI] Для осмотра газопровода выбрана труба " + best + ".");
            return list.ToArray();
        }

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
            var hud = KoksInspectBuilder.BuildSet(MakeDefs(circle3.position), circle3.position, "CanvasInspect2", "InspectMarkers2", heading, body, medium);
            if (hud == null) return;

            // длинные меши (труба, Roller1.003 — множество роликов одним мешом): ромбик на ближайшем к кругу 3 месте
            foreach (var pt in hud.points)
            {
                if (pt == null || !(pt.name.StartsWith("Pipe_") || pt.name == "Roller1.003" || pt.name == "Cube.053")) continue;
                var rr = pt.GetComponent<Renderer>();
                if (rr == null) continue;
                Bounds bb = rr.bounds;
                bool alongX = bb.size.x >= bb.size.z;
                // от круга 4 (там игрок начинает осмотр)
                Vector3 c = bb.center, from = circle4.position;
                float inset = 0.5f;
                Vector3 target = c;
                bool pipe = pt.name.StartsWith("Pipe_");
                if (alongX)
                    target.x = pipe ? Mathf.Clamp(from.x, bb.min.x + inset, bb.max.x - inset)
                                    : (from.x < c.x ? bb.min.x + inset : bb.max.x - inset);   // Cube.053 и ролики: несколько копий одним мешом — ближний к игроку край
                else
                    target.z = pipe ? Mathf.Clamp(from.z, bb.min.z + inset, bb.max.z - inset)
                                    : (from.z < c.z ? bb.min.z + inset : bb.max.z - inset);
                pt.markerOffset = new Vector3(target.x - c.x, 0f, target.z - c.z);
                if (pipe) pt.markerPull = 1.2f;   // ромбик на трубе — заметно ближе к игроку
                EditorUtility.SetDirty(pt);
            }

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

            // ---- журнал (после осмотра газопроводов): запись появляется по кнопке
            var journal = KoksJournalBuilder.Create("CanvasJournal", circle4.position + new Vector3(0f, 1.5f, 0f), heading, body, medium);
            journal.gameObject.SetActive(false);
            EditorUtility.SetDirty(journal);

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
            // шаг 5 — информационное окно (без «Приступить к осмотру»): другая сторона осматривается так же
            w5.stepText = "Шаг 5";
            w5.title = "Другая сторона печи";
            w5.description = "Оборудование на этой стороне печи зеркальное. Осмотрите его таким же образом, как на первой стороне.";
            w5.checksHeader = "Что делать";
            w5.checks = new[] { "Повторите осмотр в том же порядке", "Результат запишите в журнал" };
            w5.buttonText = "Понятно";
            w5.autoConfirmSeconds = 8f;
            w5.Apply();
            EditorUtility.SetDirty(w5);
            var m5 = circle5.GetComponent<StepMarker>();
            if (m5 == null) m5 = circle5.AddComponent<StepMarker>();
            m5.window = w5;
            EditorUtility.SetDirty(m5);

            // ---- шаг 6: круг и окно-заглушка (после «Приступить к осмотру» в окне шага 5)
            var circle6Tr = FindT("PointPosition_Step6");
            GameObject circle6;
            if (circle6Tr != null) circle6 = circle6Tr.gameObject;
            else
            {
                var prefab6 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Koksohim/Prefabs/PointPosition.prefab");
                if (prefab6 == null) { Debug.LogError("[Koks UI] Нет префаба PointPosition."); return; }
                circle6 = (GameObject)PrefabUtility.InstantiatePrefab(prefab6);
                circle6.name = "PointPosition_Step6";
                circle6.transform.position = circle5.transform.position + new Vector3(2f, 0f, 0f);
                circle6.transform.rotation = circle5.transform.rotation;
            }
            var w6Tr = FindT("CanvasStep6", true);
            StepWindow w6 = w6Tr != null ? w6Tr.GetComponent<StepWindow>() : null;
            if (w6 == null)
            {
                w6 = KoksStepBuilder.CreateWindow("CanvasStep6", circle6.transform.position + new Vector3(0f, 1.5f, 0f), heading, body, medium);
                w6.stepText = "Шаг 6";
                w6.title = "Следующий этап";
                w6.description = "Здесь будет описание следующего этапа осмотра и что в нём нужно проверить.";
                w6.checksHeader = "Что проверить";
                w6.buttonText = "Приступить к осмотру";
                w6.Apply();
                w6.gameObject.SetActive(false);
                EditorUtility.SetDirty(w6);
            }
            var m6 = circle6.GetComponent<StepMarker>();
            if (m6 == null) m6 = circle6.AddComponent<StepMarker>();
            m6.window = w6;
            EditorUtility.SetDirty(m6);
            // ---- шаг 7: круг и окно-заглушка (после окна шага 6)
            var circle7Tr = FindT("PointPosition_Step7");
            GameObject circle7;
            if (circle7Tr != null) circle7 = circle7Tr.gameObject;
            else
            {
                var prefab7 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Koksohim/Prefabs/PointPosition.prefab");
                if (prefab7 == null) { Debug.LogError("[Koks UI] Нет префаба PointPosition."); return; }
                circle7 = (GameObject)PrefabUtility.InstantiatePrefab(prefab7);
                circle7.name = "PointPosition_Step7";
                circle7.transform.position = circle6.transform.position + new Vector3(2f, 0f, 0f);
                circle7.transform.rotation = circle6.transform.rotation;
            }
            var w7Tr = FindT("CanvasStep7", true);
            StepWindow w7 = w7Tr != null ? w7Tr.GetComponent<StepWindow>() : null;
            if (w7 == null)
            {
                w7 = KoksStepBuilder.CreateWindow("CanvasStep7", circle7.transform.position + new Vector3(0f, 1.5f, 0f), heading, body, medium);
                w7.stepText = "Шаг 7";
                w7.title = "Следующий этап";
                w7.description = "Здесь будет описание следующего этапа.";
                w7.checksHeader = "Что проверить";
                w7.buttonText = "Приступить к осмотру";
                w7.Apply();
                w7.gameObject.SetActive(false);
                EditorUtility.SetDirty(w7);
            }
            var m7 = circle7.GetComponent<StepMarker>();
            if (m7 == null) m7 = circle7.AddComponent<StepMarker>();
            m7.window = w7;
            EditorUtility.SetDirty(m7);
            var act6 = new System.Collections.Generic.List<GameObject>();
            if (m6.activateOnConfirm != null) act6.AddRange(m6.activateOnConfirm.Where(g => g != null));
            if (!act6.Contains(circle7)) act6.Add(circle7);
            m6.activateOnConfirm = act6.ToArray();
            EditorUtility.SetDirty(m6);

            // круг шага 6 появляется, когда игрок нажал «Приступить» в окне шага 5
            var act5 = new System.Collections.Generic.List<GameObject>();
            if (m5.activateOnConfirm != null) act5.AddRange(m5.activateOnConfirm.Where(g => g != null));
            if (!act5.Contains(circle6)) act5.Add(circle6);
            m5.activateOnConfirm = act5.ToArray();
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
            foreach (var g in new[] { circle5, circle6, circle7, journal.gameObject, done.gameObject, w5.gameObject, w6.gameObject, w7.gameObject, w4.gameObject })
                if (!hide.Contains(g)) hide.Add(g);
            flow.hideAtStart = hide.ToArray();
            EditorUtility.SetDirty(flow);
            circle5.SetActive(false);
            circle6.SetActive(false);
            circle7.SetActive(false);

            Selection.activeGameObject = circle5;
            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Шаг 6: круг PointPosition_Step6 и окно CanvasStep6 (заглушка) созданы.");
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
