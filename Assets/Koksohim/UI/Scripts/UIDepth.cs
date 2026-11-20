using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>Рисует окно поверх геометрии сцены, чтобы корпуса и трубы его не обрезали.</summary>
    public static class UIDepth
    {
        /// <summary>Очередь отрисовки окон: после всей прозрачной геометрии и после маркеров.</summary>
        public const int WindowQueue = 4000;
        /// <summary>Очередь маркеров осмотра: над геометрией, но под окнами.</summary>
        public const int MarkerQueue = 3900;

        static Material image;

        public static void AlwaysOnTop(GameObject root)
        {
            if (image == null)
            {
                image = new Material(Shader.Find("UI/Default"));
                image.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                image.renderQueue = WindowQueue;
            }
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                var t = g as TMP_Text;
                if (t != null)
                {
                    var m = t.fontMaterial; // свой экземпляр материала у текста
                    if (m.HasProperty("unity_GUIZTestMode"))
                        m.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    m.renderQueue = WindowQueue;
                }
                else g.material = image;
            }
        }
    }
}
