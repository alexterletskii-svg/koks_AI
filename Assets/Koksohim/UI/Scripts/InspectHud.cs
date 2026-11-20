using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Общее окно осмотра: кольцо прогресса, пока луч держат на точке, и карточка с текстом после осмотра.
    /// Точки (InspectPoint) сообщают сюда о наведении и завершении. Тексты самих точек правятся на точках.
    /// </summary>
    public class InspectHud : MonoBehaviour
    {
        [Header("Размещение окна")]
        [Tooltip("Расстояние от головы игрока до окна, м")]
        public float distance = 1.1f;
        [Tooltip("Смещение окна по высоте от уровня глаз, м (минус — ниже)")]
        public float heightOffset = -0.1f;

        [Header("События")]
        [Tooltip("Осмотрены все точки")]
        public UnityEvent onAllInspected;

        [Header("Когда осмотрены все точки")]
        [Tooltip("Окно, которое покажется (например, «Клапаны осмотрены»)")]
        public StepWindow completionWindow;
        [Tooltip("Окно, после «Приступить к осмотру» в котором начинается осмотр (окно шага 1). Пока его не закрыли, маркеры скрыты. Пусто — осмотр доступен сразу.")]
        public StepWindow startWindow;
        [Tooltip("Объекты, которые включатся (например, следующий синий круг)")]
        public GameObject[] activateOnComplete;

        [Header("Точки (заполняется билдером)")]
        public System.Collections.Generic.List<InspectPoint> points = new System.Collections.Generic.List<InspectPoint>();

        [Header("Ссылки (заполняются билдером)")]
        public GameObject progressPanel;
        public GameObject cardPanel;
        public Image ringFill;
        public TMP_Text percentText;
        public TMP_Text progressTitle;
        public TMP_Text progressHint;
        public TMP_Text cardGroup;
        public TMP_Text cardTitle;
        public TMP_Text cardText;
        public TMP_Text counterText;
        public Image[] segments;

        InspectPoint current;
        int done;
        bool allFired;

        static readonly Color SegOff = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color SegOn = new Color(1f, 0.768f, 0f, 1f);

        void Awake()
        {
            // то, что должно появиться после осмотра, скрыто до этого момента
            if (activateOnComplete != null)
                foreach (var g in activateOnComplete) if (g != null) g.SetActive(false);
            Active = startWindow == null;
            if (startWindow != null) startWindow.onConfirmed.AddListener(Begin);
            LayoutSegments();
            UIDepth.AlwaysOnTop(gameObject);
            HideAll();
            Refresh();
        }

        public bool IsFree(InspectPoint p)
        {
            return current == null || current == p;
        }

        public bool AllDone { get { return allFired; } }

        /// <summary>Осмотр начат (игрок нажал «Приступить к осмотру»).</summary>
        public bool Active { get; private set; }

        public void Begin() { Active = true; }

        public int Total { get { return points.Count; } }
        public int Done { get { return done; } }

        public void ShowProgress(InspectPoint p, float t01)
        {
            if (current != p)
            {
                current = p;
                Place(p);
                progressTitle.text = p.group + ": " + p.title;
                progressHint.text = "Держите луч на объекте";
                cardPanel.SetActive(false);
                progressPanel.SetActive(true);
            }
            t01 = Mathf.Clamp01(t01);
            ringFill.fillAmount = t01;
            percentText.text = Mathf.RoundToInt(t01 * 100f) + "%";
        }

        public void ShowCard(InspectPoint p)
        {
            if (current != p) Place(p);
            current = p;
            cardGroup.text = p.group;
            cardTitle.text = p.title;
            cardText.text = p.text;
            Refresh();
            progressPanel.SetActive(false);
            cardPanel.SetActive(true);
        }

        public void Hide(InspectPoint p)
        {
            if (current != p) return;
            current = null;
            HideAll();
        }

        /// <summary>Точка осмотрена (до показа итогового окна).</summary>
        public event System.Action<InspectPoint> PointInspected;

        public void NotifyInspected(InspectPoint p)
        {
            if (PointInspected != null) PointInspected(p);
            done = 0;
            foreach (var x in points) if (x != null && x.Inspected) done++;
            Refresh();
            if (!allFired && points.Count > 0 && done >= points.Count)
            {
                allFired = true;
                current = null;
                HideAll();
                onAllInspected.Invoke();
                if (completionWindow != null) StartCoroutine(ShowWhenFree(completionWindow));
                if (activateOnComplete != null)
                    foreach (var g in activateOnComplete) if (g != null) g.SetActive(true);
            }
        }

        // итоговое окно ждёт, пока закроется другое окно шага (например, «запишите в журнал»)
        System.Collections.IEnumerator ShowWhenFree(StepWindow w)
        {
            yield return null;
            while (StepWindow.AnyOpen) yield return null;
            w.Show();
        }

        // полоски счётчика растягиваются под число точек (сколько бы их ни было)
        void LayoutSegments()
        {
            if (segments == null || segments.Length == 0 || points.Count == 0 || cardPanel == null) return;
            var cr = cardPanel.transform as RectTransform;
            float total = cr.sizeDelta.x - 56f, gap = 6f;
            int n = points.Count;
            float w = (total - gap * (n - 1)) / n;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;
                bool on = i < n;
                segments[i].gameObject.SetActive(on);
                if (!on) continue;
                var r = segments[i].rectTransform;
                r.sizeDelta = new Vector2(w, r.sizeDelta.y);
                r.anchoredPosition = new Vector2(28f + i * (w + gap), r.anchoredPosition.y);
            }
        }

        void HideAll()
        {
            if (progressPanel != null) progressPanel.SetActive(false);
            if (cardPanel != null) cardPanel.SetActive(false);
        }

        void Refresh()
        {
            if (counterText != null) counterText.text = "Осмотрено " + done + " из " + points.Count;
            if (segments == null) return;
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] != null) segments[i].color = i < done ? SegOn : SegOff;
        }

        void Place(InspectPoint p)
        {
            // окно ставится перед игроком по направлению взгляда, поэтому оно всегда в поле зрения
            Camera cam = Camera.main;
            if (cam == null) { transform.position = p.MarkerPosition; return; }
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            transform.position = cam.transform.position + fwd * distance + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }
}
