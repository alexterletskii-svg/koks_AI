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

        /// <summary>Запись для экзамена: текст зависит от того, что осмотрено; valid=false — запись не засчитывается.</summary>
        public struct Entry { public string content, remarks; public bool valid; }
        /// <summary>Если задан (экзамен) — вместо фиксированного текста берётся запись отсюда, записей может быть несколько.</summary>
        public System.Func<Entry> entryProvider;

        readonly System.Collections.Generic.List<string[]> entries = new System.Collections.Generic.List<string[]>();
        bool anyValid;
        Button closeButton;
        const int MaxEntries = 5;

        Canvas canvas;
        Vector3 baseScale;
        bool open;
        bool busy;
        bool written;
        Coroutine run;
        Coroutine fade;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            baseScale = transform.localScale;
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
            if (ExamMode.Active) BuildCloseButton();
        }

        void BuildCloseButton()
        {
            var parent = (RectTransform)transform;
            var go = new GameObject("CloseButton", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(76f, 76f);
            rt.anchoredPosition = new Vector2(parent.sizeDelta.x - 100f, -14f);
            rt.SetAsLastSibling();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.55f, 0.13f, 0.1f, 1f);
            bg.raycastTarget = true;
            bg.canvasRenderer.cullTransparentMesh = false;
            for (int i = 0; i < 2; i++)
            {
                var bar = new GameObject("Bar", typeof(RectTransform));
                var br = (RectTransform)bar.transform;
                br.SetParent(rt, false);
                br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
                br.sizeDelta = new Vector2(38f, 7f);
                br.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                var im = bar.AddComponent<Image>();
                im.color = Color.white;
                im.raycastTarget = false;
            }
            closeButton = go.AddComponent<Button>();
            closeButton.targetGraphic = bg;
            var c = closeButton.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1f, 0.75f, 0.7f, 1f);
            c.pressedColor = new Color(0.8f, 0.6f, 0.55f, 1f);
            c.selectedColor = Color.white;
            closeButton.colors = c;
            closeButton.onClick.AddListener(Hide);
            UIDepth.AlwaysOnTop(go);
        }

        public bool IsOpen { get { return open; } }

        void Clear()
        {
            if (dateText != null) dateText.text = "";
            if (contentText != null) contentText.text = "";
            if (remarksText != null) remarksText.text = "";
            if (signature != null) signature.fillAmount = 0f;
            if (stamp != null) { stamp.alpha = 0f; stamp.transform.localScale = Vector3.one * 1.4f; }
            if (ExamMode.Active) RenderEntries(-1);
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
            moving = false;
            if (playSound) UISound.WindowOpened();
            StartFade(0f, 1f, false);
        }

        public void Hide()
        {
            if (!open) return;
            if (run != null) { StopCoroutine(run); run = null; }
            StartFade(group != null ? group.alpha : 1f, 0f, true);
        }

        Vector3 moveTo;
        bool moving;

        // окно стоит на месте (чтобы на него можно было навестись) и всегда повёрнуто к игроку;
        // если игрок ушёл далеко или отвернулся — окно само перелетает перед ним и снова замирает
        void LateUpdate()
        {
            if (!open || !ExamMode.Active) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Face(cam);
            Vector3 to = transform.position - cam.transform.position;
            float flat = new Vector3(to.x, 0f, to.z).magnitude;
            Vector3 f = cam.transform.forward; f.y = 0f;
            float ang = f.sqrMagnitude > 0.01f ? Vector3.Angle(f, new Vector3(to.x, 0f, to.z)) : 0f;
            if (!moving && (flat > distance * 1.8f || ang > 60f))
            {
                float d = WindowFit.Free(cam.transform, f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward, distance, 20f);
                transform.localScale = baseScale * (d / distance);
                moveTo = cam.transform.position + (f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward) * d + Vector3.down * 0.1f * (d / distance);
                moving = true;
            }
            if (moving)
            {
                transform.position = Vector3.Lerp(transform.position, moveTo, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
                if ((transform.position - moveTo).sqrMagnitude < 0.0025f) moving = false;
            }
        }

        void Place()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            float d = WindowFit.Free(cam.transform, fwd, distance, 20f);
            transform.localScale = baseScale * (d / distance);
            transform.position = cam.transform.position + fwd * d + Vector3.down * 0.1f * (d / distance);
            Face(cam);
        }

        void Face(Camera cam)
        {
            Vector3 look = transform.position - cam.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        // экзамен: показать все прежние записи (по одной строке), последнюю — частично, если typed >= 0
        void RenderEntries(int skipLast)
        {
            if (!ExamMode.Active) return;
            foreach (var l in new[] { dateText, contentText, remarksText })
            {
                if (l == null) continue;
                l.textWrappingMode = TextWrappingModes.NoWrap;
                l.overflowMode = TextOverflowModes.Overflow;
                l.fontSize = 22f;
                l.maxVisibleCharacters = 99999;
            }
            string d = "", c = "", r = "";
            int n = entries.Count - (skipLast >= 0 ? 1 : 0);
            for (int i = 0; i < n; i++)
            {
                d += entries[i][0] + "\n"; c += entries[i][1] + "\n"; r += entries[i][2] + "\n";
            }
            if (dateText != null) dateText.text = d;
            if (contentText != null) contentText.text = c;
            if (remarksText != null) remarksText.text = r;
            if (signature != null) signature.fillAmount = anyValid && skipLast < 0 ? 1f : 0f;
        }

        // строка журнала — одна строка: длинный текст обрезается с «…», иначе он вылезает за колонку
        static string Fit(string t, int max)
        {
            t = (t ?? "").Replace("\n", " ");
            return t.Length <= max ? t : t.Substring(0, max - 1).TrimEnd() + "…";
        }

        void OnAction()
        {
            if (busy) return;
            if (ExamMode.Active) { run = StartCoroutine(WriteExam()); return; }
            if (written) { Hide(); return; }
            run = StartCoroutine(Write());
        }

        IEnumerator WriteExam()
        {
            busy = true;
            if (actionButton != null) actionButton.interactable = false;
            if (actionLabel != null) actionLabel.text = writingText;

            Entry e = entryProvider != null ? entryProvider() : new Entry { content = content, remarks = remarks, valid = true };
            entries.Add(new[] { System.DateTime.Now.ToString(dateFormat), Fit(e.content, 42), Fit(e.remarks, 16) });
            while (entries.Count > MaxEntries) entries.RemoveAt(0);
            if (e.valid) anyValid = true;
            // запись принимается (или нет) сразу: закрытие окна не потеряет результат
            if (Written != null && e.valid) Written();

            RenderEntries(1);
            string pd = dateText != null ? dateText.text : "", pc = contentText != null ? contentText.text : "", pr = remarksText != null ? remarksText.text : "";
            var last = entries[entries.Count - 1];
            if (stamp != null) { stamp.alpha = 0f; stamp.transform.localScale = Vector3.one * 1.4f; }
            if (signature != null) signature.fillAmount = 0f;

            yield return TypeLine(dateText, pd, last[0]);
            yield return TypeLine(contentText, pc, last[1]);
            yield return TypeLine(remarksText, pr, last[2]);

            if (e.valid && signature != null)
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
                var st = stamp.GetComponentInChildren<TMP_Text>(true);
                if (st != null) { st.text = e.valid ? "Записано" : "Не принято"; st.color = e.valid ? new Color(0.294f, 0.827f, 0.482f, 1f) : new Color(0.75f, 0.15f, 0.1f); }
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
            busy = false;
            run = null;
            if (actionLabel != null) actionLabel.text = writeText;
            if (actionButton != null) actionButton.interactable = true;
        }

        IEnumerator TypeLine(TMP_Text label, string prefix, string line)
        {
            if (label == null) yield break;
            label.text = prefix + line;
            label.ForceMeshUpdate();
            for (int i = 1; i <= line.Length; i++)
            {
                label.maxVisibleCharacters = prefix.Length + i;
                yield return new WaitForSecondsRealtime(charSeconds);
            }
            label.maxVisibleCharacters = 99999;
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
