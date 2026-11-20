using System.Collections;
using UnityEngine;

namespace KoksUI
{
    /// <summary>Тихая фоновая музыка сцены: по кругу, с плавным включением. Громкость правится в Inspector.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class SceneMusic : MonoBehaviour
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.12f;
        [Tooltip("Плавное включение, секунд")]
        public float fadeIn = 4f;

        IEnumerator Start()
        {
            var src = GetComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 0f;   // 2D: слышно одинаково везде
            src.playOnAwake = false;
            src.volume = 0f;
            if (clip == null) yield break;
            src.Play();
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                src.volume = volume * Mathf.Clamp01(t / Mathf.Max(0.1f, fadeIn));
                yield return null;
            }
            // дальше громкость можно крутить в Inspector прямо во время игры
            while (true) { src.volume = volume; yield return null; }
        }
    }
}
