using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Окно по ручке двери в экзамене: три карточки — «перейти в одну комнату», «перейти в другую», «завершить экзамен».
    /// Кнопки переходов повторяют кнопки обычного окна перехода (CanvasExit): названия берутся оттуда,
    /// нажатие вызывает те же действия. Собирается скриптом Koks → Build UI → 20.
    /// </summary>
    public class ExamFinishWindow : MonoBehaviour
    {
        public Button[] roomButtons;       // 2 кнопки «Перейти»
        public TMP_Text[] roomTitles;      // 2 названия комнат
        public GameObject[] roomCards;     // 2 карточки (скрываются, если переходов меньше)
        public Button finishButton;
        public Button closeButton;
        public CanvasGroup group;
        public float distance = 1.4f;
        public float fadeSeconds = 0.25f;

        Canvas canvas;
        Action onFinish, onClose;
        bool open;

        public bool IsOpen { get { return open; } }

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            UIDepth.AlwaysOnTop(gameObject);
            if (roomButtons != null) foreach (var b in roomButtons) Prepare(b);
            Prepare(finishButton);
            Prepare(closeButton);
            if (finishButton != null) finishButton.onClick.AddListener(OnFinish);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
            if (group != null) group.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
        }

        static void Prepare(Button b)
        {
            if (b == null) return;
            var target = b.targetGraphic;
            foreach (var g in b.GetComponentsInChildren<Graphic>(true))
                if (g != target) g.raycastTarget = false;
            var im = target as Image;
            if (im != null) im.raycastPadding = new Vector4(-14f, -10f, -14f, -10f);
        }

        static string LabelOf(Component c)
        {
            var t = c.GetComponentInChildren<TMP_Text>(true);
            if (t != null) return t.text;
            var u = c.GetComponentInChildren<Text>(true);
            return u != null ? u.text : "";
        }

        /// <summary>Кнопки переходов из обычного окна перехода: у каждой берём подпись карточки.</summary>
        public static List<KeyValuePair<Button, string>> ReadRooms(GameObject legacyCanvas)
        {
            var res = new List<KeyValuePair<Button, string>>();
            var buttons = new List<Button>(legacyCanvas.GetComponentsInChildren<Button>(true));
            var go = new List<Button>();
            foreach (var b in buttons)
            {
                string l = (LabelOf(b) ?? "").Trim();
                if (l.IndexOf("ерейти", StringComparison.OrdinalIgnoreCase) >= 0) go.Add(b);
            }
            if (go.Count == 0)
                foreach (var b in buttons) if ((LabelOf(b) ?? "").Trim().Length > 1) go.Add(b);

            // подписи: текст, не лежащий внутри кнопок, ближайший к кнопке по горизонтали (над ней)
            var texts = new List<Component>();
            foreach (var t in legacyCanvas.GetComponentsInChildren<TMP_Text>(true))
                if (t.GetComponentInParent<Button>() == null && !string.IsNullOrWhiteSpace(t.text)) texts.Add(t);
            foreach (var t in legacyCanvas.GetComponentsInChildren<Text>(true))
                if (t.GetComponentInParent<Button>() == null && !string.IsNullOrWhiteSpace(t.text)) texts.Add(t);

            foreach (var b in go)
            {
                Vector3 bl = legacyCanvas.transform.InverseTransformPoint(b.transform.position);
                string best = "Перейти"; float bestScore = float.MaxValue;
                foreach (var tc in texts)
                {
                    Vector3 tl = legacyCanvas.transform.InverseTransformPoint(tc.transform.position);
                    float dx = Mathf.Abs(tl.x - bl.x), dy = bl.y - tl.y;   // подпись выше кнопки
                    if (dy < -0.0001f) continue;
                    float score = dx * 3f + Mathf.Abs(dy);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        var tm = tc as TMP_Text;
                        best = tm != null ? tm.text : ((Text)tc).text;
                    }
                }
                res.Add(new KeyValuePair<Button, string>(b, best));
            }
            // слева направо
            res.Sort((a, b2) => legacyCanvas.transform.InverseTransformPoint(a.Key.transform.position).x
                .CompareTo(legacyCanvas.transform.InverseTransformPoint(b2.Key.transform.position).x));
            return res;
        }

        public void Show(List<KeyValuePair<Button, string>> rooms, Action finish, Action close)
        {
            if (open) return;
            open = true;
            onFinish = finish; onClose = close;

            for (int i = 0; roomButtons != null && i < roomButtons.Length; i++)
            {
                bool has = rooms != null && i < rooms.Count;
                if (roomCards != null && i < roomCards.Length && roomCards[i] != null) roomCards[i].SetActive(has);
                if (!has) continue;
                if (roomTitles != null && i < roomTitles.Length && roomTitles[i] != null) roomTitles[i].text = rooms[i].Value;
                var legacy = rooms[i].Key;
                roomButtons[i].onClick.RemoveAllListeners();
                roomButtons[i].onClick.AddListener(() => { Close(); legacy.onClick.Invoke(); });
            }

            StepWindow.ExternalOpen(true);
            Camera cam = Camera.main;
            if (canvas != null)
            {
                if (canvas.worldCamera == null) canvas.worldCamera = cam;
                canvas.enabled = true;
            }
            if (cam != null)
            {
                Vector3 fwd = cam.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                fwd.Normalize();
                transform.position = cam.transform.position + fwd * distance;
                Vector3 look = transform.position - cam.transform.position; look.y = 0f;
                if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }
            UISound.WindowOpened();
            StopAllCoroutines();
            StartCoroutine(Fade());
        }

        System.Collections.IEnumerator Fade()
        {
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (group != null) group.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        void Close()
        {
            open = false;
            StopAllCoroutines();
            if (group != null) group.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
            StepWindow.ExternalOpen(false);
        }

        void OnFinish() { var a = onFinish; Close(); if (a != null) a(); }
        void OnClose() { var a = onClose; Close(); if (a != null) a(); }
    }
}
