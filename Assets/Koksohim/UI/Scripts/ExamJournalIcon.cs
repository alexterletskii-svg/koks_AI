using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Значок «Журнал» на экзамене: стоит неподвижно у входа в тоннель (ставится туда, где игрок вошёл), как стенд с журналом.
    /// Наведите луч и нажмите триггер — открывается журнал, где результаты осмотра записываются самому.
    /// </summary>
    public class ExamJournalIcon : MonoBehaviour
    {
        public Button button;
        public JournalWindow journal;
        [Tooltip("Куда ставить относительно игрока в момент входа в тоннель, м: вперёд / влево / вниз от уровня глаз")]
        public float forward = 1.5f;
        public float left = 1.0f;
        public float down = 0.15f;

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

        /// <summary>Поставить панель у входа: перед игроком чуть левее, лицом к нему. Дальше она стоит на месте.</summary>
        public void PlaceAtEntrance()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Transform c = cam.transform;
            Vector3 fwd = c.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 pos = c.position + fwd * forward - right * left;
            pos.y = c.position.y - down;
            transform.position = pos;
            Vector3 look = pos - c.position; look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }
    }
}
