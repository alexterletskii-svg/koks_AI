using System.Collections;
using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Проверка сточной линии газоподогревателя: когда точка осмотра (кран) осмотрена, кран поворачивается вокруг оси Z,
    /// из линии идёт вода (система частиц), затем показывается окно и включается следующий круг.
    /// </summary>
    public class DrainValve : MonoBehaviour
    {
        public InspectHud hud;
        [Tooltip("Кран (Torus.003): поворачивается вокруг своей оси Z")]
        public Transform valve;
        [Tooltip("Вода (дочерний объект DrainWater у cil_water)")]
        public ParticleSystem water;
        [Tooltip("На сколько градусов повернуть кран")]
        public float spinDegrees = 540f;
        public float spinSeconds = 2.2f;
        [Tooltip("Сколько секунд идёт вода до следующего окна")]
        public float flowSeconds = 6f;
        [Tooltip("Окно после того, как вода пошла")]
        public StepWindow window;
        [Tooltip("Что включить после (следующий синий круг)")]
        public GameObject[] activateAfter;

        bool started, subscribed;

        void OnEnable() { Subscribe(); }
        void Start() { Subscribe(); }
        void OnDisable()
        {
            if (hud != null && subscribed) hud.PointInspected -= OnInspected;
            subscribed = false;
        }

        void Subscribe()
        {
            if (subscribed || hud == null) return;
            hud.PointInspected += OnInspected;
            subscribed = true;
        }

        void OnInspected(InspectPoint p)
        {
            if (started) return;
            started = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Quaternion start = valve != null ? valve.localRotation : Quaternion.identity;
            float t = 0f;
            while (t < spinSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(0.1f, spinSeconds)));
                if (valve != null) valve.localRotation = start * Quaternion.Euler(0f, 0f, spinDegrees * k);
                if (t > spinSeconds * 0.35f && water != null && !water.isPlaying) water.Play();   // вода начинает идти, когда кран уже приоткрыт
                yield return null;
            }
            if (valve != null) valve.localRotation = start * Quaternion.Euler(0f, 0f, spinDegrees);
            if (water != null && !water.isPlaying) water.Play();
            Debug.Log("[DrainValve] кран открыт, вода идёт");

            yield return new WaitForSeconds(flowSeconds);
            if (window != null)
            {
                while (StepWindow.AnyOpen) yield return null;
                window.Show();
            }
            if (activateAfter != null)
                foreach (var g in activateAfter) if (g != null) g.SetActive(true);
        }
    }
}
