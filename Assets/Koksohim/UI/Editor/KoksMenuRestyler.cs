using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 4. Перестилизовать окна меню.
    /// Меняет только внешний вид панелей «Обучение» и «Выбор сценария» в MenuScene.
    /// Кнопки, их onClick-вызовы и объекты не пересоздаются — логика сцены остаётся прежней.
    /// Можно запускать повторно: скрипт сам удаляет свои прежние украшения.
    /// </summary>
    public static class KoksMenuRestyler
    {
        const string Root = "Assets/Koksohim/UI";
        const string FontsDir = Root + "/Fonts";
        const string SpritesDir = Root + "/Sprites";

        static readonly Color Heat = Hex("FFC400");
        static readonly Color HeatHi = Hex("FFD84D");
        static readonly Color Ink = Hex("EEF1F3");
        static readonly Color Mute = Hex("A4AEB6");
        static readonly Color Dark = Hex("14181C");
        static readonly Color Card = Hex("1E252B");

        static TMP_FontAsset heading, body, medium;

        static Color Hex(string h, float a = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + h, out Color c);
            c.a = a;
            return c;
        }

        [MenuItem("Koks/Build UI/4. Перестилизовать окна меню (MenuScene)")]
        public static void Restyle()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз.");
                return;
            }

            KoksUIBuilder.PrepareAssets();
            heading = Font("RobotoCondensed-ExtraBold");
            body = Font("Inter-Regular");
            medium = Font("Inter-Medium");
            if (heading == null || body == null || medium == null)
            {
                Debug.LogError("[Koks UI] Нет шрифтов TMP. Запустите пункт 1.");
                return;
            }

            Canvas canvas = null;
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.gameObject.name == "CanvasMenu") { canvas = c; break; }
            if (canvas == null)
            {
                Debug.LogError("[Koks UI] В открытой сцене нет CanvasMenu. Откройте MenuScene.");
                return;
            }

            Transform tutorial = canvas.transform.Find("PanelTutorial");
            Transform select = canvas.transform.Find("PanelSelect");
            if (tutorial == null || select == null)
            {
                Debug.LogError("[Koks UI] Не найдены PanelTutorial / PanelSelect.");
                return;
            }

            RestyleTutorial(tutorial);
            RestyleSelect(select);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Debug.Log("[Koks UI] Окна меню перестилизованы.");
        }

        // ------------------------------------------------------ Обучение

        static void RestyleTutorial(Transform panel)
        {
            StylePanel(panel);

            foreach (Transform ch in panel)
            {
                if (ch.name.StartsWith("Koks")) continue;
                var rt = (RectTransform)ch;
                var btn = ch.GetComponent<Button>();
                var img = ch.GetComponent<Image>();

                if (btn != null)
                {
                    // «Нажмите для выбора сценария»
                    SetRect(rt, 0f, -432f, 800f, 100f);
                    StyleHeatButton(btn, img, 40f);
                    continue;
                }

                if (rt.sizeDelta.x > 1800f)
                {
                    // схема контроллеров — картинка как есть
                    continue;
                }

                var tmp = FirstOriginalText(ch);

                if (rt.sizeDelta.x >= 1600f)
                {
                    // приветствие
                    SetRect(rt, 0f, 385f, 1700f, 170f);
                    Fill(img, Card);
                    AddFrame(rt, new Color(1f, 1f, 1f, 0.16f));
                    AddAccent(rt, Heat);
                    if (tmp != null)
                    {
                        SetRect((RectTransform)tmp.transform, 18f, 0f, 1620f, 140f);
                        Style(tmp, medium, 36f, Ink, TextAlignmentOptions.MidlineLeft, true, 24f, 40f);
                    }
                }
                else if (rt.sizeDelta.x >= 550f)
                {
                    // заголовок-плашка «Как происходит управление…»
                    SetRect(rt, 0f, 250f, 780f, 80f);
                    Fill(img, Heat);
                    if (tmp != null)
                    {
                        SetRect((RectTransform)tmp.transform, 0f, 0f, 740f, 70f);
                        Style(tmp, heading, 36f, Dark, TextAlignmentOptions.Center, false, 0f, 0f);
                    }
                }
                else
                {
                    // боковые текстовые блоки
                    float x = rt.anchoredPosition.x < 0f ? -602f : 602f;
                    SetRect(rt, x, -108f, 500f, 500f);
                    Fill(img, Card);
                    AddFrame(rt, new Color(1f, 1f, 1f, 0.16f));
                    AddAccent(rt, Heat);
                    if (tmp != null)
                    {
                        SetRect((RectTransform)tmp.transform, 8f, 0f, 440f, 460f);
                        Style(tmp, body, 28f, Ink, TextAlignmentOptions.TopLeft, true, 20f, 30f);
                    }
                }
            }

            // кнопка — последним, чтобы ничто не перекрывало луч
            foreach (Transform ch in panel)
                if (ch.GetComponent<Button>() != null) ch.SetAsLastSibling();

            DisableRaycasts(panel);
        }

        // -------------------------------------------------- Выбор сценария

        static void RestyleSelect(Transform panel)
        {
            StylePanel(panel);

            var title = EnsureText(panel, "KoksTitle", "Выберите сценарий", heading, 64f, Color.white,
                                   TextAlignmentOptions.MidlineLeft, -300f, 420f, 1000f, 90f);
            title.raycastTarget = false;
            var hint = EnsureText(panel, "KoksHint", "Наведите луч на сценарий и нажмите триггер", body, 26f, Mute,
                                  TextAlignmentOptions.Center, 0f, -430f, 1600f, 44f);
            hint.raycastTarget = false;

            string[] names = { "ButtonTop", "ButtonTop (1)", "ButtonTop (2)" };
            float[] ys = { 250f, 56f, -138f };

            for (int i = 0; i < names.Length; i++)
            {
                Transform row = panel.Find(names[i]);
                if (row == null) { Debug.LogWarning("[Koks UI] Нет кнопки " + names[i]); continue; }

                var rt = (RectTransform)row;
                var btn = row.GetComponent<Button>();
                var img = row.GetComponent<Image>();
                bool ready = i == 0; // у остальных сценариев пока нет onClick

                SetRect(rt, 0f, ys[i], 1640f, 170f);
                img.sprite = Spr("chip");
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                img.raycastTarget = true;

                var cb = ColorBlock.defaultColorBlock;
                cb.normalColor = Card;
                cb.highlightedColor = Hex("2B343C");
                cb.selectedColor = Card;
                cb.pressedColor = Hex("3A3520");
                cb.disabledColor = Hex("1E252B", 0.6f);
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.08f;
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = img;
                btn.colors = cb;

                Clean(row, "KoksFrame");
                Clean(row, "KoksAccent");
                Clean(row, "KoksNum");
                Clean(row, "KoksAction");
                AddFrame(rt, new Color(1f, 1f, 1f, ready ? 0.22f : 0.1f));
                AddAccent(rt, ready ? Heat : Hex("3A444C"));

                var num = EnsureText(row, "KoksNum", "0" + (i + 1), heading, 72f, ready ? Heat : Hex("5A656D"),
                                     TextAlignmentOptions.Center, -700f, 0f, 160f, 110f);
                num.raycastTarget = false;
                var act = EnsureText(row, "KoksAction", ready ? "Выбрать" : "Скоро", heading, 36f,
                                     ready ? Heat : Hex("5A656D"),
                                     TextAlignmentOptions.MidlineRight, 690f, 0f, 220f, 60f);
                act.raycastTarget = false;

                var tmp = FirstOriginalText(row);
                if (tmp != null)
                {
                    SetRect((RectTransform)tmp.transform, -20f, 0f, 1160f, 150f);
                    Style(tmp, heading, 44f, ready ? Color.white : Mute, TextAlignmentOptions.MidlineLeft, true, 26f, 46f);
                }
            }

            DisableRaycasts(panel);
        }

        // ------------------------------------------------------- общее

        static void StylePanel(Transform panel)
        {
            var rt = (RectTransform)panel;
            var img = panel.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = Spr("panel_fill");
                img.type = Image.Type.Sliced;
                img.color = Hex("14191E", 0.95f);
            }

            Clean(panel, "KoksFrame");
            Clean(panel, "KoksHazard");

            var frame = NewImage("KoksFrame", panel, Spr("panel_frame"), new Color(1f, 1f, 1f, 0.16f), Image.Type.Sliced);
            Stretch((RectTransform)frame.transform);
            frame.transform.SetSiblingIndex(0);

            var haz = NewImage("KoksHazard", panel, Spr("hazard"), Color.white, Image.Type.Simple);
            var hr = (RectTransform)haz.transform;
            hr.anchorMin = hr.anchorMax = new Vector2(0f, 1f);
            hr.pivot = new Vector2(0f, 1f);
            hr.anchoredPosition = Vector2.zero;
            hr.sizeDelta = new Vector2(260f, 10f);
            haz.transform.SetSiblingIndex(1);
        }

        static void StyleHeatButton(Button btn, Image img, float size)
        {
            img.sprite = Spr("btn_heat");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true;

            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Heat;
            cb.highlightedColor = HeatHi;
            cb.selectedColor = Heat;
            cb.pressedColor = Hex("E0AB00");
            cb.disabledColor = Hex("6B6F73");
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            btn.colors = cb;

            var tmp = FirstOriginalText(btn.transform);
            if (tmp != null)
            {
                Stretch((RectTransform)tmp.transform);
                Style(tmp, heading, size, Dark, TextAlignmentOptions.Center, false, 0f, 0f);
            }
        }

        static void Fill(Image img, Color color)
        {
            if (img == null) return;
            img.sprite = Spr("chip");
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        static void AddFrame(RectTransform parent, Color color)
        {
            Clean(parent, "KoksFrame");
            var f = NewImage("KoksFrame", parent, Spr("card"), color, Image.Type.Sliced);
            Stretch((RectTransform)f.transform);
            // сразу после фона: ниже текста
            f.transform.SetSiblingIndex(0);
        }

        static void AddAccent(RectTransform parent, Color color)
        {
            Clean(parent, "KoksAccent");
            var a = NewImage("KoksAccent", parent, null, color, Image.Type.Simple);
            var r = (RectTransform)a.transform;
            r.anchorMin = new Vector2(0f, 0f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(10f, 0f);
            a.transform.SetSiblingIndex(1);
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Color color, Image.Type type)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = sprite;
            im.type = sprite == null ? Image.Type.Simple : type;
            im.color = color;
            im.raycastTarget = false;
            return im;
        }

        static TextMeshProUGUI EnsureText(Transform parent, string name, string text, TMP_FontAsset font,
                                          float size, Color color, TextAlignmentOptions align,
                                          float x, float y, float w, float h)
        {
            Clean(parent, name);
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            SetRect((RectTransform)go.transform, x, y, w, h);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            Style(t, font, size, color, align, false, 0f, 0f);
            t.raycastTarget = false;
            return t;
        }

        static TextMeshProUGUI FirstOriginalText(Transform row)
        {
            foreach (Transform ch in row)
            {
                if (ch.name.StartsWith("Koks")) continue;
                if (ch.GetComponent<Text>() != null || ch.GetComponent<TextMeshProUGUI>() != null)
                    return ToTmp(ch.gameObject);
            }
            return null;
        }

        /// <summary>Заменяет legacy Text на TextMeshPro, сохраняя текст.</summary>
        static TextMeshProUGUI ToTmp(GameObject go)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
            var old = go.GetComponent<Text>();
            if (old == null) return null;
            string s = old.text;
            Object.DestroyImmediate(old);
            tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = s;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Style(TextMeshProUGUI t, TMP_FontAsset font, float size, Color color,
                          TextAlignmentOptions align, bool wrap, float autoMin, float autoMax)
        {
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            if (autoMax > 0f)
            {
                t.enableAutoSizing = true;
                t.fontSizeMin = autoMin;
                t.fontSizeMax = autoMax;
            }
            else
            {
                t.enableAutoSizing = false;
            }
        }

        static void DisableRaycasts(Transform panel)
        {
            foreach (var g in panel.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            foreach (var s in panel.GetComponentsInChildren<Selectable>(true))
                if (s.targetGraphic != null)
                {
                    s.targetGraphic.raycastTarget = true;
                    s.targetGraphic.canvasRenderer.cullTransparentMesh = false;
                }
        }

        static void Clean(Transform parent, string name)
        {
            var t = parent.Find(name);
            while (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
                t = parent.Find(name);
            }
        }

        static void SetRect(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        static TMP_FontAsset Font(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsDir + "/" + name + " SDF.asset");
        }

        static Sprite Spr(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/" + name + ".png");
            if (s == null) Debug.LogError("[Koks UI] Нет спрайта " + name);
            return s;
        }
    }
}
