using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Значок «Журнал» на экзамене: висит перед игроком слева-внизу и следует за головой.
    /// Наведите луч и нажмите триггер — открывается журнал, где результаты осмотра записываются самому.
    /// </summary>
    public class ExamJournalIcon : MonoBehaviour
    {
        public Button button;
        public JournalWindow journal;
        public float distance = 0.9f;
        public float shiftLeft = 0.32f;
        public float shiftDown = 0.3f;

        Canvas canvas;
        bool visible;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            UIDepth.AlwaysOnTop(gameObject);
            if (button != null)
            {
                button.onClick.AddListener(Open);
                var im = button.targetGraphic as Image;
                if (im != null) im.raycastPadding = new Vector4(-25f, -25f, -25f, -25f);
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
            Vector3 fwd = c.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 target = c.position + fwd * distance - right * shiftLeft + Vector3.down * shiftDown;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            Vector3 look = transform.position - c.position; look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }
    }
}
