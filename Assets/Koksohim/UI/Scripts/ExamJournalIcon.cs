using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Значок «Журнал» на экзамене: висит в верхнем правом углу поля зрения и следует за головой.
    /// Наведите луч и нажмите триггер — открывается журнал, где результаты осмотра записываются самому.
    /// </summary>
    public class ExamJournalIcon : MonoBehaviour
    {
        public Button button;
        public JournalWindow journal;
        [Tooltip("Где держится относительно взгляда, м: вперёд / влево (минус — вправо) / вверх. Сейчас — верхний правый угол")]
        public float forward = 1.0f;
        public float left = -0.85f;
        public float up = 0.42f;
        [Tooltip("Плавность следования за головой (больше — резче)")]
        public float follow = 9f;

        bool placed;

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

        /// <summary>Значок висит перед взглядом слева (на уровне глаз), не закрывая центр, и плавно следует за головой.</summary>
        void LateUpdate()
        {
            if (!visible) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Transform c = cam.transform;
            Vector3 target = c.position + c.forward * forward - c.right * left + c.up * up;
            bool first = !placed;
            placed = true;
            transform.position = first ? target : Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime));
            Vector3 look = transform.position - c.position;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, c.up);
        }

        /// <summary>Совместимость: при старте экзамена значок встанет на место сразу.</summary>
        public void PlaceOnDoor(Transform ignored) { placed = false; }
    }
}
