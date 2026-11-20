using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KoksUI
{
    /// <summary>Когда окно закрыто кнопкой (или само по таймеру), загружает сцену (например, главное меню).</summary>
    public class SceneLoadOnConfirm : MonoBehaviour
    {
        public StepWindow window;
        public string sceneName = "MenuScene";
        public float delay = 0.4f;

        bool subscribed, loading;

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
            if (loading) return;
            loading = true;
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            yield return new WaitForSecondsRealtime(delay);
            Debug.Log("[Scene] загрузка сцены " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}
