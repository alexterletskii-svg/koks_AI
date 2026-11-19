using UnityEngine;
using UnityEngine.Events;

namespace KoksUI
{
    /// <summary>
    /// Показывает окно, когда игрок впервые оказывается в заданной точке сцены
    /// (например, после телепорта в газовый тоннель). Тексты правятся в окне (Step Window).
    /// </summary>
    public class ArrivalTrigger : MonoBehaviour
    {
        public StepWindow window;
        [Tooltip("Точка, куда телепортируется игрок (мировые координаты)")]
        public Vector3 position;
        [Tooltip("Радиус по горизонтали, м (с запасом: голова после телепорта может быть в стороне от точки)")]
        public float zoneRadius = 12f;
        [Tooltip("Допуск по высоте головы относительно точки: от и до, м")]
        public float minHeight = -1f;
        public float maxHeight = 3.5f;
        public UnityEvent onArrived;

        bool shown;

        void Update()
        {
            if (shown || window == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 d = cam.transform.position - position;
            float dy = d.y;
            d.y = 0f;
            if (d.magnitude > zoneRadius || dy < minHeight || dy > maxHeight) return;
            shown = true;
            onArrived.Invoke();
            window.Show();
        }
    }
}
