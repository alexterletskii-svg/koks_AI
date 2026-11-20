using System;
using UnityEngine;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>
    /// Окно «Завершить экзамен?» — появляется, когда игрок нажимает на ручку двери выхода.
    /// Две кнопки: завершить (показать итоги) или продолжить осмотр. Собирается скриптом Koks → Build UI → 20.
    /// </summary>
    public class ExamFinishWindow : MonoBehaviour
    {
        public Button finishButton;
        public Button continueButton;
        public CanvasGroup group;
        public float distance = 1.3f;
        public float fadeSeconds = 0.25f;

        Canvas canvas;
        Action onFinish, onContinue;
        bool open;

        public bool IsOpen { get { return open; } }

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            UIDepth.AlwaysOnTop(gameObject);
            Prepare(finishButton, OnFinish);
            Prepare(continueButton, OnContinue);
            if (group != null) group.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
        }

        static void Prepare(Button b, UnityEngine.Events.UnityAction a)
        {
            if (b == null) return;
            b.onClick.AddListener(a);
            var target = b.targetGraphic;
            foreach (var g in b.GetComponentsInChildren<Graphic>(true))
                if (g != target) g.raycastTarget = false;
            var im = target as Image;
            if (im != null) im.raycastPadding = new Vector4(-20f, -14f, -20f, -14f);
        }

        public void Show(Action finish, Action cont)
        {
            if (open) return;
            open = true;
            onFinish = finish; onContinue = cont;
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
        void OnContinue() { var a = onContinue; Close(); if (a != null) a(); }
    }
}
