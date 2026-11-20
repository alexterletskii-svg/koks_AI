using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace KoksUI.Editor
{
    /// <summary>Собирает окно «Оперативный журнал» (используется меню 13).</summary>
    internal static class KoksJournalBuilder
    {
        const float W = 1000f, H = 690f;

        static Image Line(Transform parent, float x, float y, float w, float h)
        {
            return KoksUIBuilder.Img("Line", parent, null, KoksUIBuilder.Hex("3A372F"), x, y, w, h, Image.Type.Simple);
        }

        internal static JournalWindow Create(string name, Vector3 pos, TMP_FontAsset heading, TMP_FontAsset body, TMP_FontAsset medium)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.parent == null && t.name == name) Object.DestroyImmediate(t.gameObject);

            var root = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(W, H);
            rt.localScale = Vector3.one * 0.001f;   // 1000 px = 1 м
            rt.position = pos;
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
            var group = root.AddComponent<CanvasGroup>();

            Color heat = KoksUIBuilder.Heat, dark = KoksUIBuilder.Dark;
            Color ink = KoksUIBuilder.Hex("1F3A6E");        // «синие чернила»
            Color pen = KoksUIBuilder.Hex("2F2D27");        // печатный текст бланка
            Color paperCol = KoksUIBuilder.Hex("E0DAC8");

            KoksUIBuilder.Img("PanelFill", root.transform, KoksUIBuilder.Spr("panel_fill"), KoksUIBuilder.Hex("14191E", 0.95f), 0, 0, W, H);
            KoksUIBuilder.Img("Hazard", root.transform, KoksUIBuilder.Spr("hazard"), Color.white, 0, 0, 260, 10, Image.Type.Simple);
            KoksUIBuilder.Img("PanelFrame", root.transform, KoksUIBuilder.Spr("panel_frame"), new Color(1f, 1f, 1f, 0.16f), 0, 0, W, H);

            var chip = KoksUIBuilder.Img("StepChip", root.transform, KoksUIBuilder.Spr("chip"), Color.white, 28, 30, 200, 48);
            KoksUIBuilder.Img("Dot", chip.transform, KoksUIBuilder.Spr("dot"), heat, 14, 15, 18, 18, Image.Type.Simple);
            KoksUIBuilder.Txt("Text", chip.transform, "Журнал", medium, 24, Color.white, TextAlignmentOptions.MidlineLeft, 40, 0, 154, 48);
            KoksUIBuilder.Txt("Title", root.transform, "Запишите результат осмотра", heading, 40, Color.white,
                TextAlignmentOptions.MidlineLeft, 252, 26, 720, 56);

            // ---- бумажная страница
            const float x0 = 28f, y0 = 100f, pw = 944f, ph = 450f;
            var paper = KoksUIBuilder.Img("Paper", root.transform, null, paperCol, x0, y0, pw, ph, Image.Type.Simple).transform;
            KoksUIBuilder.Txt("JournalTitle", paper, "ОПЕРАТИВНЫЙ ЖУРНАЛ", heading, 30, pen, TextAlignmentOptions.Center, 0, 4, pw, 40);
            Line(paper, 0, 44, pw, 2);
            KoksUIBuilder.Txt("H1", paper, "Дата и время", body, 24, pen, TextAlignmentOptions.Center, 4, 50, 162, 76, true);
            KoksUIBuilder.Txt("H2", paper, "Содержание сообщений в течение смены, подписи о сдаче и приёмке смены", body, 24, pen, TextAlignmentOptions.Center, 174, 50, 552, 76, true);
            KoksUIBuilder.Txt("H3", paper, "Визы, замечания", body, 24, pen, TextAlignmentOptions.Center, 734, 50, 206, 76, true);
            Line(paper, 0, 130, pw, 2);
            KoksUIBuilder.Txt("N1", paper, "1", body, 22, pen, TextAlignmentOptions.Center, 0, 134, 170, 26);
            KoksUIBuilder.Txt("N2", paper, "2", body, 22, pen, TextAlignmentOptions.Center, 170, 134, 560, 26);
            KoksUIBuilder.Txt("N3", paper, "3", body, 22, pen, TextAlignmentOptions.Center, 730, 134, 214, 26);
            Line(paper, 0, 162, pw, 2);
            Line(paper, 0, 208, pw, 1);
            Line(paper, 0, 340, pw, 1);
            Line(paper, 0, 386, pw, 1);
            Line(paper, 170, 44, 2, ph - 44);
            Line(paper, 730, 44, 2, ph - 44);

            var dateT = KoksUIBuilder.Txt("Date", paper, "", body, 28, ink, TextAlignmentOptions.TopLeft, 10, 216, 152, 120, true);
            var contentT = KoksUIBuilder.Txt("Content", paper, "", body, 30, ink, TextAlignmentOptions.TopLeft, 184, 216, 536, 120, true);
            var remarksT = KoksUIBuilder.Txt("Remarks", paper, "", body, 28, ink, TextAlignmentOptions.TopLeft, 742, 214, 196, 46, true);
            var sig = KoksUIBuilder.Img("Signature", paper, KoksUIBuilder.Spr("journal_signature"), ink, 738, 264, 196, 59, Image.Type.Filled);
            sig.fillMethod = Image.FillMethod.Horizontal;
            sig.fillOrigin = (int)Image.OriginHorizontal.Left;
            sig.fillAmount = 0f;

            // штамп «Записано»: чуть повёрнут
            var stampImg = KoksUIBuilder.Img("Stamp", paper, null, new Color(paperCol.r, paperCol.g, paperCol.b, 0.94f), 690, 382, 240, 62, Image.Type.Simple);
            var sr = (RectTransform)stampImg.transform;
            sr.pivot = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = new Vector2(690f + 120f, -(382f + 31f));
            sr.localRotation = Quaternion.Euler(0f, 0f, 6f);
            KoksUIBuilder.Img("StampFrame", stampImg.transform, KoksUIBuilder.Spr("frame4"), KoksUIBuilder.Ok, 0, 0, 240, 62);
            KoksUIBuilder.Txt("StampText", stampImg.transform, "Записано", heading, 34, KoksUIBuilder.Ok, TextAlignmentOptions.Center, 0, 0, 240, 62);
            var stampGroup = stampImg.gameObject.AddComponent<CanvasGroup>();
            stampGroup.alpha = 0f;

            // ---- кнопка
            var btnImg = KoksUIBuilder.Img("ActionButton", root.transform, KoksUIBuilder.Spr("btn_heat"), Color.white, 28, 584, 944, 84);
            btnImg.raycastTarget = true;
            var btn = btnImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.colors = KoksUIBuilder.Tint(heat, KoksUIBuilder.HeatHi, KoksUIBuilder.Hex("E0AB00"), KoksUIBuilder.Hex("6B6F73"));
            var label = KoksUIBuilder.Txt("Label", btnImg.transform, "Записать результаты осмотра", heading, 34, dark, TextAlignmentOptions.Center, 0, 0, 944, 84);
            var hover = KoksUIBuilder.Img("Hover", btnImg.transform, KoksUIBuilder.Spr("btn_heat"), new Color(1f, 0.92f, 0.55f, 0.3f), 0, 0, 944, 84);
            hover.gameObject.SetActive(false);
            btnImg.gameObject.AddComponent<UIHover>().target = hover.gameObject;

            var win = root.AddComponent<JournalWindow>();
            win.dateText = dateT;
            win.contentText = contentT;
            win.remarksText = remarksT;
            win.signature = sig;
            win.stamp = stampGroup;
            win.actionButton = btn;
            win.actionLabel = label;
            win.group = group;
            return win;
        }
    }
}
