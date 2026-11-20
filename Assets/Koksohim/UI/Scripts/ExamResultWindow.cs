using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Итоговое окно экзамена: время, сколько заданий выполнено, список заданий (зелёная точка — выполнено,
    /// красная — нет) и кнопка «В меню выбора сценариев». Собирается скриптом Koks → Build UI → 20.
    /// </summary>
    public class ExamResultWindow : MonoBehaviour
    {
        public TMP_Text timeLabel;
        public TMP_Text scoreLabel;
        public TMP_Text[] rowLabels;
        public Image[] rowDots;
        public Button menuButton;
        public CanvasGroup group;
        public string menuScene = "MenuScene";
        public float distance = 1.5f;
        public float fadeSeconds = 0.35f;

        static readonly Color Ok = new Color(0.294f, 0.827f, 0.482f, 1f);
        static readonly Color Bad = new Color(0.94f, 0.42f, 0.42f, 1f);
        static readonly Color Ink = new Color(0.933f, 0.945f, 0.953f, 1f);
        static readonly Color Mute = new Color(0.643f, 0.682f, 0.714f, 1f);

        Canvas canvas;
        bool open;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            UIDepth.AlwaysOnTop(gameObject);
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(GoToMenu);
                var target = menuButton.targetGraphic;
                foreach (var g in menuButton.GetComponentsInChildren<Graphic>(true))
                    if (g != target) g.raycastTarget = false;
                var im = target as Image;
                if (im != null) im.raycastPadding = new Vector4(-30f, -20f, -30f, -20f);
            }
            if (group != null) group.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
        }

        public bool IsOpen { get { return open; } }

        public void Show(string[] names, bool[] done, float seconds)
        {
            if (open) return;
            open = true;
            int ok = 0;
            for (int i = 0; i < done.Length; i++) if (done[i]) ok++;
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            if (timeLabel != null) timeLabel.text = (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
            if (scoreLabel != null) scoreLabel.text = ok + " из " + done.Length;
            for (int i = 0; rowLabels != null && i < rowLabels.Length; i++)
            {
                bool has = names != null && i < names.Length;
                if (rowLabels[i] != null)
                {
                    rowLabels[i].gameObject.SetActive(has);
                    if (has)
                    {
                        rowLabels[i].text = names[i];
                        rowLabels[i].color = done[i] ? Ink : Mute;
                    }
                }
                if (rowDots != null && i < rowDots.Length && rowDots[i] != null)
                {
                    rowDots[i].gameObject.SetActive(has);
                    if (has) rowDots[i].color = done[i] ? Ok : Bad;
                }
            }
            StepWindow.ExternalOpen(true);
            Camera cam = Camera.main;
            if (canvas != null)
            {
                if (canvas.worldCamera == null) canvas.worldCamera = cam;
                canvas.enabled = true;
            }
            Place(cam);
            UISound.WindowOpened();
            StartCoroutine(Fade());
        }

        Vector3 baseScale;

        void Place(Camera cam)
        {
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            if (baseScale == Vector3.zero) baseScale = transform.localScale;
            float d = WindowFit.Free(cam.transform, fwd, distance, 14f);
            transform.localScale = baseScale * (d / distance);
            transform.position = cam.transform.position + fwd * d;
            Vector3 look = transform.position - cam.transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
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

        void GoToMenu()
        {
            StepWindow.ExternalOpen(false);
            SceneManager.LoadScene(menuScene);
        }
    }
}
