using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI.Editor
{
    /// <summary>
    /// Koks → Build UI → 17. СИЗ: газоанализатор. Запускать вне Play. Открывает MenuScene, добавляет в выбор СИЗ
    /// строку «Газоанализатор» (миниатюра в списке), слой на человечке (на груди с левой стороны человека),
    /// подключает его к проверке (DressingChecker, считается правильным СИЗ), сохраняет сцену. Можно повторно.
    /// </summary>
    public static class KoksGasAnalyzerBuilder
    {
        const string LayerPath = "Assets/Koksohim/Sprites/GasAnalyzer.png";
        const string IconPath = "Assets/Koksohim/Sprites/ScrollBarContent/GasAnalyzer.png";

        static Sprite LoadSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            if (imp.textureType != TextureImporterType.Sprite || !imp.alphaIsTransparency)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Koks/Build UI/17. СИЗ: газоанализатор (выбор + человечек)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Koks UI] Остановите Play и запустите ещё раз."); return; }
            AssetDatabase.Refresh();
            var layerSprite = LoadSprite(LayerPath);
            var iconSprite = LoadSprite(IconPath);
            if (layerSprite == null || iconSprite == null) { Debug.LogError("[Koks UI] Нет GasAnalyzer.png (слой и миниатюра). Сделайте Pull origin."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string back = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity", OpenSceneMode.Single);
            try { Run(scene, layerSprite, iconSprite); }
            finally
            {
                if (!string.IsNullOrEmpty(back) && back != scene.path) EditorSceneManager.OpenScene(back, OpenSceneMode.Single);
            }
        }

        static void Run(UnityEngine.SceneManagement.Scene scene, Sprite layerSprite, Sprite iconSprite)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var panel = all.FirstOrDefault(t => t.name == "PanelSiz");
            if (panel == null) { Debug.LogError("[Koks UI] В MenuScene нет PanelSiz."); return; }
            var content = panel.Find("Scroll View/Viewport/Content");
            var srcRow = content != null ? content.Find("Respirator") : null;
            var srcLayer = panel.Find("Respirator");
            if (content == null || srcRow == null || srcLayer == null) { Debug.LogError("[Koks UI] Не нашёл строку или слой Respirator в PanelSiz."); return; }

            // прошлая сборка
            var oldRow = content.Find("GasAnalyzer"); if (oldRow != null) Object.DestroyImmediate(oldRow.gameObject);
            var oldLayer = panel.Find("GasAnalyzer"); if (oldLayer != null) Object.DestroyImmediate(oldLayer.gameObject);

            // ---- строка в списке выбора
            var rows = content.Cast<Transform>().Select(t => t as RectTransform).Where(r => r != null).ToList();
            var first = rows.First(); var last = rows.Last();
            float pitch = rows.Count > 1 ? Mathf.Abs(rows[1].anchoredPosition.y - rows[0].anchoredPosition.y) : 160f;
            if (pitch < 1f) pitch = 160f;
            float sign = last.anchoredPosition.y >= first.anchoredPosition.y ? 1f : -1f;
            var row = (RectTransform)Object.Instantiate(srcRow.gameObject, content).transform;
            row.name = "GasAnalyzer";
            row.SetAsLastSibling();
            row.anchoredPosition = new Vector2(last.anchoredPosition.x, last.anchoredPosition.y + sign * pitch);
            var cr = (RectTransform)content;
            cr.sizeDelta = new Vector2(cr.sizeDelta.x, cr.sizeDelta.y + pitch);

            var toggle = row.GetComponentsInChildren<Toggle>(true).First();
            toggle.gameObject.name = "ToggleGasAnalyzer";
            toggle.isOn = false;
            var bgTr = toggle.transform.Find("Background");
            var bg = bgTr != null ? bgTr.GetComponent<Image>() : toggle.targetGraphic as Image;
            if (bg != null) { bg.sprite = iconSprite; bg.preserveAspect = true; EditorUtility.SetDirty(bg); }
            var tmp = row.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault();
            if (tmp != null) { tmp.text = "Газоанализатор"; EditorUtility.SetDirty(tmp); }
            else
            {
                var label = row.GetComponentsInChildren<Text>(true).FirstOrDefault();
                if (label != null) { label.text = "Газоанализатор"; EditorUtility.SetDirty(label); }
            }

            // ---- слой на человечке: поверх остальных слоёв
            var layerGo = Object.Instantiate(srcLayer.gameObject, panel);
            layerGo.name = "GasAnalyzer";
            var li = layerGo.GetComponent<Image>();
            li.sprite = layerSprite;
            EditorUtility.SetDirty(li);
            int maxIdx = 0;
            foreach (var n in new[] { "Respirator", "HelmetRed", "HelmetYellow", "Leash", "Gloves", "Mask", "Bots", "Suit", "Glasess", "Headphones" })
            {
                var l = panel.Find(n);
                if (l != null) maxIdx = Mathf.Max(maxIdx, l.GetSiblingIndex());
            }
            layerGo.transform.SetSiblingIndex(maxIdx + 1);
            layerGo.SetActive(false);

            // ---- привязка к проверке
            var ci = toggle.GetComponent("ClothingItem");
            if (ci == null) { Debug.LogError("[Koks UI] На переключателе нет ClothingItem."); return; }
            var so = new SerializedObject(ci);
            so.FindProperty("toggle").objectReferenceValue = toggle;
            so.FindProperty("layer").objectReferenceValue = layerGo;
            so.FindProperty("isCorrect").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var checker = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(m => m != null && m.GetType().Name == "DressingChecker");
            if (checker != null)
            {
                var cso = new SerializedObject(checker);
                var arr = cso.FindProperty("allItems");
                // убрать пустые ссылки от прошлой сборки и добавить новый элемент
                for (int i = arr.arraySize - 1; i >= 0; i--)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == null) arr.DeleteArrayElementAtIndex(i);
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = ci;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Koks UI] DressingChecker не найден: газоанализатор не попадёт в проверку.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks UI] Газоанализатор добавлен: строка в списке, слой на человечке (грудь, левая сторона человека), в проверке — правильный СИЗ.");
        }
    }
}
