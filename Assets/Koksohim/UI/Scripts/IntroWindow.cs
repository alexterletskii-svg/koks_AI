using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Вводное окно сценария: появляется при старте сцены слева-вверху перед игроком,
    /// через closeAfterSeconds плавно гаснет. Закрыть раньше можно крестиком.
    /// Тексты правятся прямо в Inspector (поля «Тексты»).
    /// </summary>
    public class IntroWindow : MonoBehaviour
    {
        [Header("Тексты (правятся здесь)")]
        public string statusText = "Сценарий начался";
        public string title = "Осмотр оборудования тоннелей коксовой батареи";
        [TextArea(2, 6)]
        public string description = "Здесь будет вводный текст: что это за сценарий и зачем он нужен.";
        [Tooltip("До трёх пунктов «что вы изучите». Пустой пункт скрывается.")]
        public string[] bullets = { "Что вы изучите: пункт 1", "Что вы изучите: пункт 2", "Что вы изучите: пункт 3" };

        [Header("Показ")]
        [Tooltip("Сколько секунд окно висит до автоматического закрытия")]
        public float closeAfterSeconds = 10f;
        public float fadeSeconds = 0.5f;
        [Tooltip("Расстояние от игрока, м")]
        public float distance = 1.3f;
        [Tooltip("Смещение влево от центра взгляда, м")]
        public float shiftLeft = 0.45f;
        [Tooltip("Смещение вверх от уровня глаз, м")]
        public float shiftUp = 0.25f;

        [Header("Ссылки (заполняются билдером)")]
        public TMP_Text statusLabel;
        public TMP_Text titleLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text[] bulletLabels;
        public GameObject[] bulletBars;
        public RectTransform progressFill;
        public CanvasGroup group;
        public Button closeButton;

        bool closing;

        void Awake()
        {
            Apply();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        void OnValidate()
        {
            Apply();
        }

        void Apply()
        {
            if (statusLabel != null) statusLabel.text = statusText;
            if (titleLabel != null) titleLabel.text = title;
            if (descriptionLabel != null) descriptionLabel.text = description;
            if (bulletLabels == null) return;
            for (int i = 0; i < bulletLabels.Length; i++)
            {
                string t = (bullets != null && i < bullets.Length) ? bullets[i] : "";
                if (bulletLabels[i] != null) bulletLabels[i].text = t;
                bool on = !string.IsNullOrEmpty(t);
                if (bulletLabels[i] != null) bulletLabels[i].gameObject.SetActive(on);
                if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
                    bulletBars[i].SetActive(on);
            }
        }

        IEnumerator Start()
        {
            if (group != null) group.alpha = 0f;
            SetProgress(1f);

            // ждём, пока XR-камера встанет на место
            float wait = 0f;
            while ((Camera.main == null || wait < 0.4f) && wait < 3f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Place();
            UISound.WindowOpened();

            yield return Fade(0f, 1f, 0.4f);

            float t = 0f;
            while (t < closeAfterSeconds && !closing)
            {
                t += Time.unscaledDeltaTime;
                SetProgress(1f - t / Mathf.Max(0.01f, closeAfterSeconds));
                yield return null;
            }
            if (!closing) Close();
        }

        // окно остаётся на месте, но поворачивается лицом к игроку, как остальные окна
        void Update()
        {
            if (closing) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 look = transform.position - cam.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        void Place()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            Vector3 pos = cam.transform.position + fwd * distance - right * shiftLeft + Vector3.up * shiftUp;
            transform.position = pos;

            Vector3 look = pos - cam.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        void SetProgress(float v)
        {
            if (progressFill == null) return;
            var s = progressFill.localScale;
            s.x = Mathf.Clamp01(v);
            progressFill.localScale = s;
        }

        public void Close()
        {
            if (closing) return;
            closing = true;
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            float from = group != null ? group.alpha : 1f;
            yield return Fade(from, 0f, fadeSeconds);
            gameObject.SetActive(false);
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            if (group == null) yield break;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / Mathf.Max(0.01f, seconds));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
