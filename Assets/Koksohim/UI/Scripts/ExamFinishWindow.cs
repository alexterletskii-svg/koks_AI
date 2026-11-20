using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Окно по ручке двери в экзамене: копия обычного окна перехода (CanvasExit), три карточки — диспетчерская,
    /// комната лебёдок, «завершить экзамен». Названия и значки переходов берутся из обычных окон, нажатие вызывает те же действия. Собирается скриптом Koks → Build UI → 20.
    /// </summary>
    public class ExamFinishWindow : MonoBehaviour
    {
        public Button[] roomButtons;       // 2 карточки-кнопки переходов
        public Text[] roomTitles;          // названия комнат на карточках
        public Image[] roomIcons;          // значки комнат
        public Button finishButton;
        public Button closeButton;
        public CanvasGroup group;
        public float distance = 1.5f;
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

        public class Room
        {
            public Button button;
            public string title;
            public Sprite icon;
        }

        static string TextOf(Component c)
        {
            var tm = c as TMP_Text;
            if (tm != null) return tm.text;
            var u = c as Text;
            return u != null ? u.text : "";
        }

        /// <summary>Все переходы из обычных окон перехода (CanvasExit): кнопка-карточка, название и значок комнаты.</summary>
        public static List<Room> ReadRooms(IEnumerable<GameObject> legacyCanvases)
        {
            var res = new List<Room>();
            foreach (var cv in legacyCanvases)
            {
                if (cv == null) continue;
                foreach (var b in cv.GetComponentsInChildren<Button>(true))
                {
                    // название — самый длинный текст внутри карточки; у кнопки «X» названия нет
                    string best = "";
                    foreach (var t in b.GetComponentsInChildren<Text>(true)) if (t.text != null && t.text.Trim().Length > best.Length) best = t.text.Trim();
                    foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) if (t.text != null && t.text.Trim().Length > best.Length) best = t.text.Trim();
                    if (best.Length < 8) continue;
                    Sprite icon = null;
                    foreach (var im in b.GetComponentsInChildren<Image>(true))
                        if (im.name == "KoksIcon") { icon = im.sprite; break; }
                    res.Add(new Room { button = b, title = best, icon = icon });
                }
            }
            return res;
        }

        static Room Find(List<Room> rooms, string needleA, string needleB)
        {
            foreach (var r in rooms)
            {
                string t = r.title.ToLowerInvariant();
                if (t.Contains(needleA) || (needleB != null && t.Contains(needleB))) return r;
            }
            return null;
        }

        public void Show(List<Room> all, Action finish, Action close)
        {
            if (open) return;
            open = true;
            onFinish = finish; onClose = close;

            // карточки: диспетчерская и комната лебёдок (если названия не нашлись — первые найденные переходы)
            var chosen = new List<Room>();
            var disp = Find(all, "диспетчер", null);
            var winch = Find(all, "лебедок", "лебёдок");
            if (disp != null) chosen.Add(disp);
            if (winch != null) chosen.Add(winch);
            foreach (var r in all) { if (chosen.Count >= 2) break; if (!chosen.Contains(r)) chosen.Add(r); }

            for (int i = 0; roomButtons != null && i < roomButtons.Length; i++)
            {
                bool has = i < chosen.Count;
                roomButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                if (roomTitles != null && i < roomTitles.Length && roomTitles[i] != null) roomTitles[i].text = chosen[i].title;
                if (roomIcons != null && i < roomIcons.Length && roomIcons[i] != null && chosen[i].icon != null) roomIcons[i].sprite = chosen[i].icon;
                var legacy = chosen[i].button;
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
