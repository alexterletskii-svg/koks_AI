using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 19. Окно «Вы в газовом тоннеле» (CanvasTunnel): блок про газоанализатор (иконка + текст)
    /// над кнопкой. Из вводного окна блок убирается. Настройки окон не трогает. Можно повторно.
    /// </summary>
    public static class KoksIntroGasInfoBuilder
    {
        const float Extra = 170f;
        const string IconPath = "Assets/Koksohim/Sprites/ScrollBarContent/GasAnalyzer.png";
        const string Title = "Газоанализатор";
        const string Info = "Газоанализатор на груди сигнализирует об опасных газах в воздухе, в том числе об угарном газе (СО). Частый и громкий сигнал означает повышенную концентрацию: прекратите осмотр и покиньте зону.";

        static RectTransform FindRoot(string name)
        {
            foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.parent == null && t.name == name) return t;
            return null;
        }

        // убрать блок и вернуть исходный размер окна
        static void Strip(RectTransform rt, string[] moveBack)
        {
            var old = rt.Find("GasInfo");
            if (old == null) return;
            Object.DestroyImmediate(old.gameObject);
            Resize(rt, -Extra, moveBack);
        }

        static void Resize(RectTransform rt, float dy, string[] moved)
        {
            float W = rt.sizeDelta.x, H = rt.sizeDelta.y + dy;
            rt.sizeDelta = new Vector2(W, H);
            foreach (var n in new[] { "PanelFill", "PanelFrame" })
            {
                var t = rt.Find(n) as RectTransform;
                if (t != null) t.sizeDelta = new Vector2(W, H);
            }
            foreach (var n in moved)
            {
                var t = rt.Find(n) as RectTransform;
                if (t != null) t.anchoredPosition = new Vector2(t.anchoredPosition.x, t.anchoredPosition.y - dy);
            }
        }

        [MenuItem("Koks/Build UI/19. Окно газового тоннеля: блок про газоанализатор (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorSceneManager.GetActiveScene().path.EndsWith("FirstScene.unity"))
                EditorSceneManager.OpenScene("Assets/Scenes/FirstScene.unity", OpenSceneMode.Single);

            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            if (icon == null) { Debug.LogError("[Koks UI] Нет " + IconPath); return; }
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");

            // из вводного окна блок убираем (там он был в прошлой версии)
            var intro = FindRoot("CanvasIntro");
            if (intro != null)
            {
                var g = intro.Find("GasInfo");
                if (g != null)
                {
                    Object.DestroyImmediate(g.gameObject);
                    Resize(intro, -Extra, new[] { "ProgressTrack", "ProgressFill" });
                }
            }

            var rt = FindRoot("CanvasTunnel");
            if (rt == null) { Debug.LogError("[Koks UI] Нет CanvasTunnel. Сначала меню 10."); return; }
            var moved = new[] { "ConfirmButton" };
            Strip(rt, moved);

            float baseH = rt.sizeDelta.y;
            float W = rt.sizeDelta.x;
            var btn = rt.Find("ConfirmButton") as RectTransform;
            float y0 = btn != null ? -btn.anchoredPosition.y - 6f : baseH - 100f;
            Resize(rt, Extra, moved);
            float H = rt.sizeDelta.y;

            var block = KoksUIBuilder.R("GasInfo", rt, 0, 0, W, H);
            KoksUIBuilder.Img("Divider", block, null, new Color(1f, 1f, 1f, 0.16f), 28, y0, W - 56f, 2);
            var im = KoksUIBuilder.Img("Icon", block, icon, Color.white, 28, y0 + 24f, 96, 96, Image.Type.Simple);
            im.preserveAspect = true;
            KoksUIBuilder.Txt("InfoTitle", block, Title, medium, 26, KoksUIBuilder.Heat, TextAlignmentOptions.TopLeft, 144, y0 + 16f, W - 172f, 34);
            KoksUIBuilder.Txt("InfoText", block, Info, body, 22, KoksUIBuilder.Mute, TextAlignmentOptions.TopLeft, 144, y0 + 52f, W - 172f, 110, true);
            block.SetSiblingIndex(rt.childCount - 1);

            EditorUtility.SetDirty(rt.gameObject);
            if (intro != null) EditorUtility.SetDirty(intro.gameObject);
            EditorSceneManager.MarkSceneDirty(rt.gameObject.scene);
            EditorSceneManager.SaveScene(rt.gameObject.scene);
            Debug.Log("[Koks UI] Блок про газоанализатор добавлен в CanvasTunnel (из вводного окна убран). Текст — CanvasTunnel/GasInfo/InfoText.");
        }
    }
}
