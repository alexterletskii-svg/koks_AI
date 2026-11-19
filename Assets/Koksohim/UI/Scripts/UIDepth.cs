using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>Рисует окно поверх геометрии сцены, чтобы корпуса и трубы его не обрезали.</summary>
    public static class UIDepth
    {
        static Material image;

        public static void AlwaysOnTop(GameObject root)
        {
            if (image == null)
            {
                image = new Material(Shader.Find("UI/Default"));
                image.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                var t = g as TMP_Text;
                if (t != null)
                {
                    var m = t.fontMaterial; // свой экземпляр материала у текста
                    if (m.HasProperty("unity_GUIZTestMode"))
                        m.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                }
                else g.material = image;
            }
        }
    }
}
