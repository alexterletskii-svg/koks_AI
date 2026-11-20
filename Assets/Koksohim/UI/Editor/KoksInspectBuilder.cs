using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 8. Собрать осмотр клапанов (FirstScene).</summary>
    public static class KoksInspectBuilder
    {
        internal struct Def
        {
            public string obj, group, title, text;
            public Def(string o, string g, string t, string x) { obj = o; group = g; title = t; text = x; }
        }

        internal const string TextPlate = "Произвести осмотр открытия газовоздушных клапанов (положения пластин).";
        internal const string TextDisc = "Произвести осмотр положения штоков тарелок газовоздушного клапана.";
        internal const string TextUnit = "Произвести осмотр целостности узлов газовоздушных клапанов.";

        internal static readonly Def[] Defs =
        {
            new Def("Cylinder.095", "Клапан 1", "Пластина", TextPlate),
            new Def("Cylinder.105", "Клапан 1", "Тарелка", TextDisc),
            new Def("Cylinder.001", "Клапан 1", "Узел", TextUnit),
            new Def("Zaslonka2.003", "Клапан 2", "Пластина", TextPlate),
            new Def("Cylinder.004", "Клапан 2", "Тарелка", TextDisc),
            new Def("Cylinder.078", "Клапан 2", "Узел", TextUnit),
        };

        const float PW = 520f, PH = 150f;   // окно с кольцом
        const float CW = 620f, CH = 400f;   // карточка

        [MenuItem("Koks/Build UI/8. Собрать осмотр клапанов (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
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

            // ориентир для выбора одноимённых объектов — синий круг в тоннеле
            Vector3 near = Vector3.zero;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "PointPosition" && t.parent == null) { near = t.position; break; }

            // находим объекты
            var targets = new List<GameObject>();
            foreach (var d in Defs)
            {
                var found = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(t => t.name == d.obj && t.gameObject.scene.IsValid())
                    .OrderBy(t => (t.position - near).sqrMagnitude).ToList();
                if (found.Count == 0)
                {
                    Debug.LogError("[Koks UI] Не найден объект " + d.obj + " (" + d.group + ", " + d.title + "). Откройте FirstScene.");
                    return;
                }
                if (found.Count > 1)
                    Debug.LogWarning("[Koks UI] Объектов с именем " + d.obj + " несколько (" + found.Count + "), взят ближайший к кругу: " + found[0].position);
                targets.Add(found[0].gameObject);
            }

            // чистим прошлую сборку
            foreach (var n in new[] { "CanvasInspect", "InspectMarkers" })
            {
                var o = GameObject.Find(n);
                while (o != null) { Object.DestroyImmediate(o); o = GameObject.Find(n); }
            }

            // ---- окно
            var hud = BuildHud(heading, body, medium, Defs.Length);

            // ---- маркеры и точки
            var markersRoot = new GameObject("InspectMarkers");
            var points = new List<InspectPoint>();
            for (int i = 0; i < Defs.Length; i++)
            {
                var go = targets[i];
                var old = go.GetComponent<VRInspectableObject>();
                if (old != null) Object.DestroyImmediate(old);
                var oldPoint = go.GetComponent<InspectPoint>();
                if (oldPoint != null) Object.DestroyImmediate(oldPoint);

                if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
                if (go.GetComponent<XRSimpleInteractable>() == null) go.AddComponent<XRSimpleInteractable>();

                var p = go.AddComponent<InspectPoint>();
                p.group = Defs[i].group;
                p.title = Defs[i].title;
                p.text = Defs[i].text;
                p.hud = hud;
                BuildMarker(markersRoot.transform, "InspectMarker_" + Defs[i].group + "_" + Defs[i].title, p);
                points.Add(p);
                EditorUtility.SetDirty(p);
            }
            hud.points = points;
            EditorUtility.SetDirty(hud);

            var others = Object.FindObjectsByType<VRInspectableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (others.Length > 0)
                Debug.Log("[Koks UI] Старый VRInspectableObject остался на " + others.Length + " объектах: " +
                          string.Join(", ", others.Select(x => x.name)));

            Selection.activeGameObject = hud.gameObject;
            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Осмотр клапанов собран: " + points.Count + " точек. Тексты — на каждом объекте в компоненте Inspect Point.");
        }

        static InspectHud BuildHud(TMP_FontAsset heading, TMP_FontAsset body, TMP_FontAsset medium, int count)
        {
            Color heat = KoksUIBuilder.Heat, ok = KoksUIBuilder.Ok, ink = KoksUIBuilder.Ink, mute = KoksUIBuilder.Mute;

            var root = new GameObject("CanvasInspect", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(CW, CH);
            rt.localScale = Vector3.one * 0.001f;
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            // GraphicRaycaster не вешаем: окно не должно перехватывать луч у объектов

            // --- панель прогресса
            var pp = Panel("Progress", root.transform, PW, PH);
            var ringBg = KoksUIBuilder.Img("RingBg", pp.transform, KoksUIBuilder.Spr("dot"), new Color(1f, 1f, 1f, 0.14f), 28, 33, 84, 84, Image.Type.Simple);
            var ring = KoksUIBuilder.Img("RingFill", pp.transform, KoksUIBuilder.Spr("dot"), heat, 28, 33, 84, 84, Image.Type.Filled);
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            KoksUIBuilder.Img("RingHole", pp.transform, KoksUIBuilder.Spr("dot"), KoksUIBuilder.Hex("14191E"), 40, 45, 60, 60, Image.Type.Simple);
            var pct = KoksUIBuilder.Txt("Percent", pp.transform, "0%", medium, 22, Color.white, TextAlignmentOptions.Center, 28, 33, 84, 84);
            var ptitle = KoksUIBuilder.Txt("Title", pp.transform, "", heading, 32, Color.white, TextAlignmentOptions.MidlineLeft, 136, 32, 368, 50);
            ptitle.enableAutoSizing = true; ptitle.fontSizeMin = 22; ptitle.fontSizeMax = 32;
            var phint = KoksUIBuilder.Txt("Hint", pp.transform, "", body, 23, mute, TextAlignmentOptions.MidlineLeft, 136, 82, 368, 40);

            // --- карточка
            var cp = Panel("Card", root.transform, CW, CH);
            var chip = KoksUIBuilder.Img("Chip", cp.transform, KoksUIBuilder.Spr("chip"), Color.white, 28, 32, 190, 44);
            KoksUIBuilder.Img("Dot", chip.transform, KoksUIBuilder.Spr("dot"), ok, 14, 13, 18, 18, Image.Type.Simple);
            KoksUIBuilder.Txt("Text", chip.transform, "Осмотрено", medium, 22, Color.white, TextAlignmentOptions.MidlineLeft, 40, 0, 148, 44);
            var group = KoksUIBuilder.Txt("Group", cp.transform, "", medium, 22, mute, TextAlignmentOptions.MidlineRight, 330, 32, 262, 44);
            var ctitle = KoksUIBuilder.Txt("Title", cp.transform, "", heading, 42, Color.white, TextAlignmentOptions.MidlineLeft, 28, 90, 564, 60);
            ctitle.enableAutoSizing = true; ctitle.fontSizeMin = 26; ctitle.fontSizeMax = 42;
            var ctext = KoksUIBuilder.Txt("Description", cp.transform, "", body, 25, mute, TextAlignmentOptions.TopLeft, 28, 160, 564, 134, true);

            var segs = new Image[count];
            float gap = 6f, w = (CW - 56f - gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
                segs[i] = KoksUIBuilder.Img("Seg" + (i + 1), cp.transform, null, new Color(1f, 1f, 1f, 0.14f), 28 + i * (w + gap), 318, w, 8);
            var counter = KoksUIBuilder.Txt("Counter", cp.transform, "", medium, 22, mute, TextAlignmentOptions.MidlineLeft, 28, 340, 564, 40);

            var hud = root.AddComponent<InspectHud>();
            hud.progressPanel = pp;
            hud.cardPanel = cp;
            hud.ringFill = ring;
            hud.percentText = pct;
            hud.progressTitle = ptitle;
            hud.progressHint = phint;
            hud.cardGroup = group;
            hud.cardTitle = ctitle;
            hud.cardText = ctext;
            hud.counterText = counter;
            hud.segments = segs;
            pp.SetActive(false);
            cp.SetActive(false);
            return hud;
        }

        static GameObject Panel(string name, Transform parent, float w, float h)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(w, h);
            KoksUIBuilder.Img("Fill", g.transform, KoksUIBuilder.Spr("panel_fill"), KoksUIBuilder.Hex("14191E", 0.95f), 0, 0, w, h);
            KoksUIBuilder.Img("Hazard", g.transform, KoksUIBuilder.Spr("hazard"), Color.white, 0, 0, 220, 8, Image.Type.Simple);
            KoksUIBuilder.Img("Frame", g.transform, KoksUIBuilder.Spr("panel_frame"), new Color(1f, 1f, 1f, 0.16f), 0, 0, w, h);
            return g;
        }

        static Image Part(string name, Transform parent, float size, float x, float y, float rotZ, Color c, float w = -1f, float h = -1f)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w < 0 ? size : w, h < 0 ? size : h);
            r.localRotation = Quaternion.Euler(0, 0, rotZ);
            var im = g.GetComponent<Image>();
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        static void BuildMarker(Transform parent, string name, InspectPoint p)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.sizeDelta = new Vector2(120, 120);
            r.localScale = Vector3.one * 0.0016f; // ~15 см
            var cv = g.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;

            Color heat = KoksUIBuilder.Heat, dark = KoksUIBuilder.Hex("14191E");
            var outer = Part("Outer", g.transform, 84, 0, 0, 45, heat);
            Part("Inner", g.transform, 56, 0, 0, 45, dark);
            var center = Part("Center", g.transform, 26, 0, 0, 45, heat);

            var check = new GameObject("Check", typeof(RectTransform));
            check.transform.SetParent(g.transform, false);
            var cr = (RectTransform)check.transform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = Vector2.zero;
            Part("Short", check.transform, 0, -9, -6, 45, KoksUIBuilder.Hex("14191E"), 9, 22);
            Part("Long", check.transform, 0, 8, 0, -45, KoksUIBuilder.Hex("14191E"), 9, 40);
            check.SetActive(false);

            p.marker = g.transform;
            p.markerOuter = outer;
            p.markerCenter = center.gameObject;
            p.markerCheck = check;
        }
    }
}
