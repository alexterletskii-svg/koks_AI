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
        [Tooltip("До трёх пунктов. Пустой пункт скрывается.")]
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

        bool wired;
        Coroutine fade;

        void Awake()
        {
            Apply();
            Wire();
        }

        void Wire()
        {
            if (wired || confirmButton == null) return;
            confirmButton.onClick.AddListener(Confirm);
            wired = true;
        }

        void OnValidate()
        {
            Apply();
        }

        void Apply()
        {
            if (stepLabel != null) stepLabel.text = stepText;
            if (titleLabel != null) titleLabel.text = title;
            if (descriptionLabel != null) descriptionLabel.text = description;
            if (checksHeaderLabel != null) checksHeaderLabel.text = checksHeader;
            if (buttonLabel != null) buttonLabel.text = buttonText;
            if (checkLabels == null) return;
            for (int i = 0; i < checkLabels.Length; i++)
            {
                string t = (checks != null && i < checks.Length) ? checks[i] : "";
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

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public void Show()
        {
            if (gameObject.activeSelf) return;
            gameObject.SetActive(true);
            Wire();
            Place();
            if (group != null) group.alpha = 0f;
            StartFade(0f, 1f, false);
            onShown.Invoke();
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
            transform.position = cam.transform.position + fwd * distance + Vector3.up * shiftUp;
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
            if (!gameObject.activeSelf) return;
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
            if (deactivate) gameObject.SetActive(false);
            fade = null;
        }
    }
}
