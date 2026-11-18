using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Скрипт "осматриваемого объекта" в VR.
/// Каждый объект имеет свой уникальный текст,
/// который передаётся в VRInspectUI при наведении.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRInspectableObject : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Перетащите сюда Canvas со скриптом VRInspectUI")]
    [SerializeField] private VRInspectUI inspectUI;

    [Header("Object Settings")]
    [Tooltip("Текст, который будет показан после осмотра этого объекта")]
    [SerializeField] private string inspectionText = "Осмотрено";  // Текст по умолчанию

    private XRSimpleInteractable interactable;
    private bool isInspected = false;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (isInspected)
        {
            // Объект уже осмотрен — показываем его текст
            inspectUI.ShowCompleted(inspectionText);
        }
        else
        {
            // Объект новый — подписываемся на событие и запускаем заполнение
            // Передаём свой текст
            inspectUI.OnInspectionComplete += HandleInspectionComplete;
            inspectUI.StartFilling(inspectionText);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        inspectUI.OnInspectionComplete -= HandleInspectionComplete;
        inspectUI.StopFilling();
    }

    private void HandleInspectionComplete()
    {
        isInspected = true;
        inspectUI.OnInspectionComplete -= HandleInspectionComplete;
        Debug.Log($"{gameObject.name} успешно осмотрен!");
    }
}