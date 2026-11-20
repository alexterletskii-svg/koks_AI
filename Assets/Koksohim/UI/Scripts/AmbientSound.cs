using System.Collections;
using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Звуковое окружение цеха. Длинные записи (playlist) идут одна за другой по кругу, каждая следующая
    /// начинается ещё до конца предыдущей (перекрытие crossfade, плавное затухание и нарастание) — тишины между ними нет.
    /// Короткие звуки (accents) время от времени добавляются поверх. Громкость и время правятся в Inspector.
    /// </summary>
    public class AmbientSound : MonoBehaviour
    {
        [Header("Фон (длинные записи, по кругу)")]
        public AudioClip[] playlist;
        [Tooltip("Громкость фона в помещении с компьютерами (пока игрок не вошёл в газовый тоннель)")]
        [Range(0f, 1f)] public float quietVolume = 0.12f;
        [Tooltip("Громкость фона в газовом тоннеле")]
        [Range(0f, 1f)] public float tunnelVolume = 0.4f;
        [Tooltip("За сколько секунд звук меняется при переходе в тоннель")]
        public float rampSeconds = 4f;
        [Tooltip("Триггер прибытия в тоннель (объект TunnelArrival): после него фон становится громче")]
        public ArrivalTrigger tunnelTrigger;
        [Tooltip("Перекрытие соседних записей, секунд: следующая стартует за столько до конца предыдущей")]
        public float crossfade = 8f;
        [Tooltip("Плавное включение фона при старте сцены, секунд")]
        public float fadeIn = 3f;
        [Tooltip("Играть записи в случайном порядке")]
        public bool shuffle = false;

        [Header("Короткие звуки поверх фона")]
        public AudioClip[] accents;
        [Range(0f, 1f)] public float accentVolume = 0.3f;
        [Tooltip("Пауза между короткими звуками (случайная от и до), секунд")]
        public Vector2 accentInterval = new Vector2(25f, 60f);

        AudioSource current;
        int index = -1;
        float master;           // текущая громкость фона (плавно идёт к цели)
        float target;
        readonly System.Collections.Generic.Dictionary<AudioSource, float> factor = new System.Collections.Generic.Dictionary<AudioSource, float>();

        /// <summary>Включить громкий режим (газовый тоннель) или тихий (помещение с компьютерами).</summary>
        public void SetTunnel(bool inTunnel)
        {
            target = inTunnel ? tunnelVolume : quietVolume;
        }

        void Update()
        {
            float step = Mathf.Abs(tunnelVolume - quietVolume) / Mathf.Max(0.1f, rampSeconds) * Time.unscaledDeltaTime;
            master = Mathf.MoveTowards(master, target, Mathf.Max(step, 0.0005f));
            foreach (var kv in factor) if (kv.Key != null) kv.Key.volume = kv.Value * master;
        }

        void Start()
        {
            master = target = quietVolume;
            if (tunnelTrigger != null) tunnelTrigger.onArrived.AddListener(() => SetTunnel(true));
            if (playlist != null && playlist.Length > 0) StartCoroutine(RunPlaylist());
            if (accents != null && accents.Length > 0) StartCoroutine(RunAccents());
        }

        AudioSource NewSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.spatialBlend = 0f;       // 2D: слышно одинаково везде
            s.playOnAwake = false;
            s.loop = false;
            s.volume = 0f;
            return s;
        }

        int NextIndex()
        {
            int n = playlist.Length;
            if (n == 1) return 0;
            if (!shuffle) return (index + 1) % n;
            int i;
            do { i = Random.Range(0, n); } while (i == index);
            return i;
        }

        IEnumerator RunPlaylist()
        {
            while (true)
            {
                index = NextIndex();
                var clip = playlist[index];
                if (clip == null) { yield return null; continue; }
                var src = NewSource();
                src.clip = clip;
                src.Play();
                float fadeDur = current == null ? fadeIn : crossfade;
                factor[src] = 0f;
                StartCoroutine(Fade(src, 0f, 1f, Mathf.Max(0.05f, fadeDur), false));
                if (current != null) StartCoroutine(Fade(current, factor.ContainsKey(current) ? factor[current] : 1f, 0f, Mathf.Max(0.05f, crossfade), true));
                current = src;

                // ждём, пока до конца записи останется crossfade секунд (запись короче — сразу следующая за её длительностью)
                float wait = Mathf.Max(1f, clip.length - crossfade);
                float t = 0f;
                while (t < wait && src != null && src.isPlaying) { t += Time.unscaledDeltaTime; yield return null; }
            }
        }

        IEnumerator Fade(AudioSource s, float from, float to, float dur, bool destroyAtEnd)
        {
            float t = 0f;
            while (t < dur && s != null)
            {
                t += Time.unscaledDeltaTime;
                factor[s] = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            if (s == null) yield break;
            factor[s] = to;
            if (destroyAtEnd) { factor.Remove(s); s.Stop(); Destroy(s); }
        }

        IEnumerator RunAccents()
        {
            var src = NewSource();
            src.volume = 1f;
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(accentInterval.x, Mathf.Max(accentInterval.x, accentInterval.y)));
                var clip = accents[Random.Range(0, accents.Length)];
                if (clip != null) src.PlayOneShot(clip, accentVolume * Mathf.Clamp01(master / Mathf.Max(0.01f, tunnelVolume)));
            }
        }
    }
}
