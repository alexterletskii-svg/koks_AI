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
        [Range(0f, 1f)] public float volume = 0.35f;
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

        void Start()
        {
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
                StartCoroutine(Fade(src, 0f, volume, Mathf.Max(0.05f, fadeDur), false));
                if (current != null) StartCoroutine(Fade(current, current.volume, 0f, Mathf.Max(0.05f, crossfade), true));
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
                s.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            if (s == null) yield break;
            s.volume = to;
            if (destroyAtEnd) { s.Stop(); Destroy(s); }
        }

        IEnumerator RunAccents()
        {
            var src = NewSource();
            src.volume = 1f;
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(accentInterval.x, Mathf.Max(accentInterval.x, accentInterval.y)));
                var clip = accents[Random.Range(0, accents.Length)];
                if (clip != null) src.PlayOneShot(clip, accentVolume);
            }
        }
    }
}
