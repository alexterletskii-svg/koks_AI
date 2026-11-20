using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Окно «Оперативный журнал»: игрок нажимает «Записать результаты осмотра», запись появляется
    /// «от руки» (дата и время, содержание, замечания), рисуется подпись и ставится штамп «Записано».
    /// Через несколько секунд окно закрывается само. Тексты правятся здесь же, в Inspector.
    /// </summary>
    public class JournalWindow : MonoBehaviour
    {
        [Header("Что записывается (правится здесь)")]
        [Tooltip("Колонка «Содержание сообщений»")]
        [TextArea(1, 3)]
        public string content = "Осмотр газопроводов и кантовочного механизма выполнен.";
        [Tooltip("Колонка «Визы, замечания»")]
        public string remarks = "Замечаний нет";
        [Tooltip("Формат даты и времени (берётся текущее время компьютера)")]
        public string dateFormat = "dd.MM HH:mm";

        [Header("Тексты кнопки")]
        public string writeText = "Записать результаты осмотра";
        public string writingText = "Записываю…";
        public string doneText = "Продолжить";

        [Header("Время")]
        public float fadeSeconds = 0.25f;
        public float charSeconds = 0.025f;
        public float signatureSeconds = 0.9f;
        [Tooltip("Через сколько секунд после записи окно закроется само")]
        public float closeAfterSeconds = 3f;
        public float distance = 1.5f;
        public bool playSound = true;

        [Header("Ссылки (заполняет скрипт сборки)")]
        public TMP_Text dateText;
        public TMP_Text contentText;
        public TMP_Text remarksText;
        public Image signature;
        public CanvasGroup stamp;
        public Button actionButton;
        public TMP_Text actionLabel;
        public CanvasGroup group;

        /// <summary>Запись в журнал сделана (экзамен считает по этому событию).</summary>
        public event System.Action Written;

        Canvas canvas;
        bool open;
        bool busy;
        bool written;
        Coroutine run;
        Coroutine fade;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            UIDepth.AlwaysOnTop(gameObject);
            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnAction);
                // кнопка ловит луч одной целой областью: дочерние картинки и текст луч не перехватывают, область чуть шире видимой
                var target = actionButton.targetGraphic;
                foreach (var g in actionButton.GetComponentsInChildren<Graphic>(true))
                    if (g != target) g.raycastTarget = false;
                var im = target as Image;
                if (im != null) im.raycastPadding = new Vector4(-40f, -30f, -40f, -30f);
            }
            Clear();
        }

        public bool IsOpen { get { return open; } }

        void Clear()
        {
            if (dateText != null) dateText.text = "";
            if (contentText != null) contentText.text = "";
            if (remarksText != null) remarksText.text = "";
            if (signature != null) signature.fillAmount = 0f;
            if (stamp != null) { stamp.alpha = 0f; stamp.transform.localScale = Vector3.one * 1.4f; }
            if (actionLabel != null) actionLabel.text = writeText;
            if (actionButton != null) actionButton.interactable = true;
            busy = false;
            written = false;
        }

        public void Show()
        {
            if (open) return;
            open = true;
            StepWindow.ExternalOpen(true);
            Clear();
            if (canvas == null) canvas = GetComponent<Canvas>();
            // камера нужна до включения: иначе XR-рейкастер сыплет KeyNotFoundException
            if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = Camera.main;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (canvas != null) canvas.enabled = true;
            Place();
            if (playSound) UISound.WindowOpened();
            StartFade(0f, 1f, false);
        }

        public void Hide()
        {
            if (!open) return;
            if (run != null) { StopCoroutine(run); run = null; }
            StartFade(group != null ? group.alpha : 1f, 0f, true);
        }

        void Update()
        {
            // окно стоит на месте, где открылось: если оно «плывёт» за головой, луч промахивается мимо кнопки
        }

        void Place()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            transform.position = cam.transform.position + fwd * distance + Vector3.down * 0.1f;
            Face(cam);
        }

        void Face(Camera cam)
        {
            Vector3 look = transform.position - cam.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        void OnAction()
        {
            if (busy) return;
            if (written) { Hide(); return; }
            run = StartCoroutine(Write());
        }

        IEnumerator Write()
        {
            busy = true;
            if (actionButton != null) actionButton.interactable = false;
            if (actionLabel != null) actionLabel.text = writingText;

            yield return Type(dateText, System.DateTime.Now.ToString(dateFormat));
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Type(contentText, content);
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Type(remarksText, remarks);
            yield return new WaitForSecondsRealtime(0.1f);

            if (signature != null)
            {
                float t = 0f;
                while (t < signatureSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    signature.fillAmount = Mathf.Clamp01(t / signatureSeconds);
                    yield return null;
                }
                signature.fillAmount = 1f;
            }

            if (stamp != null)
            {
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / 0.25f);
                    stamp.alpha = k;
                    stamp.transform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, k);
                    yield return null;
                }
                stamp.alpha = 1f;
                stamp.transform.localScale = Vector3.one;
            }

            written = true;
            busy = false;
            if (Written != null) Written();
            if (actionLabel != null) actionLabel.text = doneText;
            if (actionButton != null) actionButton.interactable = true;

            yield return new WaitForSecondsRealtime(closeAfterSeconds);
            run = null;
            Hide();
        }

        IEnumerator Type(TMP_Text label, string text)
        {
            if (label == null) yield break;
            label.text = text;
            label.maxVisibleCharacters = 0;
            label.ForceMeshUpdate();
            int n = text.Length;
            for (int i = 1; i <= n; i++)
            {
                label.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(charSeconds);
            }
            label.maxVisibleCharacters = 99999;
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
                // объект не выключаем (выключение канваса с XR-рейкастером даёт ошибку) — скрываем канвас
                open = false;
                StepWindow.ExternalOpen(false);
                if (canvas != null) canvas.enabled = false;
            }
            fade = null;
        }
    }
}
