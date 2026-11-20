using System.Collections;
using UnityEngine;

namespace KoksUI
{
    /// <summary>Когда окно закрыто (кнопкой или по таймеру), через паузу показывает следующее окно.</summary>
    public class WindowChain : MonoBehaviour
    {
        public StepWindow window;
        public StepWindow next;
        public float delay = 0.4f;

        bool subscribed, running;

        void Start() { Subscribe(); }
        void OnEnable() { Subscribe(); }

        void Subscribe()
        {
            if (subscribed || window == null) return;
            window.onConfirmed.AddListener(OnConfirmed);
            subscribed = true;
        }

        void OnConfirmed()
        {
            if (running || next == null) return;
            running = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(delay);
            while (StepWindow.AnyOpen) yield return null;
            next.Show();
            running = false;
        }
    }
}
