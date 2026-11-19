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
        [Tooltip("На сколько метров окно сдвигается от маркера к игроку")]
        public float towardPlayer = 0.3f;
        [Tooltip("Подъём окна над маркером, м")]
        public float lift = 0.2f;

        [Header("События")]
        [Tooltip("Осмотрены все точки")]
        public UnityEvent onAllInspected;

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
            HideAll();
            Refresh();
        }

        public bool IsFree(InspectPoint p)
        {
            return current == null || current == p;
        }

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

        public void NotifyInspected(InspectPoint p)
        {
            done = 0;
            foreach (var x in points) if (x != null && x.Inspected) done++;
            Refresh();
            if (!allFired && points.Count > 0 && done >= points.Count)
            {
                allFired = true;
                onAllInspected.Invoke();
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
            Camera cam = Camera.main;
            Vector3 pos = p.MarkerPosition;
            if (cam == null) { transform.position = pos; return; }
            Vector3 toCam = cam.transform.position - pos;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f) toCam.Normalize();
            transform.position = pos + toCam * towardPlayer + Vector3.up * lift;
            Vector3 look = transform.position - cam.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }
    }
}
