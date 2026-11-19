using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace KoksUI.Editor
{
    /// <summary>Koks → Build UI → 6. Собрать вводное окно сценария (в открытую сцену).</summary>
    public static class KoksIntroBuilder
    {
        const float W = 700f, H = 560f;

        [MenuItem("Koks/Build UI/6. Собрать вводное окно сценария (FirstScene)")]
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

            var old = GameObject.Find("CanvasIntro");
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find("CanvasIntro");
            }

            var root = new GameObject("CanvasIntro", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(W, H);
            rt.localScale = Vector3.one * 0.001f; // 700 px = 0,7 м
            rt.position = new Vector3(0f, 1.6f, 1.5f); // на старте окно само встанет перед игроком

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

            // статус
            var chip = KoksUIBuilder.Img("StatusChip", root.transform, KoksUIBuilder.Spr("chip"), Color.white, 28, 34, 340, 48);
            KoksUIBuilder.Img("Dot", chip.transform, KoksUIBuilder.Spr("dot"), KoksUIBuilder.Ok, 14, 15, 18, 18, Image.Type.Simple);
            var status = KoksUIBuilder.Txt("Text", chip.transform, "Сценарий начался", medium, 24, Color.white,
                TextAlignmentOptions.MidlineLeft, 40, 0, 294, 48);

            // заголовок и описание
            var title = KoksUIBuilder.Txt("Title", root.transform, "", heading, 44, Color.white,
                TextAlignmentOptions.TopLeft, 28, 108, 644, 110, true);
            var desc = KoksUIBuilder.Txt("Description", root.transform, "", body, 27, mute,
                TextAlignmentOptions.TopLeft, 28, 228, 644, 112, true);

            KoksUIBuilder.Img("Divider", root.transform, null, new Color(1f, 1f, 1f, 0.16f), 28, 354, 644, 2);

            // пункты
            var labels = new TMP_Text[3];
            var bars = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 374f + i * 56f;
                bars[i] = KoksUIBuilder.Img("Bar" + (i + 1), root.transform, null, heat, 28, y + 4, 10, 40).gameObject;
                labels[i] = KoksUIBuilder.Txt("Bullet" + (i + 1), root.transform, "", medium, 28, ink,
                    TextAlignmentOptions.MidlineLeft, 54, y, 618, 48);
            }

            // таймер
            KoksUIBuilder.Img("ProgressTrack", root.transform, null, new Color(1f, 1f, 1f, 0.14f), 0, H - 10f, W, 10);
            var fill = KoksUIBuilder.Img("ProgressFill", root.transform, null, heat, 0, H - 10f, W, 10);

            // закрыть
            var closeImg = KoksUIBuilder.Img("CloseButton", root.transform, KoksUIBuilder.Spr("btn_outline"),
                Color.white, 612, 28, 60, 60);
            closeImg.raycastTarget = true;
            var closeBtn = closeImg.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = new Color(1f, 1f, 1f, 0.55f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = new Color(1f, 1f, 1f, 0.55f);
            cb.pressedColor = heat;
            cb.fadeDuration = 0.08f;
            closeBtn.colors = cb;
            KoksUIBuilder.Txt("Label", closeImg.transform, "X", heading, 34, Color.white,
                TextAlignmentOptions.Center, 0, 0, 60, 60);

            var hover = KoksUIBuilder.Img("Hover", closeImg.transform, KoksUIBuilder.Spr("btn_outline"),
                new Color(1f, 0.769f, 0f, 0.35f), 0, 0, 60, 60);
            hover.gameObject.SetActive(false);
            var hv = closeImg.gameObject.AddComponent<UIHover>();
            hv.target = hover.gameObject;

            // компонент с настройками
            var win = root.AddComponent<IntroWindow>();
            win.statusLabel = status;
            win.titleLabel = title;
            win.descriptionLabel = desc;
            win.bulletLabels = labels;
            win.bulletBars = bars;
            win.progressFill = fill.rectTransform;
            win.group = group;
            win.closeButton = closeBtn;

            status.text = win.statusText;
            title.text = win.title;
            desc.text = win.description;
            for (int i = 0; i < labels.Length; i++)
                labels[i].text = i < win.bullets.Length ? win.bullets[i] : "";

            // полоса таймера убывает слева направо: масштаб от левого края
            fill.rectTransform.pivot = new Vector2(0f, 1f);
            fill.rectTransform.anchoredPosition = new Vector2(0f, -(H - 10f));

            EditorUtility.SetDirty(win);
            Selection.activeGameObject = root;
            var scene = root.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Вводное окно собрано: CanvasIntro. Тексты — в Inspector, компонент Intro Window.");
        }
    }
}
