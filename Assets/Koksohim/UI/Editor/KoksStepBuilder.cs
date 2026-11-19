using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 7. Собрать окно шага осмотра (FirstScene).</summary>
    public static class KoksStepBuilder
    {
        const float W = 700f, H = 680f;

        [MenuItem("Koks/Build UI/7. Собрать окно шага осмотра (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
                return;
            }

            // нужный круг: PointPosition без родителя (нижнее помещение, тоннель)
            GameObject marker = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "PointPosition" && t.parent == null) { marker = t.gameObject; break; }
            }
            if (marker == null)
            {
                Debug.LogError("[Koks UI] Не найден PointPosition (корневой). Откройте FirstScene.");
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

            var old = GameObject.Find("CanvasStep1");
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find("CanvasStep1");
            }

            var root = new GameObject("CanvasStep1", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(W, H);
            rt.localScale = Vector3.one * 0.001f; // 700 px = 0,7 м
            rt.position = marker.transform.position + new Vector3(0f, 1.5f, 0f);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
            var group = root.AddComponent<CanvasGroup>();

            Color heat = KoksUIBuilder.Heat, ink = KoksUIBuilder.Ink, mute = KoksUIBuilder.Mute;
            Color dark = KoksUIBuilder.Dark;

            KoksUIBuilder.Img("PanelFill", root.transform, KoksUIBuilder.Spr("panel_fill"),
                KoksUIBuilder.Hex("14191E", 0.95f), 0, 0, W, H);
            KoksUIBuilder.Img("Hazard", root.transform, KoksUIBuilder.Spr("hazard"), Color.white, 0, 0, 260, 10, Image.Type.Simple);
            KoksUIBuilder.Img("PanelFrame", root.transform, KoksUIBuilder.Spr("panel_frame"),
                new Color(1f, 1f, 1f, 0.16f), 0, 0, W, H);

            // тег шага
            var chip = KoksUIBuilder.Img("StepChip", root.transform, KoksUIBuilder.Spr("chip"), Color.white, 28, 34, 240, 48);
            KoksUIBuilder.Img("Dot", chip.transform, KoksUIBuilder.Spr("dot"), heat, 14, 15, 18, 18, Image.Type.Simple);
            var step = KoksUIBuilder.Txt("Text", chip.transform, "", medium, 24, Color.white,
                TextAlignmentOptions.MidlineLeft, 40, 0, 194, 48);

            // иконка и название
            KoksUIBuilder.Img("IconCover", root.transform, null, KoksUIBuilder.Hex("161C21"), 28, 104, 110, 110, Image.Type.Simple);
            KoksUIBuilder.Img("IconStripes", root.transform, KoksUIBuilder.Spr("stripes_tile"), Color.white, 28, 104, 110, 110, Image.Type.Tiled);
            KoksUIBuilder.Img("IconFrame", root.transform, KoksUIBuilder.Spr("card"), new Color(1f, 1f, 1f, 0.3f), 28, 104, 110, 110);
            var icon = KoksUIBuilder.Img("Icon", root.transform, KoksUIBuilder.Spr("icon_room_valve"), heat, 46, 122, 74, 74, Image.Type.Simple);
            var title = KoksUIBuilder.Txt("Title", root.transform, "", heading, 44, Color.white,
                TextAlignmentOptions.MidlineLeft, 160, 104, 512, 110, true);
            title.enableAutoSizing = true;
            title.fontSizeMin = 28;
            title.fontSizeMax = 46;

            var desc = KoksUIBuilder.Txt("Description", root.transform, "", body, 27, mute,
                TextAlignmentOptions.TopLeft, 28, 234, 644, 112, true);

            KoksUIBuilder.Img("Divider", root.transform, null, new Color(1f, 1f, 1f, 0.16f), 28, 360, 644, 2);

            var header = KoksUIBuilder.Txt("ChecksHeader", root.transform, "", medium, 27, Color.white,
                TextAlignmentOptions.MidlineLeft, 28, 372, 644, 40);

            var labels = new TMP_Text[3];
            var bars = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 420f + i * 50f;
                bars[i] = KoksUIBuilder.Img("Bar" + (i + 1), root.transform, null, heat, 28, y + 5, 10, 36).gameObject;
                labels[i] = KoksUIBuilder.Txt("Check" + (i + 1), root.transform, "", medium, 27, ink,
                    TextAlignmentOptions.MidlineLeft, 54, y, 618, 46);
            }

            // кнопка
            var btnImg = KoksUIBuilder.Img("ConfirmButton", root.transform, KoksUIBuilder.Spr("btn_heat"), Color.white, 28, 576, 644, 84);
            btnImg.raycastTarget = true;
            var btn = btnImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.colors = KoksUIBuilder.Tint(heat, KoksUIBuilder.HeatHi, KoksUIBuilder.Hex("E0AB00"), KoksUIBuilder.Hex("6B6F73"));
            var btnLabel = KoksUIBuilder.Txt("Label", btnImg.transform, "", heading, 34, dark,
                TextAlignmentOptions.Center, 0, 0, 644, 84);
            var hover = KoksUIBuilder.Img("Hover", btnImg.transform, KoksUIBuilder.Spr("btn_heat"),
                new Color(1f, 1f, 1f, 0.35f), 0, 0, 644, 84);
            hover.gameObject.SetActive(false);
            var hv = btnImg.gameObject.AddComponent<UIHover>();
            hv.target = hover.gameObject;

            // компонент окна
            var win = root.AddComponent<StepWindow>();
            win.stepLabel = step;
            win.titleLabel = title;
            win.descriptionLabel = desc;
            win.checksHeaderLabel = header;
            win.checkLabels = labels;
            win.checkBars = bars;
            win.buttonLabel = btnLabel;
            win.group = group;
            win.confirmButton = btn;

            step.text = win.stepText;
            title.text = win.title;
            desc.text = win.description;
            header.text = win.checksHeader;
            btnLabel.text = win.buttonText;
            for (int i = 0; i < labels.Length; i++)
                labels[i].text = i < win.checks.Length ? win.checks[i] : "";

            // круг: скрипт-триггер, отключаем самоуничтожение при касании
            var sm = marker.GetComponent<StepMarker>();
            if (sm == null) sm = marker.AddComponent<StepMarker>();
            sm.window = win;
            var killer = marker.GetComponent<DestroyOnCollisionByTag>();
            if (killer != null) killer.enabled = false;
            var killer2 = marker.GetComponent<DestroyOnTriggerByTag>();
            if (killer2 != null) killer2.enabled = false;

            root.SetActive(false); // окно появится, когда игрок встанет в круг

            EditorUtility.SetDirty(win);
            EditorUtility.SetDirty(sm);
            Selection.activeGameObject = root;
            var scene = root.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Окно шага собрано: CanvasStep1, круг: " + marker.name + ". Тексты — в Inspector, компонент Step Window.");
        }
    }
}
