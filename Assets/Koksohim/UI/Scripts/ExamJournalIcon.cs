using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Значок «Журнал» на экзамене: держится у пояса слева (как планшет) и следует за игроком.
    /// Наведите луч и нажмите триггер — открывается журнал, где результаты осмотра записываются самому.
    /// </summary>
    public class ExamJournalIcon : MonoBehaviour
    {
        public Button button;
        public JournalWindow journal;
        [Tooltip("Где держится относительно игрока: вперёд / влево / вниз от уровня глаз, м")]
        public float forward = 0.5f;
        public float left = 0.3f;
        public float down = 0.5f;
        [Tooltip("Насколько лениво панель разворачивается за поворотом тела, с")]
        public float yawLag = 0.6f;

        float yaw, yawVel;
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

        /// <summary>Панель следует за игроком, как планшет у пояса: держится слева-внизу и лениво разворачивается за телом.</summary>
        void LateUpdate()
        {
            if (!visible) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Transform c = cam.transform;
            Vector3 f = c.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            float camYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            bool first = !placed;
            if (first) { yaw = camYaw; yawVel = 0f; placed = true; }
            yaw = Mathf.SmoothDampAngle(yaw, camYaw, ref yawVel, yawLag);
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Vector3 target = c.position + q * (Vector3.forward * forward + Vector3.left * left) + Vector3.down * down;
            transform.position = first ? target : Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            Vector3 look = transform.position - c.position;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        public void PlaceAtEntrance() { placed = false; }
    }
}
