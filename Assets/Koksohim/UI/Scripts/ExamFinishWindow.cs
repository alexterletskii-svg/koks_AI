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
        public TMP_Text[] roomTitles;          // названия комнат на карточках
        public Image[] roomIcons;          // значки комнат
        public Button finishButton;
        public Button closeButton;
        public CanvasGroup group;
        public float distance = 1.6f;
        public float fadeSeconds = 0.25f;

        public float yawLag = 0.45f;
        public float heightBelowEyes = 0.1f;

        Canvas canvas;
        Action onFinish, onClose;
        float yaw, yawVel;
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
            // клик по дочерним элементам карточки всплывает до кнопки; сама кнопка должна ловить луч
            var own = b.GetComponent<Graphic>();
            if (b.targetGraphic == null && own != null) b.targetGraphic = own;
            if (own != null) own.raycastTarget = true;
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
                Vector3 f = cam.transform.forward; f.y = 0f;
                if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
                yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; yawVel = 0f;
                Follow(cam, true);
            }
            UISound.WindowOpened();
            StopAllCoroutines();
            StartCoroutine(Fade());
        }

        // окно держится перед игроком и разворачивается за ним
        void Follow(Camera cam, bool snap)
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            float camYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            yaw = snap ? camYaw : Mathf.SmoothDampAngle(yaw, camYaw, ref yawVel, yawLag, Mathf.Infinity, Time.unscaledDeltaTime);
            Vector3 target = cam.transform.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance;
            target.y = cam.transform.position.y - heightBelowEyes;
            transform.position = snap ? target : Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            Vector3 look = transform.position - cam.transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        void LateUpdate()
        {
            if (!open) return;
            Camera cam = Camera.main;
            if (cam != null) Follow(cam, false);
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
