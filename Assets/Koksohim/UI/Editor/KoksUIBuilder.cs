using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace KoksUI.Editor
{
    /// <summary>Меню Koks → Build UI: шрифты TMP, спрайты, префаб окна «Лобби».</summary>
    public static class KoksUIBuilder
    {
        const string Root = "Assets/Koksohim/UI";
        const string FontsDir = Root + "/Fonts";
        const string SpritesDir = Root + "/Sprites";
        const string PrefabsDir = Root + "/Prefabs";

        internal static readonly Color Heat = Hex("FFC400");
        internal static readonly Color HeatHi = Hex("FFD84D");
        internal static readonly Color Ok = Hex("4BD37B");
        internal static readonly Color Ink = Hex("EEF1F3");
        internal static readonly Color Mute = Hex("A4AEB6");
        internal static readonly Color Dark = Hex("14181C");

        internal static Color Hex(string h, float a = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + h, out Color c);
            c.a = a;
            return c;
        }

        // ---------------------------------------------------------------- меню

        [MenuItem("Koks/Build UI/1. Подготовить шрифты и спрайты")]
        public static void PrepareAssets()
        {
            SetupSprites();
            CreateFont("RobotoCondensed-ExtraBold.ttf");
            CreateFont("Inter-Regular.otf");
            CreateFont("Inter-Medium.otf");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Koks UI] Шрифты и спрайты готовы.");
        }

        [MenuItem("Koks/Build UI/2. Собрать окно «Лобби» (префаб)")]
        public static void BuildLobbyPrefab()
        {
            PrepareAssets();
            EnsureFolder(PrefabsDir);

            var heading = LoadFont("RobotoCondensed-ExtraBold");
            var body = LoadFont("Inter-Regular");
            var medium = LoadFont("Inter-Medium");
            if (heading == null || body == null || medium == null)
            {
                Debug.LogError("[Koks UI] Не найдены шрифты TMP в " + FontsDir);
                return;
            }

            GameObject root = BuildLobby(heading, body, medium);
            string path = PrefabsDir + "/LobbyWindow.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log("[Koks UI] Готово: " + path);
        }

        [MenuItem("Koks/Build UI/3. Поставить «Лобби» в открытую сцену")]
        public static void PlaceLobbyInScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/LobbyWindow.prefab");
            if (prefab == null)
            {
                Debug.LogError("[Koks UI] Сначала запустите пункт 2.");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Vector3 pos = new Vector3(0f, 1.5f, 1.2f);
            if (SceneView.lastActiveSceneView != null)
                pos = SceneView.lastActiveSceneView.pivot;
            go.transform.position = pos;

            var canvas = go.GetComponent<Canvas>();
            if (canvas != null && Camera.main != null)
                canvas.worldCamera = Camera.main;

            Undo.RegisterCreatedObjectUndo(go, "Place Lobby window");
            Selection.activeGameObject = go;
        }

        // ---------------------------------------------------------- ассеты

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }

        static void SetupSprites()
        {
            var borders = new System.Collections.Generic.Dictionary<string, int>
            {
                { "panel_fill", 40 }, { "panel_frame", 40 }, { "btn_heat", 24 },
                { "btn_outline", 8 }, { "chip", 6 }, { "card", 8 }, { "frame4", 8 },
            };

            foreach (string file in Directory.GetFiles(SpritesDir, "*.png"))
            {
                string path = file.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.wrapMode = TextureWrapMode.Clamp;

                int b;
                ti.spriteBorder = borders.TryGetValue(name, out b) ? new Vector4(b, b, b, b) : Vector4.zero;

                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = ti.spriteBorder;
                ti.SetTextureSettings(settings);

                ti.SaveAndReimport();
            }
        }

        static void CreateFont(string fileName)
        {
            string assetName = Path.GetFileNameWithoutExtension(fileName);
            string assetPath = FontsDir + "/" + assetName + " SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null) return;

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontsDir + "/" + fileName);
            if (font == null)
            {
                Debug.LogError("[Koks UI] Нет файла шрифта: " + FontsDir + "/" + fileName);
                return;
            }

            // Динамический атлас: нужные символы (в том числе кириллица) дорисовываются сами.
            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            fa.name = assetName + " SDF";
            AssetDatabase.CreateAsset(fa, assetPath);

            if (fa.atlasTextures != null)
            {
                foreach (var tex in fa.atlasTextures)
                {
                    tex.name = assetName + " Atlas";
                    AssetDatabase.AddObjectToAsset(tex, fa);
                }
            }
            if (fa.material != null)
            {
                fa.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            EditorUtility.SetDirty(fa);
        }

        internal static TMP_FontAsset LoadFont(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsDir + "/" + assetName + " SDF.asset");
        }

        internal static Sprite Spr(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/" + name + ".png");
            if (s == null) Debug.LogError("[Koks UI] Нет спрайта " + name);
            return s;
        }

        // ------------------------------------------------------- помощники

        internal static RectTransform R(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        internal static Image Img(string name, Transform parent, Sprite sprite, Color color,
                         float x, float y, float w, float h, Image.Type type = Image.Type.Sliced)
        {
            var r = R(name, parent, x, y, w, h);
            var im = r.gameObject.AddComponent<Image>();
            im.sprite = sprite;
            im.color = color;
            im.type = sprite == null ? Image.Type.Simple : type;
            im.raycastTarget = false;
            return im;
        }

        internal static TextMeshProUGUI Txt(string name, Transform parent, string text, TMP_FontAsset font,
                                   float size, Color color, TextAlignmentOptions align,
                                   float x, float y, float w, float h, bool wrap = false)
        {
            var r = R(name, parent, x, y, w, h);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeHitButton(Transform parent, float x, float y, float w, float h, string name)
        {
            var im = Img(name, parent, null, new Color(1f, 1f, 1f, 0.004f), x, y, w, h);
            im.raycastTarget = true;
            im.canvasRenderer.cullTransparentMesh = false;
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            b.transition = Selectable.Transition.None;
            return b;
        }

        // ----------------------------------------------------------- Лобби

        const float W = 1400f;
        const float H = 1063f;

        class Module
        {
            public string title, desc, minutes, steps, tag, icon;
            public Color tagColor;
            public int tagWidth, descLines;
            public int best; // -1 = не проходили
        }

        static GameObject BuildLobby(TMP_FontAsset heading, TMP_FontAsset body, TMP_FontAsset medium)
        {
            var root = new GameObject("LobbyWindow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(W, H);
            rt.localScale = Vector3.one * 0.001f; // 1400 px = 1,4 м

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
            root.AddComponent<CanvasGroup>();

            // панель
            Img("PanelFill", root.transform, Spr("panel_fill"), Hex("14191E", 0.95f), 0, 0, W, H);
            Img("Hazard", root.transform, Spr("hazard"), Color.white, 0, 0, 260, 10, Image.Type.Simple);
            Img("PanelFrame", root.transform, Spr("panel_frame"), new Color(1f, 1f, 1f, 0.16f), 0, 0, W, H);

            // шапка
            Txt("Logo", root.transform, "НЛМК", heading, 46, Color.white,
                TextAlignmentOptions.MidlineLeft, 52, 48, 300, 52);
            Txt("Title", root.transform, "Коксовый цех", heading, 76, Color.white,
                TextAlignmentOptions.MidlineLeft, 52, 108, 800, 100);
            Txt("Subtitle", root.transform, "VR-тренажёр · обучение безопасной работе на коксовой батарее",
                body, 26, Mute, TextAlignmentOptions.MidlineLeft, 52, 205, 1000, 40);

            // статус справа
            var status = Img("StatusChip", root.transform, Spr("chip"), Color.white, 892, 84, 456, 42);
            Img("Dot", status.transform, Spr("dot"), Ok, 14, 12, 18, 18, Image.Type.Simple);
            Txt("Text", status.transform, "Шлем и контроллеры подключены", medium, 24, Ink,
                TextAlignmentOptions.MidlineLeft, 40, 0, 410, 42);
            Txt("Допуск", root.transform, "Допуск действует до 14 декабря", body, 24, Mute,
                TextAlignmentOptions.MidlineRight, 748, 140, 600, 36);
            Txt("Прогресс", root.transform, "Пройдено 7 из 12 модулей", body, 24, Mute,
                TextAlignmentOptions.MidlineRight, 748, 171, 600, 36);

            Img("Divider", root.transform, null, new Color(1f, 1f, 1f, 0.2f), 52, 274, 1296, 2);

            // карточки
            var modules = new[]
            {
                new Module { title = "Выдача кокса", desc = "Управление коксовыталкивателем и работа с коксовозом",
                    minutes = "40 мин", steps = "6 шагов", tag = "Допущен", tagColor = Ok, tagWidth = 162,
                    icon = "icon_box", descLines = 3, best = 92 },
                new Module { title = "Газовая опасность", desc = "Работа в зоне СО: газоанализатор, сигналы, эвакуация",
                    minutes = "25 мин", steps = "4 шага", tag = "Допущен", tagColor = Ok, tagWidth = 162,
                    icon = "icon_flame", descLines = 3, best = -1 },
                new Module { title = "Обход батареи", desc = "Осмотр дверей, стояков и коллекторов перед сменой",
                    minutes = "30 мин", steps = "8 шагов", tag = "Новый", tagColor = Heat, tagWidth = 132,
                    icon = "icon_search", descLines = 2, best = -1 },
            };

            var buttons = new Button[modules.Length];
            var frames = new GameObject[modules.Length];
            for (int i = 0; i < modules.Length; i++)
                BuildCard(root.transform, modules[i], 52 + i * 440, 310, heading, body, medium, out buttons[i], out frames[i]);

            // кнопки внизу
            var start = Img("StartButton", root.transform, Spr("btn_heat"), Color.white, 52, 930, 288, 84);
            start.raycastTarget = true;
            var startBtn = start.gameObject.AddComponent<Button>();
            startBtn.targetGraphic = start;
            startBtn.colors = Tint(Heat, HeatHi, Hex("E0AB00"), Hex("6B6F73"));
            Txt("Label", start.transform, "Начать модуль", heading, 30, Dark,
                TextAlignmentOptions.Center, 0, 0, 288, 84);

            var free = Img("FreeInspectButton", root.transform, Spr("btn_outline"), Color.white, 360, 930, 347, 84);
            free.raycastTarget = true;
            var freeBtn = free.gameObject.AddComponent<Button>();
            freeBtn.targetGraphic = free;
            freeBtn.colors = Tint(new Color(1f, 1f, 1f, 0.5f), Color.white, new Color(0.8f, 0.8f, 0.8f, 1f), new Color(1f, 1f, 1f, 0.2f));
            Txt("Label", free.transform, "Свободный осмотр", heading, 30, Color.white,
                TextAlignmentOptions.Center, 0, 0, 347, 84);

            Txt("Hint", root.transform, "Наведите луч на модуль и нажмите триггер", body, 24, Mute,
                TextAlignmentOptions.MidlineRight, 748, 950, 600, 44);

            var lobby = root.AddComponent<LobbyWindow>();
            lobby.moduleButtons = buttons;
            lobby.selectedFrames = frames;
            lobby.startButton = startBtn;
            lobby.freeInspectButton = freeBtn;
            lobby.selectedIndex = 0;
            for (int i = 1; i < frames.Length; i++) frames[i].SetActive(false);
            return root;
        }

        static ColorBlock Tint(Color normal, Color hi, Color pressed, Color disabled)
        {
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = normal;
            cb.highlightedColor = hi;
            cb.selectedColor = normal;
            cb.pressedColor = pressed;
            cb.disabledColor = disabled;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            return cb;
        }

        static void BuildCard(Transform parent, Module m, float x, float y,
                              TMP_FontAsset heading, TMP_FontAsset body, TMP_FontAsset medium,
                              out Button button, out GameObject selectedFrame)
        {
            const float CW = 410f, CH = 581f;
            var card = R("Card_" + m.title, parent, x, y, CW, CH);
            var t = card.transform;

            // обложка
            Img("Cover", t, null, Hex("1E252B"), 0, 0, CW, 210);
            Img("Stripes", t, Spr("stripes_tile"), Color.white, 0, 0, CW, 210, Image.Type.Tiled);
            Img("Icon", t, Spr(m.icon), Heat, (CW - 110) / 2f, 50, 110, 110, Image.Type.Simple);
            Img("CardFrame", t, Spr("card"), Color.white, 0, 0, CW, CH);

            // тег статуса
            var tag = Img("Tag", t, Spr("chip"), new Color(0.078f, 0.094f, 0.11f, 1f), 18, 18, m.tagWidth, 42);
            Img("Dot", tag.transform, Spr("dot"), m.tagColor, 14, 12, 18, 18, Image.Type.Simple);
            Txt("Text", tag.transform, m.tag, medium, 24, Color.white,
                TextAlignmentOptions.MidlineLeft, 40, 0, m.tagWidth - 44, 42);

            // текст
            Txt("Title", t, m.title, heading, 32, Color.white,
                TextAlignmentOptions.MidlineLeft, 28, 233, 354, 40);
            Txt("Description", t, m.desc, body, 25, Mute,
                TextAlignmentOptions.TopLeft, 28, 280, 354, 100, true);

            float cy = m.descLines == 3 ? 394f : 364f;
            InfoChip(t, "icon_clock", m.minutes, medium, 28, cy, 150);
            InfoChip(t, "icon_list", m.steps, medium, 189, cy, 158);

            if (m.best >= 0)
            {
                Txt("BestLabel", t, "Лучший результат", body, 24, Mute,
                    TextAlignmentOptions.MidlineLeft, 28, cy + 62, 354, 34);
                Txt("BestNumber", t, m.best + "%", heading, 40, Ok,
                    TextAlignmentOptions.MidlineLeft, 28, cy + 96, 110, 60);
                Img("BarTrack", t, null, new Color(1f, 1f, 1f, 0.14f), 120, cy + 120, 262, 12);
                Img("BarFill", t, null, Ok, 120, cy + 120, 262f * m.best / 100f, 12);
            }
            else
            {
                Txt("NotYet", t, "Ещё не проходили", body, 24, Mute,
                    TextAlignmentOptions.MidlineLeft, 28, cy + 62, 354, 34);
            }

            // подсветка выбора и наведения
            var sel = Img("Selected", t, Spr("frame4"), Heat, 0, 0, CW, CH);
            var hover = Img("Hover", t, null, new Color(1f, 1f, 1f, 0.06f), 0, 0, CW, CH);
            hover.gameObject.SetActive(false);

            button = MakeHitButton(t, 0, 0, CW, CH, "Hit");
            var hv = button.gameObject.AddComponent<UIHover>();
            hv.target = hover.gameObject;
            selectedFrame = sel.gameObject;
        }

        static void InfoChip(Transform parent, string icon, string text, TMP_FontAsset font,
                             float x, float y, float w)
        {
            var chip = Img("Chip_" + text, parent, Spr("chip"), Color.white, x, y, w, 44);
            Img("Icon", chip.transform, Spr(icon), Ink, 14, 10, 24, 24, Image.Type.Simple);
            Txt("Text", chip.transform, text, font, 24, Color.white,
                TextAlignmentOptions.MidlineLeft, 46, 0, w - 52, 44);
        }
    }
}
