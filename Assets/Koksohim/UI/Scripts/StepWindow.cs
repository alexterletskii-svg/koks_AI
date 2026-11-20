using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Окно шага осмотра: что за оборудование и что в нём проверить.
    /// Показывается через Show() (его вызывает StepMarker, когда игрок встал в круг),
    /// закрывается кнопкой «Приступить к осмотру». Тексты правятся в Inspector.
    /// </summary>
    public class StepWindow : MonoBehaviour
    {
        [Header("Тексты (правятся здесь)")]
        public string stepText = "Шаг 1";
        public string title = "Газовоздушные клапаны";
        [TextArea(2, 6)]
        public string description = "Здесь находится [название оборудования и для чего оно]. Его нужно осмотреть при приёмке смены.";
        public string checksHeader = "Что проверить";
        [Tooltip("По три пункта на страницу. Больше трёх — окно листается кнопками «Дальше» и «Назад».")]
        public string[] checks = { "Пункт проверки 1", "Пункт проверки 2", "Пункт проверки 3" };
        public string buttonText = "Приступить к осмотру";

        [Header("Показ")]
        public float fadeSeconds = 0.3f;
        [Tooltip("Если задана — окно встаёт на эту точку (например, рядом с оборудованием). Иначе — перед игроком.")]
        public Transform anchor;
        [Tooltip("Расстояние перед игроком, м (если нет точки)")]
        public float distance = 1.5f;
        [Tooltip("Смещение по высоте от уровня глаз, м (если нет точки)")]
        public float shiftUp = 0f;

        [Header("События")]
        public UnityEvent onShown;
        [Tooltip("Игрок нажал «Приступить к осмотру»")]
        public UnityEvent onConfirmed;

        [Header("Ссылки (заполняются билдером)")]
        public TMP_Text stepLabel;
        public TMP_Text titleLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text checksHeaderLabel;
        public TMP_Text[] checkLabels;
        public GameObject[] checkBars;
        public TMP_Text buttonLabel;
        public CanvasGroup group;
        public Button confirmButton;
        [Tooltip("Только для окон на несколько страниц")]
        public Button backButton;
        public TMP_Text backLabel;
        public TMP_Text pageLabel;
        public string nextText = "Дальше";
        public string backText = "Назад";

        bool wired;
        int page;
        bool open;
        [Header("Звук и таймер")]
        [Tooltip("Звук при появлении окна")]
        public bool playSound = true;
        [Tooltip("Окно закроется само через это число секунд (как после нажатия кнопки). 0 — только по кнопке. Для окон с «Приступить к осмотру» оставляйте 0.")]
        public float autoConfirmSeconds = 0f;

        bool buttonFitted;
        static int openCount;
        /// <summary>Сейчас на экране есть окно шага (маркеры осмотра в это время скрываются).</summary>
        public static bool AnyOpen { get { return openCount > 0; } }

        /// <summary>Другое окно (журнал) сообщает, что оно открыто/закрыто — маркеры осмотра на это время скрываются.</summary>
        public static void ExternalOpen(bool opened)
        {
            openCount = Mathf.Max(0, openCount + (opened ? 1 : -1));
        }

        Coroutine fade;
        Coroutine autoTimer;
        Canvas canvas;

        void Awake()
        {
            Apply();
            Wire();
            UIDepth.AlwaysOnTop(gameObject);
        }

        void Wire()
        {
            SoftenHover(confirmButton);
            SoftenHover(backButton);
            if (wired || confirmButton == null) return;
            confirmButton.onClick.AddListener(OnConfirmClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            wired = true;
        }

        // пока окно открыто, оно поворачивается лицом к игроку (чтобы текст не оказался зеркальным, если игрок обошёл окно)
        void Update()
        {
            if (!open) return;
            Camera cam = Camera.main;
            if (cam != null) FaceCamera(cam);
        }

        void OnValidate()
        {
            Apply();
        }

        int Pages()
        {
            int n = 0;
            if (checks != null)
                for (int i = 0; i < checks.Length; i++) if (!string.IsNullOrEmpty(checks[i])) n = i + 1;
            int per = checkLabels != null && checkLabels.Length > 0 ? checkLabels.Length : 3;
            return Mathf.Max(1, (n + per - 1) / per);
        }

        public void Apply()
        {
            if (stepLabel != null) stepLabel.text = stepText;
            if (titleLabel != null) titleLabel.text = title;
            if (descriptionLabel != null) descriptionLabel.text = description;
            if (checksHeaderLabel != null) checksHeaderLabel.text = checksHeader;

            int pages = Pages();
            page = Mathf.Clamp(page, 0, pages - 1);
            bool last = page >= pages - 1;
            if (buttonLabel != null)
            {
                if (!buttonFitted)
                {
                    // длинный текст кнопки («Приступить к осмотру») сам уменьшается, чтобы влезть
                    buttonFitted = true;
                    buttonLabel.enableAutoSizing = true;
                    buttonLabel.fontSizeMax = buttonLabel.fontSize;
                    buttonLabel.fontSizeMin = 16f;
                    buttonLabel.textWrappingMode = TextWrappingModes.NoWrap;
                }
                buttonLabel.text = last ? buttonText : nextText;
            }
            if (pageLabel != null) pageLabel.text = pages > 1 ? (page + 1) + " из " + pages : "";
            if (backButton != null)
            {
                backButton.gameObject.SetActive(page > 0);
                if (backLabel != null) backLabel.text = backText;
                // при кнопке «Назад» основная кнопка уже
                var cr = confirmButton != null ? confirmButton.transform as RectTransform : null;
                if (cr != null)
                {
                    float full = 644f, back = 200f, gap = 16f;
                    float w = page > 0 ? full - back - gap : full;
                    cr.sizeDelta = new Vector2(w, cr.sizeDelta.y);
                    cr.anchoredPosition = new Vector2(page > 0 ? 28f + back + gap : 28f, cr.anchoredPosition.y);
                }
            }

            if (checkLabels == null) return;
            for (int i = 0; i < checkLabels.Length; i++)
            {
                int idx = page * checkLabels.Length + i;
                string t = (checks != null && idx < checks.Length) ? checks[idx] : "";
                bool on = !string.IsNullOrEmpty(t);
                if (checkLabels[i] != null)
                {
                    checkLabels[i].text = t;
                    checkLabels[i].gameObject.SetActive(on);
                }
                if (checkBars != null && i < checkBars.Length && checkBars[i] != null)
                    checkBars[i].SetActive(on);
            }
        }

        void OnConfirmClicked()
        {
            if (page < Pages() - 1) { page++; Apply(); return; }
            Confirm();
        }

        void OnBackClicked()
        {
            if (page > 0) { page--; Apply(); }
        }

        public bool IsOpen
        {
            get { return open; }
        }

        public void Show()
        {
            if (open) return;
            open = true;
            openCount++;
            page = 0;
            Apply();
            if (canvas == null) canvas = GetComponent<Canvas>();
            // камера нужна до включения: иначе XR-рейкастер регистрирует канвас без неё и сыплет KeyNotFoundException
            if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = Camera.main;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (canvas != null) canvas.enabled = true;
            Wire();
            Place();
            if (group != null) group.alpha = 0f;
            StartFade(0f, 1f, false);
            if (playSound) UISound.WindowOpened();
            if (autoTimer != null) StopCoroutine(autoTimer);
            if (autoConfirmSeconds > 0f) autoTimer = StartCoroutine(AutoConfirm());
            onShown.Invoke();
        }

        // при наведении кнопка светлеет до тёплого жёлтого, а не белого
        static void SoftenHover(Button b)
        {
            if (b == null) return;
            var cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.87f, 0.45f, 1f);
            b.colors = cb;
            // светлая подложка при наведении — тёплая, а не белая
            var hover = b.transform.Find("Hover");
            if (hover != null)
            {
                var hi = hover.GetComponent<Image>();
                if (hi != null) hi.color = new Color(1f, 0.92f, 0.55f, 0.3f);
            }
        }

        IEnumerator AutoConfirm()
        {
            yield return new WaitForSecondsRealtime(autoConfirmSeconds);
            autoTimer = null;
            if (open) Confirm();
        }

        void Place()
        {
            Camera cam = Camera.main;
            if (anchor != null)
            {
                transform.position = anchor.position;
                if (cam != null) FaceCamera(cam);
                return;
            }
            if (cam == null) return;

            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            float dist = distance;
            transform.position = cam.transform.position + fwd * dist + Vector3.up * shiftUp;
            FaceCamera(cam);
        }

        void FaceCamera(Camera cam)
        {
            Vector3 look = transform.position - cam.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        public void Confirm()
        {
            onConfirmed.Invoke();
            Hide();
        }

        public void Hide()
        {
            if (!open) return;
            if (autoTimer != null) { StopCoroutine(autoTimer); autoTimer = null; }
            StartFade(group != null ? group.alpha : 1f, 0f, true);
        }

        void StartFade(float from, float to, bool deactivate)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Fade(from, to, deactivate));
        }

        IEnumerator Fade(float from, float to, bool deactivate)
        {
            if (group != null)
            {
                float t = 0f;
                while (t < fadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    group.alpha = Mathf.Lerp(from, to, t / Mathf.Max(0.01f, fadeSeconds));
                    yield return null;
                }
                group.alpha = to;
            }
            if (deactivate)
            {
                // объект не выключаем (выключение канваса с XR-рейкастером даёт ошибку) — просто скрываем канвас
                open = false;
                openCount = Mathf.Max(0, openCount - 1);
                if (canvas != null) canvas.enabled = false;
            }
            fade = null;
        }
    }
}
