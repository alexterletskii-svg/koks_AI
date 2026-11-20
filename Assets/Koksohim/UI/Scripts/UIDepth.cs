using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Рисует окно и маркеры поверх геометрии сцены: корпуса, трубы и анимированные крышки их не перекрывают.
    /// Для картинок — свой шейдер «Koks/UI On Top» (ZTest Always), для текста — «TextMeshPro/Distance Field Overlay».
    /// </summary>
    public static class UIDepth
    {
        static Material image;
        static Material markerMat;
        static Shader overlayText;

        /// <summary>Материал для картинок интерфейса, рисуемых поверх геометрии.</summary>
        public static Material ImageMaterial
        {
            get
            {
                if (image == null)
                {
                    var sh = Resources.Load<Shader>("KoksUIOnTop");
                    if (sh == null) sh = Shader.Find("Koks/UI On Top");
                    if (sh == null)
                    {
                        Debug.LogError("[Koks UI] Не найден шейдер Koks/UI On Top (Assets/Koksohim/UI/Resources/KoksUIOnTop.shader).");
                        sh = Shader.Find("UI/Default");
                    }
                    image = new Material(sh);
                }
                return image;
            }
        }

        /// <summary>Материал маркеров осмотра: поверх геометрии, но ниже окон по очереди отрисовки.</summary>
        public static Material MarkerMaterial
        {
            get
            {
                if (markerMat == null)
                {
                    markerMat = new Material(ImageMaterial);
                    markerMat.renderQueue = 3500;
                }
                return markerMat;
            }
        }

        public static void AlwaysOnTop(GameObject root)
        {
            if (overlayText == null) overlayText = Shader.Find("TextMeshPro/Distance Field Overlay");
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                var t = g as TMP_Text;
                if (t != null)
                {
                    if (overlayText != null)
                    {
                        var m = t.fontMaterial;     // свой экземпляр материала у текста
                        if (m.shader != overlayText) m.shader = overlayText;
                    }
                }
                else g.material = ImageMaterial;
            }
        }
    }
}
