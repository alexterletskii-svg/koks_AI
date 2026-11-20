using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 19. Вводное окно: блок про газоанализатор (иконка + текст) внизу. В открытую FirstScene,
    /// настройки окна не трогает. Можно повторно.
    /// </summary>
    public static class KoksIntroGasInfoBuilder
    {
        const float Extra = 170f;
        const string IconPath = "Assets/Koksohim/Sprites/ScrollBarContent/GasAnalyzer.png";
        const string Title = "Газоанализатор";
        const string Info = "Газоанализатор на груди сигнализирует о появлении в воздухе опасных газов, в том числе угарного газа (СО). Частый и громкий сигнал означает повышенную концентрацию: прекратите осмотр и покиньте зону.";

        [MenuItem("Koks/Build UI/19. Вводное окно: блок про газоанализатор (FirstScene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorSceneManager.GetActiveScene().path.EndsWith("FirstScene.unity"))
                EditorSceneManager.OpenScene("Assets/Scenes/FirstScene.unity", OpenSceneMode.Single);

            var root = GameObject.Find("CanvasIntro");
            if (root == null) { Debug.LogError("[Koks UI] Нет CanvasIntro. Сначала меню 6."); return; }
            var rt = (RectTransform)root.transform;
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            if (icon == null) { Debug.LogError("[Koks UI] Нет " + IconPath); return; }
            var medium = KoksUIBuilder.LoadFont("Inter-Medium");
            var body = KoksUIBuilder.LoadFont("Inter-Regular");

            var old = rt.Find("GasInfo");
            float baseH;
            if (old != null) { Object.DestroyImmediate(old.gameObject); baseH = rt.sizeDelta.y - Extra; }
            else baseH = rt.sizeDelta.y;
            float W = rt.sizeDelta.x, H = baseH + Extra;

            var oldPos = rt.position;
            rt.sizeDelta = new Vector2(W, H);
            foreach (var n in new[] { "PanelFill", "PanelFrame" })
            {
                var t = rt.Find(n) as RectTransform;
                if (t != null) t.sizeDelta = new Vector2(W, H);
            }
            foreach (var n in new[] { "ProgressTrack", "ProgressFill" })
            {
                var t = rt.Find(n) as RectTransform;
                if (t != null) t.anchoredPosition = new Vector2(t.anchoredPosition.x, -(H - 10f));
            }

            var block = KoksUIBuilder.R("GasInfo", rt, 0, 0, W, H);
            float y0 = baseH - 4f;
            KoksUIBuilder.Img("Divider", block, null, new Color(1f, 1f, 1f, 0.16f), 28, y0, W - 56f, 2);
            var im = KoksUIBuilder.Img("Icon", block, icon, Color.white, 28, y0 + 26f, 96, 96, Image.Type.Simple);
            im.preserveAspect = true;
            KoksUIBuilder.Txt("InfoTitle", block, Title, medium, 26, KoksUIBuilder.Heat, TextAlignmentOptions.TopLeft, 144, y0 + 20f, W - 172f, 34);
            KoksUIBuilder.Txt("InfoText", block, Info, body, 22, KoksUIBuilder.Mute, TextAlignmentOptions.TopLeft, 144, y0 + 56f, W - 172f, 100, true);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Debug.Log("[Koks UI] В вводное окно добавлен блок про газоанализатор. Текст — CanvasIntro/GasInfo/InfoText.");
        }
    }
}
