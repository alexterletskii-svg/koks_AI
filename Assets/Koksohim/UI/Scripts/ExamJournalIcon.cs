using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Значок «Журнал» на экзамене: висит на двери выхода со стороны игрока.
    /// Наведите луч и нажмите триггер — открывается журнал, где результаты осмотра записываются самому.
    /// </summary>
    public class ExamJournalIcon : MonoBehaviour
    {
        public Button button;
        public JournalWindow journal;
        [Tooltip("Высота центра панели над полом двери, м (ручка ниже, чтобы не закрывать её)")]
        public float heightOnDoor = 1.45f;
        [Tooltip("Отступ от поверхности двери к игроку, м")]
        public float offDoor = 0.04f;

        Canvas canvas;
        bool visible;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            UIDepth.AlwaysOnTop(gameObject);
            if (button != null)
            {
                button.onClick.AddListener(Open);
                // кликабельна только подложка: иконка, подпись и рамка не должны перехватывать луч
                var target = button.targetGraphic;
                foreach (var g in GetComponentsInChildren<Graphic>(true))
                    if (g != target) g.raycastTarget = false;
                var im = target as Image;
                if (im != null) im.raycastPadding = new Vector4(-20f, -20f, -20f, -20f);
            }
            if (canvas != null) canvas.enabled = false;
        }

        public void SetVisible(bool v)
        {
            if (v == visible) return;
            visible = v;
            if (canvas == null) return;
            if (v && canvas.worldCamera == null) canvas.worldCamera = Camera.main;
            canvas.enabled = v;
        }

        void Open()
        {
            if (journal != null && !StepWindow.AnyOpen) journal.Show();
        }

        /// <summary>Повесить панель на дверь выхода (со стороны игрока). Дальше она стоит на месте.</summary>
        public void PlaceOnDoor(Transform door)
        {
            Camera cam = Camera.main;
            if (door == null || cam == null) return;
            var rends = door.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { transform.position = door.position + Vector3.up * heightOnDoor; return; }
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            // тонкая ось двери — её нормаль; сторона — та, где игрок
            bool alongX = b.size.x < b.size.z;
            Vector3 n = alongX ? Vector3.right : Vector3.forward;
            Vector3 toPlayer = cam.transform.position - b.center;
            if (Vector3.Dot(n, toPlayer) < 0f) n = -n;
            float thick = alongX ? b.size.x : b.size.z;
            Vector3 pos = b.center + n * (thick * 0.5f + offDoor);
            pos.y = Mathf.Min(b.min.y + heightOnDoor, b.max.y - 0.25f);
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(-n, Vector3.up);   // лицом к игроку
        }
    }
}
