using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Значок «Журнал» на экзамене: висит в левом верхнем углу поля зрения и следует за головой.
    /// Наведите луч и нажмите триггер — открывается журнал, где результаты осмотра записываются самому.
    /// </summary>
    public class ExamJournalIcon : MonoBehaviour
    {
        public Button button;
        public JournalWindow journal;
        [Tooltip("Расстояние от глаз и смещение в левый верхний угол поля зрения, м")]
        public float distance = 0.95f;
        public float shiftLeft = 0.5f;
        public float shiftUp = 0.3f;

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

        void LateUpdate()
        {
            if (!visible) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Transform c = cam.transform;
            // привязана к голове: всегда в левом верхнем углу взгляда, с лёгким запаздыванием
            Vector3 target = c.position + c.forward * distance - c.right * shiftLeft + c.up * shiftUp;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            Vector3 look = transform.position - c.position;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, c.up);
        }
    }
}
