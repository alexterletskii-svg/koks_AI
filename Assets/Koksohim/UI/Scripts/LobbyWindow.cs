using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>Окно «Лобби»: выбор модуля и кнопки запуска. Собирается скриптом Koks/Build UI.</summary>
    public class LobbyWindow : MonoBehaviour
    {
        [Serializable] public class IntEvent : UnityEvent<int> { }

        public Button[] moduleButtons;
        public GameObject[] selectedFrames;
        public Button startButton;
        public Button freeInspectButton;
        public int selectedIndex;

        [Tooltip("Нажали «Начать модуль». Параметр — номер выбранной карточки (с нуля).")]
        public IntEvent onStartModule = new IntEvent();
        [Tooltip("Нажали «Свободный осмотр».")]
        public UnityEvent onFreeInspect = new UnityEvent();

        void Awake()
        {
            if (moduleButtons != null)
            {
                for (int i = 0; i < moduleButtons.Length; i++)
                {
                    int index = i;
                    if (moduleButtons[i] != null)
                        moduleButtons[i].onClick.AddListener(() => Select(index));
                }
            }
            if (startButton != null)
                startButton.onClick.AddListener(() => onStartModule.Invoke(selectedIndex));
            if (freeInspectButton != null)
                freeInspectButton.onClick.AddListener(() => onFreeInspect.Invoke());
        }

        void Start()
        {
            Select(selectedIndex);
        }

        public void Select(int index)
        {
            selectedIndex = index;
            if (selectedFrames == null) return;
            for (int i = 0; i < selectedFrames.Length; i++)
            {
                if (selectedFrames[i] != null)
                    selectedFrames[i].SetActive(i == index);
            }
        }
    }
}
