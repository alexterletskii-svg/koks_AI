using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ButtonEvents : MonoBehaviour
{
    [SerializeField] private InputActionReference _inputAction;
    [SerializeField] private UnityEvent _buttonPress;
    [SerializeField] private UnityEvent _buttonRelease;

    private void OnEnable()
    {
        if (_inputAction == null || _inputAction.action == null)
        {
            Debug.LogWarning($"{nameof(ButtonEvents)}: Input Action не назначен.", this);
            return;
        }

        _inputAction.action.performed += ActionButton;
        _inputAction.action.canceled += ActionButton;
    }

    private void OnDisable()
    {
        if (_inputAction == null || _inputAction.action == null)
            return;

        _inputAction.action.performed -= ActionButton;
        _inputAction.action.canceled -= ActionButton;
    }

    private void ActionButton(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _buttonPress?.Invoke();
            Debug.Log("1");
        }
        else if (context.canceled)
        {
            _buttonRelease?.Invoke();
            Debug.Log("2");
        }
    }
}
