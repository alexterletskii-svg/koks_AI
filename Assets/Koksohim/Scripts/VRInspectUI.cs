using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// Скрипт управления UI-индикатором осмотра объекта в VR.
/// Показывает заполняющийся круг при наведении луча,
/// а после завершения заполнения — текст объекта.
/// Каждый объект может иметь свой уникальный текст.
/// </summary>
public class VRInspectUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Image-объект круга (тип Filled, метод Radial 360)")]
    [SerializeField] private Image circleFill;

    [Tooltip("Text-объект для отображения текста после осмотра")]
    [SerializeField] private Text inspectionText;  // Изменено: теперь это Text

    [Header("Settings")]
    [Tooltip("Сколько секунд нужно держать луч на объекте для завершения осмотра")]
    [SerializeField] private float fillDuration = 2f;

    private Coroutine fillCoroutine;
    private bool isCurrentlyFilling = false;

    // Запоминаем текущий текст, чтобы показать его при завершении
    private string currentInspectionText;

    public event Action OnInspectionComplete;

    private void Awake()
    {
        circleFill.type = Image.Type.Filled;
        circleFill.fillMethod = Image.FillMethod.Radial360;
        circleFill.fillAmount = 0;

        HideAll();
    }

    /// <summary>
    /// Запускает процесс заполнения круга.
    /// text — текст, который будет показан после заполнения.
    /// </summary>
    public void StartFilling(string text)
    {
        if (isCurrentlyFilling) return;

        // Запоминаем текст этого объекта
        currentInspectionText = text;

        // Прячем текст
        inspectionText.gameObject.SetActive(false);

        // Показываем круг и сбрасываем его на 0%
        circleFill.gameObject.SetActive(true);
        circleFill.fillAmount = 0;

        isCurrentlyFilling = true;
        fillCoroutine = StartCoroutine(FillRoutine());
    }

    public void StopFilling()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        isCurrentlyFilling = false;
        HideAll();
    }

    /// <summary>
    /// Показывает текст объекта.
    /// text — текст для отображения.
    /// </summary>
    public void ShowCompleted(string text)
    {
        StopFilling();

        // Устанавливаем текст этого объекта
        inspectionText.text = text;

        // Прячем круг, показываем текст
        circleFill.gameObject.SetActive(false);
        inspectionText.gameObject.SetActive(true);
    }

    public void HideAll()
    {
        circleFill.gameObject.SetActive(false);
        inspectionText.gameObject.SetActive(false);
    }

    private IEnumerator FillRoutine()
    {
        float time = 0;
        while (time < fillDuration)
        {
            time += Time.deltaTime;
            circleFill.fillAmount = time / fillDuration;
            yield return null;
        }

        isCurrentlyFilling = false;

        // Показываем запомненный текст
        ShowCompleted(currentInspectionText);

        OnInspectionComplete?.Invoke();
    }
}