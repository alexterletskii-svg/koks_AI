using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 20. Экзамен в FirstScene: контроллер ExamController, кольцо осмотра, значок «Журнал»,
    /// итоговое окно. В режиме обучения всё это спит. Можно запускать повторно (пересоберёт заново).
    /// </summary>
    public static class KoksExamBuilder
    {
        static readonly string[] Names = { "ExamController", "CanvasExamRing", "CanvasExamJournal", "CanvasExamResult", "CanvasExamFinish" };

        [MenuItem("Koks/Build UI/20. Экзамен: контроллер, итоговое окно, значок журнала (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorSceneManager.GetActiveScene().path.EndsWith("FirstScene.unity"))
                EditorSceneManager.OpenScene("Assets/Scenes/FirstScene.unity", OpenSceneMode.Single);

            KoksUIBuilder.PrepareAssets();
            var heading = KoksUIBuilder.LoadFont("RobotoCondensed-ExtraBold");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            if (heading == null || body == null || medium == null) { Debug.LogError("[Koks UI] Нет шрифтов TMP."); return; }

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.parent == null && System.Array.IndexOf(Names, t.name) >= 0) Object.DestroyImmediate(t.gameObject);

            var ctrlGo = new GameObject("ExamController");
            var ctrl = ctrlGo.AddComponent<ExamController>();

            // ---- кольцо прогресса осмотра (перед глазами, пока держите луч на детали)
            var ring = NewCanvas("CanvasExamRing", 200, 200, 0.0007f, false);
            KoksUIBuilder.Img("RingBg", ring.transform, KoksUIBuilder.Spr("dot"), new Color(1f, 1f, 1f, 0.2f), 0, 0, 200, 200, Image.Type.Simple);
            var fill = KoksUIBuilder.Img("RingFill", ring.transform, KoksUIBuilder.Spr("dot"), KoksUIBuilder.Heat, 0, 0, 200, 200, Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            KoksUIBuilder.Img("RingHole", ring.transform, KoksUIBuilder.Spr("dot"), KoksUIBuilder.Hex("14191E", 0.96f), 28, 28, 144, 144, Image.Type.Simple);
            ring.SetActive(false);

            // ---- значок «Журнал»
            var journal = Object.FindFirstObjectByType<JournalWindow>(FindObjectsInactive.Include);
            if (journal == null) Debug.LogWarning("[Koks UI] В сцене нет JournalWindow: значок журнала ничего не откроет (запустите меню журнала).");
            var jr = NewCanvas("CanvasExamJournal", 170, 190, 0.0012f, true);
            var jbg = KoksUIBuilder.Img("Bg", jr.transform, KoksUIBuilder.Spr("panel_fill"), KoksUIBuilder.Hex("14191E", 0.95f), 0, 0, 170, 190);
            jbg.raycastTarget = true;
            KoksUIBuilder.Img("Frame", jr.transform, KoksUIBuilder.Spr("panel_frame"), KoksUIBuilder.Heat, 0, 0, 170, 190);
            KoksUIBuilder.Img("Icon", jr.transform, KoksUIBuilder.Spr("icon_list"), KoksUIBuilder.Heat, 35, 22, 100, 100, Image.Type.Simple);
            KoksUIBuilder.Txt("Label", jr.transform, "Журнал", medium, 30, Color.white, TextAlignmentOptions.Center, 0, 130, 170, 44);
            var jbtn = jbg.gameObject.AddComponent<Button>();
            jbtn.targetGraphic = jbg;
            jbtn.colors = KoksUIBuilder.Tint(Color.white, new Color(1f, 0.93f, 0.6f, 1f), new Color(0.8f, 0.7f, 0.2f, 1f), Color.gray);
            var jicon = jr.AddComponent<ExamJournalIcon>();
            jicon.button = jbtn;
            jicon.journal = journal;

            // ---- окно по ручке двери: 2 перехода + «Завершить экзамен»
            const float FW = 1028f, FH = 408f;
            var fg = NewCanvas("CanvasExamFinish", FW, FH, 0.001f, true);
            var ft = fg.transform;
            KoksUIBuilder.Img("PanelFill", ft, KoksUIBuilder.Spr("panel_fill"), KoksUIBuilder.Hex("14191E", 0.96f), 0, 0, FW, FH);
            KoksUIBuilder.Img("Hazard", ft, KoksUIBuilder.Spr("hazard"), Color.white, 0, 0, 260, 10, Image.Type.Simple);
            KoksUIBuilder.Img("PanelFrame", ft, KoksUIBuilder.Spr("panel_frame"), new Color(1f, 1f, 1f, 0.16f), 0, 0, FW, FH);
            KoksUIBuilder.Txt("Title", ft, "Выберите действие", heading, 40, Color.white, TextAlignmentOptions.MidlineLeft, 28, 28, 800, 56);
            var cardCol = KoksUIBuilder.Hex("1B2127");
            var roomBtns = new Button[2];
            var roomTitles = new TMP_Text[2];
            var roomCards = new GameObject[2];
            for (int i = 0; i < 3; i++)
            {
                float x = 28f + i * 336f;
                var card = KoksUIBuilder.Img("Card" + (i + 1), ft, KoksUIBuilder.Spr("chip"), cardCol, x, 104f, 300f, 276f);
                KoksUIBuilder.Img("Stripe", card.transform, KoksUIBuilder.Spr("hazard"), Color.white, 0, 0, 120, 8, Image.Type.Simple);
                bool fin = i == 2;
                var title = KoksUIBuilder.Txt("Title", card.transform, fin ? "Завершить экзамен и посмотреть результат" : "Помещение", medium, 28, Color.white, TextAlignmentOptions.TopLeft, 18, 26, 264, 130);
                var bi = KoksUIBuilder.Img(fin ? "FinishButton" : "RoomButton", card.transform, KoksUIBuilder.Spr(fin ? "btn_heat" : "btn_outline"), Color.white, 18, 180, 264, 74);
                bi.raycastTarget = true;
                var bb = bi.gameObject.AddComponent<Button>();
                bb.targetGraphic = bi;
                bb.colors = fin
                    ? KoksUIBuilder.Tint(KoksUIBuilder.Heat, KoksUIBuilder.HeatHi, KoksUIBuilder.Hex("E0AB00"), KoksUIBuilder.Hex("6B6F73"))
                    : KoksUIBuilder.Tint(new Color(1f, 1f, 1f, 0.5f), Color.white, new Color(0.8f, 0.8f, 0.8f, 1f), new Color(1f, 1f, 1f, 0.2f));
                KoksUIBuilder.Txt("Label", bi.transform, fin ? "Завершить" : "Перейти", heading, 30, fin ? KoksUIBuilder.Dark : Color.white, TextAlignmentOptions.Center, 0, 0, 264, 74);
                if (fin) { finishBtnRef = bb; }
                else { roomBtns[i] = bb; roomTitles[i] = title; roomCards[i] = card.gameObject; }
            }
            var xImg = KoksUIBuilder.Img("CloseButton", ft, KoksUIBuilder.Spr("chip"), KoksUIBuilder.Hex("2A3138"), FW - 28f - 56f, 24f, 56f, 56f);
            xImg.raycastTarget = true;
            var xBtn = xImg.gameObject.AddComponent<Button>();
            xBtn.targetGraphic = xImg;
            xBtn.colors = KoksUIBuilder.Tint(Color.white, new Color(1f, 0.93f, 0.6f, 1f), new Color(0.8f, 0.7f, 0.2f, 1f), Color.gray);
            KoksUIBuilder.Txt("Label", xImg.transform, "X", heading, 30, Color.white, TextAlignmentOptions.Center, 0, 0, 56, 56);
            var fwin = fg.AddComponent<ExamFinishWindow>();
            fwin.roomButtons = roomBtns; fwin.roomTitles = roomTitles; fwin.roomCards = roomCards;
            fwin.finishButton = finishBtnRef; fwin.closeButton = xBtn;
            fwin.group = fg.GetComponent<CanvasGroup>();
            fg.GetComponent<Canvas>().enabled = false;

            // ---- итоговое окно
            const float W = 700f;
            float H = 298f + ExamController.TaskNames.Length * 40f + 16f + 84f + 32f;
            var rg = NewCanvas("CanvasExamResult", W, H, 0.001f, true);
            var rt = rg.transform;
            KoksUIBuilder.Img("PanelFill", rt, KoksUIBuilder.Spr("panel_fill"), KoksUIBuilder.Hex("14191E", 0.96f), 0, 0, W, H);
            KoksUIBuilder.Img("Hazard", rt, KoksUIBuilder.Spr("hazard"), Color.white, 0, 0, 260, 10, Image.Type.Simple);
            KoksUIBuilder.Img("PanelFrame", rt, KoksUIBuilder.Spr("panel_frame"), new Color(1f, 1f, 1f, 0.16f), 0, 0, W, H);
            var chip = KoksUIBuilder.Img("StatusChip", rt, KoksUIBuilder.Spr("chip"), Color.white, 28, 34, 340, 48);
            KoksUIBuilder.Img("Dot", chip.transform, KoksUIBuilder.Spr("dot"), KoksUIBuilder.Ok, 14, 15, 18, 18, Image.Type.Simple);
            KoksUIBuilder.Txt("Text", chip.transform, "Экзамен завершён", medium, 24, Color.white, TextAlignmentOptions.MidlineLeft, 40, 0, 294, 48);
            KoksUIBuilder.Txt("Title", rt, "Осмотр газового тоннеля", heading, 42, Color.white, TextAlignmentOptions.MidlineLeft, 28, 96, 644, 60);

            var card = KoksUIBuilder.Hex("1B2127");
            var b1 = KoksUIBuilder.Img("StatTime", rt, KoksUIBuilder.Spr("chip"), card, 28, 172, 316, 92);
            KoksUIBuilder.Txt("Caption", b1.transform, "Время", body, 22, KoksUIBuilder.Mute, TextAlignmentOptions.MidlineLeft, 18, 8, 280, 30);
            var timeLbl = KoksUIBuilder.Txt("Value", b1.transform, "00:00", heading, 46, Color.white, TextAlignmentOptions.MidlineLeft, 18, 36, 280, 52);
            var b2 = KoksUIBuilder.Img("StatScore", rt, KoksUIBuilder.Spr("chip"), card, 356, 172, 316, 92);
            KoksUIBuilder.Txt("Caption", b2.transform, "Выполнено заданий", body, 22, KoksUIBuilder.Mute, TextAlignmentOptions.MidlineLeft, 18, 8, 280, 30);
            var scoreLbl = KoksUIBuilder.Txt("Value", b2.transform, "0 из 9", heading, 46, Color.white, TextAlignmentOptions.MidlineLeft, 18, 36, 280, 52);

            KoksUIBuilder.Img("Divider", rt, null, new Color(1f, 1f, 1f, 0.16f), 28, 284, 644, 2);
            int n = ExamController.TaskNames.Length;
            var labels = new TMP_Text[n];
            var dots = new Image[n];
            for (int i = 0; i < n; i++)
            {
                float y = 298f + i * 40f;
                dots[i] = KoksUIBuilder.Img("Dot" + (i + 1), rt, KoksUIBuilder.Spr("dot"), KoksUIBuilder.Ok, 28, y + 12, 16, 16, Image.Type.Simple);
                labels[i] = KoksUIBuilder.Txt("Task" + (i + 1), rt, ExamController.TaskNames[i], medium, 24, KoksUIBuilder.Ink, TextAlignmentOptions.MidlineLeft, 58, y, 614, 40);
            }

            var btnImg = KoksUIBuilder.Img("MenuButton", rt, KoksUIBuilder.Spr("btn_heat"), Color.white, 28, 298f + n * 40f + 16f, 644, 84);
            btnImg.raycastTarget = true;
            var btn = btnImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.colors = KoksUIBuilder.Tint(KoksUIBuilder.Heat, KoksUIBuilder.HeatHi, KoksUIBuilder.Hex("E0AB00"), KoksUIBuilder.Hex("6B6F73"));
            KoksUIBuilder.Txt("Label", btnImg.transform, "В меню выбора сценариев", heading, 32, KoksUIBuilder.Dark, TextAlignmentOptions.Center, 0, 0, 644, 84);
            var hover = KoksUIBuilder.Img("Hover", btnImg.transform, KoksUIBuilder.Spr("btn_heat"), new Color(1f, 1f, 1f, 0.35f), 0, 0, 644, 84);
            hover.gameObject.SetActive(false);
            btnImg.gameObject.AddComponent<UIHover>().target = hover.gameObject;

            var win = rg.AddComponent<ExamResultWindow>();
            win.timeLabel = timeLbl; win.scoreLabel = scoreLbl;
            win.rowLabels = labels; win.rowDots = dots;
            win.menuButton = btn;
            win.group = rg.GetComponent<CanvasGroup>();

            ctrl.result = win;
            ctrl.finishWindow = fwin;
            ctrl.journalIcon = jicon;
            ctrl.ringRoot = ring;
            ctrl.ringFill = fill;
            ctrl.journal = journal;

            // окна лежат в стороне и не видны до своего часа
            rg.GetComponent<Canvas>().enabled = false;
            jr.GetComponent<Canvas>().enabled = false;

            EditorUtility.SetDirty(ctrl);
            Selection.activeGameObject = ctrlGo;
            EditorSceneManager.MarkSceneDirty(ctrlGo.scene);
            EditorSceneManager.SaveScene(ctrlGo.scene);
            Debug.Log("[Koks UI] Экзамен собран: ExamController, CanvasExamRing, CanvasExamJournal, CanvasExamResult. Проверьте, что в сцене есть объект door.004.");
        }

        static Button finishBtnRef;

        static GameObject NewCanvas(string name, float w, float h, float scale, bool raycaster)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(w, h);
            rt.localScale = Vector3.one * scale;
            rt.position = new Vector3(0f, -50f, 0f);   // далеко внизу: появится перед игроком по команде скрипта
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            if (raycaster) root.AddComponent<TrackedDeviceGraphicRaycaster>();
            root.AddComponent<CanvasGroup>();
            return root;
        }
    }
}
