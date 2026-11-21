using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 14. Шаги 6–9. Запускать в FirstScene вне Play, после меню 13.
    /// Шаг 6: ролики кантовочного механизма. Шаг 8: сточные линии газоподогревателя (кран + вода).
    /// Шаг 9: уровень воды в конденсатоотводчике, затем возврат в MenuScene.
    /// Круги: PointPosition_Step6 (уже есть), PointPosition_Step8, PointPosition_Step9. Можно запускать повторно
    /// (после перестановки кругов — чтобы выбрать ближайшие объекты и ромбики).
    /// </summary>
    public static class KoksStep6Builder
    {
        static readonly string[] RollerCandidates = { "Roller1", "Roller1.002" };

        static Transform FindT(string name, bool rootOnly = false)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t != null && t.gameObject.scene.IsValid() && t.name == name && (!rootOnly || t.parent == null));
        }

        static GameObject Circle(string name, Vector3 pos, Quaternion rot)
        {
            var t = FindT(name, true);
            if (t != null) return t.gameObject;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Koksohim/Prefabs/PointPosition.prefab");
            if (prefab == null) { Debug.LogError("[Koks UI] Нет префаба PointPosition."); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            return go;
        }

        static StepWindow Win(string name, Vector3 near, TMPro.TMP_FontAsset h, TMPro.TMP_FontAsset b, TMPro.TMP_FontAsset m,
                              string step, string title, string desc, string header, string[] checks, string button, float auto)
        {
            var old = FindT(name, true);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var w = KoksStepBuilder.CreateWindow(name, near + new Vector3(0f, 1.5f, 0f), h, b, m);
            w.stepText = step; w.title = title; w.description = desc;
            w.checksHeader = header; w.checks = checks; w.buttonText = button;
            w.autoConfirmSeconds = auto;
            w.Apply();
            w.gameObject.SetActive(false);
            EditorUtility.SetDirty(w);
            return w;
        }

        static void Clean(params string[] names)
        {
            foreach (var n in names)
            {
                Transform o;
                while ((o = FindT(n, true)) != null) Object.DestroyImmediate(o.gameObject);
            }
        }

        // длинные меши (много копий одним мешом): ромбик на ближнем к кругу конце
        static void NearestEnd(InspectPoint pt, Vector3 from)
        {
            var rr = pt.GetComponent<Renderer>();
            if (rr == null) return;
            Bounds bb = rr.bounds;
            bool alongX = bb.size.x >= bb.size.z;
            if (Mathf.Max(bb.size.x, bb.size.z) < 3f) return;
            Vector3 c = bb.center, target = c;
            if (alongX) target.x = from.x < c.x ? bb.min.x + 0.5f : bb.max.x - 0.5f;
            else target.z = from.z < c.z ? bb.min.z + 0.5f : bb.max.z - 0.5f;
            pt.markerOffset = new Vector3(target.x - c.x, 0f, target.z - c.z);
            EditorUtility.SetDirty(pt);
        }

        // область наведения луча — по габаритам детали с запасом (маленькую деталь иначе трудно поймать лучом)
        static void EnlargeHitArea(GameObject go, float pad)
        {
            var r = go.GetComponentInChildren<Renderer>();
            if (r == null) return;
            foreach (var old in go.GetComponents<BoxCollider>()) Object.DestroyImmediate(old);   // прошлая сборка / запасной коллайдер
            Bounds b = r.bounds;
            Vector3 ls = go.transform.lossyScale;
            Vector3 inv = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
            Vector3 size = b.size + Vector3.one * pad;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = go.transform.InverseTransformPoint(b.center);
            bc.size = Vector3.Scale(size, inv);
            EditorUtility.SetDirty(bc);
        }

        static ParticleSystem BuildWater(Transform cilWater)
        {
            var old = cilWater.Find("DrainWater");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            string matPath = "Assets/Koksohim/UI/Resources/KoksWater.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var sh = Shader.Find("Sprites/Default");
                mat = new Material(sh);
                mat.mainTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var go = new GameObject("DrainWater");
            go.transform.SetParent(cilWater, false);
            // из нижней точки cil_water, струя вниз
            var rend = cilWater.GetComponentInChildren<Renderer>();
            Vector3 at = rend != null ? new Vector3(rend.bounds.center.x, rend.bounds.min.y, rend.bounds.center.z) : cilWater.position;
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.75f, 1f, 0.85f), new Color(0.75f, 0.88f, 1f, 0.7f));
            main.gravityModifier = 1.2f;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            var em = ps.emission;
            em.rateOverTime = 140f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.02f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = mat;
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        [MenuItem("Koks/Build UI/14. Шаги 6–9: ролики, сточные линии, конденсатоотводчик, выход в меню")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз."); return; }

            var circle6 = FindT("PointPosition_Step6", true);
            var w6Tr = FindT("CanvasStep6", true);
            if (circle6 == null) { Debug.LogError("[Koks UI] Нет PointPosition_Step6. Сначала запустите меню 13."); return; }

            KoksUIBuilder.PrepareAssets();
            var heading = KoksUIBuilder.LoadFont("RobotoCondensed-ExtraBold");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            if (heading == null || body == null || medium == null) { Debug.LogError("[Koks UI] Нет шрифтов TMP."); return; }

            Clean("CanvasInspect3", "InspectMarkers3", "CanvasInspect4", "InspectMarkers4", "CanvasInspect5", "InspectMarkers5", "CanvasStep9Done");

            var circle8 = Circle("PointPosition_Step8", circle6.position + new Vector3(3f, 0f, 0f), circle6.rotation);
            var circle9 = Circle("PointPosition_Step9", circle6.position + new Vector3(6f, 0f, 0f), circle6.rotation);
            if (circle8 == null || circle9 == null) return;

            // ===== шаг 6: ролики =====
            // два ролика: Roller1 и Roller1.002 (остальные ролики и блочки — в тексте окон)
            var rollerDefs = new List<KoksInspectBuilder.Def>();
            foreach (var r in RollerCandidates)
            {
                if (FindT(r) == null) { Debug.LogWarning("[Koks UI] Не найден " + r); continue; }
                rollerDefs.Add(new KoksInspectBuilder.Def(r, "Кантовочный механизм", "Опорный ролик",
                    "Произвести осмотр состояния целостности блочков и опорных роликов кантовочного механизма. Остальные ролики и блочки осматриваются так же."));
            }
            if (rollerDefs.Count == 0) { Debug.LogError("[Koks UI] Не найдены ролики Roller1 и Roller1.002."); return; }
            var hud3 = KoksInspectBuilder.BuildSet(rollerDefs.ToArray(), circle6.position, "CanvasInspect3", "InspectMarkers3", heading, body, medium);
            if (hud3 == null) return;
            foreach (var p in hud3.points)
            {
                NearestEnd(p, circle6.position);
                p.markerSize = 0.2f;       // маркер крупнее — его видно издалека
                p.markerPull = 0.6f;
                p.markerAtCenter = true;
                EnlargeHitArea(p.gameObject, 0.7f);
                EditorUtility.SetDirty(p);
            }

            StepWindow w6 = w6Tr != null ? w6Tr.GetComponent<StepWindow>() : null;
            if (w6 == null) w6 = Win("CanvasStep6", circle6.position, heading, body, medium, "", "", "", "", new string[0], "", 0f);
            w6.stepText = "Шаг 6";
            w6.title = "Опорные ролики";
            w6.description = "Следующим этапом осмотра является осмотр состояния целостности блочков и опорных роликов кантовочного механизма. Осмотрите ролики, отмеченные ромбиками, остальные ролики и блочки осматриваются так же.";
            w6.checksHeader = "Что проверить";
            w6.checks = new[] { "Целостность блочков кантовочного механизма", "Целостность опорных роликов: осмотрите два отмеченных ролика" };
            w6.buttonText = "Приступить к осмотру";
            w6.autoConfirmSeconds = 0f;
            w6.Apply();
            EditorUtility.SetDirty(w6);
            var m6 = circle6.GetComponent<StepMarker>();
            if (m6 == null) m6 = circle6.gameObject.AddComponent<StepMarker>();
            m6.window = w6;
            m6.activateOnConfirm = new GameObject[0];   // круг 7 в цепочке не участвует
            EditorUtility.SetDirty(m6);

            var done6 = Win("CanvasStep6Done", circle6.position, heading, body, medium,
                "Шаг 6 выполнен", "Осмотр роликов завершён",
                "Остальные ролики и блочки осматриваются так же. Переходим к осмотру сточных линий газоподогревателя.",
                "Что дальше", new[] { "Пройдите к синему кругу" }, "Понятно", 6f);
            hud3.startWindow = w6;
            hud3.completionWindow = done6;
            hud3.activateOnComplete = new[] { circle8 };

            // ===== шаг 8: сточные линии (кран + вода) =====
            var torus = FindT("Torus.003");
            var cil = FindT("cil_water");
            if (torus == null || cil == null) { Debug.LogError("[Koks UI] Не найдены Torus.003 и/или cil_water."); return; }
            var hud4 = KoksInspectBuilder.BuildSet(new[]
            {
                new KoksInspectBuilder.Def("Torus.003", "Сточные линии", "Кран сточной линии",
                    "Проверьте работоспособность сточных линий газоподогревателя: откройте кран и убедитесь, что идёт вода.")
            }, circle8.transform.position, "CanvasInspect4", "InspectMarkers4", heading, body, medium);
            if (hud4 == null) return;
            var w8 = Win("CanvasStep8", circle8.transform.position, heading, body, medium,
                "Шаг 8", "Сточные линии газоподогревателя",
                "Нужно проверить работоспособность сточных линий газоподогревателя.",
                "Что проверить", new[] { "Откройте кран: наведите луч и держите", "Убедитесь, что вода идёт по линии", "Закройте кран сами: наведите луч и держите" },
                "Приступить к осмотру", 0f);
            var done8 = Win("CanvasStep8Done", circle8.transform.position, heading, body, medium,
                "Шаг 8 выполнен", "Кран закрыт",
                "Вы закрыли кран: сточная линия проверена. Переходим к проверке конденсатоотводчика.",
                "Что дальше", new[] { "Пройдите к синему кругу" }, "Понятно", 6f);
            var close8 = Win("CanvasStep8Close", circle8.transform.position, heading, body, medium,
                "Шаг 8", "Кран открыт, вода идёт",
                "Сточная линия работает. Теперь закройте кран сами.",
                "Что сделать", new[] { "Наведите луч на кран и держите", "Дождитесь, пока кран закроется" }, "Понятно", 0f);
            var m8 = circle8.GetComponent<StepMarker>();
            if (m8 == null) m8 = circle8.AddComponent<StepMarker>();
            m8.window = w8;
            EditorUtility.SetDirty(m8);
            hud4.startWindow = w8;
            var water = BuildWater(cil);
            var dv = hud4.GetComponent<DrainValve>();
            if (dv == null) dv = hud4.gameObject.AddComponent<DrainValve>();
            dv.hud = hud4;
            dv.valve = torus;
            dv.water = water;
            dv.closeWindow = close8;
            dv.window = done8;
            dv.activateAfter = new[] { circle9 };
            EditorUtility.SetDirty(dv);

            // ===== шаг 9: осмотр конденсатоотводчика ромбиком (как остальные точки), затем итог и выход в меню =====
            var coll = FindT("CondensateCollector");
            if (coll == null) { Debug.LogError("[Koks UI] Не найден CondensateCollector."); return; }
            var hud5 = KoksInspectBuilder.BuildSet(new[]
            {
                new KoksInspectBuilder.Def("CondensateCollector", "Конденсатоотводчик", "Уровень воды",
                    "Визуально проверить уровень воды в конденсатоотводчике.")
            }, circle9.transform.position, "CanvasInspect5", "InspectMarkers5", heading, body, medium);
            if (hud5 == null) return;
            var condTarget = FindT("ExamCondensateTarget");
            if (condTarget == null) Debug.LogWarning("[Koks UI] Нет объекта ExamCondensateTarget: ромбик встанет на модель.");
            foreach (var p in hud5.points)
            {
                p.markerSize = 0.2f;
                p.markerPull = 0.3f;
                p.markerAtCenter = true;
                p.anchor = condTarget;     // ромбик и луч — только у этой точки в яме
                EditorUtility.SetDirty(p);
            }
            var w9 = Win("CanvasStep9", circle9.transform.position, heading, body, medium,
                "Шаг 9", "Конденсатоотводчик",
                "Последний этап осмотра — конденсатоотводчик. Наведите луч на ромбик и удерживайте, пока шкала не заполнится.",
                "Что проверить", new[] { "Визуально проверьте уровень воды в конденсатоотводчике" },
                "Приступить к осмотру", 0f);
            var done9 = Win("CanvasStep9Done", circle9.transform.position, heading, body, medium,
                "Шаг 9 выполнен", "Конденсатоотводчик осмотрен",
                "Уровень воды в конденсатоотводчике проверен.",
                "Результат", new[] { "Уровень воды в норме" }, "Понятно", 4f);
            var finish = Win("CanvasFinish", circle9.transform.position, heading, body, medium,
                "Осмотр завершён", "Сценарий завершён",
                "Учебный сценарий осмотра газового тоннеля завершён.",
                "Что дальше", new[] { "Вы вернётесь в меню выбора сценариев" }, "В меню выбора сценариев", 12f);
            var m9 = circle9.GetComponent<StepMarker>();
            if (m9 == null) m9 = circle9.AddComponent<StepMarker>();
            m9.window = w9;
            EditorUtility.SetDirty(m9);
            hud5.startWindow = w9;
            hud5.completionWindow = done9;
            var ch1 = w9.GetComponent<WindowChain>();     // старая цепочка w9→done9 больше не нужна: дальше ведёт осмотр
            if (ch1 != null) { ch1.next = null; EditorUtility.SetDirty(ch1); }
            var ch2 = done9.GetComponent<WindowChain>();
            if (ch2 == null) ch2 = done9.gameObject.AddComponent<WindowChain>();
            ch2.window = done9; ch2.next = finish;
            EditorUtility.SetDirty(ch2);
            var sl = finish.GetComponent<SceneLoadOnConfirm>();
            if (sl == null) sl = finish.gameObject.AddComponent<SceneLoadOnConfirm>();
            sl.window = finish;
            sl.sceneName = "MenuScene";
            EditorUtility.SetDirty(sl);

            foreach (var h in new[] { hud3, hud4, hud5 }) EditorUtility.SetDirty(h);

            // ===== всё последующее скрыто при старте =====
            var flowTr = FindT("StepFlow", true);
            StepFlow flow = flowTr != null ? flowTr.GetComponent<StepFlow>() : new GameObject("StepFlow").AddComponent<StepFlow>();
            var hide = new List<GameObject>();
            if (flow.hideAtStart != null) hide.AddRange(flow.hideAtStart.Where(g => g != null));
            foreach (var g in new[] { circle8, circle9, w6.gameObject, done6.gameObject, w8.gameObject, close8.gameObject, done8.gameObject, w9.gameObject, done9.gameObject, finish.gameObject })
                if (!hide.Contains(g)) hide.Add(g);
            flow.hideAtStart = hide.ToArray();
            EditorUtility.SetDirty(flow);
            circle8.SetActive(false);
            circle9.SetActive(false);

            Selection.activeGameObject = circle8;
            var scene = hud3.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Шаги 6–9 собраны: ролики " + hud3.points.Count + " шт. Круги PointPosition_Step8 и PointPosition_Step9 созданы — " +
                      "переставьте их (включить галочку, переставить, снять), затем запустите меню 14 ещё раз, чтобы выбрать ближайшие объекты.");
        }
    }
}
