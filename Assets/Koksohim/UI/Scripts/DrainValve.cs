using System.Collections;
using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Проверка сточной линии газоподогревателя в два захода:
    /// 1) игрок осматривает кран — кран открывается, из линии идёт вода (система частиц);
    /// 2) показывается подсказка «закройте кран», игрок осматривает кран ещё раз — кран закрывается, вода останавливается;
    /// затем окно «Кран закрыт» и включается следующий круг.
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
        [Tooltip("Сколько секунд идёт вода до подсказки «закройте кран»")]
        public float flowSeconds = 2.5f;
        [Tooltip("Подсказка после открытия: «Закройте кран»")]
        public StepWindow closeWindow;
        [Tooltip("Окно после того, как кран закрыт")]
        public StepWindow window;
        [Tooltip("Что включить после (следующий синий круг)")]
        public GameObject[] activateAfter;

        int phase;      // 0 — кран закрыт, ждём открытия; 1 — открыт, ждём закрытия; 2 — готово
        bool subscribed;
        Quaternion closedRot;

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
            if (phase == 0) { phase = 1; StartCoroutine(Open(p)); }
            else if (phase == 1) { phase = 2; StartCoroutine(Close()); }
        }

        IEnumerator Spin(float degrees, float stopWaterAt)
        {
            Quaternion start = valve != null ? valve.localRotation : Quaternion.identity;
            float t = 0f;
            while (t < spinSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(0.1f, spinSeconds)));
                if (valve != null) valve.localRotation = start * Quaternion.Euler(0f, 0f, degrees * k);
                if (water != null)
                {
                    // открываем: вода начинается, когда кран уже приоткрыт; закрываем: вода останавливается ближе к концу поворота
                    if (degrees > 0f && t > spinSeconds * 0.35f && !water.isPlaying) water.Play();
                    if (degrees < 0f && t > spinSeconds * stopWaterAt && water.isEmitting) water.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                yield return null;
            }
            if (valve != null) valve.localRotation = start * Quaternion.Euler(0f, 0f, degrees);
        }

        IEnumerator Open(InspectPoint p)
        {
            closedRot = valve != null ? valve.localRotation : Quaternion.identity;
            yield return Spin(spinDegrees, 0f);
            if (water != null && !water.isPlaying) water.Play();
            Debug.Log("[DrainValve] кран открыт, вода идёт");
            yield return new WaitForSeconds(flowSeconds);

            if (closeWindow != null)
            {
                while (StepWindow.AnyOpen) yield return null;
                closeWindow.Show();
                yield return null;
                while (closeWindow.IsOpen) yield return null;
            }
            // кран нужно закрыть самому: точка осмотра снова доступна
            if (p != null) p.ResetForRepeat("Закрыть кран", "Кран закрыт, вода перекрыта.");
        }

        IEnumerator Close()
        {
            yield return Spin(-spinDegrees, 0.6f);
            if (valve != null) valve.localRotation = closedRot;
            if (water != null) water.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Debug.Log("[DrainValve] кран закрыт игроком, вода перекрыта");
            yield return new WaitForSeconds(1.2f);
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
