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
            RestyleSelect(select, tutorial);
            Transform result = canvas.transform.Find("PanelResult");
            if (result != null) RestyleResult(result);
            Transform siz = canvas.transform.Find("PanelSiz");
            if (siz != null) RestyleSiz(siz);
            else Debug.LogWarning("[Koks UI] Не найдена PanelSiz.");

            // меню чуть крупнее: 1,8 м -> 2,16 м по ширине
            canvas.transform.localScale = Vector3.one * 0.0012f;

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Debug.Log("[Koks UI] Окна меню перестилизованы.");
        }

        [MenuItem("Koks/Build UI/5. Перестилизовать меню перехода (FirstScene)")]
        public static void RestyleExitMenus()
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

            int n = 0;
            UnityEngine.SceneManagement.Scene scene = default;
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.gameObject.name != "CanvasExit") continue;
                Transform panel = c.transform.Find("Panel");
                if (panel == null) continue;
                c.transform.localScale = Vector3.one * 0.0006f; // чуть крупнее (было 0.0005)
                RestyleExitPanel(panel);
                scene = c.gameObject.scene;
                n++;
            }

            if (n == 0)
            {
                Debug.LogError("[Koks UI] В открытой сцене нет CanvasExit. Откройте FirstScene.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Перестилизовано меню перехода: " + n);
        }

        /// <summary>Номер локации из persistent-вызова TeleportToLocN на кнопке (0, если не найден).</summary>
        static int DestinationLocation(Button btn)
        {
            var so = new SerializedObject(btn);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null) return 0;
            for (int i = 0; i < calls.arraySize; i++)
            {
                string m = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_MethodName").stringValue;
                if (m != null && m.StartsWith("TeleportToLoc"))
                {
                    int n;
                    if (int.TryParse(m.Substring("TeleportToLoc".Length), out n)) return n;
                }
            }
            return 0;
        }

        static void RestyleExitPanel(Transform panel)
        {
            StylePanel(panel);
            var line = new Color(1f, 1f, 1f, 0.16f);

            // заголовок
            var head = panel.Find("Image");
            if (head != null)
            {
                var r = (RectTransform)head;
                SetRect(r, -40f, 250f, 1120f, 110f);
                Fill(head.GetComponent<Image>(), Card);
                AddFrame(r, line);
                AddAccent(r, Heat);
                var t = FirstOriginalText(head);
                if (t != null)
                {
                    SetRect((RectTransform)t.transform, 14f, 0f, 1070f, 100f);
                    Style(t, heading, 40f, Color.white, TextAlignmentOptions.MidlineLeft, true, 26f, 42f);
                }
            }

            // кнопка закрытия
            var exit = panel.Find("ButtonExit");
            if (exit != null)
            {
                var r = (RectTransform)exit;
                SetRect(r, 640f, 305f, 80f, 80f);
                var img = exit.GetComponent<Image>();
                var btn = exit.GetComponent<Button>();
                img.sprite = Spr("btn_outline");
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                var cb = ColorBlock.defaultColorBlock;
                cb.normalColor = new Color(1f, 1f, 1f, 0.55f);
                cb.highlightedColor = Color.white;
                cb.selectedColor = new Color(1f, 1f, 1f, 0.55f);
                cb.pressedColor = Hex("FFC400");
                cb.fadeDuration = 0.08f;
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = img;
                btn.colors = cb;
                var t = FirstOriginalText(exit);
                if (t != null)
                {
                    Stretch((RectTransform)t.transform);
                    Style(t, heading, 40f, Color.white, TextAlignmentOptions.Center, false, 0f, 0f);
                }
                AddHover(exit, Spr("btn_outline"), new Color(1f, 0.769f, 0f, 0.35f), new Color(1f, 0.769f, 0f, 1f), false);
            }

            // кнопки переходов — карточки с иконкой
            string[] names = { "ButtonTop", "ButtonBottom" };
            string[] fallbackIcons = { "icon_box", "icon_flame" };
            const float CW = 640f, CH = 440f;
            for (int i = 0; i < names.Length; i++)
            {
                Transform card = panel.Find(names[i]);
                if (card == null) continue;
                var r = (RectTransform)card;
                var img = card.GetComponent<Image>();
                var btn = card.GetComponent<Button>();

                for (int k = card.childCount - 1; k >= 0; k--)
                    if (card.GetChild(k).name.StartsWith("Koks"))
                        Object.DestroyImmediate(card.GetChild(k).gameObject);

                SetRect(r, i == 0 ? -330f : 330f, -60f, CW, CH);
                img.sprite = Spr("chip");
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                var cb = ColorBlock.defaultColorBlock;
                cb.normalColor = Card;
                cb.highlightedColor = Hex("2B343C");
                cb.selectedColor = Card;
                cb.pressedColor = Hex("3A3520");
                cb.fadeDuration = 0.08f;
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = img;
                btn.colors = cb;

                // иконка по помещению назначения: 1 — диспетчерская, 2 — лебёдки, 3 — тоннель клапанов
                int loc = DestinationLocation(btn);
                string icon = loc == 1 ? "icon_room_control"
                            : loc == 2 ? "icon_room_winch"
                            : loc == 3 ? "icon_room_valve" : fallbackIcons[i];

                KImg(card, "KoksCover", null, Hex("161C21"), 0, 0, CW, 190, Image.Type.Simple);
                KImg(card, "KoksStripes", Spr("stripes_tile"), Color.white, 0, 0, CW, 190, Image.Type.Tiled);
                KImg(card, "KoksIcon", Spr(icon), Heat, (CW - 120f) / 2f, 35f, 120f, 120f, Image.Type.Simple);

                var action = KImg(card, "KoksAction", Spr("btn_heat"), Heat, 28, 342, CW - 56f, 70, Image.Type.Sliced);
                KTxt(action.transform, "KoksActionText", "Перейти", heading, 32f, Dark,
                     TextAlignmentOptions.Center, 0, 0, CW - 56f, 70);

                var t = FirstOriginalText(card);
                if (t != null)
                {
                    SetRect((RectTransform)t.transform, 0f, -45f, CW - 56f, 110f);
                    Style(t, heading, 38f, Color.white, TextAlignmentOptions.TopLeft, true, 24f, 38f);
                    t.transform.SetAsLastSibling();
                }

                AddHover(card, Spr("chip"), new Color(1f, 1f, 1f, 0.07f), Heat, true);
                var fr = KImg(card, "KoksFrame", Spr("card"), line, 0, 0, CW, CH, Image.Type.Sliced);
                fr.transform.SetAsLastSibling();
            }

            DisableRaycasts(panel);
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

                if (rt.sizeDelta.x > 1800f || (img != null && img.sprite != null && img.sprite.name == "i2"))
                {
                    // схема контроллеров: ровно между боковыми блоками
                    SetRect(rt, 0f, -108f, 680f, 382f);
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color = Color.white;
                    AddFrame(rt, new Color(1f, 1f, 1f, 0.3f));
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

        static void RestyleSelect(Transform panel, Transform tutorial)
        {
            StylePanel(panel);

            var title = EnsureText(panel, "KoksTitle", "Выберите сценарий", heading, 64f, Color.white,
                                   TextAlignmentOptions.MidlineLeft, -300f, 420f, 1000f, 90f);
            title.raycastTarget = false;
            var hint = EnsureText(panel, "KoksHint", "Наведите луч на карточку и нажмите триггер", body, 26f, Mute,
                                  TextAlignmentOptions.Center, 0f, -440f, 1600f, 44f);
            hint.raycastTarget = false;

            // кнопка «Назад» -> обратно на экран обучения
            Clean(panel, "KoksBack");
            var back = KImg(panel, "KoksBack", Spr("btn_outline"), Color.white, 1520f, 24f, 260f, 84f, Image.Type.Sliced);
            ((RectTransform)back.transform).anchorMin = ((RectTransform)back.transform).anchorMax = new Vector2(0f, 1f);
            back.raycastTarget = true;
            var backBtn = back.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = back;
            var bcb = ColorBlock.defaultColorBlock;
            bcb.normalColor = new Color(1f, 1f, 1f, 0.55f);
            bcb.highlightedColor = Color.white;
            bcb.selectedColor = new Color(1f, 1f, 1f, 0.55f);
            bcb.pressedColor = Hex("FFC400");
            bcb.fadeDuration = 0.08f;
            backBtn.colors = bcb;
            KTxt(back.transform, "KoksBackText", "Назад", heading, 34f, Color.white,
                 TextAlignmentOptions.Center, 0, 0, 260, 84);
            AddHover(back.transform, Spr("btn_outline"), new Color(1f, 0.769f, 0f, 0.35f), new Color(1f, 0.769f, 0f, 1f), false);
            AddSetActive(backBtn, panel.gameObject, false);
            AddSetActive(backBtn, tutorial.gameObject, true);

            string[] names = { "ButtonTop", "ButtonTop (1)", "ButtonTop (2)" };
            string[] icons = { "icon_search", "icon_flame", "icon_box" };
            string[] subs = { "Учебный сценарий", "Сценарий в разработке", "Сценарий в разработке" };
            const float CW = 533f, CH = 640f;

            for (int i = 0; i < names.Length; i++)
            {
                Transform card = panel.Find(names[i]);
                if (card == null) { Debug.LogWarning("[Koks UI] Нет кнопки " + names[i]); continue; }

                var rt = (RectTransform)card;
                var btn = card.GetComponent<Button>();
                var img = card.GetComponent<Image>();
                bool ready = i == 0; // у остальных сценариев пока нет onClick

                // убрать прежние украшения
                for (int k = card.childCount - 1; k >= 0; k--)
                    if (card.GetChild(k).name.StartsWith("Koks"))
                        Object.DestroyImmediate(card.GetChild(k).gameObject);

                SetRect(rt, (i - 1) * 573f, -50f, CW, CH);
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

                Color accent = ready ? Heat : Hex("5A656D");

                // обложка с иконкой
                KImg(card, "KoksCover", null, Hex("161C21"), 0, 0, CW, 230, Image.Type.Simple);
                KImg(card, "KoksStripes", Spr("stripes_tile"), Color.white, 0, 0, CW, 230, Image.Type.Tiled);
                KImg(card, "KoksIcon", Spr(icons[i]), accent, (CW - 130f) / 2f, 50f, 130f, 130f, Image.Type.Simple);

                // тег статуса
                var tag = KImg(card, "KoksTag", Spr("chip"), new Color(0.078f, 0.094f, 0.11f, 1f), 18, 18, 160, 42, Image.Type.Sliced);
                KImg(tag.transform, "KoksDot", Spr("dot"), ready ? Hex("4BD37B") : Hex("5A656D"), 14, 12, 18, 18, Image.Type.Simple);
                KTxt(tag.transform, "KoksTagText", ready ? "Доступен" : "Скоро", medium, 24f, Color.white,
                     TextAlignmentOptions.MidlineLeft, 40, 0, 116, 42);

                // подпись и «кнопка» (вся карточка — кнопка, это только вид)
                KTxt(card, "KoksSub", subs[i], body, 26f, Mute, TextAlignmentOptions.MidlineLeft, 28, 430, 477, 40);
                var action = KImg(card, "KoksAction", Spr(ready ? "btn_heat" : "btn_outline"),
                                  ready ? Heat : new Color(1f, 1f, 1f, 0.25f), 28, 528, 477, 84, Image.Type.Sliced);
                KTxt(action.transform, "KoksActionText", ready ? "Выбрать" : "Скоро", heading, 34f,
                     ready ? Dark : Mute, TextAlignmentOptions.Center, 0, 0, 477, 84);

                // название — прежний Text, теперь TMP
                var tmp = FirstOriginalText(card);
                if (tmp != null)
                {
                    SetRect((RectTransform)tmp.transform, 0f, -5f, 477f, 190f);
                    Style(tmp, heading, 40f, ready ? Color.white : Mute, TextAlignmentOptions.TopLeft, true, 26f, 42f);
                    tmp.transform.SetAsLastSibling();
                }

                // подсветка при наведении
                AddHover(card, Spr("chip"), new Color(1f, 1f, 1f, 0.07f), Heat, true);

                // рамка поверх всего
                var frame = KImg(card, "KoksFrame", Spr("card"), new Color(1f, 1f, 1f, ready ? 0.3f : 0.12f), 0, 0, CW, CH, Image.Type.Sliced);
                frame.transform.SetAsLastSibling();
            }

            DisableRaycasts(panel);
        }

        /// <summary>Подсветка при наведении луча: скрытый объект KoksHover, UIHover включает его.</summary>
        static void AddHover(Transform host, Sprite fillSprite, Color fill, Color frameColor, bool onTop)
        {
            Clean(host, "KoksHover");
            var go = new GameObject("KoksHover", typeof(RectTransform));
            go.transform.SetParent(host, false);
            Stretch((RectTransform)go.transform);

            var f = NewImage("KoksHoverFill", go.transform, fillSprite, fill, Image.Type.Sliced);
            Stretch((RectTransform)f.transform);
            if (frameColor.a > 0f)
            {
                var fr = NewImage("KoksHoverFrame", go.transform, Spr("frame4"), frameColor, Image.Type.Sliced);
                Stretch((RectTransform)fr.transform);
            }
            go.SetActive(false);

            var hv = host.GetComponent<UIHover>();
            if (hv == null) hv = host.gameObject.AddComponent<UIHover>();
            hv.target = go;

            if (onTop) go.transform.SetAsLastSibling(); else go.transform.SetSiblingIndex(0);
        }

        /// <summary>Рамка «выбрано» у плитки СИЗ: включается переключателем Toggle.</summary>
        static void AddSelected(Transform item, Toggle toggle)
        {
            Clean(item, "KoksSel");
            var go = new GameObject("KoksSel", typeof(RectTransform));
            go.transform.SetParent(item, false);
            Stretch((RectTransform)go.transform);

            var f = NewImage("KoksSelFill", go.transform, Spr("chip"), new Color(1f, 0.769f, 0f, 0.16f), Image.Type.Sliced);
            Stretch((RectTransform)f.transform);
            var fr = NewImage("KoksSelFrame", go.transform, Spr("frame4"), Heat, Image.Type.Sliced);
            Stretch((RectTransform)fr.transform);

            go.SetActive(toggle.isOn);
            go.transform.SetSiblingIndex(0);
            AddSetActive(toggle, go, false, true);
        }

        /// <summary>Добавляет в onClick кнопки вызов GameObject.SetActive(value) на target.</summary>
        static void AddSetActive(Button btn, GameObject target, bool value)
        {
            AddSetActive(btn, target, value, false);
        }

        // dynamic = true: значение берётся из события (для Toggle.onValueChanged)
        static void AddSetActive(Object owner, GameObject target, bool value, bool dynamic)
        {
            var so = new SerializedObject(owner);
            string evName = dynamic ? "onValueChanged" : "m_OnClick";
            var calls = so.FindProperty(evName + ".m_PersistentCalls.m_Calls");
            if (calls == null) { Debug.LogError("[Koks UI] Не найдено событие " + evName); return; }

            // убрать «осиротевшие» вызовы от прежних запусков
            for (int k = calls.arraySize - 1; k >= 0; k--)
            {
                var e = calls.GetArrayElementAtIndex(k);
                if (e.FindPropertyRelative("m_MethodName").stringValue == "SetActive" &&
                    e.FindPropertyRelative("m_Target").objectReferenceValue == null)
                    calls.DeleteArrayElementAtIndex(k);
            }

            int i = calls.arraySize;
            calls.arraySize = i + 1;
            var c = calls.GetArrayElementAtIndex(i);
            c.FindPropertyRelative("m_Target").objectReferenceValue = target;
            c.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "UnityEngine.GameObject, UnityEngine";
            c.FindPropertyRelative("m_MethodName").stringValue = "SetActive";
            c.FindPropertyRelative("m_Mode").intValue = dynamic ? 0 : 6; // 0 = аргумент из события, 6 = Bool
            c.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = value;
            c.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
            so.ApplyModifiedProperties();
        }

        /// <summary>Дочерний Image, привязка к левому верхнему углу родителя.</summary>
        static Image KImg(Transform parent, string name, Sprite sprite, Color color,
                          float x, float y, float w, float h, Image.Type type)
        {
            var im = NewImage(name, parent, sprite, color, type);
            var r = (RectTransform)im.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return im;
        }

        static TextMeshProUGUI KTxt(Transform parent, string name, string text, TMP_FontAsset font, float size,
                                    Color color, TextAlignmentOptions align, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            Style(t, font, size, color, align, false, 0f, 0f);
            t.raycastTarget = false;
            return t;
        }

        // --------------------------------------------------- Результат

        static void RestyleResult(Transform panel)
        {
            var info = panel.Find("ImageInfo");
            if (info == null) return;
            var r = (RectTransform)info;
            Fill(info.GetComponent<Image>(), Hex("14191E", 0.97f));
            AddFrame(r, new Color(1f, 1f, 1f, 0.2f));
            AddAccent(r, Heat);
            // текст результата не трогаем: его цвет и смайлики задаёт DressingChecker

            foreach (string n in new[] { "ButtonNext", "ButtonReturn" })
            {
                var b = info.Find(n);
                if (b == null) continue;
                StyleHeatButton(b.GetComponent<Button>(), b.GetComponent<Image>(), 30f);
            }
        }

        // ------------------------------------------------------ Выбор СИЗ

        static void RestyleSiz(Transform panel)
        {
            StylePanel(panel);
            var line = new Color(1f, 1f, 1f, 0.16f);

            // шапка с заданием
            var hdr = panel.Find("Header");
            if (hdr != null)
            {
                var r = (RectTransform)hdr;
                SetRect(r, -376f, 405f, 970f, 100f);
                Fill(hdr.GetComponent<Image>(), Card);
                AddFrame(r, line);
                AddAccent(r, Heat);
                var t = FirstOriginalText(hdr);
                if (t != null)
                {
                    SetRect((RectTransform)t.transform, 12f, 0f, 930f, 90f);
                    Style(t, medium, 26f, Ink, TextAlignmentOptions.MidlineLeft, true, 20f, 28f);
                }
            }

            // список СИЗ
            var scroll = panel.Find("Scroll View");
            if (scroll != null)
            {
                var r = (RectTransform)scroll;
                r.anchoredPosition = new Vector2(r.anchoredPosition.x, -5f);
                Fill(scroll.GetComponent<Image>(), Hex("161C21"));

                var viewport = scroll.Find("Viewport");
                if (viewport != null) Fill(viewport.GetComponent<Image>(), Hex("161C21"));

                var content = scroll.Find("Viewport/Content");
                if (content != null)
                {
                    foreach (Transform item in content)
                    {
                        var itemImg = item.GetComponent<Image>();
                        Fill(itemImg, Hex("252D34"));
                        itemImg.raycastTarget = true; // плитка должна ловить луч целиком
                        AddFrame((RectTransform)item, line);
                        var t = FirstOriginalText(item);
                        if (t != null)
                            Style(t, medium, 24f, Ink, TextAlignmentOptions.MidlineLeft, true, 16f, 26f);
                        AddHover(item, Spr("chip"), new Color(1f, 1f, 1f, 0.07f), new Color(1f, 1f, 1f, 0.5f), true);
                        var tg = item.GetComponentInChildren<Toggle>(true);
                        if (tg != null)
                        {
                            AddSelected(item, tg);
                            var proxy = item.GetComponent<UIToggleProxy>();
                            if (proxy == null) proxy = item.gameObject.AddComponent<UIToggleProxy>();
                            proxy.toggle = tg;
                        }
                    }
                }

                var bar = scroll.Find("Scrollbar Vertical");
                if (bar != null)
                {
                    Fill(bar.GetComponent<Image>(), Hex("0F1317"));
                    var handle = bar.Find("Sliding Area/Handle");
                    if (handle != null) Fill(handle.GetComponent<Image>(), Heat);
                }

                AddFrame(r, line);
                Transform f = scroll.Find("KoksFrame");
                if (f != null) f.SetAsLastSibling(); // рамка поверх списка
            }

            // карточка с фигурой (слои одежды лежат сверху и не трогаются)
            var bg = panel.Find("Bg");
            if (bg != null)
            {
                var r = (RectTransform)bg;
                SetRect(r, 507f, -5f, 740f, 960f);
                Fill(bg.GetComponent<Image>(), Hex("232B33"));
                AddFrame(r, line);
            }

            // кнопка «Проверить»
            var check = panel.Find("Button (Legacy)");
            if (check != null)
            {
                SetRect((RectTransform)check, -416f, -430f, 300f, 80f);
                StyleHeatButton(check.GetComponent<Button>(), check.GetComponent<Image>(), 34f);
            }
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
            AddHover(btn.transform, Spr("btn_heat"), new Color(1f, 1f, 1f, 0.35f), new Color(1f, 1f, 1f, 0f), false);
        }

        static void Fill(Image img, Color color)
        {
            if (img == null) return;
            img.sprite = Spr("chip");
            img.type = Image.Type.Sliced;
            img.color = color;
            img.canvasRenderer.SetAlpha(1f);
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
            r.localScale = Vector3.one; // в сцене у части объектов масштаб 0.5
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
